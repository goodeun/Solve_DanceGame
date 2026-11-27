using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 보스(라이벌)전 진행.
/// 등장: 씬 전환 → 라이벌 위쪽 플랫폼 등장 → 등장 연출(카메라·대사) → 기본 카메라 → 보스전 안내 대사 → UI 오픈 → 카운트다운 → 시작
/// 전투: 4키 유지(6키 폐기). 속성 페이즈는 Villain으로 고정. QEAD 사각형과 WASD 마름모를 약 4~5회 교체한다.
///       변경할 마디 첫 박에 CHANGE NEXT로 예고하고 1·2·3박은 기존 배치로 판정, 쉬는 박(4박)에 객석이 움직이며
///       새 배치로 바뀐다. 플레이어는 쉬는 박에 바뀐 객석을 보고 다음 마디 1박부터 새 배치로 친다.
///       보스전은 입력을 멈추지 않는다.
/// 승패: 기존 객석 점수(StageVisual.PlayerPoints, 객석 색과 같은 값)로 판정한다.
///       객석 색 기준선으로 플레이어 객석(파랑, playerPointThreshold 이상)과 라이벌 객석(빨강, rivalPointThreshold 미만)을 센다.
///       더 많은 객석을 차지한 쪽이 승리. 객석 수 동률이면 총 분위기(전체 객석 점수 합이 최대치의 절반을 넘는지),
///       완전 동률이면 플레이어 승리. 중립(보라) 객석은 어느 쪽에도 세지 않는다.
/// </summary>
public class BossBattleDirector : MonoBehaviour
{
    [Header("References (비우면 자동 탐색)")]
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private RhythmWheelUI wheel;
    [SerializeField] private StageController stageController;
    [SerializeField] private PhaseController phaseController;
    [SerializeField] private CountDownController countdown;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ComboAndMagnification combo;
    [SerializeField] private NGBubbleManager bubbles;
    [SerializeField] private BannerView banner;

    [Header("Intro")]
    [Tooltip("위쪽 플랫폼의 라이벌. 지정하면 위에서 내려오며 등장한다.")]
    [SerializeField] private Transform rival;
    [SerializeField] private float rivalDropHeight = 6f;
    [SerializeField, Min(0f)] private float rivalDropDuration = 1.2f;
    [Tooltip("카메라 무빙 + 대사 등장 연출(없으면 introDialogue만 재생)")]
    [SerializeField] private CinematicDialoguePlayer introCinematic;
    [SerializeField] private StoryDialogueSequence introDialogue;
    [Tooltip("보스전 규칙 안내(튜토리얼) 대사")]
    [SerializeField] private StoryDialogueSequence ruleDialogue;
    [Tooltip("라이벌전 종료 직후 대사(승패 공개 전). 승패 대사는 결과 씬에서 나온다.")]
    [SerializeField] private StoryDialogueSequence finishDialogue;
    [SerializeField] private bool useDefaultLines = true;

    [Header("Battle")]
    [Tooltip("제한 시간(마디). 곡이 먼저 끝나면 그 시점에 종료한다.")]
    [SerializeField, Min(4)] private int battleBars = 32;
    [Tooltip("배치 변경 마디(비우면 아래 범위로 무작위 생성)")]
    [SerializeField] private List<int> changeBars = new List<int>();
    [SerializeField] private Vector2Int randomChangeCount = new Vector2Int(3, 3);
    [SerializeField, Min(2)] private int minBarsBetweenChanges = 6;
    [Tooltip("쉬는 박 시작 후 키 배치를 바꾸기까지의 지연(박). 3박째 늦은 입력이 기존 키로 판정되도록 반 박 늦춘다.")]
    [SerializeField, Range(0f, 0.9f)] private float keySwapDelayBeats = 0.5f;
    [SerializeField] private string changeNextText = "CHANGE NEXT";

    [Header("ABC Command")]
    [Tooltip("보스전 ABC 커맨드 성공 시 마지막 키 객석에 더하는 객석 점수(라이벌 분위기를 밀어냄)")]
    [SerializeField, Min(0)] private int abcSeatPoints = 40;

    [Header("Flow")]
    [SerializeField] private bool hideHudBeforeStart = true;
    [Tooltip("비우면 DemoUISettings의 결과 씬")]
    [SerializeField] private string resultSceneOverride;

