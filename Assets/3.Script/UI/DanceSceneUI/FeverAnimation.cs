using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the Fever presentation as one unit: beat-synced scale pulses and
/// continuous mirror-ball rotation. Attach this to the Fever parent object.
/// </summary>
public sealed class FeverAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [SerializeField] private Transform mirrorBall;

    [Header("Beat Pulse")]
    [SerializeField, Min(1f)] private float peakScaleMultiplier = 1.12f;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.16f;

    [Header("Mirror Ball Rotation")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationDegreesPerBeat = 90f;

    private Vector3 baseScale;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        baseScale = transform.localScale;

        if (rhythmWheel == null)
            rhythmWheel = FindFirstObjectByType<RhythmWheelUI>();

        if (mirrorBall == null)
        {
            Transform found = transform.Find("Mirror Ball");
            if (found != null)
                mirrorBall = found;
        }
    }

    private void OnEnable()
    {
        if (rhythmWheel == null)
            rhythmWheel = FindFirstObjectByType<RhythmWheelUI>();

        if (rhythmWheel != null)
            rhythmWheel.OnBeatStarted += Pulse;
    }

    private void OnDisable()
    {
        if (rhythmWheel != null)
            rhythmWheel.OnBeatStarted -= Pulse;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        transform.localScale = baseScale;
    }

    private void Update()
    {
        if (mirrorBall == null || rhythmWheel == null || rhythmWheel.BeatDurationSeconds <= 0f)
            return;

        float degreesPerSecond = rotationDegreesPerBeat / rhythmWheel.BeatDurationSeconds;
        mirrorBall.Rotate(rotationAxis.normalized, degreesPerSecond * Time.deltaTime, Space.Self);
    }

    private void Pulse(int _)
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        Vector3 peakScale = baseScale * peakScaleMultiplier;
        float growDuration = pulseDuration * 0.25f;
        float shrinkDuration = pulseDuration - growDuration;

        yield return ScaleOverTime(baseScale, peakScale, growDuration);
        yield return ScaleOverTime(peakScale, baseScale, shrinkDuration);

        transform.localScale = baseScale;
        pulseRoutine = null;
    }

    private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        transform.localScale = to;
    }
}
