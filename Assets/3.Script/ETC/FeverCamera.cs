using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening;

public class FeverCamera : MonoBehaviour
{
    [SerializeField] private FeverUI feverUI;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform player;

    [SerializeField] private CinemachineBrain mainCameraBrain;
    [SerializeField] private CinemachineCamera defaultCamera;
    [SerializeField] private CinemachineCamera[] feverCameras;

    private void Start()
    {
        feverUI = FindAnyObjectByType<FeverUI>();
        feverUI.FeverCameraGroup = this;

        //TODO 플레이어 애니메이터 연결. 임시로 태그 달아뒀습니다.
        player = GameObject.FindWithTag("Player").transform;

        Setting();
    }

    public void Setting()
    {
        if(player == null)
        {
            Debug.Log("FeverCamera가 player를 찾지 못함");
        }

        for (int i = 0; i < feverCameras.Length; i++)
        {
            feverCameras[i].LookAt = player;
        }
    }
    public void FeverCameraOnAir(float currentFeverDuration)
    {
        StartCoroutine(FeverSuccess_co(currentFeverDuration));
    }

    private IEnumerator FeverSuccess_co(float duration)
    {
        //TODO 플레이어 애니메이터 연결
        //playerAnimator.SetTrigger("FeverStart");

        mainCameraBrain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Cut;

        //TODO BPM 계산하여 카메라 연출
        float timePerCut = duration / feverCameras.Length;

        for (int i = 0; i < feverCameras.Length; i++)
        {
            feverCameras[i].Priority = 20;

            CinemachineSplineDolly dolly = feverCameras[i].GetComponent<CinemachineSplineDolly>();

            float currentDuration = (i == feverCameras.Length - 1) ? (timePerCut + 2f) : timePerCut;

            if (dolly != null)
            {
                dolly.CameraPosition = 0;

                DOTween.To(() => dolly.CameraPosition, x => dolly.CameraPosition = x, 1f, currentDuration)
                    .SetEase(Ease.Linear);
            }

            //TODO 플레이어 마지막 포징

            yield return new WaitForSeconds(currentDuration);

            feverCameras[i].Priority = 10;
        }

        mainCameraBrain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
        defaultCamera.Priority = 20;
    }

}
