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
    private void Awake() { timing = GetComponent<RhythmWheelTiming>(); combo ??= GetComponent<ComboAndMagnification>(); feverCamera ??= FindFirstObjectByType<FeverCamera>(); hud ??= FindFirstObjectByType<DanceSceneHUD>(); }
    private void OnEnable() { if (combo != null) { combo.OnComboSaved += Save; combo.OnComboFailed += Fail; } timing.PhraseCompleted += StartFever; timing.BeatStarted += CheckFeverEnd; }
    private void OnDisable() { if (combo != null) { combo.OnComboSaved -= Save; combo.OnComboFailed -= Fail; } timing.PhraseCompleted -= StartFever; timing.BeatStarted -= CheckFeverEnd; }
    private void Save(Keys _) { if (active) return; gauge = Mathf.Clamp01(gauge + gain); queued |= gauge >= 1; hud?.AddFeverGage(gauge); }
    private void Fail() { if (active) return; gauge = Mathf.Clamp01(gauge - loss); hud?.AddFeverGage(gauge); }
    private void StartFever()
    {
        if (!queued || active) return;
        queued = false;
        active = true;
        int beatCount = Mathf.CeilToInt(durationBeats / 4f) * 4;
        feverEndBeat = timing.CurrentBeat + beatCount;
        // This is display-only. Gameplay ends on the FMOD beat callback below.
        hud?.RequestFeverTime(true, beatCount * timing.BeatDurationSeconds, timing.BeatDurationSeconds);
    }

    private void CheckFeverEnd(int beat)
    {
        if (!active || beat < feverEndBeat) return;
        feverEndBeat = -1;
        gauge = 0f;
        active = false;
        hud?.AddFeverGage(gauge);
    }
}
