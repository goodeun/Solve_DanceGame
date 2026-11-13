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

    [SerializeField] private string hiddenLayerName = "UI";
    private int originalCullingMask;
    private int hiddenLayerMask;

    public bool IsCameraSequenceActive { get; private set; }
    private Coroutine feverSequenceCoroutine;

    private void Start()
    {
        feverUI = FindAnyObjectByType<FeverUI>();
        feverUI.FeverCameraGroup = this;

        //TODO 플레이어 애니메이터 연결. 임시로 태그 달아뒀습니다.
        player = GameObject.FindWithTag("Player").transform;

        Setting();

        if (Camera.main != null)
        {
            originalCullingMask = Camera.main.cullingMask;
            hiddenLayerMask = 1 << LayerMask.NameToLayer(hiddenLayerName);
        }
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
    public void FeverCameraOnAir(float currentFeverDuration, float returnBlendDuration)
    {
        if (feverSequenceCoroutine != null)
            StopCoroutine(feverSequenceCoroutine);

        IsCameraSequenceActive = true;
        feverSequenceCoroutine = StartCoroutine(FeverSuccess_co(currentFeverDuration, returnBlendDuration));
    }

    private IEnumerator FeverSuccess_co(float duration, float returnBlendDuration)
    {
        //TODO 플레이어 애니메이터 연결
        //playerAnimator.SetTrigger("FeverStart");

        mainCameraBrain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Cut;

        //TODO BPM 계산하여 카메라 연출
        float timePerCut = duration / feverCameras.Length;

        Camera.main.cullingMask &= ~hiddenLayerMask;

        for (int i = 0; i < feverCameras.Length; i++)
        {
            feverCameras[i].Priority = 20;

            CinemachineSplineDolly dolly = feverCameras[i].GetComponent<CinemachineSplineDolly>();

            float currentDuration = timePerCut;

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
        mainCameraBrain.DefaultBlend.Time = returnBlendDuration;
        defaultCamera.Priority = 20;

        // Wait one frame for the priority change to start its blend, then keep
        // judgements locked until the default camera is fully active again.
        yield return null;
        while (mainCameraBrain != null && mainCameraBrain.IsBlending)
            yield return null;

        Camera.main.cullingMask = originalCullingMask;
        IsCameraSequenceActive = false;
        feverSequenceCoroutine = null;
    }

}
