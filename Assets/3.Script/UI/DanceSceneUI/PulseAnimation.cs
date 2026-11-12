using System.Collections;
using UnityEngine;

public class PulseAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [SerializeField] private Transform[] Objects = new Transform[4];

    [Header("Beat Pulse")]
    [SerializeField, Min(1f)] private float peakScaleMultiplier = 1.12f;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.16f;

    private Vector3[] baseScales;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        CaptureBaseScales();

        if (rhythmWheel == null)
            rhythmWheel = FindFirstObjectByType<RhythmWheelUI>();
    }

    private void CaptureBaseScales()
    {
        baseScales = new Vector3[Objects.Length];
        for (int i = 0; i < Objects.Length; i++)
            if (Objects[i] != null)
                baseScales[i] = Objects[i].localScale;
    }

    private void OnEnable()
    {
        SubscribeToBeats();
    }

    private void Start()
    {
        // Start runs after every Awake/OnEnable in the scene. Re-register here
        // so this component cannot miss a wheel that initializes later.
        SubscribeToBeats();
    }

    private void SubscribeToBeats()
    {
        if (rhythmWheel == null)
            rhythmWheel = FindFirstObjectByType<RhythmWheelUI>();

        if (rhythmWheel != null)
        {
            rhythmWheel.OnBeatStarted -= Pulse;
            rhythmWheel.OnBeatStarted += Pulse;
        }
    }

    private void OnDisable()
    {
        if (rhythmWheel != null)
            rhythmWheel.OnBeatStarted -= Pulse;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        RestoreBaseScales();
    }

    private void Pulse(int _)
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float growDuration = pulseDuration * 0.25f;
        float shrinkDuration = pulseDuration - growDuration;

        yield return ScaleOverTime(0f, 1f, growDuration);
        yield return ScaleOverTime(1f, 0f, shrinkDuration);

        RestoreBaseScales();
        pulseRoutine = null;
    }

    private IEnumerator ScaleOverTime(float from, float to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            SetPulseAmount(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetPulseAmount(to);
    }

    private void SetPulseAmount(float amount)
    {
        // Do not cache the peak scale. This is deliberately evaluated on
        // every frame, so changing Peak Scale Multiplier in the Inspector
        // takes effect immediately for every array element, including [0].
        float multiplier = Mathf.Lerp(1f, peakScaleMultiplier, amount);
        for (int i = 0; i < Objects.Length; i++)
            if (Objects[i] != null && IsPrimaryController(Objects[i]))
                Objects[i].localScale = baseScales[i] * multiplier;
    }

    private void RestoreBaseScales()
    {
        if (baseScales == null) return;
        for (int i = 0; i < Objects.Length; i++)
            if (Objects[i] != null && IsPrimaryController(Objects[i]))
                Objects[i].localScale = baseScales[i];
    }

    // A target can accidentally be registered in more than one
    // PulseAnimation. Prefer the component with the larger target list, so a
    // single-object leftover component cannot overwrite this controller's
    // multiplier (BingSpeaker was registered in both).
    private bool IsPrimaryController(Transform target)
    {
        int thisTargetCount = TargetCount();
        PulseAnimation[] controllers = FindObjectsByType<PulseAnimation>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (PulseAnimation controller in controllers)
        {
            if (controller == this || !controller.Contains(target))
                continue;

            if (controller.TargetCount() > thisTargetCount)
                return false;
        }

        return true;
    }

    private bool Contains(Transform target)
    {
        foreach (Transform item in Objects)
            if (item == target)
                return true;
        return false;
    }

    private int TargetCount()
    {
        int count = 0;
        foreach (Transform item in Objects)
            if (item != null)
                count++;
        return count;
    }
}
