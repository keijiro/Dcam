using UnityEngine;

namespace Dcam {

public sealed class Controller : MonoBehaviour
{
    [SerializeField] RenderTexture _buffer = null;

    WebCamTexture _webcam;

    async Awaitable Start()
    {
        // Webcam activation
        await Application.RequestUserAuthorization(UserAuthorization.WebCam);
        _webcam = new WebCamTexture(1920, 1080, 30);
        _webcam.Play();
    }

    void Update()
    {
        // Webcam state check
        if (_webcam == null || _webcam.width < 16) return;

        // Crop and copy
        var vflip = _webcam.videoVerticallyMirrored;
        var srcRatio = (float)_webcam.width / _webcam.height;
        var dstRatio = (float)_buffer.width / _buffer.height;
        var scale = new Vector2(dstRatio / srcRatio, vflip ? -1 : 1);
        var offset = new Vector2((srcRatio / dstRatio - 1) / 2, vflip ? 1 : 0);
        Graphics.Blit(_webcam, _buffer, scale, offset);

        // Metadata update
        var metadata = new Metadata
          { InputState = GetComponent<InputHandle>().InputState };
        GetComponent<Klak.Ndi.NdiSender>().metadata = metadata.Serialize();
    }
}

} // namespace Dcam
