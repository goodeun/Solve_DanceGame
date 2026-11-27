using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pause 버튼 선택·호버 반응(임시). 버튼이 짧게 튕기고, 캐릭터가 작게 반응한다.
/// 최종 버튼 연출을 직접 만들면 이 컴포넌트를 빼거나 PausePresentation의 Animate Buttons를 끈다.
/// </summary>
public class PauseButtonFx : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private PausePresentation presentation;

    public void Bind(PausePresentation target) => presentation = target;

    public void OnPointerEnter(PointerEventData eventData) => Highlight();
    public void OnSelect(BaseEventData eventData) => Highlight();

    private void Highlight()
    {
        if (presentation == null || !presentation.IsActive) return;

        transform.DOKill(true);
        transform.DOPunchScale(new Vector3(0.18f, 0.18f, 0f), 0.22f, 7, 0.6f).SetUpdate(true);
        transform.DOPunchRotation(new Vector3(0f, 0f, 5f), 0.22f, 7, 0.6f).SetUpdate(true);
        presentation.OnButtonHighlighted((RectTransform)transform);
    }
}
