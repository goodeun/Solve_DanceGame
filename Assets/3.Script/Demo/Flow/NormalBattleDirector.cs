using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 일반전 진행표: 페이즈 변경에 맞춰 좌석(키 배치)을 바꾼다. 한 판에 최대 maxSeatChanges회(기본 2회).
///
/// 기본(ScheduledBars): 새 페이즈가 시작되는 마디(곡의 BPM이 바뀌는 마디)를 지정하면,
/// 그 직전 transitionBars 마디를 전환 구간으로 쓴다. 공지가 먼저 나오고, 색·BPM은 전환이 끝나는 박에 함께 바뀐다.
///   첫 마디 1~4박   : PHASE CHANGE 경고 + 좌석 이동. 입력·판정·득점 중지, 리듬 휠 펄스 숨김
///   둘째 마디 1박   : 경고 유지, 이동 마무리
///   둘째 마디 2~4박 : 3, 2, 1 (숫자당 1박)
///   새 페이즈 첫 박 : 경고를 닫고 페이즈 색 적용(곡의 BPM 변경과 같은 박), 새 키 배치로 재개
/// 전환 구간은 곡 시작 시 미리 판정 제외로 등록하므로 자동 Miss, 마디 실패, HP·분위기·배율·피버 감소가 없고,
/// 피버도 전환 구간과 겹치면 시작하지 않는다.
/// OnPhaseChange 모드는 BPM이 바뀐 뒤 다음 마디에 전환한다(공지가 색·BPM 변경보다 늦음).
/// </summary>
public class NormalBattleDirector : MonoBehaviour
{
    public enum TriggerMode
    {
        [Tooltip("곡의 BPM 변화로 속성 페이즈가 바뀌면(FmodMusicClock 자동 페이즈) 다음 마디에 전환")]
        OnPhaseChange,
        [Tooltip("Scheduled Changes의 마디에 새 페이즈가 시작되도록, 그 앞 마디들에서 미리 공지·좌석 이동")]
        ScheduledBars,
    }

    [Serializable]
    public class ScheduledChange
    {
        [Tooltip("새 페이즈(색·BPM)가 시작되는 마디(0부터). 곡의 템포 마커 위치와 맞춘다.")]
        [Min(1)] public int bar = 44;
        public Phase phase = Phase.Pop;
    }

    [Header("References (비우면 자동 탐색)")]
    [SerializeField] private RhythmWheelUI wheel;
    [SerializeField] private StageController stageController;
    [SerializeField] private PhaseController phaseController;
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private CountDownController countdown;
    [SerializeField] private ComboAndMagnification combo;
    [SerializeField] private BannerView banner;

