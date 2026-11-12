using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleManager : MonoBehaviour
{
    [SerializeField] private Bubble bubble_pref;

    [SerializeField] private BoxCollider spawnArea;

    [Header("말풍선 콜라이더 방향")]
    [SerializeField] private bool IsColiderRotation = false;

    [Header("Gameplay Spawn")]
    [SerializeField] private bool spawnRandomBubblesDuringGameplay = true;
    [SerializeField, Min(0f)] private float initialSpawnDelay = 1f;
    [SerializeField, Min(0f)] private float randomSpawnDelay = 5f;
    [SerializeField, Range(0f, 1f)] private float positiveChance = 0.8f;

    [Header("Camera Visibility")]
    [SerializeField] private bool keepSpawnInsideCameraView = true;
    [SerializeField, Range(0f, 0.45f)] private float cameraViewportMargin = 0.08f;
    [SerializeField] private Camera targetCamera;

    private List<Bubble> positiveBubbles = new List<Bubble>();
    private List<Bubble> negativeBubbles = new List<Bubble>();
    private List<Bubble> persistentBubbles = new List<Bubble>();

    private Queue<Bubble> disableBubbles = new Queue<Bubble>();
    private Camera cachedCamera;

    // All seats share one spawn sequence: one new bubble at a time, placed
    // at a randomly selected seat. The bubbles themselves remain active.
    private static BubbleManager gameplaySpawnController;

    private void Start()
    {
        for (int i = 0; i < 15; i++)
        {
            Bubble b = Instantiate(bubble_pref, transform);
            disableBubbles.Enqueue(b);
            b.gameObject.SetActive(false);
        }

        if (spawnRandomBubblesDuringGameplay && gameplaySpawnController == null)
        {
            gameplaySpawnController = this;
            StartCoroutine(SpawnRandomBubbles());
        }
    }

    private void OnDisable()
    {
        if (gameplaySpawnController == this)
            gameplaySpawnController = null;
    }

    private IEnumerator SpawnRandomBubbles()
    {
        while (true)
        {
            yield return new WaitForSeconds(initialSpawnDelay + Random.Range(0f, randomSpawnDelay));

            BubbleManager[] seats = FindObjectsByType<BubbleManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<BubbleManager> eligibleSeats = new List<BubbleManager>();

            foreach (BubbleManager seat in seats)
                if (seat.spawnRandomBubblesDuringGameplay && seat.spawnArea != null)
                    eligibleSeats.Add(seat);

            if (eligibleSeats.Count == 0)
                continue;

            BubbleManager selectedSeat = eligibleSeats[Random.Range(0, eligibleSeats.Count)];
            selectedSeat.AddBubble(
                Random.value <= selectedSeat.positiveChance ? BubbleState.positive : BubbleState.negative,
                true);
        }
    }

    public void AddBubble(BubbleState state)
    {
        AddBubble(state, false);
    }

    private void AddBubble(BubbleState state, bool persistent)
    {
        Bubble additiveBubble;

        if (disableBubbles.Count <= 0)
        {
            Bubble newBubble = Instantiate(bubble_pref, transform);
            newBubble.gameObject.SetActive(false);
            disableBubbles.Enqueue(newBubble);
        }

        additiveBubble = disableBubbles.Dequeue();

        if (!IsColiderRotation)
        {
            // 1. 말풍선 항상 정면 보는 방식
            Bounds bounds = spawnArea.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            additiveBubble.transform.position = new Vector3(randomX, randomY, randomZ);
        }
        else
        {
            // 2. 말풍선 콜라이더 방향 보는 방식
            Vector3 center = spawnArea.center;
            Vector3 extents = spawnArea.size / 2f;

            float randomX = Random.Range(center.x - extents.x, center.x + extents.x);
            float randomY = Random.Range(center.y - extents.y, center.y + extents.y);
            float randomZ = Random.Range(center.z - extents.z, center.z + extents.z);
            Vector3 localRandomPos = new Vector3(randomX, randomY, randomZ);

            additiveBubble.transform.position = spawnArea.transform.TransformPoint(localRandomPos);
            additiveBubble.transform.rotation = spawnArea.transform.rotation;
        }

        additiveBubble.transform.position = ClampSpawnPositionToCamera(additiveBubble.transform.position);

        //TODO 플레이어 전체 스탯 객체 가져와야 함
        PlayerStats myStats = new PlayerStats { rhythm = 20, groove = 15, power = 10, style = 5, condition = 80 };
        string targetLine = DialogueManager.Instance.GetDialogue("Dance", state.ToString(), myStats);

        additiveBubble.Initialize(state, targetLine);

        switch (state)
        {
            case BubbleState.positive:
                if (!persistent)
                    positiveBubbles.Add(additiveBubble);
                break;
            case BubbleState.negative:
                if (!persistent)
                    negativeBubbles.Add(additiveBubble);
                break;
        }

        if (persistent)
            persistentBubbles.Add(additiveBubble);

        additiveBubble.gameObject.SetActive(true);
    }

    private Vector3 ClampSpawnPositionToCamera(Vector3 worldPosition)
    {
        if (!keepSpawnInsideCameraView)
            return worldPosition;

        cachedCamera ??= targetCamera != null ? targetCamera : Camera.main;
        if (cachedCamera == null)
            return worldPosition;

        Vector3 viewportPosition = cachedCamera.WorldToViewportPoint(worldPosition);
        if (viewportPosition.z <= 0f)
            return worldPosition;

        viewportPosition.x = Mathf.Clamp(viewportPosition.x, cameraViewportMargin, 1f - cameraViewportMargin);
        viewportPosition.y = Mathf.Clamp(viewportPosition.y, cameraViewportMargin, 1f - cameraViewportMargin);
        return cachedCamera.ViewportToWorldPoint(viewportPosition);
    }

    public void ClearPersistentBubbles()
    {
        foreach (Bubble bubble in persistentBubbles)
        {
            bubble.Despawn_Anim(() =>
            {
                bubble.gameObject.SetActive(false);
                disableBubbles.Enqueue(bubble);
            });
        }

        persistentBubbles.Clear();
    }

    public int PopPositiveBubble() // 터진 말풍선 갯수 반환. 점수로 치환 바람
    {
        int poped = 0;

        foreach (Bubble bubble in positiveBubbles)
        {
            bubble.Despawn_Anim(() =>
            {
                bubble.gameObject.SetActive(false);
                disableBubbles.Enqueue(bubble);
            });
            poped++;
        }

        positiveBubbles.Clear();
        return poped;
    }

    public void PopNegativeBubble()
    {
        foreach(Bubble bubble in negativeBubbles)
        {
            bubble.Despawn_Anim(() =>
            {
                bubble.gameObject.SetActive(false);
                disableBubbles.Enqueue(bubble);
            });
        }
        negativeBubbles.Clear();
    }

    #region 디버그

    public void DEBUG_AppearBubble()
    {
        AddBubble(BubbleState.positive);
    }

    public void DEBUG_BubbleTest()
    {
        StartCoroutine(DEBUG_BubbleTest_co());
    }

    public IEnumerator DEBUG_BubbleTest_co()
    {
        for (int i = 0; i < 5; i++)
        {
            AddBubble(BubbleState.positive);
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(2f);

        AddBubble(BubbleState.negative);

        yield return new WaitForSeconds(2f);

        PopPositiveBubble();

        yield return new WaitForSeconds(2f);

        PopNegativeBubble();
    }

    #endregion
}
