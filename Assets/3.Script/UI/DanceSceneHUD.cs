using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanceSceneHUD : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvas;

    // Dance 파트 HUD 관리
    [SerializeField] private JudgementUI judgementUI;
    [SerializeField] private FeverGageUI feverGageUI;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private HealthUI healthUI;
    [SerializeField] private FeverUI feverUI;

    private void Awake()
    {
        canvas = GetComponent<CanvasGroup>();
    }
    private void OnEnable()
    {
        if (feverUI == null)
        {
            feverUI = FindAnyObjectByType<FeverUI>();
        }

        feverUI.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (feverUI != null)
        {
            feverUI.gameObject.SetActive(false);
        }
    }

    public void JudgementEffect(Judge judge) // 판정시 나오는 이펙트
    {
        judgementUI.JudgementUpdate(judge);
    }

    public void AddFeverGage(float value) // 피버 게이지
    {
        feverGageUI.UpdateFeverGage(value);
    }

    public void AddScore(int totalScore) // 전체 점수 - 점수 오를 때마다 호출해서 전체 점수 입력
    {
        scoreUI.ScoreUpdate(totalScore);
    }

    public void HealthDamaged(int damage) // 실수시 체력 닳았을 때
    {
        healthUI.Damage(damage);
    }

    public void HealthHealed(int healValue) // 체력 회복할 때
    {
        healthUI.Heal(healValue);
    }

    public void RequestFeverTime(bool isFever, float feverDuration, float returnBlendDuration = 0.5f) // 피버 타임 얼마나 지속할지 bpm, 마디 계산해서 총 피버 duration 입력
                                                                    // isFever  true - 피버 진입 성공 | false - 피버 진입 실패
    {
        if (isFever)
        {
            if (hideHudRoutine != null) StopCoroutine(hideHudRoutine);
            hideHudRoutine = StartCoroutine(HideHud(feverDuration));
            feverUI.FeverSuccess(feverDuration, returnBlendDuration);
        }
        else
        {
            feverUI.FeverFailure();
        }
    }

    private Coroutine hideHudRoutine;

    // 피버 중 HUD 숨김. 게임플레이 종료(박 기준)에 맞춰 EndFeverTime에서 복구하며,
    // 시간 기준 복구는 EndFeverTime이 호출되지 않는 경우의 안전장치다.
    private IEnumerator HideHud(float feverDuration)
    {
        canvas.alpha = 0;

        yield return new WaitForSeconds(feverDuration + 2f);

        canvas.alpha = 1;
        hideHudRoutine = null;
    }

    /// <summary>피버 종료 박에서 HUD를 복구한다.</summary>
    public void EndFeverTime()
    {
        if (hideHudRoutine != null)
        {
            StopCoroutine(hideHudRoutine);
            hideHudRoutine = null;
        }

        if (canvas != null)
            canvas.alpha = 1;
    }

    #region 디버그

    public void DEBUG_FeverTime(bool isFever)
    {
        RequestFeverTime(isFever, 10f);
    }

    #endregion

}
