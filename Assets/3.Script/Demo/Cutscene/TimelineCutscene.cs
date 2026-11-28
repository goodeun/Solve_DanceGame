using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// Unity Timeline으로 만든 컷씬을 게임 흐름에 붙이는 재생기.
/// - Timeline(PlayableDirector)을 처음부터 끝까지 재생한다. 카메라 전환·캐릭터 애니메이션·오브젝트 켜고 끄기는 Timeline에서 만든다.
/// - Dialogue Cues: 지정한 시간이 되면 Timeline을 잠깐 멈추고 대사를 띄운 뒤 이어서 재생한다.
/// - Moves: 오브젝트(예: 걸어나오는 보스)를 경로 점들을 따라 지정한 시간 동안 이동시킨다(Timeline 시간 기준이라 멈추면 같이 멈춤).
/// - Cutscene Only Objects: 컷씬 동안에만 켜 두고, 끝나면(Finish) 끈다(컷씬용 보스·카메라 등).
/// - 위아래 검은 띠(레터박스), Space 길게 눌러 건너뛰기.
/// 만드는 방법은 CUTSCENE_GUIDE.md 참고. 메뉴 Solve/Demo/Create Boss Intro Cutscene으로 기본 틀을 만들 수 있다.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class TimelineCutscene : MonoBehaviour
{
    [Serializable]
    public class DialogueCue
    {
        [Tooltip("메모(아무 내용이나)")]
        public string memo;
        [Tooltip("Timeline 시간(초). 이 시간에 Timeline이 멈추고 대사가 나온다")]
        [Min(0f)] public float time;
        public List<StoryDialogueLine> lines = new List<StoryDialogueLine>();
        [NonSerialized] public bool fired;
    }

    [Serializable]
    public class PathMove
    {
        public string memo;
        [Tooltip("움직일 오브젝트(예: WalkingRival)")]
        public Transform target;
        [Tooltip("지나갈 점들(순서대로). 씬에서 빈 오브젝트를 옮겨서 경로를 정한다")]
        public Transform[] points = new Transform[0];
        [Tooltip("이동 시작 시간(초, Timeline 기준)")]
        [Min(0f)] public float startTime;
        [Min(0.01f)] public float duration = 3f;
        public Ease ease = Ease.Linear;
        [Tooltip("가는 방향을 바라보게 회전")]
        public bool faceMoveDirection = true;
        [Tooltip("도착한 뒤 바라볼 방향(비우면 마지막 진행 방향 유지)")]
        public Transform lookAtWhenArrived;

        // Timeline 애니메이션이 매 프레임 루트 회전을 덮어쓰므로 회전은 따로 기억해 둔다.
        [NonSerialized] public Quaternion rotation;
        [NonSerialized] public bool hasRotation;
    }

    [SerializeField] private PlayableDirector director;

    [Header("Content")]
    [Tooltip("컷씬 동안에만 켜는 오브젝트(컷씬용 보스, 컷씬 카메라 묶음 등). 끝나면 꺼진다")]
    [SerializeField] private List<GameObject> cutsceneOnlyObjects = new List<GameObject>();
    [SerializeField] private List<DialogueCue> dialogueCues = new List<DialogueCue>();
    [SerializeField] private List<PathMove> moves = new List<PathMove>();

    [Header("Presentation")]
    [SerializeField] private bool letterbox = true;
    [Tooltip("위아래 띠 높이(화면 높이 비율)")]
    [SerializeField, Range(0f, 0.25f)] private float letterboxHeight = 0.11f;
    [SerializeField, Min(0f)] private float letterboxDuration = 0.45f;

    [Header("Skip")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField, Min(0.1f)] private float skipHoldSeconds = 0.8f;
    [SerializeField] private string skipText = "Space 길게 눌러 건너뛰기";

    private Canvas overlay;
    private RectTransform topBar, bottomBar;
    private TMPro.TextMeshProUGUI skipLabel;
    private Coroutine routine;
    private bool running;
    private bool finished = true;
    private float skipHold;

    public bool HasContent => director != null && director.playableAsset != null;
    public bool IsPlaying => running;

    private void Reset() => director = GetComponent<PlayableDirector>();

    private void Awake()
    {
        if (director == null) director = GetComponent<PlayableDirector>();
        if (director != null)
        {
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.Hold;
        }
        SetObjectsActive(false);
    }

    // ================= Play =================

    /// <summary>컷씬을 재생한다. 끝나면 마지막 장면에서 멈춘 채 onComplete를 부른다(정리는 Finish).</summary>
    public void Play(Action onComplete)
    {
        if (!HasContent)
        {
            onComplete?.Invoke();
            return;
        }
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine(onComplete));
    }

    private IEnumerator PlayRoutine(Action onComplete)
    {
        running = true;
        finished = false;
        skipHold = 0f;
        foreach (DialogueCue cue in dialogueCues) if (cue != null) cue.fired = false;
        foreach (PathMove move in moves) if (move != null) move.hasRotation = false;

        SetObjectsActive(true);
        if (letterbox) ShowLetterbox(true);

        director.time = 0;
        director.Play();
        SetSpeed(1);

        double duration = director.duration;
        bool skipped = false;

        while (director.time < duration - 0.0001)
        {
            // 대사 큐
            DialogueCue cue = NextCue(director.time);
            if (cue != null)
            {
                cue.fired = true;
                SetSpeed(0);
                bool done = false;
                StoryDialogueView.GetOrCreate().Play(cue.lines, () => done = true);
                while (!done && !skipped)
                {
                    skipped = UpdateSkip();
                    yield return null;
                }
                if (skipped) StoryDialogueView.GetOrCreate().Stop(false);
                SetSpeed(1);
            }

            if (skipped || UpdateSkip())
            {
                skipped = true;
                break;
            }
            yield return null;
        }

        // 마지막 장면으로(건너뛰기 포함)
        director.time = duration;
        director.Evaluate();
        SetSpeed(0);
        ApplyMoves(duration);
        if (skipLabel != null) skipLabel.gameObject.SetActive(false);

        running = false;
        routine = null;
        onComplete?.Invoke();
    }

    /// <summary>컷씬 정리: 컷씬 전용 오브젝트를 끄고 레터박스를 걷는다(카메라는 게임 카메라로 돌아간다).</summary>
    public void Finish()
    {
        if (finished) return;
        finished = true;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        running = false;

        if (director != null && director.playableGraph.IsValid()) director.Stop();
        SetObjectsActive(false);
        if (letterbox) ShowLetterbox(false);
        if (skipLabel != null) skipLabel.gameObject.SetActive(false);
    }

    private DialogueCue NextCue(double time)
    {
        foreach (DialogueCue cue in dialogueCues)
            if (cue != null && !cue.fired && time >= cue.time && cue.lines != null && cue.lines.Count > 0)
                return cue;
        return null;
    }

    private void SetSpeed(double speed)
    {
        if (director != null && director.playableGraph.IsValid())
            director.playableGraph.GetRootPlayable(0).SetSpeed(speed);
    }

    private void SetObjectsActive(bool active)
    {
        foreach (GameObject go in cutsceneOnlyObjects)
            if (go != null) go.SetActive(active);
    }

    // ================= Skip =================

    private bool UpdateSkip()
    {
        if (!allowSkip || Time.timeScale == 0f) return false;

        if (skipLabel == null) EnsureOverlay();
        Keyboard keyboard = Keyboard.current;
        bool holding = keyboard != null && keyboard.spaceKey.isPressed;
        skipHold = holding ? skipHold + Time.unscaledDeltaTime : 0f;

        if (skipLabel != null)
        {
            skipLabel.gameObject.SetActive(true);
            float k = Mathf.Clamp01(skipHold / skipHoldSeconds);
            skipLabel.alpha = Mathf.Lerp(0.45f, 1f, k);
            skipLabel.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, k);
        }
        return skipHold >= skipHoldSeconds;
    }

    // ================= Moves =================

    private void LateUpdate()
    {
        // 컷씬이 끝나 마지막 장면에서 멈춰 있는 동안(Finish 전)에도 위치를 유지한다.
        if (finished || director == null) return;
        ApplyMoves(director.time);
    }

    private void ApplyMoves(double time)
    {
        foreach (PathMove move in moves)
        {
            if (move == null || move.target == null || move.points == null || move.points.Length == 0) continue;
            float t = Mathf.Clamp01((float)((time - move.startTime) / move.duration));
            float e = DOVirtual.EasedValue(0f, 1f, t, move.ease);

            Vector3 position = Evaluate(move.points, e, out Vector3 direction);
            move.target.position = position;
            if (!move.hasRotation)
            {
                move.rotation = move.target.rotation;
                move.hasRotation = true;
            }

            if (t >= 1f && move.lookAtWhenArrived != null)
            {
                Vector3 look = move.lookAtWhenArrived.position - position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                    move.rotation = Quaternion.Slerp(move.rotation, Quaternion.LookRotation(look), Time.deltaTime * 6f);
            }
            else if (move.faceMoveDirection && t > 0f && t < 1f)
            {
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                    move.rotation = Quaternion.Slerp(move.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
            }
            move.target.rotation = move.rotation;
        }
    }

    /// <summary>점들을 잇는 꺾은선 위의 위치(0~1, 거리 기준).</summary>
    private static Vector3 Evaluate(Transform[] points, float t, out Vector3 direction)
    {
        direction = Vector3.zero;
        var list = new List<Vector3>();
        foreach (Transform p in points) if (p != null) list.Add(p.position);
        if (list.Count == 0) return Vector3.zero;
        if (list.Count == 1) return list[0];

        float total = 0f;
        for (int i = 1; i < list.Count; i++) total += Vector3.Distance(list[i - 1], list[i]);
        float target = total * t;
        for (int i = 1; i < list.Count; i++)
        {
            float length = Vector3.Distance(list[i - 1], list[i]);
            direction = list[i] - list[i - 1];
            if (target <= length || i == list.Count - 1)
                return Vector3.Lerp(list[i - 1], list[i], length > 0f ? Mathf.Clamp01(target / length) : 1f);
            target -= length;
        }
        return list[list.Count - 1];
    }

    // ================= Letterbox =================

    private void EnsureOverlay()
    {
        if (overlay != null) return;
        overlay = DemoUI.CreateOverlayCanvas("CutsceneOverlay", 600, transform);
        overlay.GetComponent<GraphicRaycaster>().enabled = false;

        topBar = Bar("Top", new Vector2(0f, 1f));
        bottomBar = Bar("Bottom", new Vector2(0f, 0f));

        skipLabel = DemoUI.CreateText("Skip", overlay.transform, skipText, 26f, TMPro.TextAlignmentOptions.Right);
        var rect = skipLabel.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(600f, 44f);
        rect.anchoredPosition = new Vector2(-40f, 24f);
        skipLabel.color = Color.white;
        skipLabel.gameObject.SetActive(false);
    }

    private RectTransform Bar(string name, Vector2 pivot)
    {
        var image = DemoUI.CreatePanel(name, overlay.transform, Color.black);
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, pivot.y);
        rect.anchorMax = new Vector2(1f, pivot.y);
        rect.pivot = new Vector2(0.5f, pivot.y);
        rect.sizeDelta = new Vector2(0f, 0f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private void ShowLetterbox(bool show)
    {
        EnsureOverlay();
        float height = ((RectTransform)overlay.transform).rect.height;
        if (height <= 0f) height = 1080f;
        float target = show ? height * letterboxHeight : 0f;
        foreach (RectTransform bar in new[] { topBar, bottomBar })
        {
            RectTransform b = bar;
            b.DOKill();
            DOTween.To(() => b.sizeDelta.y, v => b.sizeDelta = new Vector2(0f, v), target, letterboxDuration)
                .SetEase(Ease.OutCubic).SetTarget(b).SetLink(b.gameObject);
        }
    }

    // ================= Editor =================

#if UNITY_EDITOR
    /// <summary>에디터 메뉴(기본 틀 만들기)에서 쓰는 설정 함수.</summary>
    public void EditorSetup(PlayableDirector playableDirector, List<GameObject> onlyObjects, List<PathMove> pathMoves, List<DialogueCue> cues)
    {
        director = playableDirector;
        cutsceneOnlyObjects = onlyObjects;
        moves = pathMoves;
        dialogueCues = cues;
    }
#endif

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.9f, 1f);
        foreach (PathMove move in moves)
        {
            if (move == null || move.points == null) continue;
            Transform previous = null;
            foreach (Transform p in move.points)
            {
                if (p == null) continue;
                Gizmos.DrawSphere(p.position, 0.3f);
                if (previous != null) Gizmos.DrawLine(previous.position, p.position);
                previous = p;
            }
        }
    }
}
