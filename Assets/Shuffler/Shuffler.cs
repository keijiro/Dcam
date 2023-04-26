#define ENABLE_MLSD

using UnityEngine;
using System.Collections.Generic;
using ImageSource = Klak.TestTools.ImageSource;
using ComputeUnits = MLStableDiffusion.ComputeUnits;
using SDPipeline = MLStableDiffusion.Pipeline;

public sealed class Shuffler : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] int _displayFps = 24;
    [SerializeField] string _resourceDir = "StableDiffusion";
    [Space]
    [SerializeField] float _flipDuration = 0.175f;
    [SerializeField] int _queueLength = 9;
    [SerializeField] int _insertionCount = 5;
    [Space]
    [SerializeField] string _prompt = "Surrealistic painting by J. C. Leyendecker";
    [SerializeField] float _strength = 0.7f;
    [SerializeField] int _stepCount = 7;
    [SerializeField] float _guidance = 10;
    [Space]
    [SerializeField] ImageSource _source = null;
    [SerializeField] CameraController _camera = null;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Mesh _pageMesh = null;
    [SerializeField, HideInInspector] Material _pageMaterial = null;
    [SerializeField, HideInInspector] ComputeShader _sdPreprocess = null;

    #endregion

    #region Private objects

    string ResourcePath => Application.streamingAssetsPath + "/" + _resourceDir;

    // Frame textures
    Queue<RenderTexture> _frameQueue;
    RenderTexture _latestFrame;
    (RenderTexture flip, RenderTexture sheet) _bgFrames;
    (RenderTexture back, RenderTexture front) _fgFrames;

    // Page rendering parameters
    (MaterialPropertyBlock props, RenderParams rparams, Matrix4x4 matrix) _bgParams;
    (MaterialPropertyBlock props, RenderParams rparams, Matrix4x4 matrix) _fgParams;

    // Animation parameters
    float _flipTime;
    int _flipCount;

    // Stable Diffusion pipeline
    SDPipeline _sdPipeline;

    #endregion

    #region Private methods

    Matrix4x4 MakeTSMatrix(float z, float scale)
      => Matrix4x4.TRS(Vector3.forward * z, Quaternion.identity, Vector3.one * scale);

    async Awaitable InitObjects()
    {
        var width = MLStableDiffusion.Pipeline.Width;

        // Application frame rate setting
        Application.targetFrameRate = _displayFps;

        // Frame queues
        _frameQueue = new Queue<RenderTexture>();
        for (var i = 0; i < _queueLength; i++)
            _frameQueue.Enqueue(new RenderTexture(width, width, 0));
        _latestFrame = new RenderTexture(width, width, 0);
        _bgFrames.flip  = new RenderTexture(width, width, 0);
        _bgFrames.sheet = new RenderTexture(width, width, 0);
        _fgFrames.back  = new RenderTexture(width, width, 0);
        _fgFrames.front = new RenderTexture(width, width, 0);

        // Page rendering parameters
        _bgParams.props = new MaterialPropertyBlock();
        _fgParams.props = new MaterialPropertyBlock();
        _bgParams.rparams = new RenderParams(_pageMaterial){ matProps = _bgParams.props };
        _fgParams.rparams = new RenderParams(_pageMaterial){ matProps = _fgParams.props };
        _bgParams.matrix = MakeTSMatrix(0.01f, 3);
        _fgParams.matrix = Matrix4x4.identity;
        _bgParams.props.SetFloat("_Occlusion", 1);

        // Stable Diffusion pipeline
#if ENABLE_MLSD
        _sdPipeline = new SDPipeline(_sdPreprocess);
        Debug.Log("Loading the Stable Diffusion mode...");
        await _sdPipeline.InitializeAsync(ResourcePath, ComputeUnits.CpuAndGpu);
        Debug.Log("Done.");
#else
        await Awaitable.NextFrameAsync();
#endif
    }

    void ReleaseObjects()
    {
        while (_frameQueue.Count > 0) Destroy(_frameQueue.Dequeue());

        Destroy(_latestFrame);
        _latestFrame = null;

        Destroy(_bgFrames.flip);
        Destroy(_bgFrames.sheet);
        _bgFrames = (null, null);

        Destroy(_fgFrames.back);
        Destroy(_fgFrames.front);
        _fgFrames = (null, null);

        _sdPipeline?.Dispose();
        _sdPipeline = null;
    }

    async Awaitable RunSDPipelineAsync()
    {
        if (_sdPipeline != null)
        {
            _sdPipeline.Prompt = _prompt;
            _sdPipeline.Strength = _strength;
            _sdPipeline.StepCount = _stepCount;
            _sdPipeline.Seed = Random.Range(1, 2000000000);
            _sdPipeline.GuidanceScale = _guidance;
            await _sdPipeline.RunAsync
              (_latestFrame, _fgFrames.back, destroyCancellationToken);
        }
        else
        {
            Graphics.Blit(_latestFrame, _fgFrames.back);
            await Awaitable.WaitForSecondsAsync(Random.Range(0.5f, 2.0f));
        }
    }

    #endregion

    #region MonoBehaviour implementation

    async Awaitable Start()
    {
        // Initialization
        await InitObjects();

        for (var genTask = (Awaitable)null;;)
        {
            // Push the previous "latest" frame to the queue.
            _frameQueue.Enqueue(_latestFrame);

            // Reuse the previous "sheet" frame to store the latest frame.
            _latestFrame = _bgFrames.sheet;
            Graphics.Blit(_source.Texture, _latestFrame);

            // The previous "flip" frame becomes the "sheet" frame.
            _bgFrames.sheet = _bgFrames.flip;

            // Get a frame from the queue and make it flipping.
            _bgFrames.flip = _frameQueue.Dequeue();

            // Flip animation restart
            _flipTime = 0;

            // Generator task cycle
            if (_flipCount >= _queueLength && (genTask == null || genTask.IsCompleted))
            {
                _fgFrames = (_fgFrames.front, _fgFrames.back);
                genTask = RunSDPipelineAsync();
                _flipCount = 0;
                _camera.RenewTarget();
            }

            // Per-flip wait
            await Awaitable.WaitForSecondsAsync(_flipDuration);

            _flipCount++;
        }
    }

    void OnDestroy() => ReleaseObjects();

    void Update()
    {
        // Flip animation time step
        _flipTime += Time.deltaTime / _flipDuration;

        // Foreground page insertion
        var fgTex1 = _flipCount < _insertionCount ? _fgFrames.front : _bgFrames.flip;
        var fgTex2 = _flipCount == _insertionCount ? _fgFrames.front : _bgFrames.sheet;
        var fgTime = _flipCount > 0 && _flipCount < _insertionCount ? 1 : _flipTime;

        // Rendering
        _bgParams.props.SetTexture("_Texture1", _bgFrames.flip);
        _fgParams.props.SetTexture("_Texture1", fgTex1);

        _bgParams.props.SetTexture("_Texture2", _bgFrames.sheet);
        _fgParams.props.SetTexture("_Texture2", fgTex2);

        _bgParams.props.SetFloat("_Progress", Mathf.Clamp01(_flipTime));
        _fgParams.props.SetFloat("_Progress", Mathf.Clamp01(fgTime));

        Graphics.RenderMesh(_bgParams.rparams, _pageMesh, 0, _bgParams.matrix);
        Graphics.RenderMesh(_fgParams.rparams, _pageMesh, 0, _fgParams.matrix);
    }

    #endregion
}
