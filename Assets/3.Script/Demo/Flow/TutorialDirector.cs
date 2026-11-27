using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 튜토리얼 씬 진행(게임 플레이 씬과 별도 씬, 플레이 시 1회).
/// 안내 → 실습 → 안내 → 실습 순서로 진행한다.
/// - 안내: 대사 시스템으로 조작·시스템 설명. ESC는 Pause, 그 외 키는 대사 넘김. 다른 조작은 막힌다.
/// - 실습: 배경 곡은 루프로 계속 흐르고, 대사가 끝나면 다음 마디 첫 박부터 지정 마디 수만큼 실습한다.
///         실습 중에는 메트로놈이 함께 출력된다.
/// 기본 구성: 키 입력·판정 2마디 / 객석 분위기 4마디 / 태클 처리 6마디(2·4마디째 태클, 결과별 대사) / 키 변화 2마디
/// 화면의 SKIP 버튼 → 확인 팝업 → 튜토리얼을 통째로 넘기고 게임 플레이 씬으로 이동.
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    public enum Practice { None, Basic, Mood, Tackle, KeyChange }

    [Serializable]
    public class Step
    {
        public string name;
        public List<StoryDialogueLine> guide = new List<StoryDialogueLine>();
        public Practice practice = Practice.Basic;
        [Min(0)] public int practiceBars = 2;
        [Tooltip("태클 실습: 실습 시작(첫 마디 첫 박)에 동시에 띄울 태클 수(3개 동시 태클 기믹)")]
        [Min(0)] public int tackleGimmickAtStart = 0;
        [Tooltip("태클 실습: 몇 번째 실습 마디(1부터)에 태클을 하나씩 더 띄울지")]
        public List<int> tackleBars = new List<int>();
        [Tooltip("실습 성공 시(태클 실습: 태클을 모두 처리)")]
        public List<StoryDialogueLine> successLines = new List<StoryDialogueLine>();
        [Tooltip("실습 실패 시")]
        public List<StoryDialogueLine> failLines = new List<StoryDialogueLine>();
    }

    [Header("References (비우면 자동 탐색)")]
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private RhythmWheelUI wheel;
    [SerializeField] private StageController stageController;
    [SerializeField] private NGBubbleManager bubbles;
    [SerializeField] private BannerView banner;

    [Header("Steps (비우면 기본 시나리오)")]
    [SerializeField] private List<Step> steps = new List<Step>();
    [SerializeField] private List<StoryDialogueLine> outroLines = new List<StoryDialogueLine>();

    [Header("Mood Practice")]
    [Tooltip("분위기 실습 중 상승 폭 배수(한 번 입력으로 변화를 크게 보여준다)")]
    [SerializeField, Min(1f)] private float moodPracticeGainScale = 3f;

    [Header("Metronome")]
    [Tooltip("FMOD 메트로놈 이벤트 경로. 비우면 내장 클릭음을 사용")]
    [SerializeField] private string metronomeEvent;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.6f;

    [Header("Skip")]
    [SerializeField] private string skipLabel = "SKIP ▶";
    [SerializeField] private string skipMessage = "튜토리얼을 건너뛰고\n바로 게임을 시작할까요?";
    [Tooltip("비우면 DemoUISettings의 메인 씬")]
    [SerializeField] private string nextSceneOverride;

    public UnityEvent onTutorialFinished = new UnityEvent();

    private bool practicing;
    private int practiceStartBeat;
    private int practiceEndBeat;
    private Step currentStep;
    private AudioSource clickSource;
    private AudioClip accentClip;
    private AudioClip clickClip;
    private bool leaving;

    private void Awake()
    {
        if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>();
        if (wheel == null) wheel = FindFirstObjectByType<RhythmWheelUI>();
        if (stageController == null) stageController = FindFirstObjectByType<StageController>();
        if (bubbles == null) bubbles = FindFirstObjectByType<NGBubbleManager>();
        if (banner == null) banner = GetComponent<BannerView>();
        if (banner == null) banner = gameObject.AddComponent<BannerView>();

        // 안내 중에는 판정을 막는다. 배경 곡은 루프로 계속 흐른다.
        wheel?.SetJudgementSuspended(true);
        musicClock?.SetAutoPhase(false);

        // 튜토리얼에서는 피버를 쓰지 않는다.
        RhythmWheelFever fever = wheel != null ? wheel.GetComponent<RhythmWheelFever>() : null;
        if (fever != null)
        {
            fever.FeverEnabled = false;
            fever.SetGauge(0f);
        }

        if (steps == null || steps.Count == 0) steps = DefaultSteps();
        if (outroLines == null || outroLines.Count == 0)
            outroLines = StoryDialogueLine.List(
                StoryDialogueLine.Npc("클럽 매니저", "좋아, 기본은 다 익혔어. 이제 진짜 무대로 가 볼까?"),
                StoryDialogueLine.Player("두근거린다… 가자!", PlayerStanding.Happy, DialogueStyle.Emphasis));

        BuildMetronome();
    }

    private void OnEnable()
    {
        if (wheel != null) wheel.OnBeatStarted += HandleBeat;
        if (musicClock != null) musicClock.MusicEnded += HandleMusicEnded;
    }

    private void OnDisable()
    {
        if (wheel != null) wheel.OnBeatStarted -= HandleBeat;
        if (musicClock != null) musicClock.MusicEnded -= HandleMusicEnded;
    }

    private IEnumerator Start()
    {
        DemoUI.EnsureEventSystem();
        BuildSkipButton();
        if (bubbles != null) bubbles.RandomGimmickEnabled = false;

        yield return null;
        if (musicClock != null && !musicClock.IsStarted) musicClock.StartMusic();

        foreach (Step step in steps)
        {
            if (leaving) yield break;
            yield return RunStep(step);
        }

        yield return PlayLines(outroLines);
        Finish();
    }

    private IEnumerator RunStep(Step step)
    {
        currentStep = step;

        // 키 변화 학습: 안내 전에 배치를 바꿔 보여준다.
        if (step.practice == Practice.KeyChange && stageController != null)
        {
            banner?.Show("PHASE CHANGE", DemoUI.Settings.magenta, DemoUI.Settings.phaseChangeSprite);
            Stage next = stageController.currentPhase == Stage.Square ? Stage.Rhombus : Stage.Square;
            stageController.ChangePhase(next);
            yield return new WaitForSeconds(Mathf.Max(1f, stageController.TransitionDuration));
            banner?.Hide();
        }

        yield return PlayLines(step.guide);

        if (step.practice == Practice.None || step.practiceBars <= 0) yield break;

        AtmosphereController[] moods = FindObjectsByType<AtmosphereController>(FindObjectsSortMode.None);
        if (step.practice == Practice.Mood)
            foreach (var mood in moods) mood.GainScale = moodPracticeGainScale;

        if (step.practice == Practice.Tackle) bubbles?.ClearBubbles();

        // 다음 마디 첫 박부터 실습
        // 한 마디 = 1·2·3박 + 쉬는 박. 실습은 마지막 마디의 쉬는 박까지이며,
        // 다음 마디 첫 박(종료 박)은 미리 판정을 닫아 이른 입력이 섞이지 않게 한다.
        practiceStartBeat = wheel != null ? wheel.NextBarStartBeat : 0;
        practiceEndBeat = practiceStartBeat + step.practiceBars * 4;
        wheel?.ResumeJudgementAtBeat(practiceStartBeat);
        wheel?.CloseJudgementFromBeat(practiceEndBeat);
        practicing = true;

        int endBeat = practiceEndBeat;
        while (wheel != null && wheel.CurrentBeat < endBeat && !leaving)
            yield return null;

        practicing = false;
        wheel?.SetJudgementSuspended(true);

        foreach (var mood in moods)
            if (mood != null) mood.GainScale = 1f;

        if (step.practice == Practice.Tackle)
        {
            bool success = bubbles == null || bubbles.ActiveBubble <= 0;
            bubbles?.ClearBubbles();
            yield return PlayLines(success ? step.successLines : step.failLines);
        }
        else
        {
            yield return PlayLines(step.successLines);
        }
    }

    private IEnumerator PlayLines(List<StoryDialogueLine> lines)
    {
        if (lines == null || lines.Count == 0 || leaving) yield break;

        bool done = false;
        StoryDialogueView.GetOrCreate().Play(lines, () => done = true);
        while (!done && !leaving) yield return null;
    }

    private void HandleBeat(int beat)
    {
        if (!practicing || beat < practiceStartBeat || beat >= practiceEndBeat) return;

        PlayClick(beat % 4 == 0);

        if (currentStep != null && currentStep.practice == Practice.Tackle && beat % 4 == 0 && bubbles != null)
        {
            int practiceBar = (beat - practiceStartBeat) / 4 + 1;
            if (practiceBar == 1 && currentStep.tackleGimmickAtStart > 0)
                bubbles.SpawnBubbles(currentStep.tackleGimmickAtStart);
            if (currentStep.tackleBars.Contains(practiceBar))
                bubbles.SpawnBubble();
        }
    }

    private void HandleMusicEnded()
    {
        // 튜토리얼 배경 곡은 루프로 계속 흐른다.
        if (!leaving) musicClock?.RestartMusic();
    }

    // ---------------- Metronome ----------------

    private void BuildMetronome()
    {
        clickSource = gameObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;
        clickSource.volume = clickVolume;
        accentClip = CreateClick(1760f);
        clickClip = CreateClick(1175f);
    }

    private static AudioClip CreateClick(float frequency)
    {
        const int sampleRate = 44100;
        int length = sampleRate / 25; // 40ms
        var data = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-t * 90f);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        AudioClip clip = AudioClip.Create($"Metronome_{frequency}", length, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void PlayClick(bool accent)
    {
        if (!string.IsNullOrEmpty(metronomeEvent))
        {
            try { FMODUnity.RuntimeManager.PlayOneShot(metronomeEvent); return; }
            catch (FMODUnity.EventNotFoundException) { metronomeEvent = null; }
        }

        if (clickSource != null)
            clickSource.PlayOneShot(accent ? accentClip : clickClip);
    }

    // ---------------- Skip ----------------

    private void BuildSkipButton()
    {
        Canvas canvas = DemoUI.CreateOverlayCanvas("TutorialSkipCanvas", 750, transform);
        var button = DemoUI.CreateButton("Skip", canvas.transform, skipLabel, new Vector2(260f, 90f), AskSkip);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
    }

    private void AskSkip()
    {
        if (leaving) return;

        StoryDialogueView.InputBlockCount++;
        DemoUI.ShowConfirmPopup(skipMessage, "건너뛰기", () =>
        {
            StoryDialogueView.InputBlockCount = Mathf.Max(0, StoryDialogueView.InputBlockCount - 1);
            DemoSession.TutorialSkipped = true;
            Finish();
        }, "계속하기", () => StoryDialogueView.InputBlockCount = Mathf.Max(0, StoryDialogueView.InputBlockCount - 1));
    }

    private void Finish()
    {
        if (leaving) return;
        leaving = true;

        StoryDialogueView.GetOrCreate().Stop(false);
        wheel?.SetJudgementSuspended(true);
        onTutorialFinished?.Invoke();
        SceneFlow.Load(string.IsNullOrEmpty(nextSceneOverride) ? DemoUI.Settings.mainScene : nextSceneOverride);
    }

    // ---------------- Default Scenario ----------------

    private static List<Step> DefaultSteps()
    {
        const string guide = "클럽 매니저";
        return new List<Step>
        {
            new Step
            {
                name = "기본 입력과 판정",
                practice = Practice.Basic,
                practiceBars = 2,
                guide = StoryDialogueLine.List(
                    StoryDialogueLine.Npc(guide, "어서 와! 무대에 오르기 전에 기본부터 짚고 가자."),
                    StoryDialogueLine.Npc(guide, "Q·E는 팔, A·D는 다리 동작이야. 정해진 노트 없이 리듬에 맞춰 네 마음대로 춤을 추면 돼."),
                    StoryDialogueLine.Npc(guide, "한 마디는 4박. 앞의 세 박에 키를 하나씩 누르고, 네 번째 박은 쉬는 박이야."),
                    StoryDialogueLine.Npc(guide, "박자에 정확할수록 Just, 조금 어긋나면 Good·Bad. 세 박을 모두 성공하면 콤보가 쌓여!", DialogueStyle.Emphasis),
                    StoryDialogueLine.Npc(guide, "메트로놈 소리에 맞춰 2마디만 춰 볼까?")),
                successLines = StoryDialogueLine.List(StoryDialogueLine.Npc(guide, "좋아, 리듬감 있는데?")),
            },
            new Step
            {
                name = "객석 분위기",
                practice = Practice.Mood,
                practiceBars = 4,
                guide = StoryDialogueLine.List(
                    StoryDialogueLine.Npc(guide, "객석마다 '분위기'가 있어. 마디의 마지막 키에 해당하는 객석이 반응하지."),
                    StoryDialogueLine.Npc(guide, "같은 키만 누르면 배율이 떨어지고, 다른 키를 섞으면 배율이 올라가. 여러 객석을 고루 달궈 봐!"),
                    StoryDialogueLine.Npc(guide, "지금은 연습이니까 한 번만 성공해도 객석이 확 달아오를 거야. 4마디 동안 해 보자.")),
                successLines = StoryDialogueLine.List(StoryDialogueLine.Npc(guide, "관객들이 같이 춤추기 시작했어! 이게 이 클럽의 재미지.")),
            },
            new Step
            {
                name = "태클 처리",
                practice = Practice.Tackle,
                practiceBars = 6,
                tackleGimmickAtStart = 3,
                tackleBars = new List<int>(),
                guide = StoryDialogueLine.List(
                    StoryDialogueLine.Npc(guide, "가끔 객석에서 태클 말풍선이 날아와. 놔두면 전체 점수 배율이 계속 떨어져.", DialogueStyle.Angry),
                    StoryDialogueLine.Npc(guide, "같은 층 키를 두 번 누르고 다른 층 키로 마무리하는 AAB 커맨드! 마지막 키의 객석 태클을 물리칠 수 있어."),
                    StoryDialogueLine.Npc(guide, "예를 들어 Q-E-A면 A 객석, A-D-Q면 Q 객석이야."),
                    StoryDialogueLine.Npc(guide, "실습을 시작하면 태클이 한꺼번에 세 개 날아올 거야. 6마디 안에 전부 처리해 봐!", DialogueStyle.Emphasis)),
                successLines = StoryDialogueLine.List(StoryDialogueLine.Npc(guide, "완벽해! 태클 따위 문제없지?", DialogueStyle.Emphasis)),
                failLines = StoryDialogueLine.List(
                    StoryDialogueLine.Npc(guide, "아직 태클이 남았네… 괜찮아, 본 무대에서 다시 해 보자.", DialogueStyle.Gloomy),
                    StoryDialogueLine.Npc(guide, "같은 층 두 번, 다른 층 한 번. 마지막 키가 태클이 뜬 객석이어야 해!")),
            },
            new Step
            {
                name = "키 변화",
                practice = Practice.KeyChange,
                practiceBars = 2,
                guide = StoryDialogueLine.List(
                    StoryDialogueLine.Npc(guide, "곡의 분위기가 바뀌면 무대와 객석이 움직이고 키 배치도 바뀌어!", DialogueStyle.Emphasis),
                    StoryDialogueLine.Npc(guide, "마름모 배치에선 W는 머리, A·D는 팔, S는 스텝이야. 경고와 카운트다운이 끝난 마디부터 새 키를 쓰면 돼."),
                    StoryDialogueLine.Npc(guide, "새 배치로 2마디 춰 보자.")),
                successLines = StoryDialogueLine.List(StoryDialogueLine.Npc(guide, "적응이 빠른걸!")),
            },
        };
    }
}
