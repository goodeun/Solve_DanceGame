using System.Collections;
using UnityEngine;
using DG.Tweening;

public class FeverGageUI : MonoBehaviour
{
    [SerializeField] private PhaseController phaseController;
    [SerializeField] private Renderer feverGage_img;
    [SerializeField, Range(0f, 1f)] private float sphereFillCompensation = 1f;

    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int ColorTopId = Shader.PropertyToID("_ColorTop");
    private static readonly int ColorBottomId = Shader.PropertyToID("_ColorBottom");
    private static readonly int Color1Id = Shader.PropertyToID("_Color1");
    private static readonly int Color2Id = Shader.PropertyToID("_Color2");
    private static readonly int Color3Id = Shader.PropertyToID("_Color3");

    private Material runtimeMaterial;
    private Tween fillTween;
    private float fillAmount;

    private void Awake()
    {
        feverGage_img ??= GetComponent<Renderer>();
        phaseController ??= PhaseController.Instance ?? FindFirstObjectByType<PhaseController>();
        if (feverGage_img != null)
            runtimeMaterial = feverGage_img.material;
    }

    private void OnEnable()
    {
        if (phaseController == null)
            phaseController = PhaseController.Instance ?? FindFirstObjectByType<PhaseController>();
        if (phaseController != null)
        {
            phaseController.PhaseChanged += SetGaugeColor;
            SetGaugeColor(phaseController.CurrentPhase);
        }

        runtimeMaterial?.SetFloat(FillAmountId, 0f);
        fillAmount = 0f;
    }

    private void OnDisable()
    {
        if (phaseController != null)
            phaseController.PhaseChanged -= SetGaugeColor;
        fillTween?.Kill();
    }

    public void UpdateFeverGage(float targetValue)
    {
        fillAmount = Mathf.Clamp01(targetValue);
        float visualFill = ToVisualFill(fillAmount);
        if (runtimeMaterial == null)
            return;

        fillTween?.Kill();
        fillTween = runtimeMaterial.DOFloat(visualFill, FillAmountId, 0.2f).SetEase(Ease.OutQuad);
    }

    private float ToVisualFill(float logicalFill)
    {
        float sphereCorrected = 0.5f + Mathf.Asin(logicalFill * 2f - 1f) / Mathf.PI;
        return Mathf.Lerp(logicalFill, sphereCorrected, sphereFillCompensation);
    }

    private void SetGaugeColor(Phase phase)
    {
        if (runtimeMaterial == null)
            return;

        Color top;
        Color bottom;
        switch (phase)
        {
            case Phase.Techno:
                top = runtimeMaterial.GetColor(Color1Id);
                bottom = runtimeMaterial.GetColor(Color2Id);
                break;
            case Phase.Pop:
                top = runtimeMaterial.GetColor(Color2Id);
                bottom = runtimeMaterial.GetColor(Color3Id);
                break;
            case Phase.HipHop:
                top = runtimeMaterial.GetColor(Color3Id);
                bottom = runtimeMaterial.GetColor(Color1Id);
                break;
            case Phase.Villain:
                top = runtimeMaterial.GetColor(Color1Id);
                bottom = runtimeMaterial.GetColor(Color3Id);
                break;
            default:
                return;
        }

        runtimeMaterial.SetColor(ColorTopId, top);
        runtimeMaterial.SetColor(ColorBottomId, bottom);
    }

    public void DEBUG_FeverGage() => StartCoroutine(DEBUG_feverGage_co());

    private IEnumerator DEBUG_feverGage_co()
    {
        UpdateFeverGage(0.1f);
        yield return new WaitForSeconds(5f);
        UpdateFeverGage(0.6f);
        yield return new WaitForSeconds(5f);
        UpdateFeverGage(1f);
    }
}
