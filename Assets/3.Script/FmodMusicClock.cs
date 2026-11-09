using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FmodMusicClock : MonoBehaviour
{
    [SerializeField] private string musicEventPath = "event:/RetroVision - Puzzle (2024 Edit) [NCS Release]";
    [SerializeField] private float beatOffsetSeconds;

    private EventInstance musicInstance;

    private void Awake()
    {
        try
        {
            musicInstance = RuntimeManager.CreateInstance(musicEventPath);
            musicInstance.start();
        }
        catch (EventNotFoundException)
        {
            Debug.LogError($"FMOD music event was not found: {musicEventPath}", this);
        }
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

        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
    }
}
