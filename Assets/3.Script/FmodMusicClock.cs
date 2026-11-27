using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FmodMusicClock : MonoBehaviour
{
    [SerializeField] private bool autoStart = true;

    [SerializeField] private PhaseController phase;

    [SerializeField] private string musicEventPath = "event:/RetroVision - Puzzle (2024 Edit) [NCS Release]";
    [SerializeField] private float beatOffsetSeconds;
    [SerializeField, Min(1f)] private float bpm = 128f;

    [Header("Automatic Phase Thresholds")]
    [SerializeField] private bool autoPhaseFromBpm = true;
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
    /// <summary>곡 재생이 끝났을 때(FMOD 재생 상태가 STOPPED가 됨) 한 번 발생.</summary>
    public event Action MusicEnded;

    public bool IsEnded { get; private set; }
    public bool AutoPhaseFromBpm => autoPhaseFromBpm;
    /// <summary>곡이 시작되기 전의 기본 BPM(카운트다운 길이 계산용).</summary>
    public float ConfiguredBpm => bpm;

    private EventInstance musicInstance;
    private readonly ConcurrentQueue<TimelineBeatData> pendingTimelineBeats = new();
    private FMOD.Studio.EVENT_CALLBACK timelineCallback;
    private float currentTempo;

    public bool IsStarted { get; private set; }  = false;
    public bool IsPaused { get; private set; } = false;

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
        }
        catch (EventNotFoundException)
        {
            Debug.LogError($"FMOD music event was not found: {musicEventPath}", this);
        }
    }
    private void Start()
    {
        if(autoStart)
        {
            StartMusic();
        }
    }

    private void Update()
    {
        if (!IsStarted || !musicInstance.isValid() || IsPaused) return;

        if (!IsEnded)
        {
            musicInstance.getPlaybackState(out PLAYBACK_STATE playbackState);
            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                IsEnded = true;
                pendingTimelineBeats.Clear();
                MusicEnded?.Invoke();
                return;
            }
        }

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

            if (autoPhaseFromBpm)
                UpdatePhaseFromBpm(currentBpmDisplay);
        }
    }

    public void StartMusic()
    {
        if (IsStarted || !musicInstance.isValid()) return;

        musicInstance.start();
        IsStarted = true;
        IsPaused = false;
    }

    /// <summary>씬 흐름 관리자가 대사·카운트다운 후 직접 곡을 시작할 때 Awake에서 호출한다.</summary>
    public void DisableAutoStart() => autoStart = false;

    /// <summary>보스전처럼 속성 페이즈를 고정할 때 BPM 기반 자동 전환을 끈다.</summary>
    public void SetAutoPhase(bool enabled) => autoPhaseFromBpm = enabled;

    /// <summary>곡을 처음부터 다시 재생한다(튜토리얼 루프용). 게임 박 번호는 이어서 증가한다.</summary>
    public void RestartMusic()
    {
        if (!musicInstance.isValid()) return;

        musicInstance.start();
        IsStarted = true;
        IsPaused = false;
        IsEnded = false;
    }

    public void StopMusic(bool allowFadeOut = true)
    {
        if (!IsStarted || !musicInstance.isValid()) return;

        musicInstance.stop(allowFadeOut ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    public void SetPause(bool paused)
    {
        if (!IsStarted || !musicInstance.isValid()) return;

        musicInstance.setPaused(paused);
        IsPaused = paused;
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
