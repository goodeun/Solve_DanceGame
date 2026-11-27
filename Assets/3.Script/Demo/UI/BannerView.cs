using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 중앙 경고 배너("PHASE CHANGE", "CHANGE NEXT", 승패 표시 등).
/// 이미지가 지정되면 이미지를, 아니면 텍스트 배너를 띄운다. 최종 아트는 DemoUISettings 또는 Inspector에서 교체.
/// </summary>
public class BannerView : MonoBehaviour
{
    [SerializeField] private int sortingOrder = 700;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, 220f);

    private RectTransform root;
    private Image background;
    private Image image;
    private TextMeshProUGUI label;

    public bool IsShowing => root != null && root.gameObject.activeSelf;

    private void EnsureBuilt()
    {
        if (root != null) return;

        Canvas canvas = DemoUI.CreateOverlayCanvas("BannerCanvas", sortingOrder, transform);

        background = DemoUI.CreatePanel("Banner", canvas.transform, new Color(0.05f, 0f, 0.08f, 0.85f));
        root = (RectTransform)background.transform;
        root.anchorMin = new Vector2(0f, 0.5f);
        root.anchorMax = new Vector2(1f, 0.5f);
        root.sizeDelta = new Vector2(0f, 170f);
        root.anchoredPosition = anchoredPosition;

        image = DemoUI.CreatePanel("Image", root, Color.white);
        DemoUI.Place((RectTransform)image.transform, new Vector2(0.5f, 0.5f), new Vector2(900f, 170f), Vector2.zero);
        image.preserveAspect = true;

        label = DemoUI.CreateText("Label", root, "", 110f, TextAlignmentOptions.Center, true);
        DemoUI.Stretch((RectTransform)label.transform);
        label.characterSpacing = 8f;

        root.gameObject.SetActive(false);
    }

    public void Show(string text, Color color, Sprite sprite = null)
    {
        EnsureBuilt();

        root.DOKill();
        root.gameObject.SetActive(true);

        bool useSprite = sprite != null;
        image.gameObject.SetActive(useSprite);
        label.gameObject.SetActive(!useSprite);
        if (useSprite) image.sprite = sprite;

        label.text = text;
        label.color = color;

        root.localScale = new Vector3(1f, 0f, 1f);
        root.DOScaleY(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        label.transform.DOKill();
        label.transform.localScale = Vector3.one;
        label.transform.DOPunchScale(Vector3.one * 0.12f, 0.4f, 6, 0.6f).SetUpdate(true);
    }

    public void Hide()
    {
        if (root == null || !root.gameObject.activeSelf) return;

        root.DOKill();
        root.DOScaleY(0f, 0.15f).SetUpdate(true).OnComplete(() => root.gameObject.SetActive(false));
    }
}
