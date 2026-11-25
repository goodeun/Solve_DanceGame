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
    private RhythmWheelInput input;
    public int Score => judgement.Score;
    public int Combo => judgement.Combo;
    public float BeatDurationSeconds => timing.BeatDurationSeconds;
    public bool IsJudgementLocked => fever.IsJudgementLocked;
    public bool IsFeverActive => fever != null && fever.IsActive;
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
        input = GetComponent<RhythmWheelInput>() ?? gameObject.AddComponent<RhythmWheelInput>();

        if (GetComponent<RhythmWheelPulseVisual>() == null) 
            gameObject.AddComponent<RhythmWheelPulseVisual>();

        timing.BeatStarted += value => OnBeatStarted?.Invoke(value);
        timing.FourthBeatStarted += () => OnFourthBeatStarted?.Invoke();
        timing.FourthPulsePeak += () => OnFourthPulsePeak?.Invoke();
        timing.PhraseCompleted += () => OnPhraseCompleted?.Invoke();
        judgement.Judged += (key, result, offset) => OnJudged?.Invoke(key, result, offset);
    }
    public void JudgeInput(Keys key) => judgement.JudgeInput(key, timing.ElapsedSeconds);
    public void SetInputLocked(bool isLocked) => input.SetInputLocked(isLocked);
}
