using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDanceAnimator : MonoBehaviour
{
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [SerializeField] private RuntimeAnimatorController[] phaseControllers;

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

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (rhythmWheel == null) rhythmWheel = FindAnyObjectByType<RhythmWheelUI>();
    }

    private void OnEnable()
    {
        rhythmWheel.OnJudged += PlayDance;
    }
    private void OnDisable()
    {
        rhythmWheel.OnJudged -= PlayDance;
    }

    private void PlayDance(Keys key, Judge judge, float offset)
    {
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
}
