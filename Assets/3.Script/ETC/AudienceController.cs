using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudienceController : MonoBehaviour
{
    [SerializeField] private Animator audienceAnim;
    [SerializeField] private AtmosphereController atmosphereController;

    [SerializeField] private CrowdMood currentMood;
    [SerializeField] private int HeatingScore;
    [SerializeField] private int BurningScore;
    [SerializeField] private int ScoreOffset = 3;
    [SerializeField] private string idleState = "Base Layer.Idle";
    [SerializeField] private string pointingState = "Base Layer.Pointing Forward";
    [SerializeField] private string jumpingState = "Base Layer.Joyful Jump";
    [SerializeField, Min(0.1f)] private float actionSwitchInterval = 2f;

    private bool thresholdsInitialized;
    private Coroutine actionRoutine;

    private void Awake()
    {
        if(audienceAnim == null) TryGetComponent(out audienceAnim);
        if(atmosphereController == null) atmosphereController = GetComponentInParent<AtmosphereController>();

        SetAtmosphereController(atmosphereController);
    }

    private void OnEnable() => atmosphereController?.Register(this);
    private void OnDisable()
    {
        atmosphereController?.Unregister(this);

        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
            actionRoutine = null;
        }
    }

    public void SetAtmosphereController(AtmosphereController controller)
    {
        if (atmosphereController != controller)
            atmosphereController?.Unregister(this);

        atmosphereController = controller;
        if (atmosphereController == null)
            return;

        if (!thresholdsInitialized)
        {
            HeatingScore = Random.Range(atmosphereController.Heating - ScoreOffset, atmosphereController.Heating + ScoreOffset + 1);
            BurningScore = Random.Range(atmosphereController.Burning - ScoreOffset, atmosphereController.Burning + ScoreOffset + 1);
            thresholdsInitialized = true;
        }

        atmosphereController.Register(this);
    }

    public void SetMoodScore(float score)
    {
        CrowdMood nextMood = score >= BurningScore ? CrowdMood.Burning :
                             score >= HeatingScore ? CrowdMood.Heating : CrowdMood.Normal;

        if (currentMood != nextMood || actionRoutine == null)
        {
            currentMood = nextMood;
            PlayMoodActions();
        }
    }

    public CrowdMood Mood => currentMood;

    private void PlayMoodActions()
    {
        if (actionRoutine != null)
            StopCoroutine(actionRoutine);

        if (currentMood == CrowdMood.Normal)
        {
            PlayState(idleState);
            actionRoutine = null;
            return;
        }

        actionRoutine = StartCoroutine(PlayAlternatingActions());
    }

    private IEnumerator PlayAlternatingActions()
    {
        string firstState = currentMood == CrowdMood.Heating ? idleState : pointingState;
        string secondState = currentMood == CrowdMood.Heating ? pointingState : jumpingState;
        bool useFirstState = Random.value > 0.5f;

        while (currentMood != CrowdMood.Normal)
        {
            PlayState(useFirstState ? firstState : secondState);
            useFirstState = !useFirstState;
            yield return new WaitForSeconds(actionSwitchInterval);
        }
    }

    private void PlayState(string stateName)
    {
        if (audienceAnim == null)
            return;

        audienceAnim.CrossFade(Animator.StringToHash(stateName), 0.15f, 0, Random.value);
    }
}

public enum CrowdMood { Normal, Heating, Burning }
