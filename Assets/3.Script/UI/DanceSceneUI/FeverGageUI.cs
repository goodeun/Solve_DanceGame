
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FeverGageUI : MonoBehaviour
{
    [SerializeField] private Renderer feverGage_img;
    [SerializeField, Range(0f, 1f)]
    private float sphereFillCompensation = 1f;
    private float fillAmount_m;

    private void Awake()
    {
        feverGage_img = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        feverGage_img.material.SetFloat("_FillAmount", 0f);
    }

    public void UpdateFeverGage(float targetValue)
    {
        fillAmount_m = Mathf.Clamp01(targetValue);
        float visualFill = ToVisualFill(fillAmount_m);

        if(feverGage_img != null && feverGage_img.material != null)
        {
            feverGage_img.material.
                DOFloat(visualFill, "_FillAmount", 0.2f).SetEase(Ease.OutQuad);
        }
    }

    // A sphere's silhouette is narrow at the top and bottom. The shader's
    // linear latitude fill therefore looks slow at both ends and fast at the
    // centre. Inverse-sine mapping makes the visible fill edge move at a
    // constant screen-space speed while keeping the gameplay value linear.
    private float ToVisualFill(float logicalFill)
    {
        float sphereCorrected = 0.5f + Mathf.Asin(logicalFill * 2f - 1f) / Mathf.PI;
        return Mathf.Lerp(logicalFill, sphereCorrected, sphereFillCompensation);
    }

    #region 디버그
    public void DEBUG_FeverGage()
    {
        StartCoroutine(DEBUG_feverGage_co());
    }

    private IEnumerator DEBUG_feverGage_co()
    {

        UpdateFeverGage(0.1f); 
        yield return new WaitForSeconds(5f);

        UpdateFeverGage(0.6f); 

        yield return new WaitForSeconds(5f);

        UpdateFeverGage(1f); 

    }

    #endregion
}
