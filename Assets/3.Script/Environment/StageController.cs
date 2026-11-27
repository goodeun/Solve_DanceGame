using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class StageController : MonoBehaviour
{
    public static StageController Instance { get; private set; }
    // 전 객석 이동 로직 컨트롤

    [SerializeField] private MainFloorController mainDanceFloor;
    [SerializeField] private RhythmWheelFormation rhythmWheelFormation;
    [SerializeField] private StageVisual[] seats = new StageVisual[4];

    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private float hiddenOffsetY = 5.0f;
    [SerializeField] private Ease transitionEase = Ease.InOutBack;

    [SerializeField] private Transform[] squareAnchors = new Transform[4];
    [SerializeField] private Transform[] rhombusAnchors = new Transform[4];
    [Header("Seat key order by stage (same order as Seats)")]
    [SerializeField] private Keys[] squareSeatKeys = { Keys.Q, Keys.E, Keys.A, Keys.D };
    [SerializeField] private Keys[] rhombusSeatKeys = { Keys.W, Keys.D, Keys.A, Keys.S };

    public Stage currentPhase = Stage.Square;

    private void Awake()
    {
        Instance = this;
        mainDanceFloor=GetComponent<MainFloorController>();
        rhythmWheelFormation ??= FindFirstObjectByType<RhythmWheelFormation>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int SeatCount => seats != null ? seats.Length : 0;
    public StageVisual GetSeat(int index) => seats != null && index >= 0 && index < seats.Length ? seats[index] : null;

    /// <summary>현재 배치에서 키에 해당하는 객석 번호(Seats 배열 순서). 없으면 -1.</summary>
    public int GetSeatIndexForKey(Keys key) => GetSeatIndex(key);

    /// <summary>현재 배치에서 객석 번호에 해당하는 키.</summary>
    public Keys GetKeyForSeat(int index)
    {
        Keys[] stageKeys = currentPhase == Stage.Square ? squareSeatKeys : rhombusSeatKeys;
        return stageKeys != null && index >= 0 && index < stageKeys.Length ? stageKeys[index] : Keys.Q;
    }

    public float TransitionDuration => transitionDuration;

    public StageVisual GetSeatForKey(Keys key)
    {
        int seatIndex = GetSeatIndex(key);
        return seatIndex >= 0 && seatIndex < seats.Length ? seats[seatIndex] : null;
    }

    public void ChangePhase(Stage targetPhase)
    {
        if (currentPhase == targetPhase) return;

        if (targetPhase == Stage.Square)
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
        }

        currentPhase = targetPhase;
        rhythmWheelFormation?.ApplyStage(targetPhase);
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

    private int GetSeatIndex(Keys key)
    {
        Keys[] stageKeys = currentPhase == Stage.Square ? squareSeatKeys : rhombusSeatKeys;
        if (stageKeys == null)
            return -1;

        for (int i = 0; i < stageKeys.Length && i < seats.Length; i++)
        {
            if (stageKeys[i] == key)
                return i;
        }

        return -1;
    }

    #region 디버그

    public void DEBUG_ToSquare()
    {
        ChangePhase(Stage.Square);
    }

    public void DEBUG_ToRhomBus()
    {
        ChangePhase(Stage.Rhombus);
    }

    #endregion
}
