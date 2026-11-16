using UnityEngine;

public sealed class RhythmClock : MonoBehaviour
{
    [SerializeField, Min(1)] private int bpm = 120;
    [SerializeField] private FmodMusicClock musicClock;
    public float BeatDurationSeconds => musicClock != null ? musicClock.BeatDurationSeconds : 60f / bpm;
    public event System.Action<int> OnBeatStarted;
    private float fallbackStart;
    private int lastBeat = -1;
    private int fmodBeatSequence = -1;
    private void Awake() { fallbackStart = Time.unscaledTime; if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>(); }
    private void OnEnable() { fmodBeatSequence = -1; if (musicClock != null) musicClock.TimelineBeat += HandleTimelineBeat; }
    private void OnDisable() { if (musicClock != null) musicClock.TimelineBeat -= HandleTimelineBeat; }
    private void Update()
    {
        if (musicClock != null && musicClock.CurrentBar > 0) return;
        float elapsed = musicClock != null && musicClock.TryGetElapsedSeconds(out float value) ? value : Time.unscaledTime - fallbackStart;
        DispatchBeat(Mathf.FloorToInt(elapsed / BeatDurationSeconds));
    }

    private void HandleTimelineBeat(int bar, int beatInBar, float tempo, int upper, int lower)
        => DispatchBeat(++fmodBeatSequence);

    private void DispatchBeat(int beat)
    {
        for (int i = lastBeat + 1; i <= beat; i++) if (i >= 0) OnBeatStarted?.Invoke(i);
        lastBeat = beat;
    }
}
