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
        // 판정을 하지 않는 연출 구간(곡 시작 전·대사·곡 종료 후)에서는 카운트다운 없이 바로 복귀한다.
        if (!IsGameplayActive())
        {
            onReadyToResume?.Invoke();
            return;
        }

        // 카운트다운은 씬 흐름 컴포넌트가 실행 중에 추가할 수 있으므로 늦게 찾는다.
        if (countDownCon == null) countDownCon = FindAnyObjectByType<CountDownController>();

        if (countDownCon != null)
        {
            countDownCon.StartCountdown(onReadyToResume);
        }
        else
        {
            onReadyToResume?.Invoke();
        }
    }

    private bool IsGameplayActive()
    {
        if (musicClock == null || !musicClock.IsStarted || musicClock.IsEnded) return false;
        if (rhythmWheel != null && rhythmWheel.Judgement != null && rhythmWheel.Judgement.IsSuspended) return false;
        return true;
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
