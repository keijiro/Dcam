using UnityEngine;
using System.Collections.Generic;
using Klak.TestTools;

public sealed class Shuffler : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] int _baseFps = 24;
    [SerializeField] ImageSource _source = null;
    [SerializeField] float _minorTime = 0.175f;
    [SerializeField] float _majorTime = 1.2f;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Mesh _quadMesh = null;
    [SerializeField, HideInInspector] Material _minorMaterial = null;
    [SerializeField, HideInInspector] Material _majorMaterial = null;

    #endregion

    #region Private objects

    const int Size = 512;

    DummyPipeline _pipeline;

    (MaterialPropertyBlock minor, MaterialPropertyBlock major) _props;

    (RenderTexture current, RenderTexture next) _activeFrames;
    Queue<RenderTexture> _freeFrames = new Queue<RenderTexture>();
    Queue<RenderTexture> _stockFrames = new Queue<RenderTexture>();

    float _progress;

    #endregion

    #region Private methods

    void InitObjects()
    {
        Application.targetFrameRate = _baseFps;

        _pipeline = new DummyPipeline(_source);

        _props.minor = new MaterialPropertyBlock();
        _props.major = new MaterialPropertyBlock();

        _activeFrames.current = new RenderTexture(Size, Size, 0);
        _activeFrames.next = new RenderTexture(Size, Size, 0);

        for (var i = 0; i < _majorTime / _minorTime + 1; i++)
            _freeFrames.Enqueue(new RenderTexture(Size, Size, 0));
    }

    void ReleaseObjects()
    {
        Destroy(_activeFrames.current);
        Destroy(_activeFrames.next);
        while (_freeFrames.Count > 0) Destroy(_freeFrames.Dequeue());
        while (_stockFrames.Count > 0) Destroy(_stockFrames.Dequeue());
    }

    #endregion

    #region MonoBehaviour implementation

    async Awaitable Start()
    {
        InitObjects();

        while (true)
        {
            var heavyRT = _freeFrames.Dequeue();
            var heavyTask = _pipeline.ProcessHeavy(heavyRT);

            while (_stockFrames.Count > 0)
            {
                _freeFrames.Enqueue(_activeFrames.current);
                _activeFrames = (_activeFrames.next, _stockFrames.Dequeue());
                _progress = 0;
                await Awaitable.WaitForSecondsAsync(_minorTime);
            }

            await heavyTask;

            _freeFrames.Enqueue(_activeFrames.current);
            _activeFrames = (_activeFrames.next, heavyRT);
            _progress = 0;

            while (_freeFrames.Count > 1)
            {
                var rt = _freeFrames.Dequeue();

                var task1 = _pipeline.ProcessLight(rt);
                var task2 = Awaitable.WaitForSecondsAsync(_minorTime);

                await task1;
                await task2;

                _stockFrames.Enqueue(rt);
            }
        }
    }

    void OnDestroy() => ReleaseObjects();

    void Update()
    {
        _progress = Mathf.Min(1, _progress + Time.deltaTime / _minorTime);

        _props.minor.SetTexture("_Texture1", _activeFrames.next);
        _props.minor.SetTexture("_Texture2", _activeFrames.current);
        _props.minor.SetFloat("_Progress", _progress);

        Graphics.RenderMesh
          (new RenderParams(_minorMaterial) { matProps = _props.minor },
           _quadMesh, 0, Matrix4x4.identity);
    }

    #endregion
}
