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
    public bool IsJudgementLocked => feverCamera != null && feverCamera.IsCameraSequenceActive;
    private void Awake() { timing = GetComponent<RhythmWheelTiming>(); combo ??= GetComponent<ComboAndMagnification>(); feverCamera ??= FindFirstObjectByType<FeverCamera>(); hud ??= FindFirstObjectByType<DanceSceneHUD>(); }
    private void OnEnable() { if (combo != null) { combo.OnComboSaved += Save; combo.OnComboFailed += Fail; } timing.PhraseCompleted += StartFever; }
    private void OnDisable() { if (combo != null) { combo.OnComboSaved -= Save; combo.OnComboFailed -= Fail; } timing.PhraseCompleted -= StartFever; }
    private void Save(Keys _) { if (active) return; gauge = Mathf.Clamp01(gauge + gain); queued |= gauge >= 1; hud?.AddFeverGage(gauge); }
    private void Fail() { if (active) return; gauge = Mathf.Clamp01(gauge - loss); hud?.AddFeverGage(gauge); }
    private void StartFever() { if (!queued || active) return; queued = false; active = true; float seconds = Mathf.Ceil(durationBeats / 4) * 4 * timing.BeatDurationSeconds; hud?.RequestFeverTime(true, seconds, timing.BeatDurationSeconds); StartCoroutine(ResetAfter(seconds)); }
    private System.Collections.IEnumerator ResetAfter(float seconds) { yield return new WaitForSeconds(seconds); gauge = 0f; active = false; hud?.AddFeverGage(gauge); }
}
