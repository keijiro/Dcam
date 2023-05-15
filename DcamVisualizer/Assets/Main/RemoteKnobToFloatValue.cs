using UnityEngine;
using UnityEngine.Events;

namespace Dcam {

public sealed class RemoteKnobToFloatValue : MonoBehaviour
{
    [SerializeField] int _knobIndex = 0;
    [SerializeField] UnityEvent<float> _event = null;

    InputHandle _input;
    float _prev;

    void Start()
      => _input = FindFirstObjectByType<InputHandle>();

    void Update()
    {
        var current = _input.GetKnob(_knobIndex);
        if (current == _prev) return;
        _event.Invoke(current);
        _prev = current;
    }
}

} // namespace Dcam
