using UnityEngine;
using UnityEngine.UIElements;
using VJUITK;

namespace Dcam {

public sealed class CameraSwitcher : MonoBehaviour
{
    [SerializeField] RenderTexture _target = null;

    static readonly string[] DeviceNames =
      {"Back Dual Camera", "Back Triple Camera", "Back Ultra Wide Camera"};

    WebCamTexture _webcam;

    void SelectCamera(int index)
    {
        if (_webcam != null) Destroy(_webcam);
        _webcam = new WebCamTexture(DeviceNames[index]);
        _webcam.Play();
    }

    async Awaitable Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Input handle as a data source
        root.Q("remote").dataSource = GetComponent<InputHandle>();

        // Camera button callbacks
        root.Q<VJButton>("camera-telephoto").Clicked += () => SelectCamera(0);
        root.Q<VJButton>("camera-wide")     .Clicked += () => SelectCamera(1);
        root.Q<VJButton>("camera-ultrawide").Clicked += () => SelectCamera(2);

        // Webcam activation
        await Application.RequestUserAuthorization(UserAuthorization.WebCam);

        // Initial camera (wide)
        SelectCamera(1);
    }

    void Update()
    {
        // Webcam state check
        if (_webcam == null || _webcam.width < 32) return;

        // Crop and copy
        var vflip = _webcam.videoVerticallyMirrored;
        var srcRatio = (float)_webcam.width / _webcam.height;
        var dstRatio = (float)_target.width / _target.height;
        var scale = new Vector2(1, (vflip ? -1 : 1) * srcRatio / dstRatio);
        var offs = new Vector2(0, -0.5f * scale.y + 0.5f);
        Graphics.Blit(_webcam, _target, scale, offs);
    }
}

} // namespace Dcam
