using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance = null;

    public PlayerStats currentStats;

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
        currentStats = new PlayerStats { rhythm = 20, groove = 15, power = 10, style = 5, condition = 80 };
    }

}
