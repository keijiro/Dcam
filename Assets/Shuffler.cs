using UnityEngine;
using Klak.TestTools;

public sealed class Shuffler : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] int _baseFps = 24;
    [SerializeField] ImageSource _source = null;
    [SerializeField] float _flipTime = 0.15f;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Mesh _quadMesh = null;
    [SerializeField, HideInInspector] Material _baseMaterial = null;

    #endregion

    #region Private objects

    const int Size = 512;

    DummyPipeline _pipeline;
    (RenderTexture, RenderTexture, RenderTexture) _frames;
    MaterialPropertyBlock _baseProps;
    float _progress;

    #endregion

    #region MonoBehaviour implementation

    async Awaitable Start()
    {
        Application.targetFrameRate = _baseFps;

        _pipeline = new DummyPipeline(_source);
        _frames.Item1 = new RenderTexture(Size, Size, 0);
        _frames.Item2 = new RenderTexture(Size, Size, 0);
        _frames.Item3 = new RenderTexture(Size, Size, 0);
        _baseProps = new MaterialPropertyBlock();

        while (true)
        {
            _progress = 0;
            _baseProps.SetTexture("_Texture1", _frames.Item1);
            _baseProps.SetTexture("_Texture2", _frames.Item2);

            var task1 = _pipeline.ProcessLight(_frames.Item3);
            var task2 = Awaitable.WaitForSecondsAsync(0.2f);
            await task1;
            await task2;

            _frames = (_frames.Item3, _frames.Item1, _frames.Item2);
        }
    }

    void OnDestroy()
    {
        Destroy(_frames.Item1);
        Destroy(_frames.Item2);
        Destroy(_frames.Item3);
    }

    void Update()
    {
        _progress = Mathf.Min(1, _progress + Time.deltaTime / _flipTime);
        _baseProps.SetFloat("_Progress", _progress);

        Graphics.RenderMesh
          (new RenderParams(_baseMaterial) { matProps = _baseProps },
           _quadMesh, 0, Matrix4x4.identity);
    }

    #endregion
}

public sealed class DummyPipeline
{
    ImageSource _source;

    public DummyPipeline(ImageSource source)
      => _source = source;

    public async Awaitable ProcessLight(RenderTexture dest)
    {
        await Awaitable.WaitForSecondsAsync(0.1f);
        Graphics.Blit(_source.Texture, dest);
    }

    public async Awaitable ProcessHeavy(RenderTexture dest)
    {
        await Awaitable.WaitForSecondsAsync(1);
        Graphics.Blit(_source.Texture, dest);
    }
}
