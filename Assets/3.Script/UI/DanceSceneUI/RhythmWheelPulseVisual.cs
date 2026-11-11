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
        float shifted = timing.ElapsedSeconds - judgement.JustWindowSeconds;
        int cycle = Mathf.FloorToInt(shifted / timing.BeatDurationSeconds);
        timing.NotifyPulseCycle(cycle);
        int targetBeat = cycle + 1;
        runtimeMaterial.SetColor("_Ring_Color", ((targetBeat % 4) + 4) % 4 == 3 ? fourthBeatPulseColor : normalPulseColor);
        runtimeMaterial.SetFloat(ProgressProperty, Mathf.Repeat(shifted / timing.BeatDurationSeconds, 1f) * .5f);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
