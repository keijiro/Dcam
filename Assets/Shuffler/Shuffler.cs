using UnityEngine;
using System.Collections.Generic;
using CancellationToken = System.Threading.CancellationToken;
using ImageSource = Klak.TestTools.ImageSource;
using ComputeUnits = MLStableDiffusion.ComputeUnits;
using SDPipeline = MLStableDiffusion.Pipeline;

public sealed class Shuffler : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] int _displayFps = 24;
    [SerializeField] string _resourceDir = "StableDiffusion";
    [Space]
    [SerializeField] float _pauseDuration = 1;
    [SerializeField] float _flipDuration = 0.175f;
    [SerializeField] int _flipCount = 4;
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

    // Frame queues
    (Queue<RenderTexture> flip, Queue<RenderTexture> back) _queue;

    // Active page pair
    (RenderTexture baseRT, RenderTexture flapRT,
     MaterialPropertyBlock props, float progress, float speed) _page;

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
        _queue.flip = new Queue<RenderTexture>();
        _queue.back = new Queue<RenderTexture>();
        _queue.flip.Enqueue(new RenderTexture(width, width, 0));
        for (var i = 0; i < _flipCount; i++)
            _queue.back.Enqueue(new RenderTexture(width, width, 0));

        // Active page pair
        _page.baseRT = new RenderTexture(width, width, 0);
        _page.flapRT = new RenderTexture(width, width, 0);
        _page.props = new MaterialPropertyBlock();

        // Stable Diffusion pipeline
        _sdPipeline = new SDPipeline(_sdPreprocess);
        Debug.Log("Loading the Stable Diffusion mode...");
        await _sdPipeline.InitializeAsync(ResourcePath, ComputeUnits.CpuAndGpu);
        Debug.Log("Done.");
    }

    void ReleaseObjects()
    {
        while (_queue.flip.Count > 0) Destroy(_queue.flip.Dequeue());
        while (_queue.back.Count > 0) Destroy(_queue.back.Dequeue());

        Destroy(_page.baseRT);
        Destroy(_page.flapRT);
        _page.baseRT = null;
        _page.flapRT = null;

        _sdPipeline?.Dispose();
        _sdPipeline = null;
    }

    async Awaitable RunSDPipelineAsync(RenderTexture dest, CancellationToken cancel)
    {
        _sdPipeline.Prompt = _prompt;
        _sdPipeline.Strength = _strength;
        _sdPipeline.StepCount = _stepCount;
        _sdPipeline.Seed = Random.Range(1, 2000000000);
        _sdPipeline.GuidanceScale = _guidance;
        await _sdPipeline.RunAsync(_source.Texture, dest, cancel);
    }

    #endregion

    #region MonoBehaviour implementation

    async Awaitable Start()
    {
        var cancel = destroyCancellationToken;

        // Initialization
        await InitObjects();

        while (true)
        {
            var genTask = RunSDPipelineAsync(_page.baseRT, cancel);
            _queue.back.Enqueue(_page.baseRT);

            _page.baseRT = _page.flapRT;
            _page.flapRT = _queue.flip.Dequeue();
            (_page.progress, _page.speed) = (0, 0.5f);

            (_queue.flip, _queue.back) = (_queue.back, _queue.flip);

            // Pause
            await Awaitable.WaitForSecondsAsync(_pauseDuration);

            // Page flips
            for (var i = 0; i < _flipCount; i++)
            {
                // Use the "stay" RT as a new back frame.
                Graphics.Blit(_source.Texture, _page.baseRT);
                _queue.back.Enqueue(_page.baseRT);

                // New page pair
                _page.baseRT = _page.flapRT;
                _page.flapRT = _queue.flip.Dequeue();
                (_page.progress, _page.speed) = (0, 1);

                await Awaitable.WaitForSecondsAsync(_flipDuration);
            }

            // Complete the major frame generation.
            await genTask;

            _camera.RenewTarget();
        }
    }

    void OnDestroy() => ReleaseObjects();

    void Update()
    {
        var dt = Time.deltaTime * _page.speed;
        _page.progress = Mathf.Min(1, _page.progress + dt / _flipDuration);

        _page.props.SetTexture("_Texture1", _page.flapRT);
        _page.props.SetTexture("_Texture2", _page.baseRT);
        _page.props.SetFloat("_Progress", _page.progress);

        Graphics.RenderMesh
          (new RenderParams(_pageMaterial){ matProps = _page.props },
           _pageMesh, 0, Matrix4x4.identity);
    }

    #endregion
}
