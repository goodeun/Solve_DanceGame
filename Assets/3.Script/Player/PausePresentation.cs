using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 페르소나식 Pause 연출(카메라 + 플레이어 모션). DancePauseHandler가 호출한다.
///
/// 진입: 시간 정지 중에도 Brain이 움직이도록 IgnoreTimeScale을 켜고, Pause 카메라를 최우선으로 올려
///       고정 샷으로 컷 전환한 뒤 정해진 궤적(시작 → 끝)으로 unscaled 이동한다.
///       플레이어 Animator만 UnscaledTime으로 바꿔 Pause 진입 모션 → Pause 루프 모션을 재생한다.
///       직전 댄스 상태(레이어별 상태·진행 위치·속도)는 저장한다.
/// 복귀: 저장한 댄스 상태를 그대로 복원(시간 정지 중이라 그 자세로 멈춰 있음)하고,
///       카운트다운 길이만큼 원래 카메라(일반/피버)로 블렌드해 돌아간다.
/// 완료: Brain·Animator 설정을 원래대로 돌려 게임 진행을 이어간다.
///
/// 플레이어가 화면 같은 위치·각도·크기로 보이도록 카메라 위치는 플레이어 기준 고정 오프셋으로 잡는다.
/// 샷을 직접 배치하려면 Shot Start / Shot End에 Transform을 지정한다(지정 시 오프셋보다 우선).
/// </summary>
public class PausePresentation : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("비우면 실행 시 자동 생성")]
    [SerializeField] private CinemachineCamera pauseCamera;
    [SerializeField] private CinemachineBrain brain;
    [Tooltip("피버 카메라(20)보다 높아야 한다")]
    [SerializeField] private int pausePriority = 100;
    [SerializeField, Range(10f, 90f)] private float fieldOfView = 32f;

    [Header("Shot (플레이어 로컬 기준)")]
    [Tooltip("지정하면 이 위치·회전에서 샷 시작(오프셋 무시)")]
    [SerializeField] private Transform shotStart;
    [Tooltip("지정하면 이 위치·회전으로 샷 이동(오프셋 무시)")]
    [SerializeField] private Transform shotEnd;
    [SerializeField] private Vector3 startOffset = new Vector3(1.6f, 1.1f, 2.6f);
    [SerializeField] private Vector3 endOffset = new Vector3(0.9f, 0.95f, 1.9f);
    [Tooltip("카메라가 바라볼 지점(플레이어 로컬)")]
    [SerializeField] private Vector3 lookOffset = new Vector3(0.35f, 0.75f, 0f);
    [Tooltip("진입 카메라 이동 시간(초, 실시간)")]
    [SerializeField, Range(0.5f, 2f)] private float enterMoveDuration = 1.2f;
    [SerializeField] private Ease enterMoveEase = Ease.OutCubic;
    [Tooltip("카운트다운이 없는 복귀(대사 중 등)의 카메라 복귀 블렌드(초)")]
    [SerializeField, Min(0f)] private float exitBlendWithoutCountdown = 0.6f;

    [Header("Player Motion")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("Pause 진입 모션 상태 이름(Base Layer). 없으면 임시 상태 사용")]
    [SerializeField] private string enterState = "Pause_Enter";
    [SerializeField] private string enterStateFallback = "Fever_Finish";
    [Tooltip("Pause 반복 모션 상태 이름(Base Layer). 없으면 임시 상태 사용")]
    [SerializeField] private string loopState = "Pause_Loop";
    [SerializeField] private string loopStateFallback = "Idle";
    [SerializeField, Min(0f)] private float motionCrossFade = 0.15f;

    private Transform player;
    private bool active;
    private Tween cameraTween;
    private Coroutine motionRoutine;
    private Coroutine blendRestoreRoutine;

    // 저장해 둔 원래 상태
    private bool savedIgnoreTimeScale;
    private AnimatorUpdateMode savedUpdateMode;
    private float savedSpeed = 1f;
    private int[] savedStateHashes;
    private float[] savedNormalizedTimes;

    public bool IsActive => active;

    private void Awake()
    {
        if (brain == null) brain = FindFirstObjectByType<CinemachineBrain>();

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        if (playerAnimator == null)
        {
            PlayerDanceAnimator dance = FindFirstObjectByType<PlayerDanceAnimator>();
            if (dance != null) playerAnimator = dance.GetComponent<Animator>();
        }
        if (player == null && playerAnimator != null) player = playerAnimator.transform;

        if (pauseCamera == null)
        {
            var go = new GameObject("PauseCamera");
            go.transform.SetParent(transform, false);
            pauseCamera = go.AddComponent<CinemachineCamera>();
        }
        pauseCamera.Priority = 0;
    }

    // ---------------- Enter ----------------

    public void Enter()
    {
        if (active) return;
        active = true;

        EnterCamera();
        EnterMotion();
    }

    private void EnterCamera()
    {
        if (brain != null)
        {
            savedIgnoreTimeScale = brain.IgnoreTimeScale;
            brain.IgnoreTimeScale = true;
        }

        if (pauseCamera == null || player == null) return;

        GetShot(true, out Vector3 startPos, out Quaternion startRot);
        GetShot(false, out Vector3 endPos, out Quaternion endRot);

        Transform cam = pauseCamera.transform;
        cam.SetPositionAndRotation(startPos, startRot);
        var lens = pauseCamera.Lens;
        lens.FieldOfView = fieldOfView;
        pauseCamera.Lens = lens;

        // 기존 카메라에서 블렌드하지 않고 고정 샷으로 바로 전환한다.
        SwitchWithBlend(() => pauseCamera.Priority = pausePriority, new CinemachineBlendDefinition { Style = CinemachineBlendDefinition.Styles.Cut, Time = 0f });

        cameraTween?.Kill();
        cameraTween = DOTween.Sequence()
            .Join(cam.DOMove(endPos, enterMoveDuration).SetEase(enterMoveEase))
            .Join(cam.DORotateQuaternion(endRot, enterMoveDuration).SetEase(enterMoveEase))
            .SetUpdate(true);
    }

    private void GetShot(bool start, out Vector3 position, out Quaternion rotation)
    {
        Transform anchor = start ? shotStart : shotEnd;
        if (anchor != null)
        {
            position = anchor.position;
            rotation = anchor.rotation;
            return;
        }

        position = player.TransformPoint(start ? startOffset : endOffset);
        Vector3 look = player.TransformPoint(lookOffset);
        rotation = Quaternion.LookRotation(look - position, Vector3.up);
    }

    private void EnterMotion()
    {
        if (playerAnimator == null) return;

        int layers = playerAnimator.layerCount;
        savedStateHashes = new int[layers];
        savedNormalizedTimes = new float[layers];
        for (int i = 0; i < layers; i++)
        {
            AnimatorStateInfo info = playerAnimator.IsInTransition(i)
                ? playerAnimator.GetNextAnimatorStateInfo(i)
                : playerAnimator.GetCurrentAnimatorStateInfo(i);
            savedStateHashes[i] = info.fullPathHash;
            savedNormalizedTimes[i] = info.normalizedTime;
        }
        savedSpeed = playerAnimator.speed;
        savedUpdateMode = playerAnimator.updateMode;

        // 시간 정지 중에도 플레이어 Pause 모션만 재생한다. 박자 배속은 Pause 중에는 쓰지 않는다.
        playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        playerAnimator.speed = 1f;

        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = StartCoroutine(PlayPauseMotion());
    }

    private IEnumerator PlayPauseMotion()
    {
        int enterHash = ResolveState(enterState, enterStateFallback);
        int loopHash = ResolveState(loopState, loopStateFallback);

        if (enterHash != 0)
        {
            playerAnimator.CrossFadeInFixedTime(enterHash, motionCrossFade, 0);
            yield return null;
            yield return null;

            // 진입 모션이 끝날 때까지 기다린다(실시간). 반복 모션으로 이어지는 전이가 컨트롤러에 있으면 그대로 따른다.
            float guard = 0f;
            while (active && guard < 5f)
            {
                AnimatorStateInfo info = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash != enterHash && !playerAnimator.IsInTransition(0)) break;
                if (info.shortNameHash == enterHash && info.normalizedTime >= 0.98f) break;
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (active && loopHash != 0)
            playerAnimator.CrossFadeInFixedTime(loopHash, motionCrossFade, 0);

        motionRoutine = null;
    }

    private int ResolveState(string preferred, string fallback)
    {
        int hash = Animator.StringToHash(preferred);
        if (!string.IsNullOrEmpty(preferred) && playerAnimator.HasState(0, hash)) return hash;
        hash = Animator.StringToHash(fallback);
        if (!string.IsNullOrEmpty(fallback) && playerAnimator.HasState(0, hash)) return hash;
        return 0;
    }

    // ---------------- Exit ----------------

    /// <summary>Pause UI가 닫힌 직후(카운트다운 시작 시점). blendSeconds 동안 원래 카메라로 돌아간다.</summary>
    public void BeginExit(float blendSeconds)
    {
        if (!active) return;

        RestoreMotion();

        cameraTween?.Kill();
        if (pauseCamera != null)
        {
            var blend = blendSeconds > 0.01f
                ? new CinemachineBlendDefinition { Style = CinemachineBlendDefinition.Styles.EaseInOut, Time = blendSeconds }
                : new CinemachineBlendDefinition { Style = CinemachineBlendDefinition.Styles.Cut, Time = 0f };
            SwitchWithBlend(() => pauseCamera.Priority = 0, blend);
        }
    }

    /// <summary>카운트다운 없이 복귀할 때의 블렌드 길이.</summary>
    public float ExitBlendWithoutCountdown => exitBlendWithoutCountdown;

    /// <summary>게임 재개 직전. Brain·Animator 설정을 원래대로 돌린다.</summary>
    public void Complete()
    {
        if (!active) return;
        active = false;

        if (brain != null) brain.IgnoreTimeScale = savedIgnoreTimeScale;

        if (playerAnimator != null)
        {
            playerAnimator.updateMode = savedUpdateMode;
            playerAnimator.speed = savedSpeed;
        }
    }

    private void RestoreMotion()
    {
        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = null;

        if (playerAnimator == null || savedStateHashes == null) return;

        // 직전 댄스 상태를 같은 진행 위치로 복원한다. 아직 시간 정지 중이므로 그 자세로 멈춰 있다가 재개와 함께 이어진다.
        for (int i = 0; i < savedStateHashes.Length && i < playerAnimator.layerCount; i++)
            if (savedStateHashes[i] != 0)
                playerAnimator.Play(savedStateHashes[i], i, savedNormalizedTimes[i] > 1f ? Mathf.Repeat(savedNormalizedTimes[i], 1f) : savedNormalizedTimes[i]);

        playerAnimator.updateMode = savedUpdateMode;
        playerAnimator.speed = savedSpeed;
        playerAnimator.Update(0f);
    }

    // ---------------- Helpers ----------------

    /// <summary>
    /// 다른 기능(FeverCamera)이 Brain의 기본 블렌드를 바꿔 두므로, 이 전환에만 쓸 블렌드를 잠시 넣고 바로 되돌린다.
    /// 블렌드는 우선순위가 바뀌는 순간 만들어지므로 1~2프레임 뒤에 원래 값으로 돌려도 진행 중인 블렌드는 유지된다.
    /// </summary>
    private void SwitchWithBlend(System.Action switchAction, CinemachineBlendDefinition blend)
    {
        if (brain == null)
        {
            switchAction();
            return;
        }

        if (blendRestoreRoutine != null)
        {
            StopCoroutine(blendRestoreRoutine);
            blendRestoreRoutine = null;
        }

        CinemachineBlendDefinition previous = brain.DefaultBlend;
        brain.DefaultBlend = blend;
        switchAction();
        blendRestoreRoutine = StartCoroutine(RestoreBlendLater(previous));
    }

    private IEnumerator RestoreBlendLater(CinemachineBlendDefinition previous)
    {
        yield return null;
        yield return null;
        if (brain != null) brain.DefaultBlend = previous;
        blendRestoreRoutine = null;
    }

    private void OnDisable()
    {
        cameraTween?.Kill();
        if (active) Complete();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform target = player;
        if (target == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject == null) return;
            target = playerObject.transform;
        }

        Vector3 look = target.TransformPoint(lookOffset);
        Vector3 start = shotStart != null ? shotStart.position : target.TransformPoint(startOffset);
        Vector3 end = shotEnd != null ? shotEnd.position : target.TransformPoint(endOffset);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(start, 0.08f);
        Gizmos.DrawLine(start, end);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(end, 0.1f);
        Gizmos.DrawLine(end, look);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(look, 0.05f);
    }
#endif
}
