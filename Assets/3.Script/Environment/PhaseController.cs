using System.Collections;
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
    [SerializeField] private StageVisual[] seats;
    [SerializeField] private MainFloorController mainFloorController;
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

    [Header("Villain HDR Emission Colours")]
    [ColorUsage(true, true)] [SerializeField] private Color villainEmissionColor;
    [ColorUsage(true, true)] [SerializeField] private Color neutralEmissionColor;
    [ColorUsage(true, true)] [SerializeField] private Color playerEmissionColor;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float colorTransitionDuration = 1f;
    [SerializeField] private Ease colorTransitionEase = Ease.InOutSine;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private readonly System.Collections.Generic.List<Tween> activeColorTweens = new();
    public Phase CurrentPhase => phase;
    /// <summary>새 페이즈(팀원 FeverGageUI 등에서 사용)</summary>
    public event System.Action<Phase> PhaseChanged;
    /// <summary>(이전 페이즈, 새 페이즈). 실제로 바뀔 때만 호출(데모 진행용)</summary>
    public event System.Action<Phase, Phase> PhaseTransitioned;
    public Color CurrentEmissionColor => GetEmissionColour(phase);

    public static PhaseController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        scoreManager ??= FindFirstObjectByType<ScoreManager>();
        mainFloorController ??= FindFirstObjectByType<MainFloorController>();
    }

    private void OnEnable()
    {
        popButton?.onClick.AddListener(SetPopPhase);
        hipHopButton?.onClick.AddListener(SetHipHopPhase);
        technoButton?.onClick.AddListener(SetTechnoPhase);

        if (scoreManager != null)
            scoreManager.OnPointsAdded += HandlePointsAdded;

        SubscribeToSeatPoints();

        scoreManager?.SetPhase(phase);
        Apply();
    }

    private IEnumerator Start()
    {
        // Other scene components may initialize their materials in Start.
        // Reapply the phase and per-seat colors after those initializers run.
        yield return null;
        Apply();
    }

    private void OnDisable()
    {
        popButton?.onClick.RemoveListener(SetPopPhase);
        hipHopButton?.onClick.RemoveListener(SetHipHopPhase);
        technoButton?.onClick.RemoveListener(SetTechnoPhase);
        if (scoreManager != null)
            scoreManager.OnPointsAdded -= HandlePointsAdded;
        UnsubscribeFromSeatPoints();
        KillColorTweens();
        if (Instance == this)
            Instance = null;
    }

    public void SetPopPhase() => SetPhase(Phase.Pop);
    public void SetHipHopPhase() => SetPhase(Phase.HipHop);
    public void SetTechnoPhase() => SetPhase(Phase.Techno);

    public void SetPhase(Phase nextPhase)
    {
        Phase previous = phase;
        phase = nextPhase;
        if (previous != nextPhase)
            PhaseTransitioned?.Invoke(previous, nextPhase);
        scoreManager?.SetPhase(nextPhase);
        Apply(true);
        PhaseChanged?.Invoke(nextPhase);
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

        // Seat input flashes keep their brightness animation, but need their
        // restore colour rebased to this phase so completion cannot restore a
        // stale emission colour captured before the phase changed.
        if (seats == null)
            return;

        for (int i = 0; i < seats.Length; i++)
        {
            StageVisual seat = seats[i];
            if (seat == null)
                continue;

            Color seatColor = phase == Phase.Villain
                ? seat.GetRivalPhaseColor(villainEmissionColor, neutralEmissionColor, playerEmissionColor)
                : emissionColour;
            seat.SetGlowColor(seatColor);
            mainFloorController?.SetSeatEmissionColor(i, seatColor);
        }
    }

    private void HandlePointsAdded(Keys key, int points)
    {
        StageVisual seat = StageController.Instance != null ? StageController.Instance.GetSeatForKey(key) : null;
        if (seat == null)
            return;

        seat.AddPlayerPoints(points);
    }

    public void AddPointsToAllSeats(int points)
    {
        if (seats == null)
            return;

        foreach (StageVisual seat in seats)
            seat?.AddPlayerPoints(points);
    }

    private void SubscribeToSeatPoints()
    {
        if (seats == null)
            return;

        foreach (StageVisual seat in seats)
            if (seat != null)
                seat.OnPlayerPointsChanged += HandleSeatPointsChanged;
    }

    private void UnsubscribeFromSeatPoints()
    {
        if (seats == null)
            return;

        foreach (StageVisual seat in seats)
            if (seat != null)
                seat.OnPlayerPointsChanged -= HandleSeatPointsChanged;
    }

    private void HandleSeatPointsChanged(StageVisual seat)
    {
        if (phase != Phase.Villain || seat == null)
            return;

        Color seatColor = seat.GetRivalPhaseColor(villainEmissionColor, neutralEmissionColor, playerEmissionColor);
        seat.SetGlowColor(seatColor);

        if (seats == null || mainFloorController == null)
            return;

        for (int i = 0; i < seats.Length; i++)
            if (seats[i] == seat)
            {
                mainFloorController.SetSeatEmissionColor(i, seatColor);
                break;
            }
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
        Phase.Villain => neutralEmissionColor,
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
