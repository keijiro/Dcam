using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class KeyInputToValue : MonoBehaviour
{
    [SerializeField] Key[] _keys = null;
    [SerializeField] UnityEvent<int> _intTarget = null;
    [SerializeField] UnityEvent<float> _floatTarget = null;

    KeyControl[] _controls;

    bool TrySetUp()
    {
        if (_controls != null) return true;

        var dev = Keyboard.current;
        if (dev == null) return false;

        _controls = new KeyControl[_keys.Length];
        for (var i = 0; i < _keys.Length; i++)
            _controls[i] = dev[_keys[i]];
        return true;
    }

    void Update()
    {
        if (!TrySetUp()) return;

        for (var i = 0; i < _controls.Length; i++)
        {
            if (_controls[i].wasPressedThisFrame)
            {
                _intTarget?.Invoke(i);
                _floatTarget?.Invoke((float)i / (_controls.Length - 1));
                break;
            }
        }
    }
}
