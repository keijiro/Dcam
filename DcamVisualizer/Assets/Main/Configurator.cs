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

    public void InsertionLength(float param)
      => _target.InsertionCount = (int)(param * 5 + 1);

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

    public float AudioSensitivity { get; set; }

    #endregion

    #region Audio input to noise level

    float _audioLevel;

    public float AudioLevel { get => _audioLevel; set => SetAudioLevel(value); }

    void SetAudioLevel(float level)
    {
        _audioLevel = level;
        _target.NoiseLevel = level * AudioSensitivity;
    }

    #endregion

    #region MonoBehaviour implementation

    Shuffler _target;

    void Start()
    {
        _target = FindFirstObjectByType<Shuffler>();
        SetPromptByIndex(0);
        SetPrefilter(0);
        InsertionLength(0);
        SetTitleOpacity(0);
        SetOverlayOpacity(0);
        SetAudioLevel(0);
    }

    #endregion
}
