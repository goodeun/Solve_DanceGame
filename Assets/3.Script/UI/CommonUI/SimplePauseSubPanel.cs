using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pause 서브 패널(Option, Credit) 열기·닫기 연출(임시).
/// 열기: 오른쪽에서 비스듬히 밀려 들어와 정착. 닫기: 오른쪽으로 빠르게 빠짐.
/// 열린 뒤 패널 안의 첫 선택 가능한 항목에 키보드 포커스를 준다(OptionsMenuView는 자체적으로 탭에 포커스).
/// </summary>
public class SimplePauseSubPanel : MonoBehaviour, IPauseSubPanel
{
    [SerializeField] private float openDuration = 0.26f;
    [SerializeField] private float closeDuration = 0.16f;
    [SerializeField] private float slideDistance = 700f;

    private RectTransform Rect => (RectTransform)transform;

    private CanvasGroup Group
    {
        get
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            return group;
        }
    }

    public void PlayOpen(Action onComplete)
    {
        gameObject.SetActive(true);
        RectTransform rect = Rect;
        CanvasGroup group = Group;
        rect.DOKill();
        group.DOKill();

        rect.anchoredPosition = new Vector2(slideDistance, -60f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -6f);
        group.alpha = 0f;

        DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .Join(rect.DOAnchorPos(Vector2.zero, openDuration).SetEase(Ease.OutBack, 1.2f))
            .Join(rect.DOLocalRotate(Vector3.zero, openDuration).SetEase(Ease.OutBack))
            .Join(group.DOFade(1f, openDuration * 0.6f))
            .OnComplete(() =>
            {
                if (GetComponent<OptionsMenuView>() == null)
                {
                    Selectable first = GetComponentInChildren<Selectable>();
                    if (first != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
                }
                onComplete?.Invoke();
            });
    }

    public void PlayClose(Action onComplete)
    {
        RectTransform rect = Rect;
        CanvasGroup group = Group;
        rect.DOKill();
        group.DOKill();

        DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .Join(rect.DOAnchorPos(new Vector2(slideDistance, 40f), closeDuration).SetEase(Ease.InCubic))
            .Join(rect.DOLocalRotate(new Vector3(0f, 0f, 5f), closeDuration))
            .Join(group.DOFade(0f, closeDuration))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.identity;
                group.alpha = 1f;
                onComplete?.Invoke();
            });
    }
}
