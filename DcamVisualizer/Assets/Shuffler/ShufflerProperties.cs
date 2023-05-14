// Shuffler - Public properties and serialized fields

using UnityEngine;
using ImageSource = Klak.TestTools.ImageSource;

public sealed partial class Shuffler
{
    #region Editable fields

    [Header("Stable Diffusion")]
    [SerializeField] string _prompt = "Surrealistic painting by J. C. Leyendecker";
    [SerializeField] float _strength = 0.7f;
    [SerializeField] int _stepCount = 7;
    [SerializeField] float _guidance = 10;

    [Header("Prefilter")]
    [SerializeField] int _prefilterNumber = 0;
    [SerializeField] Texture2D _titleTexture = null;
    [SerializeField] Color _titleColor = Color.white;
    [SerializeField] Texture2D _overlayTexture = null;
    [SerializeField] Color _overlayColor = Color.white;

    [Header("Flip animation")]
    [SerializeField] float _flipDuration = 0.175f;
    [SerializeField] int _queueLength = 9;
    [SerializeField] int _insertionCount = 5;

    [Header("Compile-time settings")]
    [SerializeField] string _resourceDir = "StableDiffusion";
    [SerializeField] ImageSource _source = null;
    [SerializeField] CameraController _camera = null;

    #endregion

    #region Non-editable fields (project asset references)

    [SerializeField, HideInInspector] Mesh _pageMesh = null;
    [SerializeField, HideInInspector] Material _pageMaterial = null;
    [SerializeField, HideInInspector] ComputeShader _sdPreprocess = null;
    [SerializeField, HideInInspector] Shader _prefilterShader = null;

    #endregion

    #region Public properties

    public string Prompt
      { get => _prompt; set => _prompt = value; }

    public float Strength
      { get => _strength; set => _strength = value; }

    public int StepCount
      { get => _stepCount; set => _stepCount = value; }

    public float Guidance
      { get => _guidance; set => _guidance = value; }

    public int Prefilter
      { get => _prefilterNumber; set => _prefilterNumber = value; }

    public Color TitleColor
      { get => _titleColor; set => _titleColor = value; }

    public Color OverlayColor
      { get => _overlayColor; set => _overlayColor = value; }

    public int InsertionCount
      { get => _insertionCount; set => _insertionCount = value; }

    #endregion
}
