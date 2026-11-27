using UnityEngine;
using DG.Tweening;

public class StageVisual : MonoBehaviour
{
    [Header("Key Input Motion")]
    [SerializeField, Min(0f)] private float keyDropDistance = 0.35f;
    [SerializeField, Min(0.01f)] private float keyDropDuration = 0.04f;
    [SerializeField, Min(0.01f)] private float keyReturnDuration = 0.16f;

    [Header("Key Input Light")]
    [SerializeField] private Renderer upperRenderer;
    [SerializeField] private Renderer lowerRenderer;
    [SerializeField] private string emissionColorProperty = "_EmissionColor";
    [SerializeField, Min(1f)] private float emissionIntensityMultiplier = 2f;
    [SerializeField, Min(0)] private int rivalPointThreshold = 100;
    [SerializeField, Min(0)] private int playerPointThreshold = 200;
    [SerializeField, Min(0)] private int maxPlayerPoints = 300;

    private Tween keyInputTween;
    private Tween intensityTween;

    private int emissionColorPropertyId;
    private int upperColorPropertyId;
    private int lowerColorPropertyId;

    private Color upperEmissionBeforeAnimation;
    private Color lowerEmissionBeforeAnimation;

    private bool hasUpperEmission;
    private bool hasLowerEmission;
    private Color currentPhaseEmissionColor;
    private bool hasPhaseEmissionColor;
    private int playerPoints;

    public event System.Action<StageVisual> OnPlayerPointsChanged;

    public Renderer[] Glow => new[] { upperRenderer, lowerRenderer };
    public int PlayerPoints => playerPoints;
    public int RivalPointThreshold => rivalPointThreshold;
    public int PlayerPointThreshold => playerPointThreshold;
    public int MaxPlayerPoints => maxPlayerPoints;

    private void Awake()
    {
        emissionColorPropertyId = Shader.PropertyToID(emissionColorProperty);
        upperColorPropertyId = ResolveColorProperty(upperRenderer);
        lowerColorPropertyId = ResolveColorProperty(lowerRenderer);
        hasUpperEmission = upperColorPropertyId != 0;
        hasLowerEmission = lowerColorPropertyId != 0;
    }

