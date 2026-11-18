using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDanceAnimator : MonoBehaviour
{
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [SerializeField] private RhythmWheelFever fever;
    [SerializeField] private RuntimeAnimatorController[] phaseControllers;

    [SerializeField] private GameObject finishSparkleEffect;

    private Animator animator;

    private readonly int hashIdle = Animator.StringToHash("Idle");
    private readonly int hashDance_Q = Animator.StringToHash("Dance_Q");
    private readonly int hashDance_E = Animator.StringToHash("Dance_E");
    private readonly int hashDance_A = Animator.StringToHash("Dance_A");
    private readonly int hashDance_D = Animator.StringToHash("Dance_D");
    private readonly int hashDance_W = Animator.StringToHash("Dance_W");
    private readonly int hashDance_S = Animator.StringToHash("Dance_S");

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
            animator.CrossFade(hashIdle, 0.1f, 0, 0f);
            return;
        }

        int targetHash = GetDanceHash(key);

        animator.speed = 1f / rhythmWheel.BeatDurationSeconds;
        animator.CrossFade(targetHash, 0.1f, 0, 0f);
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
