using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PausePanelView : MonoBehaviour
{
    private IEnumerator curAnim;

    public void GameToPauseAnimation(Action onComplete)
    {
        if(curAnim != null) StopCoroutine(curAnim);


        gameObject.SetActive(true);
        curAnim = GameToPause_anim_co(onComplete);

        StartCoroutine(curAnim);
        
    }

    public void SubMenuToPauseAnimation(Action onComplete)
    {
        if (curAnim != null) StopCoroutine(curAnim);

        gameObject.SetActive(true);
        curAnim = SubMenuToPause_anim_co(onComplete);

        StartCoroutine(curAnim);
    }

    public void PauseToGameAnimation(Action onComplete)
    {

        if (curAnim != null) StopCoroutine(curAnim);

        curAnim = PauseToGame_anim_co(onComplete);

        StartCoroutine(curAnim);
    }

    public void PauseToSubmenuAnimation(Action onComplete)
    {

        if (curAnim != null) StopCoroutine(curAnim);

        curAnim = PauseToSubmenu_anim_co(onComplete);

        StartCoroutine(curAnim);
    }

    //------------ 애니메이션 -------------

    private IEnumerator GameToPause_anim_co(Action onComplete)
    {
        yield return null;

        curAnim = null;
        onComplete?.Invoke();
    }

    private IEnumerator SubMenuToPause_anim_co(Action onComplete)
    {
        yield return null;

        curAnim = null;
        onComplete?.Invoke();
    }

    private IEnumerator PauseToGame_anim_co(Action onComplete)
    {
        yield return null;

        gameObject.SetActive(false);

        curAnim = null;
        onComplete?.Invoke();
    }

    private IEnumerator PauseToSubmenu_anim_co(Action onComplete)
    {
        yield return null;

        gameObject.SetActive(false);

        curAnim = null;
        onComplete?.Invoke();
    }


}
