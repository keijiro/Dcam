//#define ENABLE_MLSD

using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using ImageSource = Klak.TestTools.ImageSource;
using ComputeUnits = MLStableDiffusion.ComputeUnits;
using SDPipeline = MLStableDiffusion.Pipeline;
using Random = Unity.Mathematics.Random;

public sealed class Shuffler : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] int _displayFps = 24;
    [SerializeField] string _resourceDir = "StableDiffusion";
    [Space]
    [SerializeField] float _flipDuration = 0.175f;
    [SerializeField] int _queueLength = 9;
    [SerializeField] int _insertionCount = 5;
    [SerializeField] int _prefilterNumber = 0;
    [SerializeField] Texture2D _titleTexture = null;
    [SerializeField] Color _titleColor = Color.white;
    [SerializeField] Texture2D _overlayTexture = null;
    [SerializeField] Color _overlayColor = Color.white;
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
    [SerializeField, HideInInspector] Shader _prefilterShader = null;

    #endregion

    #region Private objects

    const int ImageWidth = 640;
    const int ImageHeight = 384;

    string ResourcePath => Application.streamingAssetsPath + "/" + _resourceDir;

    MLStableDiffusion.ResourceInfo ResourceInfo
      => MLStableDiffusion.ResourceInfo.FixedSizeModel(ResourcePath, ImageWidth, ImageHeight);

    // Prefilter
    (Material material, RenderTexture texture) _prefilter;

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
    Random _random = new Random(123);

    // Stable Diffusion pipeline
    SDPipeline _sdPipeline;

    #endregion

    #region Private methods

    float4x4 MakePageTransform(float z, float scale)
      => math.mul(float4x4.Translate(math.float3(0, 0, z)),
                  float4x4.Scale(1, (float)ImageHeight / ImageWidth, 1) * scale);

    (float4x4, float4x4) CalculatePrefilterMatrices()
    {
        // Random TRS for the overlay layer
        var mtxRnd = float4x4.TRS
          (math.float3(_random.NextFloat2() - 0.5f, 0),
           quaternion.RotateZ(_random.NextFloat(-0.5f, 0.5f) * math.PI),
           _random.NextFloat(0.5f, 2));

        // UV (0 .. 1) -> (-0.5 .. +0.5)
        var mtxCenter = float4x4.Translate(math.float3(-0.5f, -0.5f, 0));

        // UV aspect ratio fix
        var mtxUVSS = float4x4.Scale(1, (float)ImageHeight / ImageWidth, 1);

        // Inverse aspect ratio fix for the each layer
        var mtxUV1 = float4x4.Scale(1, (float)_titleTexture.width / _titleTexture.height, 1);
        var mtxUV2 = float4x4.Scale(1, (float)_overlayTexture.width / _overlayTexture.height, 1);

        // (-0.5 .. +0.5) -> (0 .. 1)
        var mtxOffs = float4x4.Translate(math.float3(0.5f, 0.5f, 0));

        // UV transform matrices for the each layer
        var mtxBase = math.mul(mtxUVSS, mtxCenter);

        return (math.mul(math.mul(mtxOffs, mtxUV1), mtxBase),
                math.mul(math.mul(mtxOffs, mtxUV2), math.mul(mtxRnd, mtxBase)));
    }

    void UpdatePrefilterProperties()
    {
        var mtx = CalculatePrefilterMatrices();
        var mat = _prefilter.material;
        mat.SetTexture("_Layer1Texture", _titleTexture);
        mat.SetTexture("_Layer2Texture", _overlayTexture);
        mat.SetColor("_Layer1Color", _titleColor);
        mat.SetColor("_Layer2Color", _overlayColor);
        mat.SetMatrix("_Layer1Matrix", mtx.Item1);
        mat.SetMatrix("_Layer2Matrix", mtx.Item2);
        mat.SetVector("_Random", _random.NextFloat4());
    }

    async Awaitable InitObjects()
    {
        // Application frame rate setting
        Application.targetFrameRate = _displayFps;

        // Prefilter
        _prefilter.material = new Material(_prefilterShader);
        _prefilter.texture = new RenderTexture(ImageWidth, ImageHeight, 0);

        // Frame queues
        _frameQueue = new Queue<RenderTexture>();
        for (var i = 0; i < _queueLength; i++)
            _frameQueue.Enqueue(new RenderTexture(ImageWidth, ImageHeight, 0));
        _latestFrame = new RenderTexture(ImageWidth, ImageHeight, 0);
        _bgFrames.flip  = new RenderTexture(ImageWidth, ImageHeight, 0);
        _bgFrames.sheet = new RenderTexture(ImageWidth, ImageHeight, 0);
        _fgFrames.back  = new RenderTexture(ImageWidth, ImageHeight, 0);
        _fgFrames.front = new RenderTexture(ImageWidth, ImageHeight, 0);

        // Page rendering parameters
        _bgParams.props = new MaterialPropertyBlock();
        _fgParams.props = new MaterialPropertyBlock();
        _bgParams.rparams = new RenderParams(_pageMaterial){ matProps = _bgParams.props };
        _fgParams.rparams = new RenderParams(_pageMaterial){ matProps = _fgParams.props };
        _bgParams.matrix = MakePageTransform(0.01f, 3);
        _fgParams.matrix = MakePageTransform(0, 1);
        _bgParams.props.SetFloat("_OcclusionStrength", 0.85f);
        _bgParams.props.SetFloat("_AspectRatio", (float)ImageWidth / ImageHeight);

        // Stable Diffusion pipeline
#if ENABLE_MLSD
        _sdPipeline = new SDPipeline(_sdPreprocess);
        Debug.Log("Loading the Stable Diffusion mode...");
        await _sdPipeline.InitializeAsync(ResourceInfo, ComputeUnits.CpuAndGpu);
        Debug.Log("Done.");
#else
        await Awaitable.NextFrameAsync();
#endif
    }

    void ReleaseObjects()
    {
        Destroy(_prefilter.material);
        Destroy(_prefilter.texture);
        _prefilter = (null, null);

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
            _sdPipeline.Seed = _random.NextInt(1, 2000000000);
            _sdPipeline.GuidanceScale = _guidance;
            await _sdPipeline.RunAsync
              (_latestFrame, _fgFrames.back, destroyCancellationToken);
        }
        else
        {
            Graphics.Blit(_latestFrame, _fgFrames.back);
            await Awaitable.WaitForSecondsAsync(_random.NextFloat(0.5f, 2.0f));
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
            // Prefilter
            UpdatePrefilterProperties();
            Graphics.Blit(_source.Texture, _prefilter.texture, _prefilter.material, _prefilterNumber);

            // Push the previous "latest" frame to the queue.
            _frameQueue.Enqueue(_latestFrame);

            // Reuse the previous "sheet" frame to store the latest frame.
            _latestFrame = _bgFrames.sheet;
            Graphics.Blit(_prefilter.texture, _latestFrame);

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
