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

    private bool TryGetEmissionColor(Renderer target, out Color emissionColor)
    {
        emissionColor = Color.black;
        if (target == null || target.sharedMaterial == null || !target.sharedMaterial.HasProperty(emissionColorPropertyId))
            return false;

        emissionColor = target.material.GetColor(emissionColorPropertyId);
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

        target.material.SetColor(emissionColorPropertyId, emissionColor);
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
