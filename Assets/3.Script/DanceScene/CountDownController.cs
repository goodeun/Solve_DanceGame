using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공통 3 → 2 → 1 카운트다운. 숫자당 1박.
/// - StartCountdown: 음악이 멈춰 박 콜백이 없을 때(Pause 복귀, 곡 시작 전) 현재 BPM으로 1박 길이를 계산해 실시간으로 센다.
/// - ShowTick / Hide: 일반전 좌석 변경처럼 FMOD 박 콜백에 맞춰 외부에서 숫자를 직접 띄운다.
/// 표시는 OnTick을 구독하는 CountdownView가 담당한다(0 = 숨김).
/// </summary>
public class CountDownController : MonoBehaviour
{
    public event Action<int> OnTick;

    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField, Min(1)] private int count = 3;
    [Tooltip("음악 시계가 없을 때 사용할 1박 길이(초)")]
    [SerializeField, Min(0.05f)] private float fallbackBeatSeconds = 0.5f;

    public bool isRunning { get; private set; } = false;
    public int Count => count;

    private Coroutine routine;
    private PauseManager pauseManager;

    private void Awake()
    {
        if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>();
        pauseManager = FindFirstObjectByType<PauseManager>();

        if (FindFirstObjectByType<CountdownView>() == null)
            gameObject.AddComponent<CountdownView>();
    }

    public float BeatSeconds => musicClock != null ? musicClock.BeatDurationSeconds : fallbackBeatSeconds;

    /// <summary>Pause 복귀용: 일시정지 상태에서도(실시간) 숫자당 1박으로 센다.</summary>
    public void StartCountdown(Action onComplete) => StartCountdown(onComplete, false);

    /// <param name="waitWhilePaused">true면 Pause 메뉴가 열린 동안 카운트를 멈춘다(곡 시작 전 카운트다운용).</param>
    public void StartCountdown(Action onComplete, bool waitWhilePaused)
    {
        if (isRunning) return;

        routine = StartCoroutine(CountDown_co(onComplete, waitWhilePaused));
    }

    private IEnumerator CountDown_co(Action onComplete, bool waitWhilePaused)
    {
        isRunning = true;
        float beat = BeatSeconds;

        for(int i = 0; i< count; i++)
        {
            OnTick?.Invoke(count - i);

            float elapsed = 0f;
            while (elapsed < beat)
            {
                yield return null;
                if (waitWhilePaused && pauseManager != null && pauseManager.isPauseActive)
                    continue;
                elapsed += Time.unscaledDeltaTime;
            }
        }

        OnTick?.Invoke(0);
        isRunning = false;
        routine = null;
        onComplete?.Invoke();
    }

    /// <summary>박 콜백 기반 카운트다운 표시. 0이면 숨긴다.</summary>
    public void ShowTick(int number) => OnTick?.Invoke(number);

    public void Hide() => OnTick?.Invoke(0);

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        isRunning = false;
    }

}
