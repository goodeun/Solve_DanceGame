using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboAndMagnification : MonoBehaviour
{
    public event System.Action<Keys, Judge, float> OnJudged;
    public event System.Action<Keys, int, int, int, int, KeyCommand> OnPhraseHandoff;
    public event System.Action<Keys> OnComboSaved;
    public event System.Action OnComboFailed;
    /// <summary>마디 콤보 변경 시 (현재 콤보).</summary>
    public event System.Action<int> OnBarComboChanged;

    /// <summary>세 박을 모두 성공한 마디가 연속된 횟수. 마디가 끝날 때 하나씩 적립된다.</summary>
    public int BarCombo { get; private set; }
    public int MaxBarCombo { get; private set; }

    [Header("4-Beat Input Recording")]
    [SerializeField] private RhythmWheelUI rhythmWheel;

    // A four-beat phrase accepts input on its first three beats; the fourth is a rest.
    private readonly List<Keys> currentPhraseKeys = new(3);
    private readonly List<Judge> currentPhraseJudgements = new(3);
    // This is diagnostic/history data only.  Retaining every phrase turns a
    // long editor session into an ever-growing allocation.
    private const int MaxRecordedPhrases = 64;
    private readonly List<Keys[]> recordedPhrases = new(MaxRecordedPhrases);
    private bool comboSavedThisPhrase;
    private bool comboFailedThisPhrase;

    private Stage curStage = Stage.Square;
    private KeyCommand curCommand=KeyCommand.None;


    public IReadOnlyList<Keys[]> RecordedPhrases => recordedPhrases;

    private readonly Dictionary<Stage, Dictionary<Keys, int>> stageTiers = new()
    {
        [Stage.Square] = new()
        {
            [Keys.Q] = 1,
            [Keys.E] = 1,
            [Keys.A] = 2,
            [Keys.D] = 2
        },

        [Stage.Rhombus] = new()
        {
            [Keys.W] = 1,
            [Keys.A] = 2,
            [Keys.D] = 2,
            [Keys.S] = 3
        }
    };

    private void Awake()
    {
        if (rhythmWheel == null)
            rhythmWheel = FindFirstObjectByType<RhythmWheelUI>();
    }

    private void OnEnable()
    {
        if (rhythmWheel != null)
        {
            rhythmWheel.OnJudged += RecordKeyInput;
            rhythmWheel.OnFourthBeatStarted += CompletePhrase;
        }
    }

    private void OnDisable()
    {
        if (rhythmWheel != null)
        {
            rhythmWheel.OnJudged -= RecordKeyInput;
            rhythmWheel.OnFourthBeatStarted -= CompletePhrase;
        }
    }

    private void RecordKeyInput(Keys pressedKey, Judge judge, float offset)
    {
        OnJudged?.Invoke(pressedKey, judge, offset);

        currentPhraseJudgements.Add(judge);
        currentPhraseKeys.Add(pressedKey);

        if (judge == Judge.Miss)
        {
            FailCurrentPhrase();
            return;
        }

        if (currentPhraseKeys.Count < 3)
            return;

        Keys[] phrase = currentPhraseKeys.ToArray();


        if (recordedPhrases.Count == MaxRecordedPhrases)
            recordedPhrases.RemoveAt(0);
        recordedPhrases.Add(phrase);
        //currentPhraseKeys.Clear();
        comboSavedThisPhrase = true;

        BarCombo++;
        MaxBarCombo = Mathf.Max(MaxBarCombo, BarCombo);
        OnBarComboChanged?.Invoke(BarCombo);

        OnComboSaved?.Invoke(phrase[phrase.Length - 1]);
        ApplyPhraseMood(phrase[phrase.Length - 1]);

    }

    private void CompletePhrase()
    {

        // Fever can overlap a phrase.  Keep its bonus window from producing a
        // combo failure, while still allowing regular input scoring.
        // 좌석 변경·대사 등으로 판정 제외된 마디도 성공·실패로 세지 않는다.
        bool blocked = rhythmWheel != null && rhythmWheel.IsPhraseBlocked(rhythmWheel.CurrentBeat);
        if (rhythmWheel != null && (blocked || rhythmWheel.IsJudgementLocked || rhythmWheel.IsFeverActive))
        {
            currentPhraseKeys.Clear();
            currentPhraseJudgements.Clear();
            comboSavedThisPhrase = false;
            comboFailedThisPhrase = false;
            return;
        }

        if (!comboSavedThisPhrase && !comboFailedThisPhrase)
            FailCurrentPhrase();

        GetJudgeCount(out Keys lastKey, out int Just, out int Good, out int Bad, out int Miss);
        CommandCaculate(currentPhraseKeys, out curCommand, Miss);
        OnPhraseHandoff?.Invoke(lastKey, Just, Good, Bad, Miss, curCommand);

        currentPhraseKeys.Clear();
        currentPhraseJudgements.Clear();
        comboSavedThisPhrase = false;
        comboFailedThisPhrase = false;
        curCommand = KeyCommand.None;
    }

    private void FailCurrentPhrase()
    {
        if (comboSavedThisPhrase || comboFailedThisPhrase)
            return;

        comboFailedThisPhrase = true;
        if (BarCombo != 0)
        {
            BarCombo = 0;
            OnBarComboChanged?.Invoke(BarCombo);
        }
        //currentPhraseKeys.Clear();
        //currentPhraseJudgements.Clear();
        OnComboFailed?.Invoke();
    }

    private void ApplyPhraseMood(Keys phraseFinalKey)
    {
        AtmosphereController[] audienceBlocks = FindObjectsByType<AtmosphereController>(FindObjectsSortMode.None);
        if (audienceBlocks.Length == 0)
            return;

        Judge[] phraseJudgements = currentPhraseJudgements.ToArray();
        foreach (AtmosphereController audienceBlock in audienceBlocks)
            audienceBlock.ApplyPhraseMood(phraseFinalKey, phraseJudgements);
    }

    // TODO 커맨드 계산
    private bool CommandCaculate(List<Keys> phrase, out KeyCommand command, int miss)
    {
        command = KeyCommand.None;

        if (phrase == null || phrase.Count != 3) return false;
        if (miss > 0) return false ;

        int a = GetTier(phrase[0]);
        int b = GetTier(phrase[1]);
        int c = GetTier(phrase[2]);

        if (a < 1 || b < 1 || c < 1) return false;

        //AAB 커맨드
        if(a == b && Mathf.Abs(c-b) == 1)
        {
            command = KeyCommand.AAB;
            return true;
        }
        //ABA 커맨드
        if(a==c && Mathf.Abs(b-a) ==1)
        {
            command = KeyCommand.ABA;
            return true;
        }
        //ABC 커맨드
        if(b - a == c-b && Mathf.Abs(b-a)==1)
        {
            command = KeyCommand.ABC;
            return true;
        }
        return false;

    }
     
    private int GetTier(Keys key)
    {
        if(stageTiers.TryGetValue(curStage, out var tiers)
            && tiers.TryGetValue(key, out int tier))
        {
            return tier;
        }

        return -1;
    }
    private void GetJudgeCount(out Keys lastKey, out int Just, out int Good, out int Bad, out int Miss)
    {
        Just = 0;
        Good = 0;
        Bad = 0;
        Miss = 0;
        lastKey = Keys.Q;

        if (currentPhraseJudgements.Count <= 0) return;

        foreach(Judge judge in currentPhraseJudgements)
        {
            switch (judge)
            {
                case Judge.Just:
                    Just++;
                    break;
                case Judge.Good:
                    Good++;
                    break;
                case Judge.Bad:
                    Bad++;
                    break;
                case Judge.Miss:
                    Miss++;
                    break;
            }
        }

        lastKey = currentPhraseKeys[currentPhraseJudgements.Count - 1];
    }

    public void SetStage(Stage stage)
    {
        curStage = stage;
    }

    public Stage CurrentStage => curStage;

    /// <summary>진행 중인 마디 기록을 버린다. 전환 진입·재개 시 이전 입력이 섞이지 않게 한다.</summary>
    public void ResetCurrentPhrase()
    {
        currentPhraseKeys.Clear();
        currentPhraseJudgements.Clear();
        comboSavedThisPhrase = false;
        comboFailedThisPhrase = false;
        curCommand = KeyCommand.None;
    }

    public void ResetBarCombo()
    {
        BarCombo = 0;
        MaxBarCombo = 0;
        OnBarComboChanged?.Invoke(BarCombo);
    }
}

