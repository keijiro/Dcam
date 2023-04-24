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

    (RenderTexture minor, RenderTexture major, RenderTexture flap) _activeFrames;
    Queue<RenderTexture> _freeFrames = new Queue<RenderTexture>();
    Queue<RenderTexture> _stockFrames = new Queue<RenderTexture>();

    (MaterialPropertyBlock minor, MaterialPropertyBlock major) _props;
    (float minor, float major) _progress;

    #endregion

    #region Private methods

    void InitObjects()
    {
        Application.targetFrameRate = _baseFps;

        _pipeline = new DummyPipeline(_source);

        _props.minor = new MaterialPropertyBlock();
        _props.major = new MaterialPropertyBlock();

        _activeFrames.minor = new RenderTexture(Size, Size, 0);
        _activeFrames.flap = new RenderTexture(Size, Size, 0);

        for (var i = 0; i < _majorTime / _minorTime + 1; i++)
            _freeFrames.Enqueue(new RenderTexture(Size, Size, 0));
    }

    void ReleaseObjects()
    {
        Destroy(_activeFrames.minor);
        Destroy(_activeFrames.major);
        Destroy(_activeFrames.flap);
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
            var majorRT = _freeFrames.Dequeue();
            var majorTask = _pipeline.ProcessHeavy(majorRT);

            while (_stockFrames.Count > 0)
            {
                _freeFrames.Enqueue(_activeFrames.minor);
                _activeFrames.minor = _activeFrames.flap;
                _activeFrames.flap = _stockFrames.Dequeue();
                _progress.minor = 0;
                await Awaitable.WaitForSecondsAsync(_minorTime);
            }

            await majorTask;

            _freeFrames.Enqueue(_activeFrames.minor);
            _activeFrames.minor = _activeFrames.flap;
            _activeFrames.major = _activeFrames.flap = majorRT;
            _progress.minor = _progress.major = 0;

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
        var dt = Time.deltaTime;

        _progress.minor = Mathf.Min(1, _progress.minor + dt / _minorTime);
        _progress.major += dt;

        _props.minor.SetTexture("_Texture1", _activeFrames.flap);
        _props.minor.SetTexture("_Texture2", _activeFrames.minor);
        _props.minor.SetFloat("_Progress", _progress.minor);

        Graphics.RenderMesh
          (new RenderParams(_minorMaterial) { matProps = _props.minor },
           _quadMesh, 0, Matrix4x4.identity);

        if (_activeFrames.major == null) return;

        var cutoff = (_progress.major - _minorTime * 5) / _majorTime;
        var lift = (_progress.major - _minorTime);
        lift = lift * lift * lift * 0.2f;

        if (lift < 0 || cutoff > 1) return;

        _props.major.SetTexture("_Texture", _activeFrames.major);
        _props.major.SetFloat("_Cutoff", cutoff);

        Graphics.RenderMesh
          (new RenderParams(_majorMaterial) { matProps = _props.major },
           _quadMesh, 0, Matrix4x4.Translate(new Vector3(0, 0, -lift)));
    }

    #endregion
}
