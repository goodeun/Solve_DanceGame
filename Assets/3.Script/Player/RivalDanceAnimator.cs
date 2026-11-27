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

    // 오브젝트를 껐다 켜면(공중 무대 숨김·등장 등) Animator가 기본 상태(Ready)로 돌아가므로 상태를 다시 맞춘다.
    private void OnEnable() => dancing = false;

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
