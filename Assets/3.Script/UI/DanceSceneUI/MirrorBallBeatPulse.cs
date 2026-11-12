using System.Collections;
using UnityEngine;

/// <summary>
/// Briefly enlarges the mirror ball whenever the rhythm wheel starts a beat.
/// </summary>
public sealed class MirrorBallBeatPulse : MonoBehaviour
{
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [Tooltip("Leave empty to pulse the object this component is attached to.")]
    [SerializeField] private Transform pulseTarget;
    [SerializeField, Min(1f)] private float peakScaleMultiplier = 1.12f;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.16f;

    private Vector3 baseScale;
    private Coroutine pulseRoutine;

    private void Awake()
    {
        if (pulseTarget == null)
            pulseTarget = transform;

        baseScale = pulseTarget.localScale;
        if (rhythmWheel == null)
            rhythmWheel = FindFirstObjectByType<RhythmWheelUI>();
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

        if (pulseTarget != null)
            pulseTarget.localScale = baseScale;
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
            pulseTarget.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        pulseTarget.localScale = to;
    }
}