    public void PlayKeyInputDrop()
    {
        keyInputTween?.Kill();
        intensityTween?.Kill();
        // Every seat should return to the active phase colour, matching the
        // brightness applied by PhaseController when the game starts.
        if (!hasPhaseEmissionColor)
        {
            if (hasUpperEmission)
                upperEmissionBeforeAnimation = GetMaterialEmission(upperRenderer);
            if (hasLowerEmission)
                lowerEmissionBeforeAnimation = GetMaterialEmission(lowerRenderer);
        }

        Vector3 startPosition = transform.localPosition;
        keyInputTween = DOTween.Sequence()
            .Append(transform.DOLocalMoveY(startPosition.y - keyDropDistance, keyDropDuration))
            .Append(transform.DOLocalMoveY(startPosition.y, keyReturnDuration).SetEase(Ease.OutQuad))
            .OnComplete(() => keyInputTween = null);

        intensityTween = DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, keyDropDuration, SetEmissionIntensity))
            .Append(DOVirtual.Float(1f, 0f, keyReturnDuration, SetEmissionIntensity).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                RestoreEmissionColours();
                intensityTween = null;
            });
    }

    public void AddPlayerPoints(int points)
    {
        if (points <= 0)
            return;

        playerPoints = Mathf.Min(maxPlayerPoints, playerPoints + points);
        OnPlayerPointsChanged?.Invoke(this);
    }

    public void RemovePlayerPoints(int points)
    {
        if (points <= 0 || playerPoints <= 0)
            return;

        playerPoints = Mathf.Max(0, playerPoints - points);
        OnPlayerPointsChanged?.Invoke(this);
    }

    public Color GetRivalPhaseColor(Color rivalColor, Color neutralColor, Color playerColor)
    {
        if (playerPoints < rivalPointThreshold)
            return rivalColor;
        if (playerPoints < playerPointThreshold)
            return neutralColor;
        return playerColor;
    }

    public void SetGlowColor(Color color)
    {
        // Emission alpha is used as a strength channel by some project
        // shaders; a serialized HDR color with alpha 0 otherwise blacks out.
        color.a = 1f;
        currentPhaseEmissionColor = color;
        hasPhaseEmissionColor = true;
        upperEmissionBeforeAnimation = color;
        lowerEmissionBeforeAnimation = color;

        foreach (Renderer glow in Glow)
        {
            int colorProperty = GetColorPropertyId(glow);
            if (colorProperty == 0)
                continue;

            Color appliedColor = color;
            if (intensityTween != null && intensityTween.IsActive())
            {
                Color current = TryGetGlowColor(glow, out Color liveColor) ? liveColor : color;
                float amount = GetIntensityAmount(current, color);
                appliedColor = ScaleEmission(color, amount);
            }
            SetGlowRendererColor(glow, appliedColor);
        }
    }

    /// <summary>
    /// Rebase an active input flash when the global phase theme changes. The
    /// running flash keeps its progress, but its eventual restore uses the new
    /// phase colour instead of the colour captured before the transition.
    /// </summary>
    public void OnPhaseColorChanged(Color phaseEmissionColor)
    {
        currentPhaseEmissionColor = phaseEmissionColor;
        hasPhaseEmissionColor = true;

        if (hasUpperEmission)
        {
            upperEmissionBeforeAnimation = phaseEmissionColor;
            if (intensityTween == null || !intensityTween.IsActive())
                SetGlowRendererColor(upperRenderer, phaseEmissionColor);
        }
        if (hasLowerEmission)
        {
            lowerEmissionBeforeAnimation = phaseEmissionColor;
            if (intensityTween == null || !intensityTween.IsActive())
                SetGlowRendererColor(lowerRenderer, phaseEmissionColor);
        }

        if (intensityTween != null && intensityTween.IsActive())
        {
            float upperAmount = 0f;
            float lowerAmount = 0f;
            // The current flash is represented by the live material colour;
            // preserve its brightness while changing the underlying hue.
            if (hasUpperEmission && TryGetGlowColor(upperRenderer, out Color upperCurrent))
                upperAmount = GetIntensityAmount(upperCurrent, phaseEmissionColor);
            if (hasLowerEmission && TryGetGlowColor(lowerRenderer, out Color lowerCurrent))
                lowerAmount = GetIntensityAmount(lowerCurrent, phaseEmissionColor);

            SetGlowRendererColor(upperRenderer, ScaleEmission(phaseEmissionColor, upperAmount));
            SetGlowRendererColor(lowerRenderer, ScaleEmission(phaseEmissionColor, lowerAmount));
        }
    }

    private float GetIntensityAmount(Color current, Color baseColor)
    {
        float currentMagnitude = new Vector3(current.r, current.g, current.b).magnitude;
        float baseMagnitude = new Vector3(baseColor.r, baseColor.g, baseColor.b).magnitude;
        float multiplierRange = emissionIntensityMultiplier - 1f;
        if (baseMagnitude <= Mathf.Epsilon || multiplierRange <= Mathf.Epsilon)
            return 0f;

        return Mathf.Clamp01((currentMagnitude / baseMagnitude - 1f) / multiplierRange);
    }

    private int ResolveColorProperty(Renderer target)
    {
        if (target == null || target.sharedMaterial == null)
            return 0;

        // Some project shaders expose a separate emissive tint under this
        // property instead of Unity's standard _EmissionColor.
        int toonEmissionColorId = Shader.PropertyToID("_Emissive_Color");
        if (target.sharedMaterial.HasProperty(toonEmissionColorId))
            return toonEmissionColorId;
        int emissiveColorId = Shader.PropertyToID("_EmissiveColor");
        if (target.sharedMaterial.HasProperty(emissiveColorId))
            return emissiveColorId;
        if (target.sharedMaterial.HasProperty(emissionColorPropertyId))
            return emissionColorPropertyId;
        int baseColorId = Shader.PropertyToID("_BaseColor");
        if (target.sharedMaterial.HasProperty(baseColorId))
            return baseColorId;
        int colorId = Shader.PropertyToID("_Color");
        return target.sharedMaterial.HasProperty(colorId) ? colorId : 0;
    }

    private int GetColorPropertyId(Renderer target) => target == upperRenderer
        ? upperColorPropertyId
        : target == lowerRenderer ? lowerColorPropertyId : 0;

    private bool TryGetGlowColor(Renderer target, out Color color)
    {
        color = Color.black;
        int propertyId = GetColorPropertyId(target);
        if (propertyId == 0 || target == null || target.sharedMaterial == null)
            return false;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        color = target.material.GetColor(propertyId);
        if (block.HasProperty(propertyId))
            color = block.GetColor(propertyId);
        else if (hasPhaseEmissionColor)
            color = currentPhaseEmissionColor;
        return true;
    }

    private Color GetMaterialEmission(Renderer target)
    {
        return TryGetGlowColor(target, out Color color) ? color : Color.black;
    }

    private void SetEmissionIntensity(float amount)
    {
        if (hasUpperEmission)
            SetGlowRendererColor(upperRenderer, ScaleEmission(upperEmissionBeforeAnimation, amount));
        if (hasLowerEmission)
            SetGlowRendererColor(lowerRenderer, ScaleEmission(lowerEmissionBeforeAnimation, amount));
    }

    private Color ScaleEmission(Color baseColor, float amount)
    {
        Color emissionColor = baseColor * Mathf.Lerp(1f, emissionIntensityMultiplier, amount);
        emissionColor.a = baseColor.a;
        return emissionColor;
    }

    private void SetGlowRendererColor(Renderer target, Color color)
    {
        int propertyId = GetColorPropertyId(target);
        if (propertyId == 0 || target == null)
            return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        block.SetColor(propertyId, color);
        target.SetPropertyBlock(block);
    }

    private void RestoreEmissionColours()
    {
        SetGlowRendererColor(upperRenderer, upperEmissionBeforeAnimation);
        SetGlowRendererColor(lowerRenderer, lowerEmissionBeforeAnimation);
    }

    private void OnDisable()
    {
        keyInputTween?.Kill();
        intensityTween?.Kill();
        keyInputTween = null;
        intensityTween = null;

        RestoreEmissionColours();
    }
}
