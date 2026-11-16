using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboAndMagnification : MonoBehaviour
{
    public event System.Action<Keys, Judge, float> OnJudged;
    public event System.Action<Keys> OnComboSaved;
    public event System.Action OnComboFailed;

    [Header("4-Beat Input Recording")]
    [SerializeField] private RhythmWheelUI rhythmWheel;

    // A four-beat phrase accepts input on its first three beats; the fourth is a rest.
    private readonly List<Keys> currentPhraseKeys = new(3);
    private readonly List<Judge> currentPhraseJudgements = new(3);
    private readonly List<Keys[]> recordedPhrases = new();
    private bool comboSavedThisPhrase;
    private bool comboFailedThisPhrase;

    public IReadOnlyList<Keys[]> RecordedPhrases => recordedPhrases;

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

        if (judge == Judge.Miss)
        {
            FailCurrentPhrase();
            return;
        }

        currentPhraseKeys.Add(pressedKey);
        currentPhraseJudgements.Add(judge);

        if (currentPhraseKeys.Count < 3)
            return;

        Keys[] phrase = currentPhraseKeys.ToArray();
        recordedPhrases.Add(phrase);
        currentPhraseKeys.Clear();
        comboSavedThisPhrase = true;

        OnComboSaved?.Invoke(phrase[phrase.Length - 1]);
        ApplyPhraseMood(phrase[phrase.Length - 1]);

        Debug.Log(
            $"[ComboAndMagnification] 4박 키 입력 저장: {phrase[0]}, {phrase[1]}, {phrase[2]}",
            this);
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

        currentPhraseKeys.Clear();
        currentPhraseJudgements.Clear();
        comboSavedThisPhrase = false;
        comboFailedThisPhrase = false;
    }

    private void FailCurrentPhrase()
    {
        if (comboSavedThisPhrase || comboFailedThisPhrase)
            return;

        Debug.Log("[ComboAndMagnification] Combo failed", this);
        comboFailedThisPhrase = true;
        currentPhraseKeys.Clear();
        currentPhraseJudgements.Clear();
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
}
