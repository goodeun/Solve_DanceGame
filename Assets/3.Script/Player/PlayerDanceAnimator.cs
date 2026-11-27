using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDanceAnimator : MonoBehaviour
{
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [SerializeField] private RhythmWheelFever fever;
    [SerializeField] private RuntimeAnimatorController[] phaseControllers;

    [SerializeField] private GameObject finishSparkleEffect;

    [SerializeField, Range(0.02f, 0.4f)] private float danceBlendBeats = 0.12f;

    private Animator animator;

    private readonly int hashIdle = Animator.StringToHash("Idle");
    private readonly int hashDance_Q = Animator.StringToHash("Dance_Q");
    private readonly int hashDance_E = Animator.StringToHash("Dance_E");
    private readonly int hashDance_A = Animator.StringToHash("Dance_A");
    private readonly int hashDance_D = Animator.StringToHash("Dance_D");
    private readonly int hashDance_W = Animator.StringToHash("Dance_W");
    private readonly int hashDance_S = Animator.StringToHash("Dance_S");

    private readonly int hashFail_Q = Animator.StringToHash("DanceFail_Q");
    private readonly int hashFail_E = Animator.StringToHash("DanceFail_E");
    private readonly int hashFail_A = Animator.StringToHash("DanceFail_A");
    private readonly int hashFail_D = Animator.StringToHash("DanceFail_D");
    private readonly int hashFail_W = Animator.StringToHash("DanceFail_W");
    private readonly int hashFail_S = Animator.StringToHash("DanceFail_S");

    private readonly int hashFace_Idle = Animator.StringToHash("robo_idle");
    private readonly int hashFace_Just = Animator.StringToHash("Judge_Just");
    private readonly int hashFace_Good = Animator.StringToHash("Judge_Good");
    private readonly int hashFace_Bad = Animator.StringToHash("Judge_Bad");
    private readonly int hashFace_Miss = Animator.StringToHash("Judge_Miss");

    private readonly int hashFever_Loop = Animator.StringToHash("Fever_Loop");
    private readonly int hashFever_Finish = Animator.StringToHash("Fever_Finish");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (rhythmWheel == null) rhythmWheel = FindAnyObjectByType<RhythmWheelUI>();
        if (fever == null) fever = FindAnyObjectByType<RhythmWheelFever>();
    }

    private void OnEnable()
    {
        rhythmWheel.OnJudged += PlayDance;
        fever.OnFeverStarted += PlayFeverLoop;
        fever.OnFeverEnded += PlayFeverFinish;
    }
    private void OnDisable()
    {
        rhythmWheel.OnJudged -= PlayDance;
        fever.OnFeverStarted -= PlayFeverLoop;
        fever.OnFeverEnded -= PlayFeverFinish;
    }

    private void PlayDance(Keys key, Judge judge, float offset)
    {
        if (fever!=null && fever.IsActive)
        {
            return;
        }

        int faceHash = GetFaceHash(judge);
        animator.CrossFade(faceHash, 0.1f, 1, 0f);

        if (judge == Judge.Miss)
        {
            BlendDance(hashIdle);
            return;
        }

        animator.speed = 1f / Mathf.Max(0.05f, rhythmWheel.BeatDurationSeconds);
        // Bad still uses the existing score judgement; only the body performance changes.
        bool failed = judge == Judge.Bad;
        int targetHash = failed ? GetFailHash(key) : GetDanceHash(key);
        BlendDance(targetHash, failed);
    }

    private void BlendDance(int targetHash, bool restart = false)
    {
        // Keep the one-beat loop running on repeated inputs instead of rewinding it.
        AnimatorStateInfo destination = animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0)
            : animator.GetCurrentAnimatorStateInfo(0);
        if (!restart && destination.shortNameHash == targetHash) return;

        // Gameplay clips are one second long and Animator.speed maps that second to one beat.
        // Fixed-time blending avoids inheriting the length of a previous idle/fever clip.
        animator.CrossFadeInFixedTime(targetHash, danceBlendBeats, 0, 0f);
    }

    private int GetFailHash(Keys key)
    {
        switch (key)
        {
            case Keys.Q: return hashFail_Q;
            case Keys.E: return hashFail_E;
            case Keys.W: return hashFail_W;
            case Keys.A: return hashFail_A;
            case Keys.D: return hashFail_D;
            case Keys.S: return hashFail_S;
            default: return hashIdle;
        }
    }

    private int GetDanceHash(Keys key)
    {
        switch (key)
        {
            case Keys.W:
                return hashDance_W;
            case Keys.A:
                return hashDance_A;
            case Keys.S:
                return hashDance_S;
            case Keys.D:
                return hashDance_D;
            case Keys.Q:
                return hashDance_Q;
            case Keys.E:
                return hashDance_E;
        }

        return hashIdle;
    }

    private int GetFaceHash(Judge judge)
    {
        switch (judge)
        {
            case Judge.Just:
                return hashFace_Just;
            case Judge.Good:
                return hashFace_Good;
            case Judge.Bad:
                return hashFace_Bad;
            case Judge.Miss:
                return hashFace_Miss;
        }

        return hashFace_Idle;
    }

    public void ChangePhase(Phase phase)
    {
        animator.runtimeAnimatorController = phaseControllers[(int)phase];
    }

    private void PlayFeverLoop()
    {
       
        animator.speed = 1f / rhythmWheel.BeatDurationSeconds;
        animator.CrossFade(hashFever_Loop, 0.1f, 0, 0f);
        animator.CrossFade(hashFace_Good, 0.1f, 1, 0f);
    }

    private void PlayFeverFinish()
    {
        StartCoroutine(PlayFeverFinish_co());
    }

    private IEnumerator PlayFeverFinish_co()
    {
        animator.CrossFade(hashFever_Finish, 0.1f, 0, 0f);
        animator.CrossFade(hashFace_Just, 0.1f, 1, 0f);

        if (finishSparkleEffect != null) finishSparkleEffect.SetActive(true) ;

        yield return new WaitForSeconds(rhythmWheel.BeatDurationSeconds*2);

        if (finishSparkleEffect != null) finishSparkleEffect.SetActive(false);
        animator.CrossFade(hashFace_Idle, 0.1f, 1, 0f);
        animator.CrossFade(hashIdle, 0.1f, 0, 0f);

    }
}
