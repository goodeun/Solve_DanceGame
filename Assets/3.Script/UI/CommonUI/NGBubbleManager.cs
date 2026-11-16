using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NGBubbleManager : MonoBehaviour
{

    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private NegativeBubble[] negativeBubble;

    private int point3chance = 10;
    private int point6chance = 5;

    private Stage stage;

    public int ActiveBubble { get; private set; } = 0;

    private void Awake()
    {
        // 이벤트 구독
    }
    private void Start()
    {
        scoreManager = FindAnyObjectByType<ScoreManager>();

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
    }
    private void OnDestroy()
    {
        // 이벤트 해제
    }

    private void observeBubble(Keys k, int just, int good, int bad, int miss, KeyCommand command)
    {
        // 이벤트 구독 후 갯수 새고 넘김
        CreateBubble(bad + miss);

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
        if (command != KeyCommand.ABA) return;

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

        // 빈자리 계산 후 반환
        for (int i = 0; i < negativeBubble.Length; i++)
        {
            if (!negativeBubble[i].gameObject.activeSelf)
            {
                bubble = negativeBubble[i];
                return true;
            }
        }
        return false;
    }

    private void MoveStage(Stage _stage)
    {
        negativeBubble[2].MoveAnchore(_stage);
        negativeBubble[3].MoveAnchore(_stage);
        stage = _stage;
    }

    private void CaculateDecrease(int ActiveBubble)
    {
        // 배율 하락
        scoreManager.DecreaseMultipliers(ActiveBubble);
    }

}
