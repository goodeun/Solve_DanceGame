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
///       변경 직전 마디에 CHANGE NEXT로 예고하고(이 마디는 기존 배치로 계속 판정), 다음 마디 첫 박에 새 배치를 적용한다.
///       보스전은 입력을 멈추지 않는다.
/// 승패: 제한 마디가 끝나면 객석별 플레이어·라이벌 분위기를 비교해 더 많은 객석을 차지한 쪽이 승리.
///       객석 수 동률이면 총 분위기 비교, 완전 동률이면 플레이어 승리. 객석별 분위기가 같으면 어느 쪽 소유도 아니다.
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
    [SerializeField] private StoryDialogueSequence winDialogue;
    [SerializeField] private StoryDialogueSequence loseDialogue;
    [SerializeField] private bool useDefaultLines = true;

    [Header("Battle")]
    [Tooltip("제한 시간(마디). 곡이 먼저 끝나면 그 시점에 종료한다.")]
    [SerializeField, Min(4)] private int battleBars = 32;
    [Tooltip("배치 변경 마디(비우면 아래 범위로 무작위 생성)")]
    [SerializeField] private List<int> changeBars = new List<int>();
    [SerializeField] private Vector2Int randomChangeCount = new Vector2Int(4, 5);
    [SerializeField, Min(2)] private int minBarsBetweenChanges = 4;
    [SerializeField] private string changeNextText = "CHANGE NEXT";

    [Header("Mood (객석별 분위기)")]
    [SerializeField] private float justMood = 12f;
    [SerializeField] private float goodMood = 6f;
    [SerializeField] private float badMood = 2f;
    [SerializeField] private float failedPhraseLoss = 8f;
    [SerializeField] private float emptyPhraseLoss = 3f;
    [Tooltip("스타일 스탯 1당 플레이어 분위기 상승 배수 증가")]
    [SerializeField] private float styleMoodBonus = 0.02f;
    [Tooltip("라이벌이 마디마다 각 객석에서 얻는 분위기(최소~최대)")]
    [SerializeField] private Vector2 rivalMoodPerBar = new Vector2(2f, 6f);
    [Tooltip("태클 말풍선이 떠 있는 객석: 마디마다 라이벌 +, 플레이어 -")]
    [SerializeField] private float bubbleRivalGain = 6f;
    [SerializeField] private float bubblePlayerLoss = 4f;
    [Tooltip("ABC 커맨드: 마지막 키 객석의 라이벌 분위기 감소")]
    [SerializeField] private float abcRivalReduction = 20f;
    [Tooltip("피버 중 마디마다 모든 객석 플레이어 분위기 +")]
    [SerializeField] private float feverMoodPerBar = 8f;

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

    private float[] playerMood = new float[4];
    private float[] rivalMood = new float[4];
    private readonly List<int> schedule = new List<int>();
    private int nextChangeIndex;
    private int announcedBar = -1;
    private bool battling;
    private bool finished;
    private BossMoodHUD moodHud;

    public bool IsBattling => battling;
    public int SeatCount => playerMood.Length;
    public float GetPlayerMood(int seat) => seat >= 0 && seat < playerMood.Length ? playerMood[seat] : 0f;
    public float GetRivalMood(int seat) => seat >= 0 && seat < rivalMood.Length ? rivalMood[seat] : 0f;
    public Keys GetSeatKey(int seat) => stageController != null ? stageController.GetKeyForSeat(seat) : Keys.Q;
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

        int seats = stageController != null && stageController.SeatCount > 0 ? stageController.SeatCount : 4;
        playerMood = new float[seats];
        rivalMood = new float[seats];

        musicClock?.DisableAutoStart();
        musicClock?.SetAutoPhase(false);
        wheel?.SetJudgementSuspended(true);

        moodHud = gameObject.AddComponent<BossMoodHUD>();
        moodHud.Bind(this);

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
            StoryDialogueLine.Npc("클럽 매니저", "마름모 배치에선 ABC 커맨드로 라이벌의 분위기를 깎을 수 있어. 'CHANGE NEXT'가 뜨면 다음 마디부터 배치가 바뀌니 조심해.")));

        winDialogue = Ensure(winDialogue);
        winDialogue.FillIfEmpty(StoryDialogueLine.List(
            StoryDialogueLine.Npc("라이벌", "말도 안 돼… 관객들이 저 녀석을 따라 추고 있어…", DialogueStyle.Gloomy),
            StoryDialogueLine.Player("오늘 밤 무대는 내가 가져갈게!", PlayerStanding.Happy, DialogueStyle.Emphasis)));

        loseDialogue = Ensure(loseDialogue);
        loseDialogue.FillIfEmpty(StoryDialogueLine.List(
            StoryDialogueLine.Npc("라이벌", "봤지? 이 클럽의 주인공은 나야.", DialogueStyle.Emphasis),
            StoryDialogueLine.Player("다음엔… 꼭 이길 거야.", PlayerStanding.Sad, DialogueStyle.Gloomy)));
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

    // RhythmWheelUI.Awake보다 먼저 호출될 수 있으므로 컴포넌트를 직접 찾는다.
    private RhythmWheelFever FeverComponent => wheel != null ? wheel.GetComponent<RhythmWheelFever>() : null;

    private void OnEnable()
    {
        if (wheel != null) wheel.OnBeatStarted += HandleBeat;
        if (musicClock != null) musicClock.MusicEnded += HandleMusicEnded;
        if (combo != null) combo.OnPhraseHandoff += HandlePhrase;
        if (FeverComponent != null) FeverComponent.OnFeverBar += HandleFeverBar;
    }

    private void OnDisable()
    {
        if (wheel != null) wheel.OnBeatStarted -= HandleBeat;
        if (musicClock != null) musicClock.MusicEnded -= HandleMusicEnded;
        if (combo != null) combo.OnPhraseHandoff -= HandlePhrase;
        if (FeverComponent != null) FeverComponent.OnFeverBar -= HandleFeverBar;
    }

    private IEnumerator Start()
    {
        DemoUI.EnsureEventSystem();
        if (hideHudBeforeStart) HUDManager.Instance?.SetFlowHidden(true);
        moodHud.SetVisible(false);
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
        moodHud.SetVisible(true);

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
        if (!battling || beat % 4 != 0) return;

        int bar = beat / 4;
        if (bar > 0) ApplyBarMood();

        if (bar >= battleBars)
        {
            EndBattle();
            return;
        }

        // 예고된 마디의 첫 박: 새 배치 적용(입력은 계속 받는다)
        if (announcedBar == bar)
        {
            announcedBar = -1;
            banner?.Hide();
            if (stageController != null)
            {
                Stage next = stageController.currentPhase == Stage.Square ? Stage.Rhombus : Stage.Square;
                stageController.ChangePhase(next);
            }
        }

        // 다음 마디에 변경 예정이면 이번 마디에 예고한다. 피버와 겹치면 뒤로 미룬다(사전 공지 없는 변경은 없다).
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
                announcedBar = bar + 1;
                nextChangeIndex++;
                banner?.Show(changeNextText, DemoUI.Settings.cyan, DemoUI.Settings.changeNextSprite);
            }
        }
    }

    private void ApplyBarMood()
    {
        for (int i = 0; i < rivalMood.Length; i++)
        {
            rivalMood[i] += UnityEngine.Random.Range(rivalMoodPerBar.x, rivalMoodPerBar.y);

            if (bubbles != null && stageController != null && bubbles.IsBubbleActive(stageController.GetKeyForSeat(i)))
            {
                rivalMood[i] += bubbleRivalGain;
                playerMood[i] = Mathf.Max(0f, playerMood[i] - bubblePlayerLoss);
            }
        }
    }

    private void HandlePhrase(Keys lastKey, int just, int good, int bad, int miss, KeyCommand command)
    {
        if (!battling) return;

        bool empty = just + good + bad == 0;
        if (empty)
        {
            for (int i = 0; i < playerMood.Length; i++)
                playerMood[i] = Mathf.Max(0f, playerMood[i] - emptyPhraseLoss);
            return;
        }

        int seat = stageController != null ? stageController.GetSeatIndexForKey(lastKey) : -1;
        if (seat < 0 || seat >= playerMood.Length) return;

        if (miss > 0)
        {
            playerMood[seat] = Mathf.Max(0f, playerMood[seat] - failedPhraseLoss);
            return;
        }

        float style = PlayerManager.Instance != null ? PlayerManager.Instance.currentStats.style : 0f;
        float gain = (just * justMood + good * goodMood + bad * badMood) * (1f + style * styleMoodBonus);
        playerMood[seat] += gain;

        // ABC: 라이벌 분위기 감소(마름모 배치에서만 성립)
        if (command == KeyCommand.ABC)
            rivalMood[seat] = Mathf.Max(0f, rivalMood[seat] - abcRivalReduction);
    }

    private void HandleFeverBar(int _)
    {
        if (!battling) return;
        for (int i = 0; i < playerMood.Length; i++)
            playerMood[i] += feverMoodPerBar;
    }

    private void HandleMusicEnded()
    {
        if (battling) EndBattle();
    }

    /// <summary>결과 계산: 객석 수 → 총 분위기 → 완전 동률은 플레이어 승리.</summary>
    public bool EvaluateWinner(out int playerSeats, out int rivalSeats, out float playerTotal, out float rivalTotal)
    {
        playerSeats = rivalSeats = 0;
        playerTotal = rivalTotal = 0f;

        for (int i = 0; i < playerMood.Length; i++)
        {
            playerTotal += playerMood[i];
            rivalTotal += rivalMood[i];

            if (playerMood[i] > rivalMood[i]) playerSeats++;
            else if (rivalMood[i] > playerMood[i]) rivalSeats++;
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
        DemoSession.PlayerMoodTotal = Mathf.RoundToInt(playerTotal);
        DemoSession.RivalMoodTotal = Mathf.RoundToInt(rivalTotal);
        DemoSession.BossScore = scoreManager != null ? scoreManager.TotalScore : 0;
        DemoSession.AccumulateBattle(scoreManager, combo);

        StartCoroutine(EndRoutine(won));
    }

    private IEnumerator EndRoutine(bool won)
    {
        yield return new WaitForSeconds(1f);

        DemoUISettings s = DemoUI.Settings;
        banner?.Show(won ? "YOU WIN!" : "YOU LOSE...", won ? s.green : s.magenta);
        yield return new WaitForSeconds(2f);
        banner?.Hide();

        bool done = false;
        StoryDialogueSequence branch = won ? winDialogue : loseDialogue;
        if (branch != null && branch.HasLines)
        {
            branch.Play(() => done = true);
            while (!done) yield return null;
        }

        if (won) onPlayerWon?.Invoke();
        else onPlayerLost?.Invoke();

        if (!won && defeatHandledExternally) yield break;

        SceneFlow.Load(string.IsNullOrEmpty(resultSceneOverride) ? DemoUI.Settings.resultScene : resultSceneOverride);
    }
}
