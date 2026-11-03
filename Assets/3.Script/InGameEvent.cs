using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameEvent : MonoBehaviour
{
    public static event Action OnLifeToDance;

    public static event Action OnDanceToLife;

    public static void TriggerOnLifeToDance()
    {
        OnLifeToDance.Invoke();
    }

    public static void TriggerOnDanceToLife()
    {
        OnDanceToLife.Invoke();
    }
}
