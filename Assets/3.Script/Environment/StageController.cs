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
    // Seats·앵커는 시계 방향(사각형: 좌상·우상·우하·좌하 / 마름모: 위·오른쪽·아래·왼쪽) 순서로 둔다.
    // 같은 인덱스의 좌석이 사각형 ↔ 마름모 사이를 시계 방향으로 회전하고, 키는 그 자리에 맞춰 바뀐다.
    [Header("Seat key order by stage (same order as Seats, clockwise)")]
    [SerializeField] private Keys[] squareSeatKeys = { Keys.Q, Keys.E, Keys.D, Keys.A };
    [SerializeField] private Keys[] rhombusSeatKeys = { Keys.W, Keys.D, Keys.S, Keys.A };

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

    public void ChangePhase(Stage targetPhase) => ChangePhase(targetPhase, 0f);

    /// <param name="keyApplyDelay">객석 이동은 바로 시작하고, 키 배치(입력·커맨드 층)는 이 시간(초) 뒤에 바꾼다.</param>
    public void ChangePhase(Stage targetPhase, float keyApplyDelay)
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

        if (keyApplyDelay > 0f)
        {
            if (keyApplyRoutine != null) StopCoroutine(keyApplyRoutine);
            keyApplyRoutine = StartCoroutine(ApplyKeysLater(targetPhase, keyApplyDelay));
            return;
        }

        currentPhase = targetPhase;
        rhythmWheelFormation?.ApplyStage(targetPhase);
    }

    private Coroutine keyApplyRoutine;

    private IEnumerator ApplyKeysLater(Stage targetPhase, float delay)
    {
        yield return new WaitForSeconds(delay);
        currentPhase = targetPhase;
        rhythmWheelFormation?.ApplyStage(targetPhase);
        keyApplyRoutine = null;
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
