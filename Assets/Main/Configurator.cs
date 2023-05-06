using UnityEngine;

public sealed class Configurator : MonoBehaviour
{
    #region Editable fields

    [SerializeField] string[] _prompts = null;
    [SerializeField] Color _titleColor = Color.white;
    [SerializeField] Color _overlayColor = Color.white;

    #endregion

    #region Public accessors for input handlers

    public void SetPromptByIndex(int index)
      => _target.Prompt = _prompts[index];

    public void SetPrefilter(int index)
      => _target.Prefilter = index;

    public void SetStrengthAndStepCount(float param)
    {
        _target.Strength = 0.3f + param * 0.4f;
        _target.StepCount = (int)(12 - param * 4);
    }

    public void SetTitleOpacity(float opacity)
    {
        var color = _titleColor;
        color.a *= opacity;
        _target.TitleColor = color;
    }

    public void SetOverlayOpacity(float opacity)
    {
        var color = _overlayColor;
        color.a *= opacity;
        _target.OverlayColor = color;
    }

    #endregion

    #region MonoBehaviour implementation

    Shuffler _target;

    void Start()
      => _target = FindFirstObjectByType<Shuffler>();

    #endregion
}
