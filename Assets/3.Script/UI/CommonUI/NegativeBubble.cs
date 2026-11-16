using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NegativeBubble : MonoBehaviour
{
    [SerializeField] private Image bubble_img;

    [SerializeField] private TextMeshProUGUI text;
    private Vector2 originPos;
    private Vector2 startPos;
    private float offsetY = 40f;

    private void Awake()
    {
        bubble_img = GetComponentInChildren<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();

        originPos = transform.position;

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // TODO 등장 애니메이션
        gameObject.transform.position = startPos;
        transform.DOMove(originPos, 0.2f);
    }

    public void DisableBubble()
    {
        // TODO 꺼지는 애니메이션
        gameObject.SetActive(false);
    }

    public void Initialize(Vector2 offsetPos)
    {
        startPos = originPos + offsetPos;
    }

    public void MoveAnchore(Stage stage) // 3, 4 번 말풍선만 해당
    {
        if (stage == Stage.Rhombus)
        {
            originPos.y = 0;
            startPos.y = 0;

        }
        else
        {
            originPos.y = offsetY;
            startPos.y = offsetY;
        }

        if (gameObject.activeSelf)
        {
            gameObject.transform.DOMoveY(originPos.y, 0.2f).SetEase(Ease.OutBack);
        }
    }



}
