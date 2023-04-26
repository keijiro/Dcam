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

    // Frame queue
    (Queue<RenderTexture> queue, RenderTexture generated,
     RenderTexture latest, RenderTexture flip, RenderTexture oldest) _frame;

    // Page rendering
    (MaterialPropertyBlock props, RenderParams rparams, Matrix4x4 matrix, float time) _bgPage;
    (MaterialPropertyBlock props, RenderParams rparams, Matrix4x4 matrix, float time) _fgPage;

    // Stable Diffusion pipeline
    SDPipeline _sdPipeline;

    #endregion

    #region Private methods

    async Awaitable InitObjects()
    {
        var width = MLStableDiffusion.Pipeline.Width;

        // Application frame rate setting
        Application.targetFrameRate = _displayFps;

        // Frame queues
        _frame.queue = new Queue<RenderTexture>();
        for (var i = 0; i < _queueLength; i++)
            _frame.queue.Enqueue(new RenderTexture(width, width, 0));
        _frame.generated = new RenderTexture(width, width, 0);
        _frame.latest = new RenderTexture(width, width, 0);
        _frame.flip = new RenderTexture(width, width, 0);
        _frame.oldest = new RenderTexture(width, width, 0);

        // Page rendering
        _bgPage.props = new MaterialPropertyBlock();
        _fgPage.props = new MaterialPropertyBlock();
        _bgPage.rparams = new RenderParams(_pageMaterial){ matProps = _bgPage.props };
        _fgPage.rparams = new RenderParams(_pageMaterial){ matProps = _fgPage.props };
        _bgPage.matrix = Matrix4x4.TRS
          (Vector3.forward * 0.01f, Quaternion.identity, Vector3.one * 2);
        _fgPage.matrix = Matrix4x4.identity;

        // Stable Diffusion pipeline
        _sdPipeline = new SDPipeline(_sdPreprocess);
        Debug.Log("Loading the Stable Diffusion mode...");
        await _sdPipeline.InitializeAsync(ResourcePath, ComputeUnits.CpuAndGpu);
        Debug.Log("Done.");
    }

    void ReleaseObjects()
    {
        while (_frame.queue.Count > 0) Destroy(_frame.queue.Dequeue());

        Destroy(_frame.generated);
        Destroy(_frame.latest);
        Destroy(_frame.flip);
        Destroy(_frame.oldest);
        _frame.generated = null;
        _frame.latest = null;
        _frame.flip = null;
        _frame.oldest = null;

        _sdPipeline?.Dispose();
        _sdPipeline = null;
    }

    async Awaitable RunSDPipelineAsync()
    {
        _sdPipeline.Prompt = _prompt;
        _sdPipeline.Strength = _strength;
        _sdPipeline.StepCount = _stepCount;
        _sdPipeline.Seed = Random.Range(1, 2000000000);
        _sdPipeline.GuidanceScale = _guidance;
        await _sdPipeline.RunAsync(_frame.latest, _frame.generated, destroyCancellationToken);
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
            _frame.queue.Enqueue(_frame.latest);

            // Reuse the previous "oldest" frame to store the latest frame.
            _frame.latest = _frame.oldest;
            Graphics.Blit(_source.Texture, _frame.latest);

            // The previous "flip" frame becomes the "oldest" frame.
            _frame.oldest = _frame.flip;

            // Get a frame from the queue and make it flipping.
            _frame.flip = _frame.queue.Dequeue();

            // Animation restart
            _bgPage.time = 0;

            // Generator task cycle
            if (genTask?.IsCompleted ?? true)
            {
                genTask = RunSDPipelineAsync();
                _fgPage.time = 0;
                _camera.RenewTarget();
            }

            // Per-flip wait
            await Awaitable.WaitForSecondsAsync(_flipDuration);
        }
    }

    void OnDestroy() => ReleaseObjects();

    void Update()
    {
        // Animation time step
        var delta = Time.deltaTime / _flipDuration;
        _bgPage.time += delta;
        _fgPage.time += delta;

        // Foreground page insertion
        var fg = (tex1: _frame.flip, tex2: _frame.oldest, time: _bgPage.time);
        if (_fgPage.time < _insertionCount)
            (fg.tex1, fg.time) = (_frame.generated, _fgPage.time);
        else if (_fgPage.time < _insertionCount + 1)
            fg.tex2 = _frame.generated;

        // Rendering
        _bgPage.props.SetTexture("_Texture1", _frame.flip);
        _fgPage.props.SetTexture("_Texture1", fg.tex1);

        _bgPage.props.SetTexture("_Texture2", _frame.oldest);
        _fgPage.props.SetTexture("_Texture2", fg.tex2);

        _bgPage.props.SetFloat("_Progress", Mathf.Clamp01(_bgPage.time));
        _fgPage.props.SetFloat("_Progress", Mathf.Clamp01(fg.time));

        Graphics.RenderMesh(_bgPage.rparams, _pageMesh, 0, _bgPage.matrix);
        Graphics.RenderMesh(_fgPage.rparams, _pageMesh, 0, _fgPage.matrix);
    }

    #endregion
}
