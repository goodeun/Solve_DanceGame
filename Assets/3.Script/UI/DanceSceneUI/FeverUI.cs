using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;

public class FeverUI : MonoBehaviour
{
    public FeverCamera FeverCameraGroup;

    [SerializeField] private PopUpForMoment feverSuccess_img;
    [SerializeField] private PopUpForMoment feverFailure_img;
    private void Start()
    {
        feverSuccess_img.gameObject.SetActive(false);
        feverFailure_img.gameObject.SetActive(false);
    }
    public void FeverSuccess(float feverDuration, float returnBlendDuration)
    {
        StartCoroutine(SuccessEffect_co(feverDuration));

        FeverCameraGroup.FeverCameraOnAir(feverDuration, returnBlendDuration);
    }

    public void FeverFailure()
    {
        StartCoroutine(FailureEffect_co());
    }
    private IEnumerator SuccessEffect_co(float feverDuration)
    {
        feverSuccess_img.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator FailureEffect_co()
    {
        feverFailure_img.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
    }


}
