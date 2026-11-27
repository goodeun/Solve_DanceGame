using DG.Tweening;
using UnityEngine;

/// <summary>
/// 빛기둥의 실제 조명. 구역이 켜지면 기둥 색의 Point Light가 낮게 켜져 플레이어 다리·몸에 은은하게 빛이 닿는다.
/// FloorLightPillar와 분리된 컴포넌트라 이것만 끄고 켤 수 있다.
/// - 전체 끄기/켜기: FloorPillarLight.GloballyEnabled = false (옵션 메뉴 등에서 연결)
/// - 구역별 끄기: 이 컴포넌트의 Use Light 체크 해제 또는 컴포넌트 비활성화
/// FloorSection이 자동으로 붙인다.
/// </summary>
[RequireComponent(typeof(FloorLightPillar))]
public class FloorPillarLight : MonoBehaviour
{
    private static bool globallyEnabled = true;

    /// <summary>모든 빛기둥 조명 전체 스위치.</summary>
    public static bool GloballyEnabled
    {
        get => globallyEnabled;
        set
        {
            if (globallyEnabled == value) return;
            globallyEnabled = value;
            GlobalToggled?.Invoke(value);
        }
    }

    private static event System.Action<bool> GlobalToggled;

    [SerializeField] private bool useLight = true;

    [Header("Look")]
    [Tooltip("구역 크기와 상관없이 비슷한 밝기가 되도록 범위²를 곱해 쓴다")]
    [SerializeField, Min(0f)] private float strength = 0.35f;
    [SerializeField, Min(0f)] private float burstMultiplier = 2f;
    [Tooltip("빛 범위 = 구역 폭 × 이 값")]
    [SerializeField, Min(0.1f)] private float rangeScale = 1.3f;
    [Tooltip("빛 위치 = 바닥에서 기둥 높이 × 이 값")]
    [SerializeField, Range(0f, 2f)] private float heightRatio = 0.6f;
    [SerializeField] private bool normalizeColor = true;

    [Header("Timing (초)")]
    [SerializeField, Min(0.01f)] private float riseDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float fallDuration = 0.2f;

    private FloorLightPillar pillar;
    private Light pointLight;
    private Tween tween;
    private float baseIntensity;
    private bool lit;

    public bool UseLight
    {
        get => useLight;
        set { useLight = value; if (!IsOn) Off(); }
    }

    private bool IsOn => useLight && globallyEnabled && enabled;

    private void Awake() => pillar = GetComponent<FloorLightPillar>();

    private void OnEnable()
    {
        if (pillar == null) pillar = GetComponent<FloorLightPillar>();
        pillar.Risen += HandleRise;
        pillar.Fallen += HandleFall;
        pillar.Hidden += Off;
        GlobalToggled += HandleGlobal;
    }

    private void OnDisable()
    {
        if (pillar != null)
        {
            pillar.Risen -= HandleRise;
            pillar.Fallen -= HandleFall;
            pillar.Hidden -= Off;
        }
        GlobalToggled -= HandleGlobal;
        Off();
    }

    private void OnDestroy() => tween?.Kill();

    private void HandleGlobal(bool on)
    {
        if (!on) Off();
    }

    private void HandleRise(Color color)
    {
        if (!IsOn || !Build()) return;

        if (normalizeColor)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (max > 0.0001f) color = new Color(color.r / max, color.g / max, color.b / max, 1f);
        }
        pointLight.color = color;
        pointLight.enabled = true;

        tween?.Kill();
        float from = lit ? pointLight.intensity : 0f;
        lit = true;
        pointLight.intensity = from;
        tween = DOTween.Sequence().SetLink(gameObject)
            .Append(DOTween.To(() => pointLight.intensity, v => pointLight.intensity = v, baseIntensity * burstMultiplier, riseDuration).SetEase(Ease.OutQuad))
            .Append(DOTween.To(() => pointLight.intensity, v => pointLight.intensity = v, baseIntensity, riseDuration * 2.5f).SetEase(Ease.OutCubic));
    }

    private void HandleFall(bool success)
    {
        if (!lit || pointLight == null) return;
        lit = false;
        tween?.Kill();

        var s = DOTween.Sequence().SetLink(gameObject);
        if (success)
            s.Append(DOTween.To(() => pointLight.intensity, v => pointLight.intensity = v, baseIntensity * burstMultiplier, 0.06f));
        s.Append(DOTween.To(() => pointLight.intensity, v => pointLight.intensity = v, 0f, fallDuration).SetEase(Ease.InQuad))
         .OnComplete(() => { if (pointLight != null) pointLight.enabled = false; });
        tween = s;
    }

    private void Off()
    {
        lit = false;
        tween?.Kill();
        if (pointLight != null)
        {
            pointLight.intensity = 0f;
            pointLight.enabled = false;
        }
    }

    private bool Build()
    {
        if (pointLight != null) return true;

        var renderer = GetComponent<Renderer>();
        if (renderer == null) return false;
        Bounds b = renderer.bounds;
        float footprint = Mathf.Max(b.size.x, b.size.z);
        float height = pillar != null && pillar.WorldHeight > 0f ? pillar.WorldHeight : footprint * 0.3f;

        var go = new GameObject("PillarLight");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(b.center.x, b.min.y + height * heightRatio, b.center.z);

        pointLight = go.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = footprint * rangeScale;
        pointLight.shadows = LightShadows.None;
        pointLight.renderMode = LightRenderMode.Auto;
        pointLight.enabled = false;
        baseIntensity = strength * pointLight.range * pointLight.range * 0.25f;
        return true;
    }
}
