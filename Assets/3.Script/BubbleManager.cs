using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleManager : MonoBehaviour
{
    [SerializeField] private char key;

    private List<Bubble> possitiveBubbles;
    private List<Bubble> negativeBubbles;

    private List<Bubble> disableBubbles;

    private Vector3 appearArea; // 말풍선 출현 범위 (스케일)


    public int PopPossitiveBubble() // 터진 말풍선 갯수 반환. 점수로 치환 바람
    {
        int poped = 0;

        foreach(Bubble bubble in possitiveBubbles)
        {
            bubble.gameObject.SetActive(false);
            disableBubbles.Add(bubble);
            possitiveBubbles.Remove(bubble);
            poped++;
        }

        return poped;
    }

    public void PopNegativeBubble()
    {
        foreach(Bubble bubble in negativeBubbles)
        {
            bubble.gameObject.SetActive(false);
            disableBubbles.Add(bubble);
            negativeBubbles.Remove(bubble);
        }
    }
}
