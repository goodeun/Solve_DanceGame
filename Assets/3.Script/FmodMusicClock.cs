using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FmodMusicClock : MonoBehaviour
{
    [SerializeField] private PhaseController phase;

    [SerializeField] private string musicEventPath = "event:/RetroVision - Puzzle (2024 Edit) [NCS Release]";
    [SerializeField] private float beatOffsetSeconds;
    [SerializeField, Min(1f)] private float bpm = 128f;

    [Header("Automatic Phase Thresholds")]
    [SerializeField, Min(0f)] private float technoEnterBpm = 120f;
    [SerializeField, Min(0f)] private float technoExitBpm = 116f;
    [SerializeField, Min(0f)] private float popEnterBpm = 80f;
    [SerializeField, Min(0f)] private float popExitBpm = 76f;

    [Header("Runtime (Play Mode)")]
    [SerializeField] private float currentBpmDisplay;
    [SerializeField] private int currentBarDisplay;
    [SerializeField] private int currentBeatDisplay;

    /// <summary>Tempo reported by FMOD's current timeline tempo marker.</summary>
    public float BPM => currentTempo > 0f ? currentTempo : bpm;
    public float BeatDurationSeconds => 60f / BPM;
    public int CurrentBar { get; private set; }
    public int CurrentBeat { get; private set; }
    public float CurrentBeatStartSeconds { get; private set; }
    public int TimeSignatureUpper { get; private set; } = 4;
    public int TimeSignatureLower { get; private set; } = 4;

    /// <summary>Raised on Unity's main thread for every FMOD timeline beat.</summary>
    public event Action<int, int, float, int, int> TimelineBeat;

    private EventInstance musicInstance;
    private readonly ConcurrentQueue<TimelineBeatData> pendingTimelineBeats = new();
    private FMOD.Studio.EVENT_CALLBACK timelineCallback;
    private float currentTempo;

    private readonly struct TimelineBeatData
    {
        public readonly int Bar;
        public readonly int Beat;
        public readonly float Tempo;
        public readonly int Upper;
        public readonly int Lower;
        public readonly float PositionSeconds;

        public TimelineBeatData(TIMELINE_BEAT_PROPERTIES properties)
        {
            Bar = properties.bar;
            Beat = properties.beat;
            Tempo = properties.tempo;
            Upper = properties.timesignatureupper;
            Lower = properties.timesignaturelower;
            PositionSeconds = properties.position / 1000f;
        }
    }

    private void Awake()
    {
        try
        {
            musicInstance = RuntimeManager.CreateInstance(musicEventPath);
            timelineCallback = HandleTimelineCallback;
            musicInstance.setCallback(timelineCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
            musicInstance.start();
        }
        catch (EventNotFoundException)
        {
            Debug.LogError($"FMOD music event was not found: {musicEventPath}", this);
        }
    }

    private void Update()
    {
        while (pendingTimelineBeats.TryDequeue(out TimelineBeatData data))
        {
            CurrentBar = data.Bar;
            CurrentBeat = data.Beat;
            CurrentBeatStartSeconds = data.PositionSeconds + beatOffsetSeconds;
            currentTempo = data.Tempo;
            currentBpmDisplay = currentTempo > 0f ? currentTempo : bpm;
            currentBarDisplay = data.Bar;
            currentBeatDisplay = data.Beat;
            TimeSignatureUpper = data.Upper;
            TimeSignatureLower = data.Lower;
            TimelineBeat?.Invoke(data.Bar, data.Beat, data.Tempo, data.Upper, data.Lower);

            UpdatePhaseFromBpm(currentBpmDisplay);
        }
    }

    private void UpdatePhaseFromBpm(float currentBpm)
    {
        if (phase == null || currentBpm <= 0f)
            return;

        Phase targetPhase = phase.CurrentPhase switch
        {
            Phase.Techno when currentBpm >= technoExitBpm => Phase.Techno,
            Phase.Techno when currentBpm >= popExitBpm => Phase.Pop,
            Phase.Pop when currentBpm > technoEnterBpm => Phase.Techno,
            Phase.Pop when currentBpm >= popEnterBpm => Phase.Pop,
            Phase.HipHop when currentBpm > popEnterBpm => Phase.Pop,
            _ => Phase.HipHop
        };

        if (targetPhase != phase.CurrentPhase)
            phase.SetPhase(targetPhase);
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    private FMOD.RESULT HandleTimelineCallback(EVENT_CALLBACK_TYPE type, IntPtr instance, IntPtr parameters)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
        {
            TIMELINE_BEAT_PROPERTIES properties = Marshal.PtrToStructure<TIMELINE_BEAT_PROPERTIES>(parameters);
            pendingTimelineBeats.Enqueue(new TimelineBeatData(properties));
        }

        return FMOD.RESULT.OK;
    }

    public bool TryGetElapsedSeconds(out float elapsedSeconds)
    {
        elapsedSeconds = 0f;
        if (!musicInstance.isValid())
            return false;

        musicInstance.getPlaybackState(out PLAYBACK_STATE state);
        if (state != PLAYBACK_STATE.PLAYING)
            return false;

        musicInstance.getTimelinePosition(out int positionMilliseconds);
        elapsedSeconds = positionMilliseconds / 1000f + beatOffsetSeconds;
        return true;
    }

    private void OnDestroy()
    {
        if (!musicInstance.isValid())
            return;

        // FMOD can otherwise retain a native callback while its Studio system
        // is being released during Play Mode shutdown.
        musicInstance.setCallback(null, 0);
        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
        timelineCallback = null;
    }
}
