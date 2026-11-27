using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance = null;

    public PlayerStats currentStats;

    [Tooltip("대사 시스템용 플레이어 스탠딩. PlayerStanding enum 순서(Normal, Happy, Surprised, Sad, Angry)")]
    [SerializeField] private Sprite[] standingSprites;

    public Sprite GetStanding(PlayerStanding standing)
    {
        if (standingSprites == null || standingSprites.Length == 0) return null;

        int index = (int)standing;
        if (index < 0 || index >= standingSprites.Length || standingSprites[index] == null)
            index = 0;

        return standingSprites[index];
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // TODO 임시 값, 일상 씬과 연동하여 불러오기.
        currentStats = new PlayerStats { rhythm = 2000, groove = 1600, power = 1500, style = 5, condition = 80 };
    }

}
