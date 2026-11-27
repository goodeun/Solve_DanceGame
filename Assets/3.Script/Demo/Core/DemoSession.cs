using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬을 넘어 유지되는 데모 진행 기록(일반전 결과, 보스전 결과).
/// 결과 씬에서 최종 점수·판정 개수·콤보·랭크를 계산하는 데 사용한다.
/// </summary>
public static class DemoSession
{
    public static int NormalScore;
    public static int BossScore;
    public static readonly int[] JudgeCounts = new int[4];
    public static int MaxCombo;

    public static bool BossPlayed;
    public static bool BossWon;
    public static int PlayerSeats;
    public static int RivalSeats;
    public static int NeutralSeats;
    public static int PlayerMoodTotal;
    public static int RivalMoodTotal;

    public static bool TutorialSkipped;

    /// <summary>라이벌전 객석별 결과(결과 씬 땅따먹기 공개용). 라이벌 몫 = maxPoints - playerPoints.</summary>
    public struct SeatResult
    {
        public string key;
        public int playerPoints;
        public int maxPoints;
        public int RivalPoints => Mathf.Max(0, maxPoints - playerPoints);
        public float PlayerShare => maxPoints > 0 ? playerPoints / (float)maxPoints : 0f;
        /// <summary>1 = 플레이어, -1 = 라이벌, 0 = 동률</summary>
        public int Owner => playerPoints > RivalPoints ? 1 : RivalPoints > playerPoints ? -1 : 0;
    }

    public static readonly System.Collections.Generic.List<SeatResult> Seats = new System.Collections.Generic.List<SeatResult>();

    public static int FinalScore => NormalScore + BossScore;

    public static void Reset()
    {
        NormalScore = 0;
        BossScore = 0;
        System.Array.Clear(JudgeCounts, 0, JudgeCounts.Length);
        MaxCombo = 0;
        BossPlayed = false;
        BossWon = false;
        PlayerSeats = RivalSeats = NeutralSeats = 0;
        PlayerMoodTotal = RivalMoodTotal = 0;
        TutorialSkipped = false;
        Seats.Clear();
    }

    /// <summary>한 전투(일반전·보스전)의 판정 개수와 최대 콤보를 누적한다.</summary>
    public static void AccumulateBattle(ScoreManager score, ComboAndMagnification combo)
    {
        if (score != null)
            for (int i = 0; i < JudgeCounts.Length; i++)
                JudgeCounts[i] += score.GetJudgeCount((Judge)i);

        if (combo != null)
            MaxCombo = Mathf.Max(MaxCombo, combo.MaxBarCombo);
    }
}

/// <summary>데모 씬 전환. 씬마다 따로 존재하는 DontDestroyOnLoad HUD를 정리한 뒤 로드한다.</summary>
public static class SceneFlow
{
    public static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneFlow: scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneFlow: '{sceneName}' is not in Build Settings. Use the menu Solve/Demo/Register Demo Scenes In Build Settings.");
            return;
        }

        Time.timeScale = 1f;

        // HUDManager는 DontDestroyOnLoad 싱글턴이지만 각 댄스 씬이 자기 MasterCanvas를 가지고 있다.
        // 이전 씬의 HUD가 남아 있으면 다음 씬의 HUD가 파괴되므로 전환 전에 해제한다.
        HUDManager.ReleaseForSceneChange();

        SceneManager.LoadScene(sceneName);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
