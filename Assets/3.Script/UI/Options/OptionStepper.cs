using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// "◀ 값 ▶" 형태의 선택 항목. 키보드 좌우(또는 화살표 클릭)로 값을 바꾸고, 위아래는 다음 항목으로 이동한다.
/// Enter(Submit)도 다음 값으로 넘긴다.
/// </summary>
public class OptionStepper : Selectable, ISubmitHandler, IPointerClickHandler
{
    private TextMeshProUGUI valueText;
    private Func<string[]> options;
    private Func<int> getter;
    private Action<int> setter;

    public void Bind(TextMeshProUGUI value, Func<string[]> optionList, Func<int> get, Action<int> set)
    {
        valueText = value;
        options = optionList;
        getter = get;
        setter = set;
        Refresh();
    }

    public void Refresh()
    {
        if (valueText == null || options == null || getter == null) return;
        string[] list = options();
        int index = Mathf.Clamp(getter(), 0, Mathf.Max(0, list.Length - 1));
        valueText.text = list.Length > 0 ? list[index] : "-";
    }

    public void Step(int direction)
    {
        if (options == null || getter == null || setter == null) return;
        string[] list = options();
        if (list.Length == 0) return;

        int index = (getter() + direction + list.Length) % list.Length;
        setter(index);
        Refresh();

        if (valueText != null)
        {
            valueText.transform.DOKill(true);
            valueText.transform.DOPunchPosition(new Vector3(direction * 10f, 0f, 0f), 0.18f, 6, 0.5f).SetUpdate(true);
        }
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Left) { Step(-1); eventData.Use(); return; }
        if (eventData.moveDir == MoveDirection.Right) { Step(1); eventData.Use(); return; }
        base.OnMove(eventData);
    }

    public void OnSubmit(BaseEventData eventData) => Step(1);

    public void OnPointerClick(PointerEventData eventData)
    {
        // 값 영역의 왼쪽 절반 클릭은 이전, 오른쪽 절반은 다음
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 local);
        Step(local.x < ((RectTransform)transform).rect.width * 0.25f ? -1 : 1);
    }
}
