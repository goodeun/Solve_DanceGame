using System.Collections;
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
        if (feverUI != null)
            feverUI.FeverCameraGroup = this;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;

        Setting();
        if (Camera.main != null)
        {
            originalCullingMask = Camera.main.cullingMask;
            int layer = LayerMask.NameToLayer(hiddenLayerName);
            hiddenLayerMask = layer >= 0 ? 1 << layer : 0;
        }
    }

    public void Setting()
    {
        if (player == null)
            Debug.Log("FeverCamera: Player was not found.", this);

        if (feverCameras == null)
            return;
        foreach (CinemachineCamera camera in feverCameras)
            if (camera != null)
                camera.LookAt = player;
    }

    public void FeverCameraOnAir(float currentFeverDuration, float returnBlendDuration)
    {
        if (feverSequenceCoroutine != null)
            StopCoroutine(feverSequenceCoroutine);

        IsCameraSequenceActive = true;
        feverSequenceCoroutine = StartCoroutine(FeverSuccess_co(currentFeverDuration, returnBlendDuration));
    }

    public void AllowGameplayInput() => IsCameraSequenceActive = false;

    private IEnumerator FeverSuccess_co(float duration, float returnBlendDuration)
    {
        if (mainCameraBrain != null)
            mainCameraBrain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Cut;

        int cameraCount = feverCameras != null ? feverCameras.Length : 0;
        float timePerCut = cameraCount > 0 ? duration / cameraCount : 0f;

        if (Camera.main != null)
            Camera.main.cullingMask &= ~hiddenLayerMask;

        for (int i = 0; i < cameraCount; i++)
        {
            CinemachineCamera feverCamera = feverCameras[i];
            if (feverCamera == null)
                continue;

            feverCamera.Priority = 20;
            CinemachineSplineDolly dolly = feverCamera.GetComponent<CinemachineSplineDolly>();
            if (dolly != null)
            {
                dolly.CameraPosition = 0;
                DOTween.To(() => dolly.CameraPosition, x => dolly.CameraPosition = x, 1f, timePerCut)
                    .SetEase(Ease.Linear);
            }

            yield return new WaitForSeconds(timePerCut);
            feverCamera.Priority = 10;
        }

        if (mainCameraBrain != null)
        {
            mainCameraBrain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
            mainCameraBrain.DefaultBlend.Time = returnBlendDuration;
        }
        if (defaultCamera != null)
            defaultCamera.Priority = 20;

        // The fever gameplay ends on the beat boundary. Unlock input then,
        // while the camera completes its purely visual return blend.
        IsCameraSequenceActive = false;

        yield return null;
        while (mainCameraBrain != null && mainCameraBrain.IsBlending)
            yield return null;

        if (Camera.main != null)
            Camera.main.cullingMask = originalCullingMask;
        feverSequenceCoroutine = null;
    }
}
