using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleManager : MonoBehaviour
{
    [SerializeField] private char key;
    [SerializeField] private Bubble bubble_pref;

    [SerializeField] private BoxCollider spawnArea;

    private List<Bubble> positiveBubbles = new List<Bubble>();
    private List<Bubble> negativeBubbles = new List<Bubble>();

    private Queue<Bubble> disableBubbles = new Queue<Bubble>();
    private void Awake()
    {
        spawnArea = GetComponentInChildren<BoxCollider>();
    }

    private void Start()
    {
        for (int i = 0; i < 15; i++)
        {
            Bubble b = Instantiate(bubble_pref, transform);
            disableBubbles.Enqueue(b);
            b.gameObject.SetActive(false);
        }
    }

    public void AddBubble(BubbleState state)
    {
        Bubble additiveBubble;

        if (disableBubbles.Count <= 0)
        {
            disableBubbles.Enqueue(Instantiate(bubble_pref, transform));
        }

        additiveBubble = disableBubbles.Dequeue();

        Bounds bounds = spawnArea.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        additiveBubble.transform.position = new Vector3(randomX, randomY, randomZ);
        additiveBubble.Initialize(state, BubbleContentInit());

        switch (state)
        {
            case BubbleState.positive:
                positiveBubbles.Add(additiveBubble);
                break;
            case BubbleState.negative:
                negativeBubbles.Add(additiveBubble);
                break;
        }

        additiveBubble.gameObject.SetActive(true);
    }

    public string BubbleContentInit()
    {
        return "";
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
