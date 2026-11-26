using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPauseSubPanel
{
    void PlayOpen(Action onComplete);
    void PlayClose(Action onComplete);
}
