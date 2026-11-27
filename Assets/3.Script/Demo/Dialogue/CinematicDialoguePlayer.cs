using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 시네머신 카메라와 함께 쓰는 연출 대사 매니저.
/// - 카메라 무빙 하나에 대사 n개를 연결한다.
/// - 카메라 무빙이 끝나도 연결된 대사가 남아 있으면 다음 카메라로 넘어가지 않는다.
/// - 카메라를 전환하면 무빙이 시작되며, 카메라에 CinemachineSplineDolly가 있으면 트랙 위치를 DOTween Ease로 이동한다.
/// 카메라, 이동 시간, Ease, 대사를 Inspector에서 지정한다.
/// </summary>
public class CinematicDialoguePlayer : MonoBehaviour
{
    [Serializable]
    public class Shot
    {
        public CinemachineCamera camera;
        [Min(0f)] public float moveDuration = 2f;
        public Ease ease = Ease.InOutSine;
        [Tooltip("트랙 시작/끝 위치(SplineDolly CameraPosition)")]
        public Vector2 dollyRange = new Vector2(0f, 1f);
        public List<StoryDialogueLine> lines = new List<StoryDialogueLine>();
        public UnityEvent onShotStarted = new UnityEvent();
    }

    [SerializeField] private List<Shot> shots = new List<Shot>();
    [Tooltip("연출이 끝난 뒤 돌아갈 기본 카메라(비우면 원래 우선순위로 복구만 한다)")]
    [SerializeField] private CinemachineCamera returnCamera;
    [SerializeField] private int activePriority = 50;
    [SerializeField] private UnityEvent onFinished = new UnityEvent();

    private readonly Dictionary<CinemachineCamera, int> originalPriorities = new Dictionary<CinemachineCamera, int>();
    private Coroutine routine;

    public bool HasShots => shots != null && shots.Count > 0;
    public bool IsPlaying => routine != null;

    public void Play() => Play(null);

    public void Play(Action onComplete)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(Action onComplete)
    {
        originalPriorities.Clear();
        StoryDialogueView view = StoryDialogueView.GetOrCreate();
        CinemachineCamera previous = null;

        foreach (Shot shot in shots)
        {
            if (shot == null) continue;

            bool moveDone = true;
            bool linesDone = false;

            if (shot.camera != null)
            {
                if (!originalPriorities.ContainsKey(shot.camera))
                    originalPriorities[shot.camera] = shot.camera.Priority.Value;

                if (previous != null && previous != shot.camera)
                    previous.Priority = originalPriorities[previous];

                shot.camera.Priority = activePriority;
                previous = shot.camera;

                var dolly = shot.camera.GetComponent<CinemachineSplineDolly>();
                if (dolly != null && shot.moveDuration > 0f)
                {
                    moveDone = false;
                    dolly.CameraPosition = shot.dollyRange.x;
                    DOTween.To(() => dolly.CameraPosition, x => dolly.CameraPosition = x, shot.dollyRange.y, shot.moveDuration)
                        .SetEase(shot.ease)
                        .OnComplete(() => moveDone = true);
                }
                else if (shot.moveDuration > 0f)
                {
                    // 트랙이 없는 카메라는 블렌드·연출 시간만큼 유지한다.
                    moveDone = false;
                    DOVirtual.DelayedCall(shot.moveDuration, () => moveDone = true);
                }
            }

            shot.onShotStarted?.Invoke();
            view.Play(shot.lines, () => linesDone = true);

            while (!moveDone || !linesDone)
                yield return null;
        }

        // 기본 카메라로 복귀
        foreach (var pair in originalPriorities)
            if (pair.Key != null)
                pair.Key.Priority = pair.Value;

        if (returnCamera != null)
            returnCamera.Priority = activePriority - 1;

        routine = null;
        onFinished?.Invoke();
        onComplete?.Invoke();
    }
}
