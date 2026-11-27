using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>옵션 탭 버튼: 키보드로 포커스가 가거나 마우스를 올리면 그 탭을 보여준다.</summary>
public class OptionTabFx : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    private OptionsMenuView view;
    private int index;

    public void Bind(OptionsMenuView owner, int tabIndex)
    {
        view = owner;
        index = tabIndex;
    }

    public void OnSelect(BaseEventData eventData) => view?.OnTabHighlighted(index);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null && !KeyBindings.IsRebinding)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
