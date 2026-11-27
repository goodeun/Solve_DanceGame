using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NegativeBubble : MonoBehaviour
{
    [SerializeField] private Image bubble_img;

    [SerializeField] private TextMeshProUGUI text;
    private RectTransform rectTransform;
    private Vector2 originPos;
    private Vector2 startPos;
    [SerializeField] private float offsetY = -40f;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        bubble_img = GetComponentInChildren<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();

        originPos = rectTransform != null ? rectTransform.anchoredPosition : (Vector2)transform.localPosition;

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        transform.DOKill();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPos;
            rectTransform.DOAnchorPos(originPos, 0.2f);
        }
        else
        {
            transform.localPosition = startPos;
            transform.DOLocalMove(originPos, 0.2f);
        }
    }

    public void DisableBubble()
    {
        // TODO 꺼지는 애니메이션
        gameObject.SetActive(false);
    }

    public void Initialize(Vector2 offsetPos)
    {
        startPos = originPos + offsetPos;
    }

    public void MoveAnchore(Stage stage) // 3, 4 번 말풍선만 해당
    {
        if (stage == Stage.Rhombus)
        {
            originPos.y = 0;
            startPos.y = 0;
        }
        else
        {
            originPos.y = offsetY;
            startPos.y = offsetY;
        }

        transform.DOKill();
        if (!gameObject.activeSelf) return;

        if (rectTransform != null)
            rectTransform.DOAnchorPos(originPos, 0.2f).SetEase(Ease.OutBack);
        else
            transform.DOLocalMove(originPos, 0.2f).SetEase(Ease.OutBack);
    }
}
