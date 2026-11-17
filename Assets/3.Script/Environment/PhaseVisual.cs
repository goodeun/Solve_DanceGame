using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the selected music phase's colour theme to world renderers and UI images.
/// A phase manager can update this component through SetPhase.
/// </summary>
public sealed class PhaseVisual : MonoBehaviour
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

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock propertyBlock;
    public Phase CurrentPhase => phase;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        scoreManager ??= FindFirstObjectByType<ScoreManager>();
    }

    private void OnEnable()
    {
        popButton?.onClick.AddListener(SetPopPhase);
        hipHopButton?.onClick.AddListener(SetHipHopPhase);
        technoButton?.onClick.AddListener(SetTechnoPhase);

        // Preserve the authored HDR emission at startup. Visual material
        // overrides are applied only after the player selects a phase.
        scoreManager?.SetPhase(phase);
    }

    private void OnDisable()
    {
        popButton?.onClick.RemoveListener(SetPopPhase);
        hipHopButton?.onClick.RemoveListener(SetHipHopPhase);
        technoButton?.onClick.RemoveListener(SetTechnoPhase);
    }

    public void SetPopPhase() => SetPhase(Phase.Pop);
    public void SetHipHopPhase() => SetPhase(Phase.HipHop);
    public void SetTechnoPhase() => SetPhase(Phase.Techno);

    public void SetPhase(Phase nextPhase)
    {
        phase = nextPhase;
        scoreManager?.SetPhase(nextPhase);
        Apply();
    }

    public void Apply()
    {
        propertyBlock ??= new MaterialPropertyBlock();
        Color baseColour = GetBaseColour(phase);
        Color emissionColour = GetEmissionColour(phase);

        foreach (Image image in images)
            if (image != null)
                image.color = baseColour;

        foreach (Renderer target in renderers)
            ApplyRendererColours(target, baseColour, emissionColour);
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

    private void ApplyRendererColours(Renderer target, Color baseColour, Color emissionColour)
    {
        if (target == null)
            return;

        // Main-floor quads own their _Color through FloorSection and DOTween.
        // A renderer PropertyBlock would take priority over that material state,
        // so give their phase colours to FloorSection instead.
        if (target.TryGetComponent(out FloorSection floorSection))
        {
            target.SetPropertyBlock(null);
            floorSection.SetPhaseColours(baseColour, emissionColour);
            return;
        }

        // Only slot 0 is part of the phase theme. Other material slots stay untouched.
        Material sharedMaterial = target.sharedMaterial;
        if (sharedMaterial == null)
            return;

        target.GetPropertyBlock(propertyBlock);
        if (sharedMaterial.HasProperty(BaseColorId))
            propertyBlock.SetColor(BaseColorId, baseColour);
        else if (sharedMaterial.HasProperty(ColorId))
            propertyBlock.SetColor(ColorId, baseColour);
        target.SetPropertyBlock(propertyBlock);

        if (!sharedMaterial.HasProperty(EmissionColorId))
            return;

        Material runtimeMaterial = target.material;
        runtimeMaterial.EnableKeyword("_EMISSION");
        runtimeMaterial.SetColor(EmissionColorId, emissionColour);
    }
}
