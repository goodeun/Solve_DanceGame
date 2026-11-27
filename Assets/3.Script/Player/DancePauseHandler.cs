using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DancePauseHandler : MonoBehaviour, IPauseSceneHandler
{
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [SerializeField] private CountDownController countDownCon;
    [SerializeField] private HUDManager hudManager;

    private bool isMasterCanvasHiddenForPause;

    private void Awake()
    {
        if (musicClock == null) musicClock = FindAnyObjectByType<FmodMusicClock>();
        if (rhythmWheel == null) rhythmWheel = FindAnyObjectByType<RhythmWheelUI>();
        if (countDownCon == null) countDownCon = FindAnyObjectByType<CountDownController>();
        if (hudManager == null) hudManager = FindAnyObjectByType<HUDManager>();
    }
    public void OnPauseEntered()
    {
        if (musicClock !=null && rhythmWheel != null)
        {
            musicClock.SetPause(true);
            rhythmWheel.SetInputLocked(true);
        }
        Time.timeScale = 0f;

        if (hudManager == null) hudManager = HUDManager.Instance;
        if (hudManager != null)
        {
            hudManager.SetPauseHidden(true);
            isMasterCanvasHiddenForPause = true;
        }
    }

    public void OnResumeCompleted()
    {
        RestoreMasterCanvas();
        Time.timeScale = 1f;
        if(rhythmWheel !=null && musicClock != null)
        {
            rhythmWheel.SetInputLocked(false);
            musicClock.SetPause(false);
        }
    }

    public void OnResumeRequested(Action onReadyToResume)
    {
        if (countDownCon != null)
        {
            countDownCon.StartCountdown(onReadyToResume);
        }
        else
        {
            onReadyToResume?.Invoke();
        }
    }

    private void OnDisable()
    {
        RestoreMasterCanvas();
    }

    private void RestoreMasterCanvas()
    {
        if (!isMasterCanvasHiddenForPause) return;

        if (hudManager != null) hudManager.SetPauseHidden(false);
        isMasterCanvasHiddenForPause = false;
    }
}