    [Header("Events")]
    public UnityEvent onBattleStarted = new UnityEvent();
    public UnityEvent onPlayerWon = new UnityEvent();
    [Tooltip("패배 시. HP/게임오버 담당 기능과 연결할 지점")]
    public UnityEvent onPlayerLost = new UnityEvent();
    [Tooltip("true면 패배 시 결과 씬으로 자동 이동하지 않는다(게임오버 흐름이 처리)")]
    [SerializeField] private bool defeatHandledExternally = false;

    private readonly List<int> schedule = new List<int>();
    private int nextChangeIndex;
    private int announcedBar = -1;
    private bool battling;
    private bool finished;

    public bool IsBattling => battling;
    public int RemainingBars => wheel != null ? Mathf.Max(0, battleBars - Mathf.Max(0, wheel.CurrentBeat) / 4) : battleBars;

    private void Awake()
    {
        if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>();
        if (wheel == null) wheel = FindFirstObjectByType<RhythmWheelUI>();
        if (stageController == null) stageController = FindFirstObjectByType<StageController>();
        if (phaseController == null) phaseController = FindFirstObjectByType<PhaseController>();
        if (countdown == null) countdown = FindFirstObjectByType<CountDownController>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (combo == null) combo = FindFirstObjectByType<ComboAndMagnification>();
        if (bubbles == null) bubbles = FindFirstObjectByType<NGBubbleManager>();
        if (banner == null) banner = GetComponent<BannerView>();
        if (banner == null) banner = gameObject.AddComponent<BannerView>();
        if (countdown == null) countdown = gameObject.AddComponent<CountDownController>();

        musicClock?.DisableAutoStart();
        musicClock?.SetAutoPhase(false);
        wheel?.SetJudgementSuspended(true);


        if (useDefaultLines) FillDefaultLines();
        BuildSchedule();
    }

    private void FillDefaultLines()
    {
        StoryDialogueSequence Ensure(StoryDialogueSequence seq) => seq != null ? seq : gameObject.AddComponent<StoryDialogueSequence>();

        introDialogue = Ensure(introDialogue);
        introDialogue.FillIfEmpty(StoryDialogueLine.List(
            StoryDialogueLine.Npc("라이벌", "신입 주제에 내 무대에서 꽤 놀았더군.", DialogueStyle.Angry),
            StoryDialogueLine.Npc("라이벌", "이 클럽의 관객은 전부 내 거야. 한 명도 못 넘겨줘.", DialogueStyle.Emphasis),
            StoryDialogueLine.Player("그럼 춤으로 정하자!", PlayerStanding.Angry)));

        ruleDialogue = Ensure(ruleDialogue);
        ruleDialogue.FillIfEmpty(StoryDialogueLine.List(
            StoryDialogueLine.Npc("클럽 매니저", "라이벌전은 제한 시간 동안 객석을 차지하는 승부야. 끝날 때 더 많은 객석을 달군 쪽이 이겨."),
            StoryDialogueLine.Npc("클럽 매니저", "태클 말풍선을 방치하면 그 객석은 라이벌 쪽으로 넘어가. AAB로 바로바로 물리쳐!"),
            StoryDialogueLine.Npc("클럽 매니저", "객석마다 너와 라이벌의 분위기가 줄다리기 중이야. 끝날 때 네 쪽으로 더 기운 객석이 많으면 승리! 마름모 배치에선 ABC 커맨드로 객석을 확 끌어올 수 있어."),
            StoryDialogueLine.Npc("클럽 매니저", "'CHANGE NEXT'가 뜨면 그 마디 쉬는 박에 배치가 바뀌니, 다음 마디부터 새 배치로 춰!")));

        finishDialogue = Ensure(finishDialogue);
        finishDialogue.FillIfEmpty(StoryDialogueLine.List(
            StoryDialogueLine.Npc("클럽 매니저", "그만! 승부는 여기까지. 객석을 누가 더 차지했는지 확인해 보자!", DialogueStyle.Emphasis)));
    }

