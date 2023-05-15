using UnityEngine;

namespace Dcam {

// Dcam input handle class for providing accessor properties and methods with
// input state structs
public sealed class InputHandle : MonoBehaviour
{
    #region Internal state data

    bool[] _buttons = new bool[16];
    float[] _knobs = new float[8];

    #endregion

    #region Accessing by properties

    public bool Button0  { get => _buttons[ 0]; set => _buttons[ 0] = value; }
    public bool Button1  { get => _buttons[ 1]; set => _buttons[ 1] = value; }
    public bool Button2  { get => _buttons[ 2]; set => _buttons[ 2] = value; }
    public bool Button3  { get => _buttons[ 3]; set => _buttons[ 3] = value; }
    public bool Button4  { get => _buttons[ 4]; set => _buttons[ 4] = value; }
    public bool Button5  { get => _buttons[ 5]; set => _buttons[ 5] = value; }
    public bool Button6  { get => _buttons[ 6]; set => _buttons[ 6] = value; }
    public bool Button7  { get => _buttons[ 7]; set => _buttons[ 7] = value; }
    public bool Button8  { get => _buttons[ 8]; set => _buttons[ 8] = value; }
    public bool Button9  { get => _buttons[ 9]; set => _buttons[ 9] = value; }
    public bool Button10 { get => _buttons[10]; set => _buttons[10] = value; }
    public bool Button11 { get => _buttons[11]; set => _buttons[11] = value; }
    public bool Button12 { get => _buttons[12]; set => _buttons[12] = value; }
    public bool Button13 { get => _buttons[13]; set => _buttons[13] = value; }
    public bool Button14 { get => _buttons[14]; set => _buttons[14] = value; }
    public bool Button15 { get => _buttons[15]; set => _buttons[15] = value; }

    public float Knob0  { get => _knobs[ 0]; set => _knobs[ 0] = value; }
    public float Knob1  { get => _knobs[ 1]; set => _knobs[ 1] = value; }
    public float Knob2  { get => _knobs[ 2]; set => _knobs[ 2] = value; }
    public float Knob3  { get => _knobs[ 3]; set => _knobs[ 3] = value; }
    public float Knob4  { get => _knobs[ 4]; set => _knobs[ 4] = value; }
    public float Knob5  { get => _knobs[ 5]; set => _knobs[ 5] = value; }
    public float Knob6  { get => _knobs[ 6]; set => _knobs[ 6] = value; }
    public float Knob7  { get => _knobs[ 7]; set => _knobs[ 7] = value; }

    #endregion

    #region Accessing by methods

    public bool GetButton(int index) => _buttons[index];
    public void SetButton(int index, bool value) => _buttons[index] = value;

    public float GetKnob(int index) => _knobs[index];
    public void SetKnob(int index, float value) => _knobs[index] = value;

    #endregion

    #region Input State interface

    public InputState InputState
      { get => MakeInputState(); set => UpdateState(value); }

    InputState MakeInputState()
    {
        var state = new InputState();

        var bdata0 = 0;
        var bdata1 = 0;

        for (var bit = 0; bit < 8; bit++)
        {
            if (_buttons[bit + 0]) bdata0 += 1 << bit;
            if (_buttons[bit + 8]) bdata1 += 1 << bit;
        }

        state.SetButtonData(0, bdata0);
        state.SetButtonData(1, bdata1);

        for (var i = 0; i < 8; i++)
            state.SetKnobData(i, (int)(_knobs[i] * 255));

        return state;
    }

    public void UpdateState(in InputState state)
    {
        var bdata0 = state.GetButtonData(0);
        var bdata1 = state.GetButtonData(1);

        for (var bit = 0; bit < 8; bit++)
        {
            _buttons[bit + 0] = (bdata0 & (1 << bit)) != 0;
            _buttons[bit + 8] = (bdata1 & (1 << bit)) != 0;
        }

        for (var i = 0; i < 8; i++)
            _knobs[i] = state.GetKnobData(i) / 255.0f;
    }

    #endregion
}

} // namespace Dcam
