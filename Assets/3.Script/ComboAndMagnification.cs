using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboAndMagnification : MonoBehaviour
{
    public event System.Action<Keys, Judge, float> OnJudged;
    public event System.Action<Keys> OnComboSaved;

    [Header("4-Beat Input Recording")]
    [SerializeField] private RhythmWheelUI rhythmWheel;

    // A four-beat phrase accepts input on its first three beats; the fourth is a rest.
    private readonly List<Keys> currentPhraseKeys = new(3);
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

        if (currentPhraseKeys.Count < 3)
            return;

        Keys[] phrase = currentPhraseKeys.ToArray();
        recordedPhrases.Add(phrase);
        currentPhraseKeys.Clear();
        comboSavedThisPhrase = true;

        OnComboSaved?.Invoke(phrase[phrase.Length - 1]);

        Debug.Log(
            $"[ComboAndMagnification] 4박 키 입력 저장: {phrase[0]}, {phrase[1]}, {phrase[2]}",
            this);
    }

    private void CompletePhrase()
    {
        // Input judgements are intentionally paused while the fever camera is
        // playing, so a phrase that passes during that time cannot fail a combo.
        if (rhythmWheel != null && rhythmWheel.IsJudgementLocked)
        {
            currentPhraseKeys.Clear();
            comboSavedThisPhrase = false;
            comboFailedThisPhrase = false;
            return;
        }

        if (!comboSavedThisPhrase && !comboFailedThisPhrase)
            Debug.Log("[ComboAndMagnification] Combo failed", this);

        currentPhraseKeys.Clear();
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
    }
}
