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

    private int point3chance = 10;
    private int point6chance = 5;

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

        // 랜덤 기믹 출현
        int range = Random.Range(0, 100);
        if (range <= point6chance)
        {
            BubbleGimmick(6);
        }
        else if (range <= point6chance + point3chance)
        {
            BubbleGimmick(3);
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

        int index = 0;

        switch (key)
        {
            case Keys.Q:
                index = 0;
                break;
            case Keys.E:
                index = 1;
                break;
            case Keys.A:
                index = 2;
                break;
            case Keys.D:
                index = 3;
                break;
            case Keys.W:
                index = 4;
                break;
            case Keys.S:
                index = 5;
                break;
        }

        NegativeBubble curBubble = negativeBubble[index];

        if (! curBubble.gameObject.activeSelf) return;

        curBubble.DisableBubble();
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
        scoreManager.DecreaseMultipliers(ActiveBubble);
    }

}
