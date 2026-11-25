using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountDownController : MonoBehaviour
{
    public event Action<int> OnTick;

    public bool isRunning { get; private set; } = false;

    private int count = 3;

    public void StartCountdown(Action onComplete)
    {
        if (isRunning) return;

        StartCoroutine(CountDown_co(onComplete));
    }

    private IEnumerator CountDown_co(Action onComplete)
    {
        isRunning = true;

        for(int i = 0; i< count; i++)
        {
            OnTick?.Invoke(count - i);
            yield return new WaitForSecondsRealtime(1f);
        }

        isRunning = false;
        onComplete?.Invoke();
    }

    private void OnDisable()
    {
        isRunning = false;
    }

}
