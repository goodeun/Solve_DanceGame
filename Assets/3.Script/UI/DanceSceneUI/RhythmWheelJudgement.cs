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
        // A physical press at the third-beat boundary can be delivered by the
        // Input System on the first rest-beat frame. Leave a small callback
        // grace before turning that third beat into a Miss.
        if (elapsedIntoBeat <= justWindow * timing.BeatDurationSeconds + inputCallbackGraceSeconds) return;
        int previousBeat = currentBeat - 1;
        if (previousBeat % 4 != 3 && resolvedBeats.Add(previousBeat)) Apply(default, Judge.Miss, 0f);
    }

    public bool JudgeInput(Keys key, float elapsed)
    {
        float beatDuration = timing.BeatDurationSeconds;
        int beat = Mathf.FloorToInt(elapsed / beatDuration);
        float offset = elapsed - beat * beatDuration;

        // Input System can deliver a physical key event one frame after FMOD has
        // crossed into the rest beat.  Do not discard that event before it gets a
        // chance to resolve the preceding third beat.  It remains a late (Bad)
        // third-beat judgement; presses later in the actual rest beat are still
        // rejected exactly as before.
        if (beat % 4 == 3)
        {
            if (offset > inputCallbackGraceSeconds) return false;
            beat--;
            if (beat < 0 || beat % 4 == 3) return false;
            offset = beatDuration;
        }

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
