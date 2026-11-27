using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NGBubbleManager : MonoBehaviour
{
    [SerializeField] private ComboAndMagnification comboAndMagnification;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private NegativeBubble[] negativeBubble;
    [SerializeField, Min(0)] private int pointsLostPerBubblePerBeat = 10;
    [SerializeField, Min(0)] private int villainFeverPointsPerBeat = 10;

    private FmodMusicClock musicClock;
    private RhythmWheelFever fever;

    [Header("Tackle Gimmick (3개 객석 동시 태클)")]
    [Tooltip("일반전에서 마디마다 3개 동시 태클이 나올 확률(%). 기획상 보스전 기믹이므로 기본 0")]
    [SerializeField, Range(0, 100)] private int normalTripleChance = 0;
    [Tooltip("보스전에서 마디마다 3개 동시 태클이 나올 확률(%)")]
    [SerializeField, Range(0, 100)] private int bossTripleChance = 20;
    [Tooltip("보스전에서 마디마다 태클 1개가 추가로 나올 확률(%) - 더 높은 출현 빈도")]
    [SerializeField, Range(0, 100)] private int bossExtraBubbleChance = 15;
    [Tooltip("보스전 방치 불이익 배수(배율 하락)")]
    [SerializeField, Min(1f)] private float bossPenaltyScale = 2f;

    /// <summary>튜토리얼 등에서 무작위 태클 생성을 끈다(Miss로 생기는 태클은 유지).</summary>
    public bool RandomGimmickEnabled { get; set; } = true;

    private bool IsBossBattle => PhaseController.Instance != null && PhaseController.Instance.CurrentPhase == Phase.Villain;

    private Stage stage = Stage.Square;

    public int ActiveBubble { get; private set; } = 0;

    private void Start()
    {
        scoreManager = FindAnyObjectByType<ScoreManager>();
        comboAndMagnification = FindAnyObjectByType<ComboAndMagnification>();
        musicClock = FindAnyObjectByType<FmodMusicClock>();
        fever = FindAnyObjectByType<RhythmWheelFever>();

        if (comboAndMagnification != null)
            comboAndMagnification.OnPhraseHandoff += observeBubble;
        if (musicClock != null)
            musicClock.TimelineBeat += HandleTimelineBeat;
        if (fever != null)
        {
            fever.OnFeverStarted += HandleFeverStarted;
            if (fever.IsActive)
                ClearAllNegativeBubbles();
        }

        float offset = 50f;

        for (int i = 0; i < negativeBubble.Length; i++)
        {
            if (i == 0 || i == 2)
            {
                negativeBubble[i].Initialize(new Vector2(-offset, 0));
            }
            else if (i == 1 || i == 3)
            {
                negativeBubble[i].Initialize(new Vector2(offset, 0));
            }
            else if (i == 4)
            {
                negativeBubble[i].Initialize(new Vector2(0, offset));
            }
            else
            {
                negativeBubble[i].Initialize(new Vector2(0, -offset));
            }
        }
        MoveStage(stage);
    }
    private void OnDestroy()
    {
        if (comboAndMagnification != null)
            comboAndMagnification.OnPhraseHandoff -= observeBubble;
        if (musicClock != null)
            musicClock.TimelineBeat -= HandleTimelineBeat;
        if (fever != null)
            fever.OnFeverStarted -= HandleFeverStarted;
    }

    private void HandleFeverStarted() => ClearAllNegativeBubbles();

    private void ClearAllNegativeBubbles()
    {
        if (negativeBubble != null)
            foreach (NegativeBubble bubble in negativeBubble)
                if (bubble != null && bubble.gameObject.activeSelf)
                    bubble.DisableBubble();

        ActiveBubble = 0;
    }

    private void HandleTimelineBeat(int bar, int beatInBar, float tempo, int upper, int lower)
    {
        PhaseController phaseController = PhaseController.Instance;
        if (phaseController == null || phaseController.CurrentPhase != Phase.Villain)
            return;

        if (fever != null && fever.IsActive)
            phaseController.AddPointsToAllSeats(villainFeverPointsPerBeat);

        StageController stageController = StageController.Instance;
        if (stageController == null || negativeBubble == null)
            return;

        for (int i = 0; i < negativeBubble.Length; i++)
        {
            if (negativeBubble[i] == null || !negativeBubble[i].gameObject.activeInHierarchy)
                continue;

            Keys key = i switch
            {
                0 => Keys.Q,
                1 => Keys.E,
                2 => Keys.A,
                3 => Keys.D,
                4 => Keys.W,
                5 => Keys.S,
                _ => (Keys)(-1)
            };

            if ((int)key < 0)
                continue;

            stageController.GetSeatForKey(key)?.RemovePlayerPoints(pointsLostPerBubblePerBeat);
        }
    }

    private void observeBubble(Keys k, int just, int good, int bad, int miss, KeyCommand command)
    {
        if (fever != null && fever.IsActive)
            return;

        // 이벤트 구독 후 갯수 새고 넘김
        CreateBubble(miss);

        // 랜덤 기믹 출현 (6개 동시 생성은 폐기, 3개 동시 생성만 유지)
        if (RandomGimmickEnabled)
        {
            bool boss = IsBossBattle;
            int tripleChance = boss ? bossTripleChance : normalTripleChance;
            if (Random.Range(0, 100) < tripleChance)
                BubbleGimmick(3);
            else if (boss && Random.Range(0, 100) < bossExtraBubbleChance)
                BubbleGimmick(1);
        }

        RemoveBubble(command, k);
        CaculateDecrease(ActiveBubble);
    }

    private void CreateBubble(int count)
    {
        if (count <= 0) return;

        for (int i = 0; i<count; i++)
        {
            if (!GetBubble(out NegativeBubble bubble)) continue;
            bubble.gameObject.SetActive(true);
            ActiveBubble++;
        }
    }

    private void RemoveBubble(KeyCommand command, Keys key)
    {
        if (command != KeyCommand.AAB) return;

        int index = KeyToIndex(key);
        if (index < 0 || index >= negativeBubble.Length) return;

        NegativeBubble curBubble = negativeBubble[index];

        if (! curBubble.gameObject.activeSelf) return;

        curBubble.Pop(key);
        ActiveBubble--;
    }

    private void BubbleGimmick(int point)
    {
        for (int i = 0; i < point; i++)
        {
            if (!GetBubble(out NegativeBubble bubble)) continue;
            bubble.gameObject.SetActive(true);
            ActiveBubble++;
        }
    }

    private bool GetBubble(out NegativeBubble bubble)
    {
        bubble = null;
        int start = (stage == Stage.Square) ? 0 : 2;
        int end = (stage == Stage.Square) ? 4 : 6;

        // 빈자리 계산 후 반환

        for (int i = start; i < end; i++)
        {
            if (!negativeBubble[i].gameObject.activeSelf)
            {
                bubble = negativeBubble[i];
                return true;
            }
        }
        return false;
    }

    public void MoveStage(Stage _stage)
    {
        negativeBubble[2].MoveAnchore(_stage);
        negativeBubble[3].MoveAnchore(_stage);

        stage = _stage;

        int start = (stage == Stage.Square) ? 4 : 0;
        int end = (stage == Stage.Square) ? 6 : 2;

        for (int i = start; i < end; i++)
        {
            NegativeBubble b = negativeBubble[i];
            if (b.gameObject.activeSelf)
            {
                b.DisableBubble();

                if (!GetBubble(out NegativeBubble bubble))
                {
                    ActiveBubble--;
                    continue;
                }
                bubble.gameObject.SetActive(true);
            }
        }
    }

    private void CaculateDecrease(int ActiveBubble)
    {
        // 배율 하락
        scoreManager?.DecreaseMultipliers(ActiveBubble, IsBossBattle ? bossPenaltyScale : 1f);
    }

    // 말풍선 배열 순서: 0 Q, 1 E, 2 A, 3 D, 4 W, 5 S
    private static int KeyToIndex(Keys key) => key switch
    {
        Keys.Q => 0,
        Keys.E => 1,
        Keys.A => 2,
        Keys.D => 3,
        Keys.W => 4,
        Keys.S => 5,
        _ => -1
    };

    /// <summary>해당 키 객석에 태클 말풍선이 떠 있는지.</summary>
    public bool IsBubbleActive(Keys key)
    {
        int index = KeyToIndex(key);
        return negativeBubble != null && index >= 0 && index < negativeBubble.Length
            && negativeBubble[index] != null && negativeBubble[index].gameObject.activeSelf;
    }

    /// <summary>현재 배치의 빈 객석 하나에 태클 말풍선을 띄운다(튜토리얼·연출용).</summary>
    public bool SpawnBubble()
    {
        if (!GetBubble(out NegativeBubble bubble)) return false;

        bubble.gameObject.SetActive(true);
        ActiveBubble++;
        return true;
    }

    /// <summary>현재 배치의 빈 객석 count개에 동시에 태클을 띄운다.</summary>
    public int SpawnBubbles(int count)
    {
        int spawned = 0;
        for (int i = 0; i < count; i++)
            if (SpawnBubble()) spawned++;
        return spawned;
    }

    public void ClearBubbles() => ClearAllNegativeBubbles();

}
