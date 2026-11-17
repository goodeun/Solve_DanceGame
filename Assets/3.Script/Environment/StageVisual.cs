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
    private MaterialPropertyBlock upperPropertyBlock;
    private MaterialPropertyBlock lowerPropertyBlock;
    private int emissionColorPropertyId;
    private Color upperBaseEmissionColor;
    private Color lowerBaseEmissionColor;
    private bool hasUpperEmissionColor;
    private bool hasLowerEmissionColor;

    private void Awake()
    {
        upperPropertyBlock = new MaterialPropertyBlock();
        lowerPropertyBlock = new MaterialPropertyBlock();
        emissionColorPropertyId = Shader.PropertyToID(emissionColorProperty);
        hasUpperEmissionColor = TryGetBaseEmissionColor(upperRenderer, out upperBaseEmissionColor);
        hasLowerEmissionColor = TryGetBaseEmissionColor(lowerRenderer, out lowerBaseEmissionColor);
    }

    public void PlayKeyInputDrop()
    {
        keyInputTween?.Kill();
        intensityTween?.Kill();

        SetEmissionColor(upperRenderer, upperPropertyBlock, upperBaseEmissionColor, hasUpperEmissionColor);
        SetEmissionColor(lowerRenderer, lowerPropertyBlock, lowerBaseEmissionColor, hasLowerEmissionColor);

        Vector3 startPosition = transform.localPosition;
        keyInputTween = DOTween.Sequence()
            .Append(transform.DOLocalMoveY(startPosition.y - keyDropDistance, keyDropDuration))
            .Append(transform.DOLocalMoveY(startPosition.y, keyReturnDuration).SetEase(Ease.OutQuad))
            .OnComplete(() => keyInputTween = null);

        intensityTween = DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, keyDropDuration, SetEmissionIntensity))
            .Append(DOVirtual.Float(1f, 0f, keyReturnDuration, SetEmissionIntensity).SetEase(Ease.OutQuad))
            .OnComplete(() => intensityTween = null);
    }

    private bool TryGetBaseEmissionColor(Renderer target, out Color emissionColor)
    {
        emissionColor = Color.black;
        if (target == null || target.sharedMaterial == null || !target.sharedMaterial.HasProperty(emissionColorPropertyId))
            return false;

        emissionColor = target.sharedMaterial.GetColor(emissionColorPropertyId);
        return true;
    }

    private void SetEmissionIntensity(float amount)
    {
        SetEmissionColor(upperRenderer, upperPropertyBlock, ScaleEmission(upperBaseEmissionColor, amount), hasUpperEmissionColor);
        SetEmissionColor(lowerRenderer, lowerPropertyBlock, ScaleEmission(lowerBaseEmissionColor, amount), hasLowerEmissionColor);
    }

    private Color ScaleEmission(Color baseColor, float amount)
    {
        Color emissionColor = baseColor * Mathf.Lerp(1f, emissionIntensityMultiplier, amount);
        emissionColor.a = baseColor.a;
        return emissionColor;
    }

    private void SetEmissionColor(Renderer target, MaterialPropertyBlock propertyBlock, Color emissionColor, bool canSetEmissionColor)
    {
        if (!canSetEmissionColor)
            return;

        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(emissionColorPropertyId, emissionColor);
        target.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        keyInputTween?.Kill();
        intensityTween?.Kill();
        keyInputTween = null;
        intensityTween = null;

        SetEmissionColor(upperRenderer, upperPropertyBlock, upperBaseEmissionColor, hasUpperEmissionColor);
        SetEmissionColor(lowerRenderer, lowerPropertyBlock, lowerBaseEmissionColor, hasLowerEmissionColor);
    }
}
