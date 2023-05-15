using UnityEngine;

public sealed class AppSettings : MonoBehaviour
{
    [SerializeField] int _frameRate = 24;

    void Start() => Application.targetFrameRate = _frameRate;
}
