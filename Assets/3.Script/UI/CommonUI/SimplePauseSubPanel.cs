using System;
using UnityEngine;

/// <summary>단순히 켜고 끄는 Pause 서브 패널(Option, Credit).</summary>
public class SimplePauseSubPanel : MonoBehaviour, IPauseSubPanel
{
    public void PlayOpen(Action onComplete)
    {
        gameObject.SetActive(true);
        onComplete?.Invoke();
    }

    public void PlayClose(Action onComplete)
    {
        gameObject.SetActive(false);
        onComplete?.Invoke();
    }
}
