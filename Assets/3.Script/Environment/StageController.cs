using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class StageController : MonoBehaviour
{
    // 전 객석 이동 로직 컨트롤

    [SerializeField] private MainFloorController mainDanceFloor;
    [SerializeField] private StageVisual[] seats = new StageVisual[6];

    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private float hiddenOffsetY = 5.0f;
    [SerializeField] private Ease transitionEase = Ease.InOutBack;

    [SerializeField] private Transform[] squareAnchors = new Transform[4];
    [SerializeField] private Transform[] rhombusAnchors = new Transform[4];
    [SerializeField] private Transform[] hexagonAnchors = new Transform[6];

    public Stage currentPhase = Stage.Square;

    private void Awake()
    {
        mainDanceFloor=GetComponent<MainFloorController>();
    }

    public void ChangePhase(Stage targetPhase)
    {
        if (currentPhase == targetPhase) return;

        if (targetPhase == Stage.Hexagon || targetPhase == Stage.Square)
        {
            mainDanceFloor.transform.DORotate(Vector3.zero, transitionDuration).SetEase(transitionEase);
        }
        else if (targetPhase == Stage.Rhombus)
        {
            mainDanceFloor.transform.DORotate(new Vector3(0, 45, 0), transitionDuration).SetEase(transitionEase);
        }

        // 객석 이동
        switch (targetPhase)
        {
            case Stage.Square:
                MoveSeats(squareAnchors, 4);
                break;
            case Stage.Rhombus:
                MoveSeats(rhombusAnchors, 4);
                break;
            case Stage.Hexagon:
                MoveSeats(hexagonAnchors, 4);
                AppearHiddenSeats(4, 5);
                break;
        }

        currentPhase = targetPhase;
    }

    private void MoveSeats(Transform[] targetAnchors, int activeSeatCount)
    {
        for(int i = 0; i<activeSeatCount; i++)
        {
            if (seats[i] != null && targetAnchors[i] != null)
            {
                seats[i].transform.DOMove(targetAnchors[i].position, transitionDuration).SetEase(transitionEase);
            }
        }
    }

    private void AppearHiddenSeats(int topIndex, int botIndex)
    {
        PrepareSeat(topIndex, 1f);
        PrepareSeat(botIndex, -1f);

        if (seats[topIndex] != null)
        {
            seats[topIndex].transform.DOMove(hexagonAnchors[topIndex].position, transitionDuration)
                .SetEase(transitionEase);
        }

        if (seats[botIndex] != null)
        {
            seats[botIndex].transform.DOMove(hexagonAnchors[botIndex].position, transitionDuration)
                .SetEase(transitionEase);
        }
    }

    private void PrepareSeat(int seatIndex, float direction)
    {
        if (seats[seatIndex] == null || hexagonAnchors[seatIndex] == null)
        {
            return;
        }

        seats[seatIndex].gameObject.SetActive(true);

        Vector3 startPos = hexagonAnchors[seatIndex].position;
        startPos.y += Mathf.Abs(hiddenOffsetY) * direction;

        seats[seatIndex].transform.position = startPos;
    }

    public void PlayKeyInputDrop(Keys key)
    {
        int seatIndex = GetSeatIndex(key);

        if (seatIndex >= 0 && seatIndex < seats.Length && seats[seatIndex] != null)
            seats[seatIndex].PlayKeyInputDrop();
    }

    public void ClearBubblesForComboKey(Keys key)
    {
        int seatIndex = GetSeatIndex(key);
        if (seatIndex < 0 || seatIndex >= seats.Length || seats[seatIndex] == null)
            return;

        seats[seatIndex].GetComponentInChildren<BubbleManager>()?.ClearPersistentBubbles();
    }

    private static int GetSeatIndex(Keys key) => key switch
    {
        Keys.Q => 0,
        Keys.E => 1,
        Keys.A => 2,
        Keys.D => 3,
        Keys.W => 4,
        Keys.S => 5,
        _ => -1
    };

    #region 디버그

    public void DEBUG_ToSquare()
    {
        ChangePhase(Stage.Square);
    }

    public void DEBUG_ToRhomBus()
    {
        ChangePhase(Stage.Rhombus);
    }

    public void DEBUG_To6Key()
    {
        ChangePhase(Stage.Hexagon);
    }

    #endregion
}
