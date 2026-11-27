using UnityEngine;

/// <summary>Plays Rival's long dance at the current music tempo. Ready before the song, frozen by normal Pause.</summary>
[RequireComponent(typeof(Animator))]
public sealed class RivalDanceAnimator : MonoBehaviour
{
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField, Min(1f)] private float authoredBpm = 128f;
    [SerializeField, Min(0f)] private float blendSeconds = .15f;
    [SerializeField] private bool playWithoutMusicClock = true;

    private static readonly int Ready = Animator.StringToHash("Rival_Ready");
    private static readonly int Dance = Animator.StringToHash("Rival_DanceLoop");
    private Animator animator;
    private bool dancing;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (musicClock == null) musicClock = FindFirstObjectByType<FmodMusicClock>();
    }

    private void Update()
    {
        bool shouldDance = musicClock != null
            ? musicClock.IsStarted && !musicClock.IsEnded
            : playWithoutMusicClock;

        if (shouldDance != dancing)
        {
            dancing = shouldDance;
            animator.CrossFadeInFixedTime(dancing ? Dance : Ready, blendSeconds, 0, 0f);
        }

        if (dancing)
        {
            // A music-only pause must also stop this character even when timeScale is unchanged.
            animator.speed = musicClock != null && musicClock.IsPaused
                ? 0f
                : musicClock != null ? musicClock.BPM / authoredBpm : 1f;
        }
        else animator.speed = 1f;
    }
}
