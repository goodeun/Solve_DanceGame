using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-400)]
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

    // 스탯은 씬을 넘어 유지하되, 플레이어 오브젝트(애니메이터 포함)는 씬마다 새로 쓴다.
    // PlayerManager가 Player 프리팹에 붙어 있어 DontDestroyOnLoad를 쓰면 이전 씬의 플레이어가 남고
    // 새 씬의 플레이어가 파괴되어, 남은 플레이어가 사라진 이전 씬의 리듬 휠을 바라보는 문제가 있었다.
    private static PlayerStats persistentStats;

    private void Awake()
    {
        Instance = this;

        if (persistentStats == null)
        {
            // TODO 임시 값, 일상 씬과 연동하여 불러오기.
            persistentStats = new PlayerStats { rhythm = 2000, groove = 1600, power = 1500, style = 5, condition = 80 };
        }

        currentStats = persistentStats;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

}
