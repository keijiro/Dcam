// Shuffler - MonoBehaviour implementation

using UnityEngine;

public sealed partial class Shuffler : MonoBehaviour
{
    async Awaitable Start()
    {
        // Initialization
        await InitObjects();

        for (var genTask = (Awaitable)null;;)
        {
            // Prefilter
            _prefilter.Layer1Color = _titleColor;
            _prefilter.Layer2Color = _overlayColor;
            _prefilter.Apply(_source.AsTexture, _prefilterNumber);

            // Push the previous "latest" frame to the queue.
            _frameQueue.Enqueue(_latestFrame);

            // Reuse the previous "sheet" frame to store the latest frame.
            _latestFrame = _bgFrames.sheet;
            Graphics.Blit(_prefilter.Output, _latestFrame);

            // The previous "flip" frame becomes the "sheet" frame.
            _bgFrames.sheet = _bgFrames.flip;

            // Get a frame from the queue and make it flipping.
            _bgFrames.flip = _frameQueue.Dequeue();

            // Flip animation restart
            _flipTime = 0;

            // Generator task cycle
            if (_flipCount >= _queueLength && (genTask == null || genTask.IsCompleted))
            {
                _fgFrames = (_fgFrames.front, _fgFrames.back);
                genTask = RunSDPipelineAsync();
                _flipCount = 0;
                _camera.RenewTarget();
            }

            // Per-flip wait
            await Awaitable.WaitForSecondsAsync(_flipDuration);

            _flipCount++;
        }
    }

    void OnDestroy() => ReleaseObjects();

    void Update()
    {
        // Flip animation time step
        _flipTime += Time.deltaTime / _flipDuration;

        // Foreground page insertion
        var fgTex1 = _flipCount < _insertionCount ? _fgFrames.front : _bgFrames.flip;
        var fgTex2 = _flipCount == _insertionCount ? _fgFrames.front : _bgFrames.sheet;
        var fgTime = _flipCount > 0 && _flipCount < _insertionCount ? 1 : _flipTime;

        // Rendering
        _bgParams.props.SetTexture("_Texture1", _bgFrames.flip);
        _fgParams.props.SetTexture("_Texture1", fgTex1);

        _bgParams.props.SetTexture("_Texture2", _bgFrames.sheet);
        _fgParams.props.SetTexture("_Texture2", fgTex2);

        _bgParams.props.SetFloat("_Progress", Mathf.Clamp01(_flipTime));
        _fgParams.props.SetFloat("_Progress", Mathf.Clamp01(fgTime));

        _bgParams.props.SetFloat("_NoiseLevel", _noiseLevel);
        _fgParams.props.SetFloat("_NoiseLevel", _noiseLevel);

        Graphics.RenderMesh(_bgParams.rparams, _pageMesh, 0, _bgParams.matrix);
        Graphics.RenderMesh(_fgParams.rparams, _pageMesh, 0, _fgParams.matrix);
    }
}
