using UnityEngine;
using Klak.TestTools;

public sealed class DummyPipeline
{
    ImageSource _source;

    public DummyPipeline(ImageSource source)
      => _source = source;

    public async Awaitable ProcessLight(RenderTexture dest)
    {
        await Awaitable.WaitForSecondsAsync(Random.Range(0.1f, 0.17f));
        Graphics.Blit(_source.Texture, dest);
    }

    public async Awaitable ProcessHeavy(RenderTexture dest)
    {
        await Awaitable.WaitForSecondsAsync(Random.Range(1.0f, 1.5f));
        Graphics.Blit(_source.Texture, dest);
    }
}
