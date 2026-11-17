using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RhythmWheelTiming))]
[DefaultExecutionOrder(-100)]
public sealed class RhythmWheelJudgement : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float justWindow = .1f;
    [SerializeField, Range(0f, 1f)] private float goodWindow = .3f;
    [SerializeField, Min(0f)] private float inputCallbackGraceSeconds = .05f;
    [SerializeField] private DanceSceneHUD danceHUD;

    private RhythmWheelTiming timing;
    private RhythmWheelFever fever;

    private readonly HashSet<int> resolvedBeats = new();
    [SerializeField] private ScoreManager scoreManager;

    public int Score => scoreManager != null ? scoreManager.TotalScore : 0;
    public int Combo { get; private set; }
    public float JustWindowSeconds => justWindow * timing.BeatDurationSeconds;
    public event Action<Keys, Judge, float> Judged;

    private void Awake()
    {
        timing = GetComponent<RhythmWheelTiming>();
        fever = GetComponent<RhythmWheelFever>();
        scoreManager ??= FindFirstObjectByType<ScoreManager>();
        danceHUD ??= FindFirstObjectByType<DanceSceneHUD>();
    }

    private void OnEnable() { Combo = 0; resolvedBeats.Clear(); }

    private void Update()
    {
        // Fever keeps normal key scoring, but missed beats must not break the
        // combo while its bonus window is active.
        if (fever != null && fever.IsActive)
            return;

        if (!timing.TryGetCurrentBeatTiming(timing.ElapsedSeconds, out int currentBeat, out float elapsedIntoBeat, out float beatDuration))
            return;
        if (currentBeat < 0) return;
        // A press belongs to the nearest beat boundary.  Keep the preceding
        // beat open until the midpoint of this beat so its late Bad/Good side
        // can still be judged before resolving it as a Miss.
        if (elapsedIntoBeat <= beatDuration * .5f + inputCallbackGraceSeconds) return;
        //int previousBeat = currentBeat - 1;
        if (currentBeat % 4 != 3 && resolvedBeats.Add(currentBeat)) Apply(default, Judge.Miss, 0f);
    }

    public bool JudgeInput(Keys key, float elapsed)
    {
        if (!timing.TryGetCurrentBeatTiming(elapsed, out int beat, out float offset, out float beatDuration))
            return false;

        // Judge against the nearest beat boundary.  This produces a symmetric
        // Bad ??Good ??Just ??Good ??Bad timing band around every playable beat.
        if (offset > beatDuration * .5f)
        {
            beat++;
            offset -= beatDuration;
        }

        // The fourth beat in each phrase remains a rest.  The timestamp-based
        // input sampling in RhythmWheelTiming already handles callback delay.
        if (beat % 4 == 3)
            return false;

        if (!resolvedBeats.Add(beat)) 
            return false;

        float absolute = Mathf.Abs(offset);
        Judge judge = absolute <= justWindow * beatDuration ? Judge.Just : absolute <= goodWindow * beatDuration ? Judge.Good : Judge.Bad;

        Apply(key, judge, offset);

        return true;
    }

    private void Apply(Keys key, Judge judge, float offset)
    {
        if (judge == Judge.Just || judge == Judge.Good)
            Combo++;
        else
            Combo = 0;

        danceHUD?.JudgementEffect(judge);
        Judged?.Invoke(key, judge, offset);
    }
}
