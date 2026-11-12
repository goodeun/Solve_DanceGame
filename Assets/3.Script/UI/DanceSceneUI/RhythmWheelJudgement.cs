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
    private readonly HashSet<int> resolvedBeats = new();
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public float JustWindowSeconds => justWindow * timing.BeatDurationSeconds;
    public event Action<Keys, Judge, float> Judged;

    private void Awake()
    {
        timing = GetComponent<RhythmWheelTiming>();
        danceHUD ??= FindFirstObjectByType<DanceSceneHUD>();
    }

    private void OnEnable() { Score = 0; Combo = 0; resolvedBeats.Clear(); }

    private void Update()
    {
        int currentBeat = Mathf.FloorToInt(timing.ElapsedSeconds / timing.BeatDurationSeconds);
        if (currentBeat <= 0) return;
        float elapsedIntoBeat = timing.ElapsedSeconds - currentBeat * timing.BeatDurationSeconds;
        // A press belongs to the nearest beat boundary.  Keep the preceding
        // beat open until the midpoint of this beat so its late Bad/Good side
        // can still be judged before resolving it as a Miss.
        if (elapsedIntoBeat <= timing.BeatDurationSeconds * .5f + inputCallbackGraceSeconds) return;
        int previousBeat = currentBeat - 1;
        if (previousBeat % 4 != 3 && resolvedBeats.Add(previousBeat)) Apply(default, Judge.Miss, 0f);
    }

    public bool JudgeInput(Keys key, float elapsed)
    {
        float beatDuration = timing.BeatDurationSeconds;
        int beat = Mathf.FloorToInt(elapsed / beatDuration);
        float offset = elapsed - beat * beatDuration;

        // Judge against the nearest beat boundary.  This produces a symmetric
        // Bad → Good → Just → Good → Bad timing band around every playable beat.
        if (offset > beatDuration * .5f)
        {
            beat++;
            offset -= beatDuration;
        }

        // The fourth beat in each phrase remains a rest.  The timestamp-based
        // input sampling in RhythmWheelTiming already handles callback delay.
        if (beat % 4 == 3)
            return false;

        if (!resolvedBeats.Add(beat)) return false;
        float absolute = Mathf.Abs(offset);
        Judge judge = absolute <= justWindow * beatDuration ? Judge.Just : absolute <= goodWindow * beatDuration ? Judge.Good : Judge.Bad;
        Apply(key, judge, offset);
        return true;
    }

    private void Apply(Keys key, Judge judge, float offset)
    {
        if (judge == Judge.Just) { Score += 1000; Combo++; }
        else if (judge == Judge.Good) { Score += 700; Combo++; }
        else if (judge == Judge.Bad) { Score += 300; Combo = 0; }
        else Combo = 0;
        danceHUD?.JudgementEffect(judge);
        danceHUD?.AddScore(Score);
        Judged?.Invoke(key, judge, offset);
    }
}
