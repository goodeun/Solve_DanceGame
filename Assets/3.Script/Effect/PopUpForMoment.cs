using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;

public class PopUpForMoment : MonoBehaviour
{
    [Header("ÆË¾÷ ÀÚµ¿ ²¨Áü ¼³Á¤")]
    [SerializeField] private bool isAutoClose = true;

    [Header("ÆË¾÷ À¯Áö ½Ã°£")]
    [SerializeField] private float holdTime = 1f;
    [SerializeField] private Ease ease = Ease.OutBack;
    private void OnEnable()
    {
        StartCoroutine(Anim_Popup_co());
    }

    private IEnumerator Anim_Popup_co()
    {
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.2f).SetEase(ease);

        if (isAutoClose)
        {
            yield return new WaitForSeconds(holdTime);

            transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
