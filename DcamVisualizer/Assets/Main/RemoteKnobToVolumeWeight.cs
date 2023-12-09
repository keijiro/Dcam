using UnityEngine;
using UnityEngine.Rendering;

namespace Dcam {

public sealed class RemoteKnobToVolumeWeight : MonoBehaviour
{
    [SerializeField] int _knobIndex = 0;
    [SerializeField] Volume _target = null;

    InputHandle _input;
    float _prev;

    void Start()
      => _input = FindFirstObjectByType<InputHandle>();

    void Update()
    {
        var current = _input.GetKnob(_knobIndex);
        if (current == _prev) return;
        _target.weight = current;
        _prev = current;
    }
}

} // namespace Dcam
