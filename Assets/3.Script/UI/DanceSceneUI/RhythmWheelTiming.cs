using System;
using UnityEngine;

/// <summary>Owns the music clock and all beat/phrase timing events.</summary>
[DefaultExecutionOrder(-200)]
public sealed class RhythmWheelTiming : MonoBehaviour
{
    [SerializeField, Min(1)] private int bpm = 120;
    [SerializeField] private FmodMusicClock musicClock;

    private float fallbackStartTime;
    private int lastBeat = -1;
    private int fmodBeatSequence = -1;
    private int lastPulseCycle = int.MinValue;

    public float BeatDurationSeconds => musicClock != null ? musicClock.BeatDurationSeconds : 60f / bpm;
    public float ElapsedSeconds { get; private set; }
    public int CurrentBeat { get; private set; } = -1;
    public float CurrentBeatStartSeconds { get; private set; }
    public event Action<int> BeatStarted;
    public event Action FourthBeatStarted;
    public event Action PhraseCompleted;
    public event Action FourthPulsePeak;

    private void Awake() => musicClock ??= FindFirstObjectByType<FmodMusicClock>();
    private void OnValidate() => musicClock ??= FindFirstObjectByType<FmodMusicClock>();

    private void OnEnable()
    {
        fallbackStartTime = Time.unscaledTime;
        lastBeat = -1;
        fmodBeatSequence = -1;
        lastPulseCycle = int.MinValue;
        CurrentBeat = -1;
        CurrentBeatStartSeconds = 0f;
        if (musicClock != null)
            musicClock.TimelineBeat += HandleTimelineBeat;
    }

    private void OnDisable()
    {
        if (musicClock != null)
            musicClock.TimelineBeat -= HandleTimelineBeat;
    }

    private float ReadElapsedSeconds() => musicClock != null && musicClock.TryGetElapsedSeconds(out float elapsed)
        ? elapsed
        : Time.unscaledTime - fallbackStartTime;

    private void Update()
    {
        ElapsedSeconds = ReadElapsedSeconds();

        // When FMOD is active, HandleTimelineBeat drives this accurately across
        // tempo changes. Keep the elapsed-time calculation only as a fallback.
        if (musicClock != null && musicClock.CurrentBar > 0)
            return;

        int beat = Mathf.FloorToInt(ElapsedSeconds / BeatDurationSeconds);
        CurrentBeat = beat;
        CurrentBeatStartSeconds = beat * BeatDurationSeconds;
        DispatchBeat(beat);
    }

    private void HandleTimelineBeat(int bar, int beatInBar, float tempo, int upper, int lower)
    {
        // Do not derive the game beat from FMOD's bar/beat indices: their
        // starting base and time-signature value depend on Studio metadata.
        // A sequential callback count is stable across tempo changes.
        int beat = ++fmodBeatSequence;
        CurrentBeat = beat;
        CurrentBeatStartSeconds = musicClock.CurrentBeatStartSeconds;
        DispatchBeat(beat);
    }

    private void DispatchBeat(int beat)
    {
        for (int value = lastBeat + 1; value <= beat; value++)
        {
            if (value >= 0) BeatStarted?.Invoke(value);
            if (value > 0 && value % 4 == 0) PhraseCompleted?.Invoke();
            if (value >= 0 && value % 4 == 3) FourthBeatStarted?.Invoke();
        }
        lastBeat = beat;
    }

    /// <summary>
    /// Returns the music timeline at the instant a physical input occurred.
    /// CallbackContext.time is relative to realtimeSinceStartup, so subtracting
    /// its delivery delay prevents a press near a beat boundary from being
    /// judged as a press in the following (possibly rest) beat.
    /// </summary>
    public float SampleInputElapsedSeconds(double inputTime)
    {
        float elapsed = ReadElapsedSeconds();
        double deliveryDelay = Time.realtimeSinceStartupAsDouble - inputTime;

        // Ignore invalid or implausibly old timestamps rather than turning a
        // delayed application resume into an unrelated past judgement.
        if (deliveryDelay >= 0d && deliveryDelay <= .25d)
            elapsed -= (float)deliveryDelay;

        return Mathf.Max(0f, elapsed);
    }

    /// <summary>Maps a sampled timeline time to the current FMOD beat segment.</summary>
    public bool TryGetCurrentBeatTiming(float elapsed, out int beat, out float offset, out float beatDuration)
    {
        beatDuration = BeatDurationSeconds;
        if (CurrentBeat >= 0 && musicClock != null && musicClock.CurrentBar > 0)
        {
            beat = CurrentBeat;
            offset = elapsed - CurrentBeatStartSeconds;
            return true;
        }

        beat = Mathf.FloorToInt(elapsed / beatDuration);
        offset = elapsed - beat * beatDuration;
        return beat >= 0;
    }

    public void NotifyPulseCycle(int cycle)
    {
        if (lastPulseCycle != int.MinValue)
            for (int value = lastPulseCycle + 1; value <= cycle; value++)
                if (value >= 0 && value % 4 == 3) FourthPulsePeak?.Invoke();
        lastPulseCycle = cycle;
    }
}
