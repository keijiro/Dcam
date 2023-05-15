using UnityEngine;

public sealed class CameraInput : MonoBehaviour
{
    [SerializeField] RenderTexture _target = null;

    WebCamTexture _webcam;

    async Awaitable Start()
    {
        await Application.RequestUserAuthorization(UserAuthorization.WebCam);

        _webcam = new WebCamTexture(1920, 1080, 30);
        _webcam.Play();
    }

    void Update()
    {
        if (_webcam == null || _webcam.width < 16) return;

        var vflip = _webcam.videoVerticallyMirrored;
        var src_ratio = (float)_webcam.width / _webcam.height;
        var dst_ratio = (float)_target.width / _target.height;
        var scale = new Vector2(dst_ratio / src_ratio, vflip ? -1 : 1);
        var offset = new Vector2((src_ratio / dst_ratio - 1) / 2, vflip ? 1 : 0);
        Graphics.Blit(_webcam, _target, scale, offset);
    }
}
