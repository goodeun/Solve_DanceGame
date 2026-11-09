using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private MultiplierUI multiplierUI;
    [SerializeField] private ScoreUI scoreUI;
    [SerializeField] private RhythmWheelUI wheelUI;

    // 배율 상한/하한선

    private const float MIN_MULTIPLIER = 0.5f;
    private const float MAX_MULTIPLIER = 3.0f;
    private const float FEVER_MULTIPLIER = 3.0f; // 피버 고정 배율

    // 배율 상승/하강 폭 (임시)
    //TODO 상승폭 하강폭 밸런스 테스트하며 정하기.
    private const float PENALTY_PRESSED = -0.2f;
    private const float BONUS_NEXT = 0.15f;
    private const float BONUS_OTHER = 0.05f;

    private readonly Keys[] squreOrder = { Keys.Q, Keys.E, Keys.D, Keys.A };
    private readonly Keys[] hexagonOrder = { Keys.Q, Keys.W, Keys.E, Keys.D, Keys.S, Keys.A };

    private Keys[] curKeyOrder;
    private Dictionary<Keys, float> keyMultipliers = new Dictionary<Keys, float>();

    public bool isFeverTime = false;
    public int totalScore = 0;
    public int keyCount = 4;
    public Phase curPhase = Phase.Pop; // 페이즈 캐싱

    private void Awake()
    {
        SetStageMode(Stage.Square);
    }

    private void OnEnable()
    {
        if (wheelUI != null)
        {
            wheelUI.OnJudged += ProcessHit;
        }
    }

    private void OnDisable()
    {
        if (wheelUI != null)
        {
            wheelUI.OnJudged -= ProcessHit;
        }
    }

    public void ProcessHit(Keys pressedKey, Judge judge, float offset)
    {
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
            case Judge.Miss:
                judgementRate = 0f;
                break;
        }

        // 점수 획득 처리
        if (judge != Judge.Miss)
        {
            float Multiplier = isFeverTime ? FEVER_MULTIPLIER : keyMultipliers[pressedKey];
            float curScore = (baseScore * judgementRate) * Multiplier;

            int styleStat = PlayerManager.Instance.currentStats.style;
            float additionalScore = styleStat * 1.0f; // TODO 스타일 스탯 추가점수

            totalScore += Mathf.RoundToInt(curScore + additionalScore);
        }

        UpdateMultipliers(pressedKey);

    }

    public float GetBaseScore(Phase phase)
    {
        PlayerStats stats = PlayerManager.Instance.currentStats;

        switch (phase)
        {
            case Phase.Pop:
                return stats.rhythm;
            case Phase.HipHop:
                return stats.groove;
            case Phase.Techno:
                return stats.power;
            default:
                return 10f;
        }
    }

    public void SetStageMode(Stage stage)
    {
        curKeyOrder = (stage == Stage.Square) ? squreOrder : hexagonOrder;

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

        float styleBouns = PlayerManager.Instance.currentStats.style * 0.01f;

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

        multiplierUI?.UpdateUI(keyMultipliers, isFeverTime) ;
    }



}
