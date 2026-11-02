using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanceSceneHUD : MonoBehaviour
{
    // Dance 파트 HUD 관리
    [SerializeField] private JudgementUI judgementUI;
    [SerializeField] private FeverUI feverUI;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private HealthUI healthUI;


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

}
