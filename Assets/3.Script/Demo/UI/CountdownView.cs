using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// CountDownController.OnTick을 화면에 표시한다(0 = 숨김).
/// 독립 Overlay Canvas에 그리므로 Pause로 MasterCanvas가 숨겨져도 보인다.
/// numberText를 지정하지 않으면 실행 시 기본 UI를 만든다.
/// </summary>
public class CountdownView : MonoBehaviour
{
    [SerializeField] private CountDownController controller;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private int sortingOrder = 800;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CountDownController>();
        if (controller == null) controller = FindFirstObjectByType<CountDownController>();
        if (numberText == null)
        {
            Canvas canvas = DemoUI.CreateOverlayCanvas("CountdownCanvas", sortingOrder, transform);
            numberText = DemoUI.CreateText("Number", canvas.transform, "", 260f, TextAlignmentOptions.Center, true);
            DemoUI.Place((RectTransform)numberText.transform, new Vector2(0.5f, 0.5f), new Vector2(600f, 400f), Vector2.zero);
            numberText.color = DemoUI.Settings.green;
            numberText.outlineWidth = 0.25f;
            numberText.outlineColor = new Color32(40, 0, 60, 255);
        }

        numberText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (controller != null) controller.OnTick += Show;
    }

    private void OnDisable()
    {
        if (controller != null) controller.OnTick -= Show;
    }

    private void Show(int number)
    {
        if (numberText == null) return;

        numberText.transform.DOKill();

        if (number <= 0)
        {
            numberText.gameObject.SetActive(false);
            return;
        }

        numberText.gameObject.SetActive(true);
        numberText.text = number.ToString();
        numberText.transform.localScale = Vector3.one * 1.6f;
        numberText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
    }
}
