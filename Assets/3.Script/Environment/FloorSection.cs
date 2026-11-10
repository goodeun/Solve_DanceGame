using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FloorSection : MonoBehaviour
{

    [Header("Tween Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float burstDuration = 0.5f;
    [SerializeField] public float intensity = 3.0f;


    private bool isActivated = false;

    private Color baseColor;
    private Color hdrColor;

    private MeshRenderer floorMesh;
    private Material floorMat;

    private readonly int brightID = Shader.PropertyToID("_Brightness");
    private readonly int colorID = Shader.PropertyToID("_Color");
    private readonly int scaleID = Shader.PropertyToID("_Scale");

    private Sequence seq;

    private void Awake()
    {
        if (floorMesh == null)
        {
            floorMesh = GetComponent<MeshRenderer>();
        }

        floorMat = floorMesh.material;

        baseColor = floorMat.GetColor(colorID);
        hdrColor = baseColor * intensity;

        floorMat.SetColor(colorID, baseColor);

    }
    private void OnEnable()
    {
        TempEventManager.OnThemaColorChange_Temp += UpdateThemaColor;
        TempEventManager.OnMeasureEnd_Temp += HandleMeasureEnd;
        TempEventManager.OnBeat_Temp += ChangeScale;
    }
    private void OnDisable()
    {
        TempEventManager.OnThemaColorChange_Temp -= UpdateThemaColor;
        TempEventManager.OnMeasureEnd_Temp -= HandleMeasureEnd;
        TempEventManager.OnBeat_Temp -= ChangeScale;
    }

    private void UpdateThemaColor(Color newColor)
    {
        floorMat.DOColor(newColor, colorID, 0.5f);
        baseColor = newColor;
        hdrColor = newColor * intensity;
    }

    public void ActiveLight()
    {
        floorMat.DOKill();
        seq?.Kill();

        // A key press can interrupt the combo-success tween while _Brightness
        // is elevated. Reset it before playing the normal key-light response.
        floorMat.SetFloat(brightID, 0f);

        if (!isActivated)
        {
            isActivated = true;
            floorMat.DOColor(hdrColor,colorID, 0.1f);
        }
        else
        {
            seq = DOTween.Sequence();

            Color subtitleThemeGlow = Color.Lerp(Color.white, baseColor, 0.5f) * intensity;

            floorMat.SetColor(colorID, baseColor);

            seq.SetDelay(0.03f)
                .Append(floorMat.DOColor(subtitleThemeGlow, colorID, 0.07f));
        }
    }

    private void HandleMeasureEnd(bool isComboSuccess)
    {
        isActivated = false;
        floorMat.DOKill();
        floorMat.SetFloat(brightID, 0f);

        if (isComboSuccess)
        {
            seq?.Kill();
            seq = DOTween.Sequence();

            // Combo success is a quick confirmation flash, not a sustained glow.
            seq.Append(floorMat.DOFloat(0.9f, brightID, 0.01f))
                .Join(floorMat.DOColor(hdrColor, colorID, 0.01f))
                .Append(floorMat.DOFloat(0f, brightID, 0.15f))
                .Join(floorMat.DOColor(baseColor, colorID, 0.15f));
        }
        else
        {
            floorMat.DOColor(baseColor, colorID, 0.2f);
        }
    }

    private void ChangeScale()
    {
        if (isActivated) return;

        floorMat.SetFloat(scaleID, Random.Range(100f, 1000f));
    }
}
