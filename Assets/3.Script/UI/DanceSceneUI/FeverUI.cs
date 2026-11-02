using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FeverUI : MonoBehaviour
{
    [SerializeField] private Image feverGage_img;
    private float fillAmount_m;

    private void Awake()
    {
        feverGage_img = GetComponent<Image>();
    }

    private void OnEnable()
    {
        feverGage_img.material.SetFloat("_FillAmount", 0f);
    }

    public void UpdateFeverGage(float targetValue)
    {
        fillAmount_m = targetValue;

        if(feverGage_img != null && feverGage_img.material != null)
        {
            feverGage_img.material.
                DOFloat(targetValue, "_FillAmount", 0.2f).SetEase(Ease.OutQuad);
        }
    }

    #region µð¹ö±×
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
