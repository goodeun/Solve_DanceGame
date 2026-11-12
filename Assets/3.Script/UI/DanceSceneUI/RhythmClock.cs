using UnityEngine;

public sealed class RhythmClock : MonoBehaviour
{
    [SerializeField, Min(1)] private int bpm = 120;
    [SerializeField] private FmodMusicClock musicClock;
    public float BeatDurationSeconds => 60f / bpm;
    public event System.Action<int> OnBeatStarted;
    private float fallbackStart;
    private int lastBeat = -1;
    private void Awake() { fallbackStart = Time.unscaledTime; if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>(); }
    private void Update() { float elapsed = musicClock != null && musicClock.TryGetElapsedSeconds(out float value) ? value : Time.unscaledTime - fallbackStart; int beat = Mathf.FloorToInt(elapsed / BeatDurationSeconds); for (int i = lastBeat + 1; i <= beat; i++) if (i >= 0) OnBeatStarted?.Invoke(i); lastBeat = beat; }
}
