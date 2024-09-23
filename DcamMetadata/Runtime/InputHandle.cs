using UnityEngine;
using Unity.Properties;

namespace Dcam {

// Dcam input handle class for providing accessor properties and methods with
// input state structs
public sealed class InputHandle : MonoBehaviour
{
    #region Internal state data

    bool[] _buttons = new bool[32];
    float[] _knobs = new float[8];

    #endregion

    #region Accessing by properties

    [CreateProperty] public bool Button0  { get => _buttons[ 0]; set => _buttons[ 0] = value; }
    [CreateProperty] public bool Button1  { get => _buttons[ 1]; set => _buttons[ 1] = value; }
    [CreateProperty] public bool Button2  { get => _buttons[ 2]; set => _buttons[ 2] = value; }
    [CreateProperty] public bool Button3  { get => _buttons[ 3]; set => _buttons[ 3] = value; }
    [CreateProperty] public bool Button4  { get => _buttons[ 4]; set => _buttons[ 4] = value; }
    [CreateProperty] public bool Button5  { get => _buttons[ 5]; set => _buttons[ 5] = value; }
    [CreateProperty] public bool Button6  { get => _buttons[ 6]; set => _buttons[ 6] = value; }
    [CreateProperty] public bool Button7  { get => _buttons[ 7]; set => _buttons[ 7] = value; }

    [CreateProperty] public bool Button8  { get => _buttons[ 8]; set => _buttons[ 8] = value; }
    [CreateProperty] public bool Button9  { get => _buttons[ 9]; set => _buttons[ 9] = value; }
    [CreateProperty] public bool Button10 { get => _buttons[10]; set => _buttons[10] = value; }
    [CreateProperty] public bool Button11 { get => _buttons[11]; set => _buttons[11] = value; }
    [CreateProperty] public bool Button12 { get => _buttons[12]; set => _buttons[12] = value; }
    [CreateProperty] public bool Button13 { get => _buttons[13]; set => _buttons[13] = value; }
    [CreateProperty] public bool Button14 { get => _buttons[14]; set => _buttons[14] = value; }
    [CreateProperty] public bool Button15 { get => _buttons[15]; set => _buttons[15] = value; }

    [CreateProperty] public bool Button16 { get => _buttons[16]; set => _buttons[16] = value; }
    [CreateProperty] public bool Button17 { get => _buttons[17]; set => _buttons[17] = value; }
    [CreateProperty] public bool Button18 { get => _buttons[18]; set => _buttons[18] = value; }
    [CreateProperty] public bool Button19 { get => _buttons[19]; set => _buttons[19] = value; }
    [CreateProperty] public bool Button20 { get => _buttons[20]; set => _buttons[20] = value; }
    [CreateProperty] public bool Button21 { get => _buttons[21]; set => _buttons[21] = value; }
    [CreateProperty] public bool Button22 { get => _buttons[22]; set => _buttons[22] = value; }
    [CreateProperty] public bool Button23 { get => _buttons[23]; set => _buttons[23] = value; }

    [CreateProperty] public bool Button24 { get => _buttons[24]; set => _buttons[24] = value; }
    [CreateProperty] public bool Button25 { get => _buttons[25]; set => _buttons[25] = value; }
    [CreateProperty] public bool Button26 { get => _buttons[26]; set => _buttons[26] = value; }
    [CreateProperty] public bool Button27 { get => _buttons[27]; set => _buttons[27] = value; }
    [CreateProperty] public bool Button28 { get => _buttons[28]; set => _buttons[28] = value; }
    [CreateProperty] public bool Button29 { get => _buttons[29]; set => _buttons[29] = value; }
    [CreateProperty] public bool Button30 { get => _buttons[30]; set => _buttons[30] = value; }
    [CreateProperty] public bool Button31 { get => _buttons[31]; set => _buttons[31] = value; }

    [CreateProperty] public float Knob0  { get => _knobs[ 0]; set => _knobs[ 0] = value; }
    [CreateProperty] public float Knob1  { get => _knobs[ 1]; set => _knobs[ 1] = value; }
    [CreateProperty] public float Knob2  { get => _knobs[ 2]; set => _knobs[ 2] = value; }
    [CreateProperty] public float Knob3  { get => _knobs[ 3]; set => _knobs[ 3] = value; }
    [CreateProperty] public float Knob4  { get => _knobs[ 4]; set => _knobs[ 4] = value; }
    [CreateProperty] public float Knob5  { get => _knobs[ 5]; set => _knobs[ 5] = value; }
    [CreateProperty] public float Knob6  { get => _knobs[ 6]; set => _knobs[ 6] = value; }
    [CreateProperty] public float Knob7  { get => _knobs[ 7]; set => _knobs[ 7] = value; }

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

        for (var i = 0; i < 4; i++)
        {
            var bdata = 0;
            for (var bit = 0; bit < 8; bit++)
                if (_buttons[8 * i + bit]) bdata += 1 << bit;
            state.SetButtonData(i, bdata);
        }

        for (var i = 0; i < 8; i++)
            state.SetKnobData(i, (int)(_knobs[i] * 255));

        return state;
    }

    public void UpdateState(in InputState state)
    {
        for (var i = 0; i < 4; i++)
        {
            var bdata = state.GetButtonData(i);
            for (var bit = 0; bit < 8; bit++)
                _buttons[8 * i + bit] = (bdata & (1 << bit)) != 0;
        }

        for (var i = 0; i < 8; i++)
            _knobs[i] = state.GetKnobData(i) / 255.0f;
    }

    #endregion
}

} // namespace Dcam
