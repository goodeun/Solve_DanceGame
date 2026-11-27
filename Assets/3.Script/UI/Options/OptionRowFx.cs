using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>옵션 항목 선택 표시(왼쪽 강조 막대 + 배경 + 살짝 밀림). 키보드·마우스 모두.</summary>
public class OptionRowFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    private Image background;
    private RectTransform marker;
    private RectTransform content;
    private Color idleColor;
    private Color selectedColor;

    public void Bind(Image rowBackground, RectTransform selectionMarker, RectTransform shiftContent, Color idle, Color selected)
    {
        background = rowBackground;
        marker = selectionMarker;
        content = shiftContent;
        idleColor = idle;
        selectedColor = selected;
        SetSelected(false, true);
    }

    public void OnSelect(BaseEventData eventData) => SetSelected(true, false);
    public void OnDeselect(BaseEventData eventData) => SetSelected(false, false);

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스를 올리면 키보드 선택도 그 항목으로 옮긴다.
        if (EventSystem.current != null && !KeyBindings.IsRebinding)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    private void SetSelected(bool selected, bool instant)
    {
        if (background != null)
        {
            background.DOKill();
            if (instant) background.color = selected ? selectedColor : idleColor;
            else background.DOColor(selected ? selectedColor : idleColor, 0.12f).SetUpdate(true);
        }

        if (marker != null)
        {
            marker.DOKill();
            Vector3 scale = new Vector3(selected ? 1f : 0f, 1f, 1f);
            if (instant) marker.localScale = scale;
            else marker.DOScale(scale, 0.14f).SetEase(selected ? Ease.OutBack : Ease.InQuad).SetUpdate(true);
        }

        if (content != null && !instant)
        {
            content.DOKill();
            content.DOAnchorPosX(selected ? 14f : 0f, 0.14f).SetEase(Ease.OutCubic).SetUpdate(true);
        }
    }
}
