using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPauseSceneHandler
{
    void OnPauseEntered();
    void OnResumeRequested(Action onReadyToResume);
    void OnResumeCompleted();
}
