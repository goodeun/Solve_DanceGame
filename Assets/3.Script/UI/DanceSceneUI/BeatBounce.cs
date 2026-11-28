using UnityEngine;

/// <summary>
/// 붙이기만 하면 그 UI(또는 오브젝트)가 음악 박자에 맞춰 쿵쿵 뛴다(하이파이러쉬식 HUD).
/// FMOD 박 시각을 매 프레임 직접 읽어 박의 진행도로 움직이므로 프레임마다 정확히 박에 붙는다.
/// - 박이 시작되는 순간 빠르게 커지며(또는 아래로 눌리며) 튀고, 박이 끝나갈수록 탄력 있게 제자리로
/// - 마디 첫 박은 더 세게(Downbeat Accent), 쉬는 박(4박째)은 약하게
/// - 음악이 멈추면(Pause) 같이 멈춘다
/// 수치는 Inspector에서 오브젝트마다 다르게 줄 수 있다.
/// </summary>
public class BeatBounce : MonoBehaviour
{
    [Header("Scale")]
    [Tooltip("박마다 커지는 정도(0.06 = 6%)")]
    [SerializeField, Range(0f, 0.4f)] private float scaleAmount = 0.06f;
    [Tooltip("가로·세로 비율. (1,1)이면 균등, (1.2,0.8)이면 옆으로 퍼지며 눌리는 느낌")]
    [SerializeField] private Vector2 scaleAxis = new Vector2(1f, 1f);

    [Header("Thump (위치)")]
    [Tooltip("박마다 움직이는 거리(UI 픽셀). 음수면 아래로 쿵")]
    [SerializeField] private Vector2 thumpOffset = new Vector2(0f, -6f);
    [Tooltip("박마다 살짝 기울이는 각도")]
    [SerializeField, Range(0f, 10f)] private float tiltAmount = 0f;

    [Header("Rhythm")]
    [Tooltip("마디 첫 박 강조 배수")]
    [SerializeField, Range(1f, 3f)] private float downbeatAccent = 1.6f;
    [Tooltip("쉬는 박(4박째) 세기 배수")]
    [SerializeField, Range(0f, 1f)] private float restBeatScale = 0.55f;
    [Tooltip("튀어 오르는 시간(박 대비). 짧을수록 '쿵'이 날카롭다")]
    [SerializeField, Range(0.01f, 0.3f)] private float attack = 0.06f;
    [Tooltip("제자리로 돌아오는 감쇠 세기")]
    [SerializeField, Range(1f, 12f)] private float decay = 5.5f;
    [Tooltip("돌아올 때 살짝 반대로 넘어가는 탄성")]
    [SerializeField, Range(0f, 0.6f)] private float overshoot = 0.18f;
    [Tooltip("박보다 조금 일찍 튀게(초). 입력 판정의 Just 구간에 맞추고 싶을 때")]
    [SerializeField, Range(0f, 0.1f)] private float leadSeconds = 0f;

    private RhythmWheelTiming timing;
    private RectTransform rect;
    private Vector3 baseScale;
    private Vector2 baseAnchored;
    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        rect = transform as RectTransform;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        if (rect != null) baseAnchored = rect.anchoredPosition;
        else basePosition = transform.localPosition;
    }

    /// <summary>코드에서 만든 오브젝트에 수치를 줄 때(SplitBeatBounce 등).</summary>
    public void Configure(float scale, Vector2 axis, Vector2 thump, float accent, float restScale)
    {
        scaleAmount = scale;
        scaleAxis = axis;
        thumpOffset = thump;
        downbeatAccent = accent;
        restBeatScale = restScale;
    }

    /// <summary>현재 위치·크기를 기준(제자리)으로 다시 잡는다.</summary>
    public void Rebase()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        if (rect != null) baseAnchored = rect.anchoredPosition;
        else basePosition = transform.localPosition;
    }

    private void OnDisable() => Restore();

    private void LateUpdate()
    {
        if (timing == null)
        {
            timing = FindFirstObjectByType<RhythmWheelTiming>();
            if (timing == null) return;
        }

        float elapsed = timing.ElapsedSeconds + leadSeconds;
        if (!timing.TryGetCurrentBeatTiming(elapsed, out int beat, out float offset, out float duration) || beat < 0 || duration <= 0f)
        {
            Restore();
            return;
        }

        float phase = Mathf.Clamp01(offset / duration);
        float strength = beat % 4 == 0 ? downbeatAccent : beat % 4 == 3 ? restBeatScale : 1f;
        float env = Envelope(phase) * strength;

        transform.localScale = new Vector3(
            baseScale.x * (1f + scaleAmount * env * scaleAxis.x),
            baseScale.y * (1f + scaleAmount * env * scaleAxis.y),
            baseScale.z);

        Vector2 offsetPos = thumpOffset * env;
        if (rect != null) rect.anchoredPosition = baseAnchored + offsetPos;
        else transform.localPosition = basePosition + (Vector3)offsetPos;

        if (tiltAmount > 0f)
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tiltAmount * env * (beat % 2 == 0 ? 1f : -1f));
    }

    /// <summary>0 → 빠르게 1까지(attack) → 지수 감쇠하며 살짝 반대로 넘어갔다가 0.</summary>
    private float Envelope(float phase)
    {
        if (phase < attack)
        {
            float t = phase / attack;
            return 1f - (1f - t) * (1f - t); // ease out
        }

        float u = (phase - attack) / Mathf.Max(0.0001f, 1f - attack);
        float fall = Mathf.Exp(-decay * u);
        float wobble = Mathf.Cos(u * Mathf.PI * 1.5f);
        return fall * Mathf.Lerp(1f, wobble, overshoot / 0.6f);
    }

    private void Restore()
    {
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
        if (rect != null) rect.anchoredPosition = baseAnchored;
        else transform.localPosition = basePosition;
    }
}
