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
    private ComboAndMagnification combo;
    public int Score => judgement.Score;
    /// <summary>마디 단위 콤보(세 박을 모두 성공한 마디 수).</summary>
    public int Combo => combo != null ? combo.BarCombo : judgement.Combo;
    public int CurrentBeat => timing.CurrentBeat;
    public RhythmWheelTiming Timing => timing;
    public RhythmWheelJudgement Judgement => judgement;
    public RhythmWheelFever Fever => fever;
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

        combo = FindFirstObjectByType<ComboAndMagnification>();

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

    // ---- 판정 게이트 (RhythmWheelJudgement 위임) ----
    public void SetJudgementSuspended(bool value) => judgement.SetSuspended(value);
    public void BlockJudgementUntilBeat(int beat) => judgement.BlockUntilBeat(beat);
    public void ResumeJudgementAtBeat(int beat) => judgement.ResumeAtBeat(beat);
    public bool IsBeatBlocked(int beat) => judgement.IsBeatBlocked(beat);

    /// <summary>4박째(쉼) 박 번호를 받아 해당 마디의 입력 박이 판정 제외였는지 확인한다.</summary>
    public bool IsPhraseBlocked(int restBeat) => judgement.IsBeatBlocked(restBeat - 3);

    /// <summary>다음 마디 첫 박 번호. 현재 박이 마디 첫 박이면 그 다음 마디를 돌려준다.</summary>
    public int NextBarStartBeat => timing.CurrentBeat < 0 ? 0 : (timing.CurrentBeat / 4 + 1) * 4;
}
