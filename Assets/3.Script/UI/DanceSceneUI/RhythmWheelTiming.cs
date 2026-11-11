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
    private int lastPulseCycle = int.MinValue;

    public float BeatDurationSeconds => musicClock != null ? musicClock.BeatDurationSeconds : 60f / bpm;
    public float ElapsedSeconds { get; private set; }
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
        lastPulseCycle = int.MinValue;
    }

    private float ReadElapsedSeconds() => musicClock != null && musicClock.TryGetElapsedSeconds(out float elapsed)
        ? elapsed
        : Time.unscaledTime - fallbackStartTime;

    private void Update()
    {
        ElapsedSeconds = ReadElapsedSeconds();

        int beat = Mathf.FloorToInt(ElapsedSeconds / BeatDurationSeconds);
        for (int value = lastBeat + 1; value <= beat; value++)
        {
            if (value >= 0) BeatStarted?.Invoke(value);
            if (value > 0 && value % 4 == 0) PhraseCompleted?.Invoke();
            if (value >= 0 && value % 4 == 3) FourthBeatStarted?.Invoke();
        }
        lastBeat = beat;
    }

    /// <summary>
    /// Returns the FMOD timeline at the instant an input callback is handled.
    /// Input System timestamps and Unity realtime timestamps are not guaranteed
    /// to share an origin, so extrapolating one from the other can put a press
    /// in the following beat.
    /// </summary>
    public float SampleInputElapsedSeconds()
    {
        float elapsed = ReadElapsedSeconds();
        return Mathf.Max(0f, elapsed);
    }

    public void NotifyPulseCycle(int cycle)
    {
        if (lastPulseCycle != int.MinValue)
            for (int value = lastPulseCycle + 1; value <= cycle; value++)
                if (value >= 0 && value % 4 == 3) FourthPulsePeak?.Invoke();
        lastPulseCycle = cycle;
    }
}
