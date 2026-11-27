using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 커맨드(AAB·ABA·ABC) 성공 타격감 연출(임시).
/// 리듬 휠 중앙에서 번쩍임 + 충격파 링 + 별 + 방사형 불꽃, 커맨드 이름이 팡 튀어나오고,
/// 마지막 키 객석 위치에도 작은 버스트가 터진다. 리듬 휠도 살짝 튕긴다.
/// 세 박 모두 Just면 더 크고 화려하게. 색·크기·문구는 Inspector에서 조정.
/// ComboAndMagnification이 자동으로 붙인다.
/// </summary>
public class CommandSuccessFx : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private string aabLabel = "TACKLE BREAK!";
    [SerializeField] private string abaLabel = "SWING!";
    [SerializeField] private string abcLabel = "CHAIN!";
    [SerializeField] private string perfectSuffix = "\n<size=60%>PERFECT</size>";

    [Header("Colors")]
    [SerializeField] private Color aabColor = new Color(0.75f, 1f, 0.2f, 1f);
    [SerializeField] private Color abaColor = new Color(0.25f, 0.9f, 1f, 1f);
    [SerializeField] private Color abcColor = new Color(1f, 0.18f, 0.78f, 1f);

    [Header("Size")]
    [SerializeField, Min(0.1f)] private float scale = 1f;
    [SerializeField] private Vector2 labelOffset = new Vector2(0f, 190f);
    [SerializeField, Range(4, 24)] private int sparkCount = 12;
    [SerializeField] private bool seatBurst = true;
    [SerializeField] private bool punchWheel = true;
    [Tooltip("AAB(태클 처리)는 태클 말풍선의 액체(MetaBall) 연출이 터지므로 여기서는 글자와 휠 튕김만")]
    [SerializeField] private bool aabLabelOnly = true;

    private ComboAndMagnification combo;
    private RhythmWheelUI wheel;
    private RectTransform layer;

    private static Sprite softCircle;
    private static Sprite ring;
    private static Sprite star;

    private void Awake()
    {
        combo = GetComponent<ComboAndMagnification>();
        if (combo == null) combo = FindFirstObjectByType<ComboAndMagnification>();
        wheel = FindFirstObjectByType<RhythmWheelUI>();
    }

    private void OnEnable() { if (combo != null) combo.OnPhraseHandoff += HandlePhrase; }
    private void OnDisable() { if (combo != null) combo.OnPhraseHandoff -= HandlePhrase; }

    private void HandlePhrase(Keys lastKey, int just, int good, int bad, int miss, KeyCommand command)
    {
        if (command == KeyCommand.None || miss > 0) return;
        Play(command, lastKey, just >= 3);
    }

    // ================= Play =================

    public void Play(KeyCommand command, Keys lastKey, bool perfect)
    {
        EnsureLayer();

        Color color = command == KeyCommand.AAB ? aabColor : command == KeyCommand.ABA ? abaColor : abcColor;
        string label = command == KeyCommand.AAB ? aabLabel : command == KeyCommand.ABA ? abaLabel : abcLabel;
        float power = (perfect ? 1.35f : 1f) * scale;

        Vector2 wheelPos = WheelPosition();
        bool labelOnly = aabLabelOnly && command == KeyCommand.AAB;
        if (!labelOnly) Burst(wheelPos, color, power, true);
        Label(wheelPos + labelOffset, perfect ? label + perfectSuffix : label, color, power);

        if (!labelOnly && seatBurst && TrySeatPosition(lastKey, out Vector2 seatPos))
            Burst(seatPos, color, power * 0.6f, false);

        if (punchWheel && wheel != null)
        {
            wheel.transform.DOKill(true);
            wheel.transform.DOPunchScale(Vector3.one * 0.08f * power, 0.25f, 7, 0.6f);
        }
    }

    private void Burst(Vector2 position, Color color, float power, bool withStar)
    {
        var root = DemoUI.CreateRect("Burst", layer);
        root.anchoredPosition = position;
        var seq = DOTween.Sequence().SetLink(root.gameObject).OnComplete(() => Destroy(root.gameObject));

        // 하얀 번쩍임
        var flash = Img("Flash", root, softCircle, new Color(1f, 1f, 1f, 0.95f), 260f * power);
        flash.transform.localScale = Vector3.one * 0.2f;
        seq.Insert(0f, flash.transform.DOScale(1.3f, 0.22f).SetEase(Ease.OutExpo));
        seq.Insert(0.05f, flash.DOFade(0f, 0.22f));

        // 충격파 링
        var shock = Img("Ring", root, ring, color, 300f * power);
        shock.transform.localScale = Vector3.one * 0.3f;
        seq.Insert(0f, shock.transform.DOScale(1.9f, 0.42f).SetEase(Ease.OutCubic));
        seq.Insert(0.1f, shock.DOFade(0f, 0.32f));

        // 반짝이는 별
        if (withStar)
        {
            var s = Img("Star", root, star, Color.white, 230f * power);
            s.transform.localScale = Vector3.zero;
            s.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            seq.Insert(0.02f, s.transform.DOScale(1f, 0.16f).SetEase(Ease.OutBack, 3f));
            seq.Insert(0.02f, s.transform.DOLocalRotate(new Vector3(0f, 0f, 20f), 0.45f).SetEase(Ease.OutCubic));
            seq.Insert(0.22f, s.transform.DOScale(0f, 0.22f).SetEase(Ease.InBack));
        }

        // 방사형 불꽃
        int count = Mathf.Max(4, Mathf.RoundToInt(sparkCount * (withStar ? 1f : 0.6f)));
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-10f, 10f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var spark = Img("Spark", root, null, i % 3 == 0 ? Color.white : color, 0f);
            var rect = spark.rectTransform;
            rect.sizeDelta = new Vector2(7f * power, Random.Range(46f, 74f) * power);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            rect.anchoredPosition = dir * 36f * power;
            float distance = Random.Range(150f, 230f) * power;
            float t = Random.Range(0.28f, 0.4f);
            seq.Insert(0f, rect.DOAnchorPos(dir * distance, t).SetEase(Ease.OutQuart));
            seq.Insert(0f, rect.DOScaleY(0f, t).SetEase(Ease.InQuad));
            seq.Insert(t * 0.4f, spark.DOFade(0f, t * 0.6f));
        }
    }

    private void Label(Vector2 position, string text, Color color, float power)
    {
        var label = DemoUI.CreateText("CommandLabel", layer, text, 70f * power, TextAlignmentOptions.Center, true);
        var rect = (RectTransform)label.transform;
        rect.sizeDelta = new Vector2(900f, 220f);
        rect.anchoredPosition = position;
        label.color = color;
        label.outlineWidth = 0.28f;
        label.outlineColor = new Color32(25, 0, 35, 255);
        label.richText = true;

        rect.localScale = Vector3.zero;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-7f, 7f));

        DOTween.Sequence().SetLink(label.gameObject)
            .Append(rect.DOScale(1.25f, 0.12f).SetEase(Ease.OutQuad))
            .Append(rect.DOScale(1f, 0.12f).SetEase(Ease.OutBack))
            .AppendInterval(0.25f)
            .Append(rect.DOAnchorPosY(position.y + 50f, 0.3f).SetEase(Ease.InQuad))
            .Join(label.DOFade(0f, 0.3f))
            .OnComplete(() => Destroy(label.gameObject));
    }

    // ================= Helpers =================

    private void EnsureLayer()
    {
        if (layer != null) return;

        // HUD 위·대사창(650) 아래
        Canvas canvas = DemoUI.CreateOverlayCanvas("CommandFxCanvas", 640, transform);
        layer = DemoUI.CreateRect("Layer", canvas.transform);
        layer.anchorMin = layer.anchorMax = new Vector2(0.5f, 0.5f);
        layer.sizeDelta = Vector2.zero;
        canvas.GetComponent<GraphicRaycaster>().enabled = false;

        if (softCircle == null) softCircle = MakeSprite(64, (x, y) => Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)) * Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)));
        if (ring == null) ring = MakeSprite(128, (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.1f); });
        if (star == null) star = MakeSprite(128, (x, y) =>
        {
            float a = Mathf.Abs(x), b = Mathf.Abs(y);
            float s1 = Mathf.Clamp01(1f - (a * 6f + b));          // 세로 광선
            float s2 = Mathf.Clamp01(1f - (b * 6f + a));          // 가로 광선
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(a * a + b * b) * 3.2f);
            return Mathf.Clamp01(Mathf.Max(s1, s2) + core);
        });
    }

    private Vector2 WheelPosition()
    {
        if (wheel == null) return Vector2.zero;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, wheel.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local);
        return local;
    }

    private bool TrySeatPosition(Keys key, out Vector2 local)
    {
        local = Vector2.zero;
        StageVisual seat = StageController.Instance != null ? StageController.Instance.GetSeatForKey(key) : null;
        Camera cam = Camera.main;
        if (seat == null || cam == null) return false;

        Vector3 screen = cam.WorldToScreenPoint(seat.transform.position);
        if (screen.z <= 0f) return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out local);
    }

    private static Image Img(string name, RectTransform parent, Sprite sprite, Color color, float size)
    {
        var image = DemoUI.CreatePanel(name, parent, color);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        if (size > 0f) image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    private static Sprite MakeSprite(int size, System.Func<float, float, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) / size * 2f - 1f;
            float v = (y + 0.5f) / size * 2f - 1f;
            byte a = (byte)(Mathf.Clamp01(alpha(u, v)) * 255f);
            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