    [Header("Rules")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.ScheduledBars;
    [Tooltip("ScheduledBars 모드에서 곡 시작 시 페이즈")]
    [SerializeField] private Phase initialPhase = Phase.Techno;
    [SerializeField, Min(0)] private int maxSeatChanges = 2;
    [Tooltip("전환 구간 길이(마디). 마지막 3박에 카운트다운이 들어간다.")]
    [SerializeField, Min(1)] private int transitionBars = 2;
    [Tooltip("곡 시작 직후 이 마디 전까지의 페이즈 변경은 좌석 변경으로 세지 않는다(초기 페이즈 보정).")]
    [SerializeField, Min(0)] private int ignorePhaseChangesBeforeBar = 2;
    [Tooltip("현재 곡(RetroVision - Puzzle): 0초 128BPM(테크노) → 82.5초 = 44마디째 90BPM(팝)")]
    [SerializeField] private List<ScheduledChange> scheduledChanges = new List<ScheduledChange>
    {
        new ScheduledChange { bar = 44, phase = Phase.Pop },
    };

    [Header("Banner")]
    [SerializeField] private string bannerText = "PHASE CHANGE";

    [Header("Events")]
    public UnityEvent onTransitionStarted = new UnityEvent();
    public UnityEvent onTransitionFinished = new UnityEvent();

    private int changesDone;
    private bool pending;
    private Phase pendingPhase;
    private bool hasPendingPhase;
    private int transitionStartBeat = -1;
    private int scheduledStartBeat = -1;
    private int transitionEndBeat = -1;
    private bool running = true;
    private readonly List<ScheduledChange> booked = new List<ScheduledChange>();

    public bool IsTransitioning => transitionStartBeat >= 0;
    public int ChangesDone => changesDone;

    private void Awake()
    {
        if (wheel == null) wheel = FindFirstObjectByType<RhythmWheelUI>();
        if (stageController == null) stageController = FindFirstObjectByType<StageController>();
        if (phaseController == null) phaseController = FindFirstObjectByType<PhaseController>();
        if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>();
        if (countdown == null) countdown = FindFirstObjectByType<CountDownController>();
        if (combo == null) combo = FindFirstObjectByType<ComboAndMagnification>();
        if (banner == null) banner = GetComponent<BannerView>();
        if (banner == null) banner = gameObject.AddComponent<BannerView>();
        if (countdown == null)
            countdown = gameObject.AddComponent<CountDownController>();

        // 일정 모드에서는 이 컴포넌트가 페이즈를 결정하므로 BPM 자동 전환을 끈다.
        if (triggerMode == TriggerMode.ScheduledBars && musicClock != null)
            musicClock.SetAutoPhase(false);
    }

    private void Start()
    {
        if (triggerMode != TriggerMode.ScheduledBars) return;

        phaseController?.SetPhase(initialPhase);

        // 전환 구간을 곡 시작 전에 모두 판정 제외로 등록한다. 피버가 전환과 겹치지 않게 하는 용도이기도 하다.
        foreach (ScheduledChange change in scheduledChanges)
        {
            if (change == null || booked.Count >= maxSeatChanges) continue;
            int start = Mathf.Max(0, change.bar - transitionBars) * 4;
            wheel?.BlockJudgementRange(start, change.bar * 4);
            booked.Add(change);
        }
    }

    private void OnEnable()
    {
        if (wheel != null) wheel.OnBeatStarted += HandleBeat;
        if (phaseController != null) phaseController.PhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (wheel != null) wheel.OnBeatStarted -= HandleBeat;
        if (phaseController != null) phaseController.PhaseChanged -= HandlePhaseChanged;
    }

    /// <summary>곡 종료 등으로 더 이상 전환하지 않는다. 진행 중인 표시는 정리한다.</summary>
    public void StopDirecting()
    {
        running = false;
        pending = false;
        scheduledStartBeat = -1;
        banner?.Hide();
        countdown?.Hide();
        wheel?.SetPulseSuppressed(false);
        transitionStartBeat = transitionEndBeat = -1;
    }

    private void HandlePhaseChanged(Phase previous, Phase next)
    {
        if (!running || triggerMode != TriggerMode.OnPhaseChange) return;
        if (next == Phase.Villain) return;
        if (changesDone + (scheduledStartBeat >= 0 ? 1 : 0) >= maxSeatChanges) return;
        if (wheel != null && wheel.CurrentBeat < ignorePhaseChangesBeforeBar * 4) return;

        pending = true;
        if (wheel != null && !IsTransitioning && scheduledStartBeat < 0)
            TrySchedule(wheel.NextBarStartBeat);
    }

    private void HandleBeat(int beat)
    {
        if (!running) return;

        if (IsTransitioning)
        {
            UpdateTransition(beat);
            return;
        }

        if (scheduledStartBeat >= 0)
        {
            if (beat >= scheduledStartBeat) BeginTransition(scheduledStartBeat, scheduledStartBeat + transitionBars * 4);
            return;
        }

        if (beat % 4 != 0) return;

        // 일정 모드: 새 페이즈 시작 transitionBars 마디 전에 전환을 시작한다.
        if (triggerMode == TriggerMode.ScheduledBars)
        {
            foreach (ScheduledChange change in booked)
            {
                if (Mathf.Max(0, change.bar - transitionBars) * 4 == beat)
                {
                    pendingPhase = change.phase;
                    hasPendingPhase = true;
                    BeginTransition(beat, change.bar * 4);
                    return;
                }
            }
            return;
        }

        if (pending) TrySchedule(beat + 4);
    }

    /// <summary>
    /// 전환을 다음 마디 첫 박으로 예약하고 전환 구간을 미리 판정 제외로 등록한다.
    /// 예약을 미리 해 두어야 쉬는 박 뒤쪽에서 다음 마디 첫 박을 일찍 누른 입력도 함께 막힌다.
    /// 피버 중이거나 피버가 대기 중이면 다음 마디로 미룬다.
    /// </summary>
    private void TrySchedule(int startBeat)
    {
        RhythmWheelFever fever = wheel != null ? wheel.GetComponent<RhythmWheelFever>() : null;
        if (fever != null && (fever.IsActive || fever.IsQueued || fever.IsJudgementLocked)) return;

        pending = false;
        scheduledStartBeat = startBeat;
        wheel?.BlockJudgementRange(startBeat, startBeat + transitionBars * 4);
    }

    private void BeginTransition(int startBeat, int endBeat)
    {
        pending = false;
        scheduledStartBeat = -1;
        changesDone++;
        transitionStartBeat = startBeat;
        transitionEndBeat = endBeat;

        combo?.ResetCurrentPhrase();
        wheel?.SetPulseSuppressed(true);

        banner?.Show(bannerText, DemoUI.Settings.magenta, DemoUI.Settings.phaseChangeSprite);

        if (stageController != null)
        {
            Stage next = stageController.currentPhase == Stage.Square ? Stage.Rhombus : Stage.Square;
            stageController.ChangePhase(next);
        }

        onTransitionStarted?.Invoke();
        UpdateTransition(startBeat);
    }

    private void UpdateTransition(int beat)
    {
        int remaining = transitionEndBeat - beat;

        if (remaining <= 0)
        {
            // 공지가 끝난 박에 페이즈 색을 바꾼다(일정 모드에선 곡의 BPM 변경과 같은 박).
            if (hasPendingPhase)
            {
                phaseController?.SetPhase(pendingPhase);
                hasPendingPhase = false;
            }

            wheel?.SetPulseSuppressed(false);
            countdown?.Hide();
            banner?.Hide();
            combo?.ResetCurrentPhrase();
            transitionStartBeat = transitionEndBeat = -1;
            onTransitionFinished?.Invoke();
            return;
        }

        int count = countdown != null ? countdown.Count : 3;
        if (remaining <= count)
            countdown?.ShowTick(remaining);
    }
}
