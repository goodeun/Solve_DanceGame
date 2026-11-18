using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Applies the selected music phase's colour theme to world renderers and UI images.
/// A phase manager can update this component through SetPhase.
/// </summary>
public sealed class PhaseController : MonoBehaviour
{
    [SerializeField] private Phase phase = Phase.Pop;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Image[] images;

    [Header("Phase Buttons")]
    [SerializeField] private Button popButton;
    [SerializeField] private Button hipHopButton;
    [SerializeField] private Button technoButton;

    [Header("Phase Base Colours")]
    [SerializeField] private Color popColor;
    [SerializeField] private Color hipHopColor;
    [SerializeField] private Color technoColor;

    [Header("Phase HDR Emission Colours")]
    [ColorUsage(true, true)] [SerializeField] private Color popEmissionColor;
    [ColorUsage(true, true)] [SerializeField] private Color hipHopEmissionColor;
    [ColorUsage(true, true)] [SerializeField] private Color technoEmissionColor;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float colorTransitionDuration = 1f;
    [SerializeField] private Ease colorTransitionEase = Ease.InOutSine;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private readonly System.Collections.Generic.List<Tween> activeColorTweens = new();
    public Phase CurrentPhase => phase;
    public Color CurrentEmissionColor => GetEmissionColour(phase);

    public static PhaseController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        scoreManager ??= FindFirstObjectByType<ScoreManager>();
    }

    private void OnEnable()
    {
        popButton?.onClick.AddListener(SetPopPhase);
        hipHopButton?.onClick.AddListener(SetHipHopPhase);
        technoButton?.onClick.AddListener(SetTechnoPhase);

        scoreManager?.SetPhase(phase);
        Apply();
    }

    private void OnDisable()
    {
        popButton?.onClick.RemoveListener(SetPopPhase);
        hipHopButton?.onClick.RemoveListener(SetHipHopPhase);
        technoButton?.onClick.RemoveListener(SetTechnoPhase);
        KillColorTweens();
        if (Instance == this)
            Instance = null;
    }

    public void SetPopPhase() => SetPhase(Phase.Pop);
    public void SetHipHopPhase() => SetPhase(Phase.HipHop);
    public void SetTechnoPhase() => SetPhase(Phase.Techno);

    public void SetPhase(Phase nextPhase)
    {
        phase = nextPhase;
        scoreManager?.SetPhase(nextPhase);
        Apply(true);
    }

    public void Apply() => Apply(false);

    private void Apply(bool animated)
    {
        Color baseColour = GetBaseColour(phase);
        Color emissionColour = GetEmissionColour(phase);
        KillColorTweens();

        foreach (Image image in images)
            if (image != null)
            {
                if (animated && colorTransitionDuration > 0f)
                    activeColorTweens.Add(image.DOColor(baseColour, colorTransitionDuration).SetEase(colorTransitionEase));
                else
                    image.color = baseColour;
            }

        foreach (Renderer target in renderers)
            ApplyRendererColours(target, baseColour, emissionColour, animated);
    }

    private Color GetBaseColour(Phase targetPhase) => targetPhase switch
    {
        Phase.Pop => popColor,
        Phase.HipHop => hipHopColor,
        Phase.Techno => technoColor,
        _ => popColor
    };

    private Color GetEmissionColour(Phase targetPhase) => targetPhase switch
    {
        Phase.Pop => popEmissionColor,
        Phase.HipHop => hipHopEmissionColor,
        Phase.Techno => technoEmissionColor,
        _ => popEmissionColor
    };

    private void ApplyRendererColours(Renderer target, Color baseColour, Color emissionColour, bool animated)
    {
        if (target == null)
            return;

        // Only slot 0 is part of the phase theme. Other material slots stay untouched.
        Material sharedMaterial = target.sharedMaterial;
        if (sharedMaterial == null)
            return;

        Material runtimeMaterial = target.material;
        if (sharedMaterial.HasProperty(EmissionColorId))
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            AnimateColor(runtimeMaterial, EmissionColorId, emissionColour, animated);
            return;
        }

        // Shaders without a separate Emission property (such as unlit light
        // meshes) use their primary colour as the HDR display colour.
        if (sharedMaterial.HasProperty(BaseColorId))
            AnimateColor(runtimeMaterial, BaseColorId, emissionColour, animated);
        else if (sharedMaterial.HasProperty(ColorId))
            AnimateColor(runtimeMaterial, ColorId, emissionColour, animated);
    }

    private void AnimateColor(Material material, int propertyId, Color targetColor, bool animated)
    {
        if (animated && colorTransitionDuration > 0f)
            activeColorTweens.Add(material.DOColor(targetColor, propertyId, colorTransitionDuration).SetEase(colorTransitionEase));
        else
            material.SetColor(propertyId, targetColor);
    }

    private void KillColorTweens()
    {
        foreach (Tween tween in activeColorTweens)
            tween?.Kill();
        activeColorTweens.Clear();
    }
}
