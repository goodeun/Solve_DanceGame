using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // 배율 상한/하한선

    private const float MIN_MULTIPLIER = 0.5f;
    private const float MAX_MULTIPLIER = 3.0f;
    private const float FEVER_MULTIPLIER = 3.0f; // 피버 고정 배율

    // 배율 상승/하강 폭 (임시)
    //TODO 상승폭 하강폭 밸런스 테스트하며 정하기.
    private const float PENALTY_PRESSED = -0.2f;
    private const float BONUS_NEXT = 0.15f;
    private const float BONUS_OTHER = 0.05f;

    //0: Q, 1: E, 2: D, 3: A
    private float[] keyMultipliers = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };

    public bool isFeverTime = false;
    public int totalScore = 0;
    public int keyCount=4;

    public void ProcessHit(int keyIndex, Judge judge, Phase phase)
    {
        float baseScore = GetBaseScore(phase);

        float judgementRate = 0f;

        switch (judge)
        {
            case Judge.Just:judgementRate = 1.0f;
                break;
            case Judge.Good:judgementRate = 0.8f;
                break;
            case Judge.Bad:judgementRate = 0.5f;
                break;
            case Judge.Miss:judgementRate = 0f;
                break;
        }

        // 점수 획득 처리
        if(judge != Judge.Miss)
        {
            float Multiplier = isFeverTime ? FEVER_MULTIPLIER : keyMultipliers[keyIndex];

            float curScore = (baseScore * judgementRate) * Multiplier;

            int styleStat = PlayerManager.Instance.currentStats.style;
            float additionalScore = styleStat * 1.0f; // TODO 스타일 스탯 추가점수

            totalScore += Mathf.RoundToInt(curScore + additionalScore);
        }

        UpdateMultipliers(keyIndex);

    }

    public void ProcessKeyCount()
    {
        keyMultipliers = new float[6] { keyMultipliers[0], keyMultipliers[1], keyMultipliers[2], keyMultipliers[3], 1.0f, 1.0f };
        keyCount = 6;
    }

    private void UpdateMultipliers(int pressedKeyIndex)
    {
        if (isFeverTime) return;

        float styleBouns = PlayerManager.Instance.currentStats.style * 0.01f;
        int nextKeyIndex = (pressedKeyIndex + 1) % keyCount; //시계방향 다음 키 인덱스

        for (int i = 0; i < keyCount; i++)
        {
            float targetMultiplier = keyMultipliers[i];

            if(i == pressedKeyIndex)
            {
                targetMultiplier += PENALTY_PRESSED;
            }
            else if(i== nextKeyIndex)
            {
                targetMultiplier += (BONUS_NEXT + styleBouns);
            }
            else
            {
                targetMultiplier += (BONUS_OTHER + styleBouns);
            }

            keyMultipliers[i] = Mathf.Clamp(targetMultiplier, MIN_MULTIPLIER, MAX_MULTIPLIER);
        }
    }

    private float GetBaseScore(Phase phase)
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
}
