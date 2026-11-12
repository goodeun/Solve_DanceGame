using System;
using UnityEngine;

/// <summary>Compatibility facade for existing rhythm-wheel consumers.</summary>
[RequireComponent(typeof(RhythmWheelTiming), typeof(RhythmWheelInput), typeof(RhythmWheelJudgement))]
[RequireComponent(typeof(RhythmWheelPulseVisual), typeof(RhythmWheelFever))]
public sealed class RhythmWheelUI : MonoBehaviour
{
    private RhythmWheelTiming timing;
    private RhythmWheelJudgement judgement;
    private RhythmWheelFever fever;
    public int Score => judgement.Score;
    public int Combo => judgement.Combo;
    public float BeatDurationSeconds => timing.BeatDurationSeconds;
    public bool IsJudgementLocked => fever.IsJudgementLocked;
    public event Action<Keys, Judge, float> OnJudged;
    public event Action<int> OnBeatStarted;
    public event Action OnFourthBeatStarted;
    public event Action OnFourthPulsePeak;
    public event Action OnPhraseCompleted;
    private void Awake()
    {
        timing = GetComponent<RhythmWheelTiming>() ?? gameObject.AddComponent<RhythmWheelTiming>();
        judgement = GetComponent<RhythmWheelJudgement>() ?? gameObject.AddComponent<RhythmWheelJudgement>();
        fever = GetComponent<RhythmWheelFever>() ?? gameObject.AddComponent<RhythmWheelFever>();
        if (GetComponent<RhythmWheelInput>() == null) gameObject.AddComponent<RhythmWheelInput>();
        if (GetComponent<RhythmWheelPulseVisual>() == null) gameObject.AddComponent<RhythmWheelPulseVisual>();
        timing.BeatStarted += value => OnBeatStarted?.Invoke(value);
        timing.FourthBeatStarted += () => OnFourthBeatStarted?.Invoke();
        timing.FourthPulsePeak += () => OnFourthPulsePeak?.Invoke();
        timing.PhraseCompleted += () => OnPhraseCompleted?.Invoke();
        judgement.Judged += (key, result, offset) => OnJudged?.Invoke(key, result, offset);
    }
    public void JudgeInput(Keys key) => judgement.JudgeInput(key, timing.ElapsedSeconds);
}