    private void BuildSchedule()
    {
        schedule.Clear();
        if (changeBars != null && changeBars.Count > 0)
        {
            schedule.AddRange(changeBars);
        }
        else
        {
            int count = UnityEngine.Random.Range(randomChangeCount.x, randomChangeCount.y + 1);
            int first = 3;
            int last = battleBars - 2;
            int span = Mathf.Max(1, last - first);
            for (int i = 0; i < count; i++)
            {
                // 균등 분할 후 구간 안에서 흔들어 주기적이지 않게 한다.
                float slot = span / (float)count;
                int bar = first + Mathf.RoundToInt(slot * i + UnityEngine.Random.Range(0f, slot * 0.5f));
                if (schedule.Count > 0) bar = Mathf.Max(bar, schedule[schedule.Count - 1] + minBarsBetweenChanges);
                if (bar >= last) break;
                schedule.Add(bar);
            }
        }

        schedule.Sort();
        nextChangeIndex = 0;
    }

    private void OnEnable()
    {
        if (wheel != null) wheel.OnBeatStarted += HandleBeat;
        if (musicClock != null) musicClock.MusicEnded += HandleMusicEnded;
        if (combo != null) combo.OnPhraseHandoff += HandlePhrase;
    }

    private void OnDisable()
    {
        if (wheel != null) wheel.OnBeatStarted -= HandleBeat;
        if (musicClock != null) musicClock.MusicEnded -= HandleMusicEnded;
        if (combo != null) combo.OnPhraseHandoff -= HandlePhrase;
    }

    private IEnumerator Start()
    {
        DemoUI.EnsureEventSystem();
        if (hideHudBeforeStart) HUDManager.Instance?.SetFlowHidden(true);
        phaseController?.SetPhase(Phase.Villain);

        yield return null;

        // 라이벌 위쪽 플랫폼 등장
        if (rival != null && rivalDropDuration > 0f)
        {
            Vector3 target = rival.position;
            rival.position = target + Vector3.up * rivalDropHeight;
            yield return rival.DOMove(target, rivalDropDuration).SetEase(Ease.OutBounce).WaitForCompletion();
        }

        bool done = false;
        if (introCinematic != null && introCinematic.HasShots)
        {
            introCinematic.Play(() => done = true);
            while (!done) yield return null;
        }
        else if (introDialogue != null && introDialogue.HasLines)
        {
            introDialogue.Play(() => done = true);
            while (!done) yield return null;
        }

        done = false;
        if (ruleDialogue != null && ruleDialogue.HasLines)
        {
            ruleDialogue.Play(() => done = true);
            while (!done) yield return null;
        }

        // UI 오픈
        HUDManager.Instance?.SetFlowHidden(false);

        done = false;
        countdown.StartCountdown(() => done = true, true);
        while (!done) yield return null;

        battling = true;
        wheel?.ResumeJudgementAtBeat(0);
        musicClock?.StartMusic();
        onBattleStarted?.Invoke();
    }

    private void HandleBeat(int beat)
    {
        if (!battling) return;

        int bar = beat / 4;

        // 예고한 마디의 쉬는 박: 객석 이동 + 키 배치 변경(다음 마디 1박부터 새 배치)
        if (beat % 4 == 3)
        {
            if (announcedBar == bar)
            {
                announcedBar = -1;
                banner?.Hide();
                if (stageController != null)
                {
                    Stage next = stageController.currentPhase == Stage.Square ? Stage.Rhombus : Stage.Square;
                    float delay = wheel != null ? wheel.BeatDurationSeconds * keySwapDelayBeats : 0f;
                    stageController.ChangePhase(next, delay);
                }
            }
            return;
        }

        if (beat % 4 != 0) return;

        if (bar >= battleBars)
        {
            EndBattle();
            return;
        }

        // 이번 마디에 변경 예정이면 첫 박에 예고하고 이 마디의 쉬는 박에 바꾼다. 피버와 겹치면 뒤로 미룬다.
        if (announcedBar < 0 && nextChangeIndex < schedule.Count && schedule[nextChangeIndex] <= bar + 1)
        {
            RhythmWheelFever fever = wheel != null ? wheel.Fever : null;
            bool feverBusy = fever != null && (fever.IsActive || fever.IsQueued || fever.IsJudgementLocked);

            if (feverBusy || bar + 1 >= battleBars)
            {
                schedule[nextChangeIndex] = bar + 2;
            }
            else
            {
                announcedBar = bar;
                nextChangeIndex++;
                banner?.Show(changeNextText, DemoUI.Settings.cyan, DemoUI.Settings.changeNextSprite);
            }
        }
    }

