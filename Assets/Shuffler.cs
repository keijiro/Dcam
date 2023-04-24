using UnityEngine;
using System.Collections.Generic;
using Klak.TestTools;

public sealed class Shuffler : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] int _baseFps = 24;
    [SerializeField] ImageSource _source = null;
    [SerializeField] float _flipTime = 0.15f;
    [SerializeField] float _pauseTime = 1.2f;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] Mesh _quadMesh = null;
    [SerializeField, HideInInspector] Material _baseMaterial = null;

    #endregion

    #region Private objects

    const int Size = 512;

    DummyPipeline _pipeline;
    MaterialPropertyBlock _baseProps;

    (RenderTexture, RenderTexture) _activeFrames;
    Queue<RenderTexture> _freeFrames = new Queue<RenderTexture>();
    Queue<RenderTexture> _stockFrames = new Queue<RenderTexture>();

    float _progress;

    #endregion

    #region Private methods

    void InitObjects()
    {
        Application.targetFrameRate = _baseFps;

        _pipeline = new DummyPipeline(_source);
        _baseProps = new MaterialPropertyBlock();

        _activeFrames.Item1 = new RenderTexture(Size, Size, 0);
        _activeFrames.Item2 = new RenderTexture(Size, Size, 0);

        for (var i = 0; i < _pauseTime / _flipTime; i++)
            _freeFrames.Enqueue(new RenderTexture(Size, Size, 0));
    }

    void ReleaseObjects()
    {
        Destroy(_activeFrames.Item1);
        Destroy(_activeFrames.Item2);
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
                _freeFrames.Enqueue(_activeFrames.Item2);
                _activeFrames = (_stockFrames.Dequeue(), _activeFrames.Item1);
                _progress = 0;
                await Awaitable.WaitForSecondsAsync(_flipTime);
            }

            await heavyTask;

            _freeFrames.Enqueue(_activeFrames.Item2);
            _activeFrames = (heavyRT, _activeFrames.Item1);
            _progress = 0;

            while (_freeFrames.Count > 1)
            {
                var rt = _freeFrames.Dequeue();

                var task1 = _pipeline.ProcessLight(rt);
                var task2 = Awaitable.WaitForSecondsAsync(_flipTime);

                await task1;
                await task2;

                _stockFrames.Enqueue(rt);
            }
        }
    }

    void OnDestroy() => ReleaseObjects();

    void Update()
    {
        _progress = Mathf.Min(1, _progress + Time.deltaTime / _flipTime);

        _baseProps.SetTexture("_Texture1", _activeFrames.Item1);
        _baseProps.SetTexture("_Texture2", _activeFrames.Item2);
        _baseProps.SetFloat("_Progress", _progress);

        Graphics.RenderMesh
          (new RenderParams(_baseMaterial) { matProps = _baseProps },
           _quadMesh, 0, Matrix4x4.identity);
    }

    #endregion
}
