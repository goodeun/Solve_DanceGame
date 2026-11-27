using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns the wheel pulse material and its beat-synchronised display.</summary>
[RequireComponent(typeof(RhythmWheelTiming))]
[DefaultExecutionOrder(0)]
public sealed class RhythmWheelPulseVisual : MonoBehaviour
{
    [SerializeField] private Image circlePulse;
    [SerializeField] private Color normalPulseColor = Color.cyan;
    [SerializeField] private Color fourthBeatPulseColor = Color.red;

    private RhythmWheelTiming timing;
    private RhythmWheelJudgement judgement;
    private Material runtimeMaterial;
    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");

    /// <summary>true면 펄스 링을 숨긴다(페이즈 변경 전환 구간 등). 박 이벤트는 계속 흐른다.</summary>
    public bool Suppressed { get; set; }

    private void Awake()
    {
        timing = GetComponent<RhythmWheelTiming>();
        judgement = GetComponent<RhythmWheelJudgement>();
        if (circlePulse == null)
            foreach (var image in GetComponentsInChildren<Image>(true)) if (image.name == "Circle Pulse") { circlePulse = image; break; }
        if (circlePulse == null || circlePulse.material == null) return;
        runtimeMaterial = new Material(circlePulse.material);
        circlePulse.material = runtimeMaterial;
    }
    private void OnValidate()
    {
        if (circlePulse != null) return;
        foreach (var image in GetComponentsInChildren<Image>(true)) if (image.name == "Circle Pulse") { circlePulse = image; break; }
    }

    private void Update()
    {
        if (runtimeMaterial == null) return;
        if (!timing.TryGetCurrentBeatTiming(timing.ElapsedSeconds, out int beat, out float offset, out float beatDuration))
            return;

        // FMOD supplies the current beat start; never reconstruct the phase
        // by dividing the whole song time by the current BPM. Keep the
        // original Just-window display lead, but apply it within this FMOD
        // beat segment so the visual cue and judgement boundary stay aligned.
        float shiftedOffset = offset - judgement.JustWindowSeconds;
        int displayCycle = shiftedOffset >= 0f ? beat : beat - 1;
        int targetBeat = displayCycle + 1;
        timing.NotifyPulseCycle(displayCycle);

        if (circlePulse != null && circlePulse.enabled == Suppressed)
            circlePulse.enabled = !Suppressed;
        if (Suppressed) return;

        runtimeMaterial.SetColor("_Ring_Color", ((targetBeat % 4) + 4) % 4 == 3 ? fourthBeatPulseColor : normalPulseColor);
        runtimeMaterial.SetFloat(ProgressProperty, Mathf.Repeat(shiftedOffset / beatDuration, 1f) * .5f);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