    private void HandlePhrase(Keys lastKey, int just, int good, int bad, int miss, KeyCommand command)
    {
        // 객석 점수 상승은 기존 방식(점수 → PhaseController → StageVisual) 그대로 둔다.
        // ABC만 보스전 효과로 마지막 키 객석의 점수를 더 올려 라이벌 쪽으로 기운 객석을 되찾는다.
        if (!battling || command != KeyCommand.ABC || miss > 0) return;

        StageVisual seat = stageController != null ? stageController.GetSeatForKey(lastKey) : null;
        seat?.AddPlayerPoints(abcSeatPoints);
    }

    private void HandleMusicEnded()
    {
        if (battling) EndBattle();
    }

    /// <summary>결과 계산(기존 객석 점수): 객석 수 → 총 분위기 → 완전 동률은 플레이어 승리.</summary>
    public bool EvaluateWinner(out int playerSeats, out int rivalSeats, out float playerTotal, out float rivalTotal)
    {
        playerSeats = rivalSeats = 0;
        playerTotal = rivalTotal = 0f;

        int count = stageController != null ? stageController.SeatCount : 0;
        for (int i = 0; i < count; i++)
        {
            StageVisual seat = stageController.GetSeat(i);
            if (seat == null) continue;

            int points = seat.PlayerPoints;
            playerTotal += points;
            rivalTotal += Mathf.Max(0, seat.MaxPlayerPoints - points);

            // 땅따먹기: 객석 점수(플레이어 몫)와 나머지(라이벌 몫)를 비교해 더 큰 쪽이 객석을 차지한다.
            int rivalPoints = Mathf.Max(0, seat.MaxPlayerPoints - points);
            if (points > rivalPoints) playerSeats++;
            else if (rivalPoints > points) rivalSeats++;
        }

        if (playerSeats != rivalSeats) return playerSeats > rivalSeats;
        if (!Mathf.Approximately(playerTotal, rivalTotal)) return playerTotal > rivalTotal;
        return true;
    }

    private void EndBattle()
    {
        if (finished) return;
        finished = true;
        battling = false;

        wheel?.SetJudgementSuspended(true);
        bubbles?.ClearBubbles();
        banner?.Hide();
        if (musicClock != null && !musicClock.IsEnded) musicClock.StopMusic();

        bool won = EvaluateWinner(out int playerSeats, out int rivalSeats, out float playerTotal, out float rivalTotal);

        DemoSession.BossPlayed = true;
        DemoSession.BossWon = won;
        DemoSession.PlayerSeats = playerSeats;
        DemoSession.RivalSeats = rivalSeats;
        DemoSession.NeutralSeats = Mathf.Max(0, (stageController != null ? stageController.SeatCount : 0) - playerSeats - rivalSeats);
        DemoSession.PlayerMoodTotal = Mathf.RoundToInt(playerTotal);
        DemoSession.RivalMoodTotal = Mathf.RoundToInt(rivalTotal);
        DemoSession.BossScore = scoreManager != null ? scoreManager.TotalScore : 0;

        // 결과 씬의 객석 공개 연출용 기록
        DemoSession.Seats.Clear();
        int seatCount = stageController != null ? stageController.SeatCount : 0;
        for (int i = 0; i < seatCount; i++)
        {
            StageVisual seat = stageController.GetSeat(i);
            if (seat == null) continue;
            DemoSession.Seats.Add(new DemoSession.SeatResult
            {
                key = KeyBindings.DisplayName(stageController.GetKeyForSeat(i)),
                playerPoints = seat.PlayerPoints,
                maxPoints = Mathf.Max(1, seat.MaxPlayerPoints),
            });
        }
        DemoSession.AccumulateBattle(scoreManager, combo);

        StartCoroutine(EndRoutine(won));
    }

    private IEnumerator EndRoutine(bool won)
    {
        yield return new WaitForSeconds(1f);

        // 승패는 결과 씬에서 객석을 하나씩 공개하며 보여주므로 여기서는 종료만 알린다.
        banner?.Show("FINISH!", DemoUI.Settings.green);
        yield return new WaitForSeconds(2f);
        banner?.Hide();

        bool done = false;
        if (finishDialogue != null && finishDialogue.HasLines)
        {
            finishDialogue.Play(() => done = true);
            while (!done) yield return null;
        }

        if (won) onPlayerWon?.Invoke();
        else onPlayerLost?.Invoke();

        if (!won && defeatHandledExternally) yield break;

        SceneFlow.Load(string.IsNullOrEmpty(resultSceneOverride) ? DemoUI.Settings.resultScene : resultSceneOverride);
    }
}
