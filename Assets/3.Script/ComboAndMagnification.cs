using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboAndMagnification : MonoBehaviour
{
    public event System.Action<Keys, Judge, float> OnJudged;
    public event System.Action<Keys, int, int, int, int, KeyCommand> OnPhraseHandoff;
    public event System.Action<Keys> OnComboSaved;
    public event System.Action OnComboFailed;

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

        OnComboSaved?.Invoke(phrase[phrase.Length - 1]);
        ApplyPhraseMood(phrase[phrase.Length - 1]);

    }

    private void CompletePhrase()
    {

        // Fever can overlap a phrase.  Keep its bonus window from producing a
        // combo failure, while still allowing regular input scoring.
        if (rhythmWheel != null && (rhythmWheel.IsJudgementLocked || rhythmWheel.IsFeverActive))
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
}

