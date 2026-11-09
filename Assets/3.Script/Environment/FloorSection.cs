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

        if (isComboSuccess)
        {
            seq?.Kill();
            seq = DOTween.Sequence();

            seq.Append(floorMat.DOFloat(0.9f, brightID, 0.1f))
                .Join(floorMat.DOColor(hdrColor, colorID, 0.2f))
                .Append(floorMat.DOFloat(0f, brightID, 0.1f))
                .Join(floorMat.DOColor(baseColor, colorID, 0.2f));
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
