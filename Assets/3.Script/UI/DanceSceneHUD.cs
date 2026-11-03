using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanceSceneHUD : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvas;

    // Dance 파트 HUD 관리
    [SerializeField] private JudgementUI judgementUI;
    [SerializeField] private FeverUI feverUI;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private HealthUI healthUI;

    private void Awake()
    {
        canvas = GetComponent<CanvasGroup>();
    }

    public void Judgement(Judge judge)
    {
        judgementUI.JudgementUpdate(judge);
    }

    public void FeverGage(float value)
    {
        feverUI.UpdateFeverGage(value);
    }

    public void AddScore(int totalScore)
    {
        scoreUI.ScoreUpdate(totalScore);
    }

    public void Damaged(int damage)
    {
        healthUI.Damage(damage);
    }

    public void Healed(int healValue)
    {
        healthUI.Heal(healValue);
    }

    public void FeverTime()
    {
        StartCoroutine(FeverTime_co());
    }

    public IEnumerator FeverTime_co()
    {
        canvas.alpha = 0;

        yield return StaticTime.FeverTimeCount;

        canvas.alpha = 1;
    }

}
