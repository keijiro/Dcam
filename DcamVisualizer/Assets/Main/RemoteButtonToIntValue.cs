using UnityEngine;
using UnityEngine.Events;

namespace Dcam {

public sealed class RemoteKnobToIntValue : MonoBehaviour
{
    [SerializeField] int _buttonIndex = 0;
    [SerializeField] int _value = 0;
    [SerializeField] UnityEvent<int> _event = null;

    InputHandle _input;

    void Start()
      => _input = FindFirstObjectByType<InputHandle>();

    void Update()
    {
        if (_input.GetButton(_buttonIndex)) _event.Invoke(_value);
    }
}

} // namespace Dcam
