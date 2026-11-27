using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 일반전 진행표: 페이즈 변경에 맞춰 좌석(키 배치)을 바꾼다. 한 판에 최대 maxSeatChanges회(기본 2회).
///
/// 전환 구간(기본 2마디, 4/4 기준 8박) 타임라인:
///   첫 마디 1~4박   : PHASE CHANGE 경고 + 좌석 이동. 입력·판정·득점 중지
///   둘째 마디 1박   : 경고 유지, 이동 마무리
///   둘째 마디 2~4박 : 3, 2, 1 (숫자당 1박)
///   다음 마디 1박   : 경고를 닫고 새 키 배치로 입력·판정·득점 재개
/// 전환 구간은 판정 게이트(RhythmWheelJudgement.BlockUntilBeat)로 막으므로
/// 자동 Miss, 마디 실패, 분위기·배율·피버 감소가 발생하지 않고 피버 충전도 되지 않는다.
/// 피버 중에는 전환을 미루고 피버가 끝난 뒤 첫 마디에서 시작한다.
/// </summary>
public class NormalBattleDirector : MonoBehaviour
{
    public enum TriggerMode
    {
        [Tooltip("곡의 BPM 변화로 속성 페이즈가 바뀌면(FmodMusicClock 자동 페이즈) 다음 마디에 전환")]
        OnPhaseChange,
        [Tooltip("아래 Scheduled Changes에 지정한 마디에서 페이즈와 좌석을 함께 전환")]
        ScheduledBars,
    }

    [Serializable]
    public class ScheduledChange
    {
        [Min(1)] public int bar = 16;
        public Phase phase = Phase.HipHop;
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
    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnPhaseChange;
    [SerializeField, Min(0)] private int maxSeatChanges = 2;
    [Tooltip("전환 구간 길이(마디). 마지막 3박에 카운트다운이 들어간다.")]
    [SerializeField, Min(1)] private int transitionBars = 2;
    [Tooltip("곡 시작 직후 이 마디 전까지의 페이즈 변경은 좌석 변경으로 세지 않는다(초기 페이즈 보정).")]
    [SerializeField, Min(0)] private int ignorePhaseChangesBeforeBar = 2;
    [SerializeField] private List<ScheduledChange> scheduledChanges = new List<ScheduledChange>
    {
        new ScheduledChange { bar = 16, phase = Phase.HipHop },
        new ScheduledChange { bar = 32, phase = Phase.Techno },
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
    private int transitionEndBeat = -1;
    private bool running = true;

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
        banner?.Hide();
        countdown?.Hide();
        transitionStartBeat = transitionEndBeat = -1;
    }

    private void HandlePhaseChanged(Phase previous, Phase next)
    {
        if (!running || triggerMode != TriggerMode.OnPhaseChange) return;
        if (next == Phase.Villain) return;
        if (changesDone >= maxSeatChanges) return;
        if (wheel != null && wheel.CurrentBeat < ignorePhaseChangesBeforeBar * 4) return;

        pending = true;
    }

    private void HandleBeat(int beat)
    {
        if (!running) return;

        if (IsTransitioning)
        {
            UpdateTransition(beat);
            return;
        }

        if (beat % 4 != 0) return;

        if (triggerMode == TriggerMode.ScheduledBars && changesDone < maxSeatChanges)
        {
            int bar = beat / 4;
            foreach (ScheduledChange change in scheduledChanges)
            {
                if (change != null && change.bar == bar)
                {
                    pending = true;
                    pendingPhase = change.phase;
                    hasPendingPhase = true;
                    break;
                }
            }
        }

        if (!pending) return;

        // 피버와 겹치면 전환을 미룬다.
        RhythmWheelFever fever = wheel != null ? wheel.Fever : null;
        if (fever != null && (fever.IsActive || fever.IsJudgementLocked)) return;

        BeginTransition(beat);
    }

    private void BeginTransition(int startBeat)
    {
        pending = false;
        changesDone++;
        transitionStartBeat = startBeat;
        transitionEndBeat = startBeat + transitionBars * 4;

        wheel?.BlockJudgementUntilBeat(transitionEndBeat);
        combo?.ResetCurrentPhrase();

        if (hasPendingPhase)
        {
            phaseController?.SetPhase(pendingPhase);
            hasPendingPhase = false;
        }

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
