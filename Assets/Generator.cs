using UnityEngine;
using IDisposable = System.IDisposable;
using CancellationToken = System.Threading.CancellationToken;
using Serializable = System.SerializableAttribute;
using ComputeUnits = MLStableDiffusion.ComputeUnits;
using ImageSource = Klak.TestTools.ImageSource;
using OperationCanceledException = System.OperationCanceledException;

[Serializable]
public struct GeneratorConfig
{
    public string Prompt;
    public float Strength;
    public int StepCount;
    public int Seed;
    public float Guidance;

    public static GeneratorConfig Default => new GeneratorConfig()
    {
        Prompt = "vincent van gogh",
        Strength = 0.5f,
        StepCount = 5,
        Seed = 1,
        Guidance = 6
    };
}

public sealed class ImageGenerator : IDisposable
{
    MLStableDiffusion.Pipeline _pipeline;
    ImageSource _source;

    public ImageGenerator(ComputeShader preprocess, ImageSource source)
    {
        _pipeline = new MLStableDiffusion.Pipeline(preprocess);
        _source = source;
    }

    public void Dispose()
    {
        _pipeline?.Dispose();
        _pipeline = null;
    }

    public async Awaitable InitializeAsync(string resourcePath)
    {
        await _pipeline.InitializeAsync(resourcePath, ComputeUnits.CpuAndGpu);
    }

    public async Awaitable RunAsync
      (GeneratorConfig config, RenderTexture dest, CancellationToken canceller)
    {
        _pipeline.Prompt = config.Prompt;
        _pipeline.Strength = config.Strength;
        _pipeline.StepCount = config.StepCount;
        _pipeline.Seed = config.Seed;
        _pipeline.GuidanceScale = config.Guidance;
        await _pipeline.RunAsync(_source.Texture, dest, canceller);
    }
}
