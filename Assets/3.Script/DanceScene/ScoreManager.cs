using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public event Action<Keys, int> OnPointsAdded;
    /// <summary>키와 무관한 득점(피버 자동 득점, 콤보 보너스 등).</summary>
    public event Action<int> OnBonusPointsAdded;

    [SerializeField] private MultiplierUI multiplierUI;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private ComboAndMagnification comboMagnificater;
    [SerializeField] private RhythmWheelFever fever;

    // 배율 상한/하한선s

    private const float MIN_MULTIPLIER = 0.3f;
    private const float MAX_MULTIPLIER = 3.0f;
    private const float FEVER_MULTIPLIER = 3.0f; // 피버 고정 배율

    // 배율 상승/하강 폭 (임시)
    //TODO 상승폭 하강폭 밸런스 테스트하며 정하기.
    private const float PENALTY_PRESSED = -0.2f;
    private const float PENALTY_BUBBLE = -0.01f;
    private const float BONUS_NEXT = 0.1f;
    private const float BONUS_OTHER = 0.03f;

    [Header("Fever")]
    [Tooltip("피버 중 마디당 자동 득점 = 피버 시작 속성 기본 점수 × 이 값 × 피버 배수")]
    [SerializeField, Min(0f)] private float feverScorePerBar = 3f;

    [Header("Combo")]
    [Tooltip("마디 성공 시 보너스 = 현재 마디 콤보 × 이 값")]
    [SerializeField, Min(0)] private int comboBonusPerCombo = 50;
    [SerializeField, Min(0)] private int comboBonusMaxCombo = 50;

    [Header("ABC Command (일반전)")]
    [Tooltip("ABC 커맨드 성공 시 마지막 키의 배율 감소를 이 비율만큼 되돌린다. 1 = 감소 없음")]
    [SerializeField, Range(0f, 1f)] private float abcPenaltyRelief = 0.75f;

    // 사각형: 시계 방향 Q → E → D → A, 마름모: 시계 방향 W → D → S → A
    private readonly Keys[] squreOrder = { Keys.Q, Keys.E, Keys.D, Keys.A };
    private readonly Keys[] rhombusOrder = { Keys.W, Keys.D, Keys.S, Keys.A };

    private Keys[] curKeyOrder;
    private Dictionary<Keys, float> keyMultipliers = new Dictionary<Keys, float>();
    private readonly int[] judgeCounts = new int[4];

    public bool isFeverTime = false;
    public int totalScore = 0;
    public int keyCount = 4;
    public Phase curPhase = Phase.Pop; // 페이즈 캐싱
    private Phase feverPhase = Phase.Pop; // 피버 시작 시점의 속성

    public int TotalScore => totalScore;
    public Stage CurrentStage { get; private set; } = Stage.Square;

    public int GetJudgeCount(Judge judge) => judgeCounts[(int)judge];

    private void Awake()
    {
        if (comboMagnificater == null) comboMagnificater = FindFirstObjectByType<ComboAndMagnification>();
        if (multiplierUI == null) multiplierUI = FindFirstObjectByType<MultiplierUI>();
        if (scoreUI == null) scoreUI = FindFirstObjectByType<ScoreUI>();
        if (fever == null) fever = FindFirstObjectByType<RhythmWheelFever>();
        SetStageMode(Stage.Square);
    }

    private void OnEnable()
    {
        if (comboMagnificater != null)
        {
            comboMagnificater.OnJudged += ProcessHit;
            comboMagnificater.OnComboSaved += HandleComboSaved;
            comboMagnificater.OnPhraseHandoff += HandlePhraseHandoff;
        }
        else
        {
            Debug.LogError("ScoreManager: ComboAndMagnification was not found; score and multiplier updates are disabled.", this);
        }

        if (fever != null)
        {
            fever.OnFeverStarted += HandleFeverStarted;
            fever.OnFeverBar += HandleFeverBar;
            fever.OnFeverFinished += HandleFeverFinished;
        }
    }

    private void OnDisable()
    {
        if (comboMagnificater != null)
        {
            comboMagnificater.OnJudged -= ProcessHit;
            comboMagnificater.OnComboSaved -= HandleComboSaved;
            comboMagnificater.OnPhraseHandoff -= HandlePhraseHandoff;
        }

        if (fever != null)
        {
            fever.OnFeverStarted -= HandleFeverStarted;
            fever.OnFeverBar -= HandleFeverBar;
            fever.OnFeverFinished -= HandleFeverFinished;
        }
    }

    public void ProcessHit(Keys pressedKey, Judge judge, float offset)
    {
        judgeCounts[(int)judge]++;

        // 자동 Miss는 키 정보가 없으므로(default) 배율을 건드리지 않는다.
        if (judge == Judge.Miss) return;

        if (System.Array.IndexOf(curKeyOrder, pressedKey) == -1) return;

        float baseScore = GetBaseScore(curPhase);
        float judgementRate = 0f;

        switch (judge)
        {
            case Judge.Just:
                judgementRate = 1.0f;
                break;
            case Judge.Good:
                judgementRate = 0.8f;
                break;
            case Judge.Bad:
                judgementRate = 0.5f;
                break;
        }

        // 점수 획득 처리
        float Multiplier = isFeverTime ? FEVER_MULTIPLIER : keyMultipliers[pressedKey];
        float curScore = (baseScore * judgementRate) * Multiplier;

        int styleStat = PlayerManager.Instance != null ? PlayerManager.Instance.currentStats.style : 0;
        float additionalScore = styleStat * 1.0f; // TODO 스타일 스탯 추가점수

        int addedScore = Mathf.RoundToInt(curScore + additionalScore);
        totalScore += addedScore;
        OnPointsAdded?.Invoke(pressedKey, addedScore);
        scoreUI?.ScoreUpdate(totalScore);
        scoreUI?.ShowAddedScore(addedScore);

        UpdateMultipliers(pressedKey);
    }

    /// <summary>키와 무관한 점수를 더한다.</summary>
    public void AddBonusScore(int amount)
    {
        if (amount <= 0) return;

        totalScore += amount;
        OnBonusPointsAdded?.Invoke(amount);
        scoreUI?.ScoreUpdate(totalScore);
        scoreUI?.ShowAddedScore(amount);
    }

    private void HandleComboSaved(Keys _)
    {
        if (comboMagnificater == null || comboBonusPerCombo <= 0) return;

        int combo = Mathf.Min(comboMagnificater.BarCombo, comboBonusMaxCombo);
        AddBonusScore(combo * comboBonusPerCombo);
    }

    private void HandlePhraseHandoff(Keys lastKey, int just, int good, int bad, int miss, KeyCommand command)
    {
        // 보스전의 ABC는 라이벌 분위기 감소(BossBattleDirector)로 처리한다.
        if (command != KeyCommand.ABC || curPhase == Phase.Villain || isFeverTime) return;
        if (!keyMultipliers.ContainsKey(lastKey)) return;

        keyMultipliers[lastKey] = Mathf.Clamp(keyMultipliers[lastKey] - PENALTY_PRESSED * abcPenaltyRelief, MIN_MULTIPLIER, MAX_MULTIPLIER);
        multiplierUI?.UpdateUI(keyMultipliers, isFeverTime);
    }

    private void HandleFeverStarted()
    {
        isFeverTime = true;
        feverPhase = curPhase;
        multiplierUI?.UpdateUI(keyMultipliers, isFeverTime);
    }

    private void HandleFeverBar(int barIndex)
    {
        AddBonusScore(Mathf.RoundToInt(GetBaseScore(feverPhase) * feverScorePerBar * FEVER_MULTIPLIER));
    }

    private void HandleFeverFinished()
    {
        isFeverTime = false;
        multiplierUI?.UpdateUI(keyMultipliers, isFeverTime);
    }

    public float GetBaseScore(Phase phase)
    {
        if (PlayerManager.Instance == null) return 10f;

        PlayerStats stats = PlayerManager.Instance.currentStats;

        switch (phase)
        {
            case Phase.Pop:
                return stats.rhythm;
            case Phase.HipHop:
                return stats.groove;
            case Phase.Techno:
                return stats.power;
            case Phase.Villain:
                return (stats.rhythm + stats.groove + stats.power) / 3f;
            default:
                return 10f;
        }
    }

    public void SetPhase(Phase phase)
    {
        curPhase = phase;
    }

    public void SetStageMode(Stage stage)
    {
        CurrentStage = stage;
        curKeyOrder = (stage == Stage.Square) ? squreOrder : rhombusOrder;

        foreach (Keys k in curKeyOrder)
        {
            if (!keyMultipliers.ContainsKey(k))
            {
                keyMultipliers[k] = 1.0f;
            }
        }
        multiplierUI?.UpdateUI(keyMultipliers, isFeverTime);
    }

    private void UpdateMultipliers(Keys pressedKey)
    {
        if (isFeverTime) return;

        int pressedIndex = System.Array.IndexOf(curKeyOrder, pressedKey);
        int keyCount = curKeyOrder.Length;
        int nextKeyIndex = (pressedIndex + 1) % keyCount; //시계방향 다음 키 인덱스

        float styleBouns = (PlayerManager.Instance != null ? PlayerManager.Instance.currentStats.style : 0) * 0.01f;

        for (int i = 0; i < keyCount; i++)
        {
            Keys targetKey = curKeyOrder[i];
            float targetValue = keyMultipliers[targetKey];

            if (i == pressedIndex)
            {
                targetValue += PENALTY_PRESSED;
            }
            else if (i == nextKeyIndex)
            {
                targetValue += (BONUS_NEXT + styleBouns);
            }
            else
            {
                targetValue += (BONUS_OTHER + styleBouns);
            }

            keyMultipliers[targetKey] = Mathf.Clamp(targetValue, MIN_MULTIPLIER, MAX_MULTIPLIER);
        }

        multiplierUI?.UpdateUI(keyMultipliers, isFeverTime);
    }

    public void DecreaseMultipliers(int activeBubble) => DecreaseMultipliers(activeBubble, 1f);

    /// <summary>방치된 태클 말풍선 수만큼 전체 배율을 낮춘다. scale은 보스전 강화용.</summary>
    public void DecreaseMultipliers(int activeBubble, float scale)
    {
        if (isFeverTime) return;

        foreach (Keys key in curKeyOrder)
        {
            keyMultipliers[key] = Mathf.Clamp(
                keyMultipliers[key] + (PENALTY_BUBBLE * activeBubble * scale),
                MIN_MULTIPLIER,
                MAX_MULTIPLIER
                );
        }

        multiplierUI?.UpdateUI(keyMultipliers, isFeverTime);
    }

}
