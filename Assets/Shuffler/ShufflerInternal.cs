// Shuffler - Internal implementation

#define ENABLE_MLSD

using UnityEngine;
using System.Collections.Generic;
using ComputeUnits = MLStableDiffusion.ComputeUnits;
using SDPipeline = MLStableDiffusion.Pipeline;

public sealed partial class Shuffler
{
    const int ImageWidth = 640;
    const int ImageHeight = 384;

    string ResourcePath => Application.streamingAssetsPath + "/" + _resourceDir;

    MLStableDiffusion.ResourceInfo ResourceInfo
      => MLStableDiffusion.ResourceInfo.FixedSizeModel(ResourcePath, ImageWidth, ImageHeight);

    // Prefilter
    Prefilter _prefilter;

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

    Matrix4x4 MakePageTransform(float z, float scale)
      => Matrix4x4.TRS
           (Vector3.forward * z, Quaternion.identity,
            new Vector3(1, (float)ImageHeight / ImageWidth, 1) * scale);

    async Awaitable InitObjects()
    {
        // Prefilter
        _prefilter = new Prefilter(ImageWidth, ImageHeight, _prefilterShader)
          { Layer1Texture = _titleTexture, Layer2Texture = _overlayTexture };

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
        Debug.Log("Loading the Stable Diffusion model...");
        await _sdPipeline.InitializeAsync(ResourceInfo, ComputeUnits.CpuAndGpu);
        Debug.Log("Done.");
#else
        await Awaitable.NextFrameAsync();
#endif
    }

    void ReleaseObjects()
    {
        _prefilter.Destroy();
        _prefilter = null;

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
}
