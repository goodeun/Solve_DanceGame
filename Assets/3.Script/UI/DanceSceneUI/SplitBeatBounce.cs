using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 덮는 HUD 프레임 이미지(위·아래 장식이 한 장에 있는 그림)를 위/아래 두 조각으로 나눠 박자를 타게 한다.
/// - 위쪽 조각은 아래로 '쿵', 아래쪽 조각은 위로 '쿵' (화면 안쪽으로 눌리는 느낌)
/// - 크기 변화는 아주 미세하게, 화면 가장자리를 기준점으로 커져서 바깥 모양이 어긋나지 않는다
/// 원본 Image는 숨기고, 같은 스프라이트를 RectMask2D로 반씩 잘라 보여 준다. 박자 계산은 BeatBounce를 그대로 쓴다.
/// </summary>
[RequireComponent(typeof(Image))]
public class SplitBeatBounce : MonoBehaviour
{
    [Tooltip("위·아래를 나누는 높이(0 = 화면 아래, 1 = 화면 위)")]
    [SerializeField, Range(0.1f, 0.9f)] private float splitY = 0.5f;

    [Header("Top (아래로 쿵)")]
    [SerializeField] private Vector2 topThump = new Vector2(0f, -7f);
    [Header("Bottom (위로 쿵)")]
    [SerializeField] private Vector2 bottomThump = new Vector2(0f, 7f);

    [Header("Common")]
    [Tooltip("크기 변화(0.008 = 0.8%). 미세하게")]
    [SerializeField, Range(0f, 0.05f)] private float scaleAmount = 0.008f;
    [Tooltip("가로·세로 크기 변화 비율(가로는 거의 고정, 세로만 살짝)")]
    [SerializeField] private Vector2 scaleAxis = new Vector2(0.4f, 1f);
    [SerializeField, Range(1f, 3f)] private float downbeatAccent = 1.6f;
    [SerializeField, Range(0f, 1f)] private float restBeatScale = 0.55f;

    private bool built;

    private void Awake() => Build();

    private void Build()
    {
        if (built) return;
        built = true;

        var source = GetComponent<Image>();
        var rect = (RectTransform)transform;
        Vector2 size = rect.rect.size;
        if (size.x <= 0f || size.y <= 0f) size = rect.sizeDelta;

        // 예전 방식(통째로 바운스)이 같이 붙어 있으면 끈다.
        var whole = GetComponent<BeatBounce>();
        if (whole != null) whole.enabled = false;

        CreateHalf("Top", source, size, true);
        CreateHalf("Bottom", source, size, false);

        source.enabled = false;
    }

    private void CreateHalf(string name, Image source, Vector2 size, bool top)
    {
        // 잘라 보여 줄 영역(원본 사각형 안에서 위 또는 아래)
        var maskGo = new GameObject(name, typeof(RectTransform), typeof(RectMask2D));
        var mask = (RectTransform)maskGo.transform;
        mask.SetParent(transform, false);
        mask.anchorMin = new Vector2(0f, top ? splitY : 0f);
        mask.anchorMax = new Vector2(1f, top ? 1f : splitY);
        mask.offsetMin = mask.offsetMax = Vector2.zero;
        mask.pivot = new Vector2(0.5f, top ? 1f : 0f); // 화면 가장자리 기준으로 커지게

        // 원본과 같은 그림을 원래 자리에 그대로 둔다
        var copyGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var copy = copyGo.GetComponent<Image>();
        var copyRect = (RectTransform)copyGo.transform;
        copyRect.SetParent(mask, false);
        copyRect.anchorMin = copyRect.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
        copyRect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        copyRect.sizeDelta = size;
        copyRect.anchoredPosition = Vector2.zero;

        copy.sprite = source.sprite;
        copy.color = source.color;
        copy.material = source.material;
        copy.type = source.type;
        copy.preserveAspect = source.preserveAspect;
        copy.raycastTarget = false;

        var bounce = maskGo.AddComponent<BeatBounce>();
        bounce.Configure(scaleAmount, scaleAxis, top ? topThump : bottomThump, downbeatAccent, restBeatScale);
        bounce.Rebase();
    }
}
