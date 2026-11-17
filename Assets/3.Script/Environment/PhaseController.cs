using UnityEngine;
using UnityEngine.UI;

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

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    public Phase CurrentPhase => phase;

    private void Awake()
    {
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

        // Only slot 0 is part of the phase theme. Other material slots stay untouched.
        Material sharedMaterial = target.sharedMaterial;
        if (sharedMaterial == null)
            return;

        Material runtimeMaterial = target.material;
        if (sharedMaterial.HasProperty(EmissionColorId))
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor(EmissionColorId, emissionColour);
            return;
        }

        // Shaders without a separate Emission property (such as unlit light
        // meshes) use their primary colour as the HDR display colour.
        if (sharedMaterial.HasProperty(BaseColorId))
            runtimeMaterial.SetColor(BaseColorId, emissionColour);
        else if (sharedMaterial.HasProperty(ColorId))
            runtimeMaterial.SetColor(ColorId, emissionColour);
    }
}
