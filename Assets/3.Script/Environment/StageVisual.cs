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

    private Tween keyInputTween;
    private Tween intensityTween;

    private int emissionColorPropertyId;

    private Color upperEmissionBeforeAnimation;
    private Color lowerEmissionBeforeAnimation;

    private bool hasUpperEmission;
    private bool hasLowerEmission;
    private Color currentPhaseEmissionColor;
    private bool hasPhaseEmissionColor;

    private void Awake()
    {
        emissionColorPropertyId = Shader.PropertyToID(emissionColorProperty);
    }

    public void PlayKeyInputDrop()
    {
        keyInputTween?.Kill();
        intensityTween?.Kill();
        RestoreEmissionColours();
        hasUpperEmission = TryGetEmissionColor(upperRenderer, out upperEmissionBeforeAnimation);
        hasLowerEmission = TryGetEmissionColor(lowerRenderer, out lowerEmissionBeforeAnimation);

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
            upperEmissionBeforeAnimation = phaseEmissionColor;
        if (hasLowerEmission)
            lowerEmissionBeforeAnimation = phaseEmissionColor;

        if (intensityTween != null && intensityTween.IsActive())
        {
            float upperAmount = 0f;
            float lowerAmount = 0f;
            // The current flash is represented by the live material colour;
            // preserve its brightness while changing the underlying hue.
            if (hasUpperEmission && TryGetEmissionColor(upperRenderer, out Color upperCurrent))
                upperAmount = GetIntensityAmount(upperCurrent, phaseEmissionColor);
            if (hasLowerEmission && TryGetEmissionColor(lowerRenderer, out Color lowerCurrent))
                lowerAmount = GetIntensityAmount(lowerCurrent, phaseEmissionColor);

            SetEmissionColor(upperRenderer, ScaleEmission(phaseEmissionColor, upperAmount), hasUpperEmission);
            SetEmissionColor(lowerRenderer, ScaleEmission(phaseEmissionColor, lowerAmount), hasLowerEmission);
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

    private bool TryGetEmissionColor(Renderer target, out Color emissionColor)
    {
        emissionColor = Color.black;
        if (target == null || target.sharedMaterial == null || !target.sharedMaterial.HasProperty(emissionColorPropertyId))
            return false;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        emissionColor = target.material.GetColor(emissionColorPropertyId);
        if (block.HasProperty(emissionColorPropertyId))
            emissionColor = block.GetColor(emissionColorPropertyId);
        else if (hasPhaseEmissionColor)
            emissionColor = currentPhaseEmissionColor;
        return true;
    }

    private void SetEmissionIntensity(float amount)
    {
        SetEmissionColor(upperRenderer, ScaleEmission(upperEmissionBeforeAnimation, amount), hasUpperEmission);
        SetEmissionColor(lowerRenderer, ScaleEmission(lowerEmissionBeforeAnimation, amount), hasLowerEmission);
    }

    private Color ScaleEmission(Color baseColor, float amount)
    {
        Color emissionColor = baseColor * Mathf.Lerp(1f, emissionIntensityMultiplier, amount);
        emissionColor.a = baseColor.a;
        return emissionColor;
    }

    private void SetEmissionColor(Renderer target, Color emissionColor, bool canSetEmissionColor)
    {
        if (!canSetEmissionColor)
            return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        block.SetColor(emissionColorPropertyId, emissionColor);
        target.SetPropertyBlock(block);
    }

    private void RestoreEmissionColours()
    {
        SetEmissionColor(upperRenderer, upperEmissionBeforeAnimation, hasUpperEmission);
        SetEmissionColor(lowerRenderer, lowerEmissionBeforeAnimation, hasLowerEmission);
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
