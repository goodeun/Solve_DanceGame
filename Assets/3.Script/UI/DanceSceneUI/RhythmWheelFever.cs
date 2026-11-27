using UnityEngine;

public sealed class RhythmWheelFever : MonoBehaviour
{
    [SerializeField, Range(.01f, 1f)] private float gain = .1f;
    [SerializeField, Range(.01f, 1f)] private float loss = .1f;
    [SerializeField, Min(4f)] private float durationBeats = 12f;
    [SerializeField] private FeverCamera feverCamera;
    [SerializeField] private ComboAndMagnification combo;
    [SerializeField] private DanceSceneHUD hud;
    private RhythmWheelTiming timing;
    private float gauge;
    private bool queued;
    private bool active;
    private int feverEndBeat = -1;
    public bool IsActive => active;
    public bool IsJudgementLocked => feverCamera != null && feverCamera.IsCameraSequenceActive;

    public event System.Action OnFeverStarted;
    public event System.Action OnFeverEnded;

    private void Awake() { timing = GetComponent<RhythmWheelTiming>(); combo ??= GetComponent<ComboAndMagnification>(); feverCamera ??= FindFirstObjectByType<FeverCamera>(); hud ??= FindFirstObjectByType<DanceSceneHUD>(); }
    private void OnEnable() { if (combo != null) { combo.OnComboSaved += Save; combo.OnComboFailed += Fail; } timing.BeatStarted += HandleBeatStarted; }
    private void OnDisable() { if (combo != null) { combo.OnComboSaved -= Save; combo.OnComboFailed -= Fail; } timing.BeatStarted -= HandleBeatStarted; }
    private void Save(Keys _) { if (active) return; gauge = Mathf.Clamp01(gauge + gain); queued |= gauge >= 1; hud?.AddFeverGage(gauge); }
    private void Fail() { if (active) return; gauge = Mathf.Clamp01(gauge - loss); hud?.AddFeverGage(gauge); }
    private void HandleBeatStarted(int beat)
    {
        if (active)
        {
            if (beat == feverEndBeat - 1)
                EndFever();
            return;
        }

        // Start on the next phrase's first beat after the gauge fills.
        if (!queued || beat % 4 != 0)
            return;

        queued = false;
        active = true;
        int beatCount = Mathf.CeilToInt(durationBeats / 4f) * 4;
        feverEndBeat = beat + beatCount;
        // Finish on the rest beat immediately before the next phrase begins.
        float activeDuration = Mathf.Max(1, beatCount - 1) * timing.BeatDurationSeconds;
        hud?.RequestFeverTime(true, activeDuration, timing.BeatDurationSeconds);
        OnFeverStarted?.Invoke();
    }

    private void EndFever()
    {
        feverEndBeat = -1;
        gauge = 0f;
        active = false;
        feverCamera?.AllowGameplayInput();
        hud?.AddFeverGage(gauge);
        hud?.EndFeverTime();
        OnFeverEnded?.Invoke();
    }
}
