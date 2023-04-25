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
    [SerializeField] ImageSource _source = null;
    [SerializeField] string _resourceDir = "StableDiffusion";
    [Space]
    [SerializeField] float _flipTime = 0.175f;
    [SerializeField] float _queueLength = 10;
    [Space]
    [SerializeField] string _prompt = "Surrealistic painting by J. C. Leyendecker";
    [SerializeField] float _strength = 0.7f;
    [SerializeField] int _stepCount = 7;
    [SerializeField] float _guidance = 10;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Mesh _pageMesh = null;
    [SerializeField, HideInInspector] Material _pageMaterial = null;
    [SerializeField, HideInInspector] ComputeShader _sdPreprocess = null;

    #endregion

    #region Private objects

    string ResourcePath => Application.streamingAssetsPath + "/" + _resourceDir;

    // Frame queues
    Queue<RenderTexture> _freeFrames = new Queue<RenderTexture>();
    Queue<RenderTexture> _stockFrames = new Queue<RenderTexture>();

    // Page flipping animation
    (RenderTexture flip, RenderTexture stay) _pageFrames;
    MaterialPropertyBlock _pageProps;
    float _pageProgress, _pageSpeed;

    // Stable Diffusion pipeline
    SDPipeline _sdPipeline;

    #endregion

    #region Private methods

    async Awaitable InitObjects()
    {
        var width = MLStableDiffusion.Pipeline.Width;

        // Application frame rate setting
        Application.targetFrameRate = _displayFps;

        // Initial frame queue
        for (var i = 0; i < _queueLength; i++)
            _freeFrames.Enqueue(new RenderTexture(width, width, 0));

        // Page flipping animation
        _pageFrames.flip = new RenderTexture(width, width, 0);
        _pageFrames.stay = new RenderTexture(width, width, 0);
        _pageProps = new MaterialPropertyBlock();

        // Stable Diffusion pipeline
        _sdPipeline = new SDPipeline(_sdPreprocess);
        Debug.Log("Loading the Stable Diffusion mode...");
        await _sdPipeline.InitializeAsync(ResourcePath, ComputeUnits.CpuAndGpu);
        Debug.Log("Done.");
    }

    void ReleaseObjects()
    {
        while (_freeFrames.Count > 0) Destroy(_freeFrames.Dequeue());
        while (_stockFrames.Count > 0) Destroy(_stockFrames.Dequeue());

        Destroy(_pageFrames.flip);
        Destroy(_pageFrames.stay);
        _pageFrames = (null, null);

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
            // Start major frame generation.
            var majorRT = _freeFrames.Dequeue();
            var majorTask = RunSDPipelineAsync(majorRT, cancel);

            // Flip all the stocked pages during the major frame generation.
            while (_stockFrames.Count > 0)
            {
                _freeFrames.Enqueue(_pageFrames.stay);
                _pageFrames.stay = _pageFrames.flip;
                _pageFrames.flip = _stockFrames.Dequeue();
                (_pageProgress, _pageSpeed) = (0, 1);
                await Awaitable.WaitForSecondsAsync(_flipTime);
            }

            // Complete the major frame generation.
            await majorTask;

            // Start major frame animation.
            _freeFrames.Enqueue(_pageFrames.stay);
            _pageFrames.stay = _pageFrames.flip;
            _pageFrames.flip = majorRT;
            (_pageProgress, _pageSpeed) = (0, 0.5f);

            // Generate minor frames and fill the queue.
            while (_freeFrames.Count > 1)
            {
                await Awaitable.WaitForSecondsAsync(_flipTime);
                var rt = _freeFrames.Dequeue();
                Graphics.Blit(_source.Texture, rt);
                _stockFrames.Enqueue(rt);
            }
        }
    }

    void OnDestroy() => ReleaseObjects();

    void Update()
    {
        var dt = Time.deltaTime * _pageSpeed;
        _pageProgress = Mathf.Min(1, _pageProgress + dt / _flipTime);

        _pageProps.SetTexture("_Texture1", _pageFrames.flip);
        _pageProps.SetTexture("_Texture2", _pageFrames.stay);
        _pageProps.SetFloat("_Progress", _pageProgress);

        Graphics.RenderMesh
          (new RenderParams(_pageMaterial){ matProps = _pageProps },
           _pageMesh, 0, Matrix4x4.identity);
    }

    #endregion
}
