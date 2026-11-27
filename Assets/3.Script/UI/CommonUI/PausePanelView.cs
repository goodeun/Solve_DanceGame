using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Pause 메뉴 패널 전환 연출. 네 가지 전환은 서로 다른 동작이다(임시 연출, 리소스 교체 시 수정).
/// - GameToPause   : 게임 → Pause. 버튼 등장은 PausePresentation이 맡고(가운데에서 튀어나옴), 여기서는 첫 버튼에 포커스만 준다.
/// - PauseToGame   : Pause → 게임. 버튼이 아래에서부터 접히듯 작아지며 사라진다.
/// - PauseToSubmenu: Pause → 서브 메뉴. 버튼이 위에서부터 오른쪽으로 쓸려 나간다.
/// - SubMenuToPause: 서브 메뉴 → Pause. 버튼이 왼쪽에서 차례로 미끄러져 돌아오고, 들어가기 전 버튼에 포커스를 되돌린다.
/// 모든 트윈은 시간 정지 중에도 돌도록 SetUpdate(true).
/// </summary>
public class PausePanelView : MonoBehaviour
{
    [Header("Timing (초, 실시간)")]
    [SerializeField] private float collapseDuration = 0.14f;
    [SerializeField] private float swipeOutDuration = 0.18f;
    [SerializeField] private float slideInDuration = 0.24f;
    [SerializeField] private float stagger = 0.035f;
    [SerializeField] private float swipeDistance = 520f;

    private IEnumerator curAnim;
    private GameObject lastSelectedButton;

    private List<RectTransform> Buttons => PauseButtonLayout.Collect(transform);

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

        // 돌아올 때 포커스를 되돌릴 버튼
        lastSelectedButton = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        curAnim = PauseToSubmenu_anim_co(onComplete);

        StartCoroutine(curAnim);
    }

    //------------ 애니메이션 -------------

    private IEnumerator GameToPause_anim_co(Action onComplete)
    {
        yield return null;

        // 키보드로 바로 조작할 수 있게 첫 버튼(Resume)에 포커스
        List<RectTransform> buttons = Buttons;
        if (buttons.Count > 0) Select(buttons[0].gameObject);

        curAnim = null;
        onComplete?.Invoke();
    }

    private IEnumerator SubMenuToPause_anim_co(Action onComplete)
    {
        List<RectTransform> buttons = Buttons;
        for (int i = 0; i < buttons.Count; i++)
        {
            RectTransform b = buttons[i];
            Vector2 origin = PauseButtonLayout.Origin(b);
            CanvasGroup group = Group(b);
            b.DOKill();
            group.DOKill();

            b.anchoredPosition = origin + new Vector2(-swipeDistance, 0f);
            b.localScale = Vector3.one;
            b.localRotation = Quaternion.identity;
            group.alpha = 0f;

            float delay = i * stagger;
            b.DOAnchorPos(origin, slideInDuration).SetDelay(delay).SetEase(Ease.OutQuart).SetUpdate(true);
            group.DOFade(1f, slideInDuration * 0.6f).SetDelay(delay).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(slideInDuration + stagger * Mathf.Max(0, buttons.Count - 1));

        GameObject target = lastSelectedButton != null && lastSelectedButton.activeInHierarchy
            ? lastSelectedButton
            : (buttons.Count > 0 ? buttons[0].gameObject : null);
        Select(target);

        curAnim = null;
        onComplete?.Invoke();
    }

    private IEnumerator PauseToGame_anim_co(Action onComplete)
    {
        List<RectTransform> buttons = Buttons;
        for (int i = 0; i < buttons.Count; i++)
        {
            RectTransform b = buttons[buttons.Count - 1 - i];
            b.DOKill();
            b.DOScale(new Vector3(1.2f, 0f, 1f), collapseDuration).SetDelay(i * stagger * 0.5f).SetEase(Ease.InBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(collapseDuration + stagger * 0.5f * Mathf.Max(0, buttons.Count - 1));

        gameObject.SetActive(false);
        Restore(buttons);

        curAnim = null;
        onComplete?.Invoke();
    }

    private IEnumerator PauseToSubmenu_anim_co(Action onComplete)
    {
        List<RectTransform> buttons = Buttons;
        for (int i = 0; i < buttons.Count; i++)
        {
            RectTransform b = buttons[i];
            CanvasGroup group = Group(b);
            b.DOKill();
            group.DOKill();

            float delay = i * stagger;
            b.DOAnchorPos(PauseButtonLayout.Origin(b) + new Vector2(swipeDistance, -40f), swipeOutDuration).SetDelay(delay).SetEase(Ease.InBack).SetUpdate(true);
            b.DOLocalRotate(new Vector3(0f, 0f, -12f), swipeOutDuration).SetDelay(delay).SetUpdate(true);
            group.DOFade(0f, swipeOutDuration).SetDelay(delay).SetEase(Ease.InQuad).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(swipeOutDuration + stagger * Mathf.Max(0, buttons.Count - 1));

        gameObject.SetActive(false);
        Restore(buttons);

        curAnim = null;
        onComplete?.Invoke();
    }

    //------------ 도우미 -------------

    private static CanvasGroup Group(RectTransform b)
    {
        CanvasGroup group = b.GetComponent<CanvasGroup>();
        if (group == null) group = b.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    private static void Restore(List<RectTransform> buttons)
    {
        foreach (RectTransform b in buttons)
        {
            b.DOKill();
            b.anchoredPosition = PauseButtonLayout.Origin(b);
            b.localScale = Vector3.one;
            b.localRotation = Quaternion.identity;
            CanvasGroup group = b.GetComponent<CanvasGroup>();
            if (group != null) { group.DOKill(); group.alpha = 1f; }
        }
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current != null && target != null)
            EventSystem.current.SetSelectedGameObject(target);
    }
}
