using System.Collections;
using UnityEngine;

public class DanceSceneHUD : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvas;
    [SerializeField] private JudgementUI judgementUI;
    [SerializeField] private FeverGageUI feverGageUI;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private HealthUI healthUI;
    [SerializeField] private FeverUI feverUI;

    private void Awake() => canvas = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        feverUI ??= FindAnyObjectByType<FeverUI>();
        if (feverUI != null)
            feverUI.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (feverUI != null)
            feverUI.gameObject.SetActive(false);
    }

    public void JudgementEffect(Judge judge) => judgementUI.JudgementUpdate(judge);
    public void AddFeverGage(float value) => feverGageUI.UpdateFeverGage(value);
    public void AddScore(int totalScore) => scoreUI.ScoreUpdate(totalScore);
    public void HealthDamaged(int damage) => healthUI.Damage(damage);
    public void HealthHealed(int healValue) => healthUI.Heal(healValue);

    public void RequestFeverTime(bool isFever, float feverDuration, float returnBlendDuration = 0.5f)
    {
        if (isFever)
        {
            if (canvas != null)
                canvas.alpha = 0;
            feverUI?.FeverSuccess(feverDuration, returnBlendDuration);
        }
        else
        {
            feverUI?.FeverFailure();
        }
    }

    public void EndFeverTime()
    {
        if (canvas != null)
            canvas.alpha = 1;
    }

    public void DEBUG_FeverTime(bool isFever) => RequestFeverTime(isFever, 10f);
}
