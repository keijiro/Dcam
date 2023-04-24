using UnityEngine;
using Klak.TestTools;

public sealed class Shuffler : MonoBehaviour
{
    [SerializeField] int _baseFps = 24;
    [SerializeField] ImageSource _source = null;
    [SerializeField] float _frameInterval = 0.1f;

    const int Size = 512;

    (RenderTexture, RenderTexture) _frames;
    MaterialPropertyBlock _block;

    async Awaitable Start()
    {
        Application.targetFrameRate = _baseFps;

        _frames.Item1 = new RenderTexture(Size, Size, 0);
        _frames.Item2 = new RenderTexture(Size, Size, 0);
        _block = new MaterialPropertyBlock();

        var renderer = GetComponent<Renderer>();

        while (true)
        {
            _block.SetTexture("_Texture1", _frames.Item1);
            _block.SetTexture("_Texture2", _frames.Item2);

            for (var t = 0.0f; t < _frameInterval; t += Time.deltaTime)
            {
                _block.SetFloat("_Progress", t / _frameInterval);
                renderer.SetPropertyBlock(_block);
                await Awaitable.NextFrameAsync();
            }

            if (Random.value < 0.1f) await Awaitable.WaitForSecondsAsync(1);

            _frames = (_frames.Item2, _frames.Item1);
            Graphics.Blit(_source.Texture, _frames.Item1);
        }
    }
}
