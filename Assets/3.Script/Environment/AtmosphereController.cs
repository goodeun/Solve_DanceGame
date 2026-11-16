using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AtmosphereController : MonoBehaviour
{
    [FormerlySerializedAs("AtmosphereScore")]
    [SerializeField, Range(0f, 100f)] private float moodScore = 0f;

    [SerializeField] private int heating = 30;
    [SerializeField] private int burning = 70;

    [Header("Rhythm Mood Link")]
    [Tooltip("When enabled, this audience block only reacts to phrases whose final key matches this key.")]
    [SerializeField] private bool routeByFinalKey;
    [SerializeField] private Keys finalKey = Keys.Q;
    [SerializeField] private ComboAndMagnification comboAndMagnification;
    [SerializeField] private float justMoodGain = 12f;
    [SerializeField] private float goodMoodGain = 6f;
    [SerializeField] private float badMoodGain = 2f;
    [SerializeField] private float failedPhraseMoodLoss = 8f;
    [SerializeField] private float emptyPhraseMoodLoss = 3f;

    public int Heating => heating;
    public int Burning => burning;
    public float MoodScore => moodScore;

    private readonly List<AudienceController> audiences = new();
    private Keys lastPressedKey;
    private bool hasPressedKeyThisPhrase;

    private void Awake()
    {
        comboAndMagnification ??= FindFirstObjectByType<ComboAndMagnification>();
    }

    private void OnEnable()
    {
        if (comboAndMagnification == null)
            comboAndMagnification = FindFirstObjectByType<ComboAndMagnification>();

        if (comboAndMagnification == null)
            return;

        comboAndMagnification.OnJudged += HandleJudged;
        comboAndMagnification.OnComboFailed += HandleComboFailed;
    }

    private void OnDisable()
    {
        if (comboAndMagnification == null)
            return;

        comboAndMagnification.OnJudged -= HandleJudged;
        comboAndMagnification.OnComboFailed -= HandleComboFailed;
    }

    public void Register(AudienceController audience)
    {
        if (audience != null && !audiences.Contains(audience))
        {
            audiences.Add(audience);
            audience.SetMoodScore(moodScore);
        }
    }

    public void Unregister(AudienceController audience)
    {
        audiences.Remove(audience);
    }

    public void AddMood(float amount)
    {
        moodScore = Mathf.Clamp(moodScore + amount, 0f, 100f);
        BroadcastMoodScore();
    }

    public void SetMoodScore(float value)
    {
        moodScore = Mathf.Clamp(value, 0f, 100f);
        BroadcastMoodScore();
    }

    private void HandleJudged(Keys key, Judge judge, float offset)
    {
        if (judge == Judge.Miss)
            return;

        hasPressedKeyThisPhrase = true;
        lastPressedKey = key;
    }

    private void HandleComboFailed()
    {
        if (hasPressedKeyThisPhrase)
        {
            if (ReceivesPhraseFor(lastPressedKey))
                AddMood(-failedPhraseMoodLoss);
        }
        else
        {
            // Missing an entire measure lowers every audience block.
            AddMood(-emptyPhraseMoodLoss);
        }

        hasPressedKeyThisPhrase = false;
    }

    /// <summary>
    /// Applies the complete phrase's judgement gain to its final-key audience.
    /// </summary>
    public void ApplyPhraseMood(Keys phraseFinalKey, Judge[] judgements)
    {
        // Every audience block observes the phrase; clear its local failure
        // tracking even when this particular block is not the key target.
        hasPressedKeyThisPhrase = false;

        if (!ReceivesPhraseFor(phraseFinalKey) || judgements == null)
            return;

        float moodChange = 0f;
        foreach (Judge judgement in judgements)
        {
            moodChange += judgement switch
            {
                Judge.Just => justMoodGain,
                Judge.Good => goodMoodGain,
                Judge.Bad => badMoodGain,
                _ => 0f
            };
        }

        AddMood(moodChange);
    }

    private bool ReceivesPhraseFor(Keys phraseFinalKey) => !routeByFinalKey || finalKey == phraseFinalKey;

    private void BroadcastMoodScore()
    {
        for (int i = audiences.Count - 1; i >= 0; i--)
        {
            if (audiences[i] == null)
                audiences.RemoveAt(i);
            else
                audiences[i].SetMoodScore(moodScore);
        }
    }
}
