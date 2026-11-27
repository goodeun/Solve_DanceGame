using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 데모 흐름용 UI를 코드로 만드는 도우미.
/// 프리팹 없이도 동작하도록 Canvas·텍스트·패널·버튼을 생성한다. 아트가 확정되면 각 View의 참조를 교체하면 된다.
/// </summary>
public static class DemoUI
{
    private static DemoUISettings settings;

    public static DemoUISettings Settings
    {
        get
        {
            if (settings == null)
                settings = Resources.Load<DemoUISettings>("DemoUISettings");
            if (settings == null)
                settings = ScriptableObject.CreateInstance<DemoUISettings>();
            return settings;
        }
    }

    private static TMP_FontAsset dynamicBodyFont;

    public static TMP_FontAsset BodyFont
    {
        get
        {
            if (Settings.bodySourceFont != null)
            {
                // TMP 폰트 에셋이 없는 폰트 파일(Pretendard 등)은 실행 중 동적 폰트로 만들어 쓴다.
                if (dynamicBodyFont == null)
                    dynamicBodyFont = TMP_FontAsset.CreateFontAsset(Settings.bodySourceFont);
                if (dynamicBodyFont != null) return dynamicBodyFont;
            }
            return Settings.bodyFont != null ? Settings.bodyFont : TMP_Settings.defaultFontAsset;
        }
    }
    public static TMP_FontAsset TitleFont => Settings.titleFont != null ? Settings.titleFont : BodyFont;

    public static Canvas CreateOverlayCanvas(string name, int sortingOrder, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 일시정지·피버로 MasterCanvas가 숨겨져도 독립 Canvas는 계속 보인다.
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null || UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static RectTransform Stretch(RectTransform rect, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    public static Image CreatePanel(string name, Transform parent, Color color, Sprite sprite = null)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool title = false)
    {
        var rect = CreateRect(name, parent);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = title ? TitleFont : BodyFont;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = Settings.text;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Button CreateButton(string name, Transform parent, string label, Vector2 size, Action onClick)
    {
        var image = CreatePanel(name, parent, Settings.panel);
        var rect = (RectTransform)image.transform;
        rect.sizeDelta = size;

        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = Settings.magenta;
        outline.effectDistance = new Vector2(3f, -3f);

        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.6f, 0.9f);
        colors.pressedColor = Settings.magenta;
        colors.selectedColor = new Color(1f, 0.75f, 0.95f);
        button.colors = colors;
        if (onClick != null) button.onClick.AddListener(() => onClick());

        var text = CreateText("Label", rect, label, 40f);
        Stretch((RectTransform)text.transform);
        return button;
    }

    /// <summary>확인/취소 팝업. 확인 시 onConfirm, 취소 시 onCancel 후 팝업을 닫는다.</summary>
    public static GameObject ShowConfirmPopup(string message, string confirmLabel, Action onConfirm, string cancelLabel = null, Action onCancel = null, int sortingOrder = 900)
    {
        EnsureEventSystem();
        Canvas canvas = CreateOverlayCanvas("ConfirmPopup", sortingOrder);
        var root = canvas.gameObject;

        var dim = CreatePanel("Dim", canvas.transform, new Color(0f, 0f, 0f, 0.6f));
        dim.raycastTarget = true;
        Stretch((RectTransform)dim.transform);

        var box = CreatePanel("Box", canvas.transform, Settings.panel);
        Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(1000f, 460f), Vector2.zero);
        var boxOutline = box.gameObject.AddComponent<Outline>();
        boxOutline.effectColor = Settings.cyan;
        boxOutline.effectDistance = new Vector2(4f, -4f);

        var msg = CreateText("Message", box.transform, message, 44f);
        Stretch((RectTransform)msg.transform, 60f, 60f, 50f, 170f);

        bool hasCancel = !string.IsNullOrEmpty(cancelLabel);
        var confirm = CreateButton("Confirm", box.transform, confirmLabel, new Vector2(320f, 100f), () =>
        {
            UnityEngine.Object.Destroy(root);
            onConfirm?.Invoke();
        });
        Place((RectTransform)confirm.transform, new Vector2(0.5f, 0f), new Vector2(320f, 100f), new Vector2(hasCancel ? -190f : 0f, 90f));

        if (hasCancel)
        {
            var cancel = CreateButton("Cancel", box.transform, cancelLabel, new Vector2(320f, 100f), () =>
            {
                UnityEngine.Object.Destroy(root);
                onCancel?.Invoke();
            });
            Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(320f, 100f), new Vector2(190f, 90f));
        }

        EventSystem.current?.SetSelectedGameObject(confirm.gameObject);
        return root;
    }
}
