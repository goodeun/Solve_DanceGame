using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 일반전(메인 게임 플레이) 씬 흐름.
/// 게임 전 대사 → (대사가 없거나 끝나면) 3박 카운트다운 → 곡 재생·플레이 시작
/// → 곡 종료 감지 → 입력·판정 정리 → 보스전 대사 → 보스 씬 전환.
/// 대사·카운트다운 중에는 판정 게이트를 닫아 다른 플레이 조작을 막는다.
/// </summary>
public class MainGameFlow : MonoBehaviour
{
    [Header("References (비우면 자동 탐색)")]
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private RhythmWheelUI wheel;
    [SerializeField] private CountDownController countdown;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ComboAndMagnification combo;
    [SerializeField] private NGBubbleManager bubbles;
    [SerializeField] private NormalBattleDirector battleDirector;

    [Header("Dialogue")]
    [Tooltip("곡 시작 전 대사(비워두면 바로 카운트다운)")]
    [SerializeField] private StoryDialogueSequence introDialogue;
    [Tooltip("곡 종료 후 보스전으로 넘어가기 전 대사")]
    [SerializeField] private StoryDialogueSequence bossLeadInDialogue;
    [SerializeField] private bool useDefaultLines = true;

    [Header("Flow")]
    [Tooltip("0보다 크면 곡이 끝나기 전이라도 이 마디에서 일반전을 끝낸다(테스트용).")]
    [SerializeField, Min(0)] private int endAfterBars = 0;
    [SerializeField] private bool hideHudBeforeStart = true;
    [Tooltip("비우면 DemoUISettings의 보스 씬")]
    [SerializeField] private string nextSceneOverride;

    [Header("Events")]
    public UnityEvent onPlayStarted = new UnityEvent();
    public UnityEvent onPlayFinished = new UnityEvent();

    private bool finished;

    private void Awake()
    {
        if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>();
        if (wheel == null) wheel = FindFirstObjectByType<RhythmWheelUI>();
        if (countdown == null) countdown = FindFirstObjectByType<CountDownController>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (combo == null) combo = FindFirstObjectByType<ComboAndMagnification>();
        if (bubbles == null) bubbles = FindFirstObjectByType<NGBubbleManager>();
        if (battleDirector == null) battleDirector = FindFirstObjectByType<NormalBattleDirector>();
        if (countdown == null) countdown = gameObject.AddComponent<CountDownController>();
        if (battleDirector == null) battleDirector = gameObject.AddComponent<NormalBattleDirector>();

        // 곡은 대사·카운트다운 뒤에 직접 시작한다.
        musicClock?.DisableAutoStart();
        wheel?.SetJudgementSuspended(true);

        if (useDefaultLines)
        {
            if (introDialogue == null) introDialogue = gameObject.AddComponent<StoryDialogueSequence>();
            introDialogue.FillIfEmpty(StoryDialogueLine.List(
                StoryDialogueLine.Npc("클럽 매니저", "오늘 무대는 네 차례야. 관객들 반응, 기대하고 있을게."),
                StoryDialogueLine.Player("좋아… 클럽 전체를 내 리듬으로 물들여 주겠어!", PlayerStanding.Happy, DialogueStyle.Emphasis)));

            if (bossLeadInDialogue == null) bossLeadInDialogue = gameObject.AddComponent<StoryDialogueSequence>();
            bossLeadInDialogue.FillIfEmpty(StoryDialogueLine.List(
                StoryDialogueLine.Npc("???", "꽤 하는데? 하지만 이 클럽의 주인공은 따로 있지.", DialogueStyle.Angry),
                StoryDialogueLine.Player("누구야…?!", PlayerStanding.Surprised)));
        }
    }

    private void OnEnable()
    {
        if (musicClock != null) musicClock.MusicEnded += HandleMusicEnded;
        if (wheel != null) wheel.OnBeatStarted += HandleBeat;
    }

    private void OnDisable()
    {
        if (musicClock != null) musicClock.MusicEnded -= HandleMusicEnded;
        if (wheel != null) wheel.OnBeatStarted -= HandleBeat;
    }

    private IEnumerator Start()
    {
        DemoUI.EnsureEventSystem();
        if (hideHudBeforeStart) HUDManager.Instance?.SetFlowHidden(true);

        // HUD·다른 매니저 초기화가 끝난 뒤 시작한다.
        yield return null;

        bool done = false;
        if (introDialogue != null && introDialogue.HasLines)
        {
            introDialogue.Play(() => done = true);
            while (!done) yield return null;
        }

        HUDManager.Instance?.SetFlowHidden(false);

        done = false;
        countdown.StartCountdown(() => done = true, true);
        while (!done) yield return null;

        // 첫 박부터 판정을 연다.
        wheel?.ResumeJudgementAtBeat(0);
        musicClock?.StartMusic();
        onPlayStarted?.Invoke();
    }

    private void HandleBeat(int beat)
    {
        if (endAfterBars > 0 && beat >= endAfterBars * 4)
            FinishPlay();
    }

    private void HandleMusicEnded() => FinishPlay();

    private void FinishPlay()
    {
        if (finished) return;
        finished = true;

        // 곡이 끝난 뒤 입력·판정이 계속되지 않도록 정리한다.
        wheel?.SetJudgementSuspended(true);
        battleDirector?.StopDirecting();
        bubbles?.ClearBubbles();
        if (musicClock != null && !musicClock.IsEnded) musicClock.StopMusic();

        DemoSession.NormalScore = scoreManager != null ? scoreManager.TotalScore : 0;
        DemoSession.AccumulateBattle(scoreManager, combo);

        onPlayFinished?.Invoke();
        StartCoroutine(GoToBoss());
    }

    private IEnumerator GoToBoss()
    {
        // 피버 카메라 등 진행 중인 연출이 정리될 시간을 준다.
        yield return new WaitForSeconds(1f);

        bool done = false;
        if (bossLeadInDialogue != null && bossLeadInDialogue.HasLines)
        {
            bossLeadInDialogue.Play(() => done = true);
            while (!done) yield return null;
        }

        SceneFlow.Load(string.IsNullOrEmpty(nextSceneOverride) ? DemoUI.Settings.bossScene : nextSceneOverride);
    }
}
