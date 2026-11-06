using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FloorSection : MonoBehaviour
{
    [SerializeField] private SpriteRenderer floorSprite;

    [Header("Tween Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float burstDuration = 0.5f;

    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite activeSprite;

    private bool isActivated = false;

    private Color secondaryColor = Color.magenta; // 임시 컬러, 스테이지 색상 따라 바뀔 수 있도록 조정
    private Color transparent = new Color(1, 1, 1, 0);

    private void Awake()
    {
        if (floorSprite == null)
        {
            floorSprite = GetComponent<SpriteRenderer>();
        }
    }
    private void OnEnable()
    {
        TempEventManager.OnThemaColorChange_Temp += UpdateThemaColor;
        TempEventManager.OnMeasureEnd_Temp += HandleMeasureEnd;
        TempEventManager.OnBeat_Temp += ChangeIdleSprite;
    }
    private void OnDisable()
    {
        TempEventManager.OnThemaColorChange_Temp -= UpdateThemaColor;
        TempEventManager.OnMeasureEnd_Temp -= HandleMeasureEnd;
        TempEventManager.OnBeat_Temp -= ChangeIdleSprite;
    }

    private void UpdateThemaColor(Color newColor)
    {
        secondaryColor = newColor;
    }

    public void ActiveLight()
    {
        floorSprite.DOKill();

        if (!isActivated)
        {

        }
        else
        {

        }
    }

    private void HandleMeasureEnd(bool isComboSuccess)
    {
        isActivated = false;

        floorSprite.DOKill();

        if (isComboSuccess)
        {
        }
        else
        {
        }
    }

    private void ChangeIdleSprite()
    {
        if (isActivated) return;

        int randomIndex = Random.Range(0, idleSprites.Length);
        floorSprite.sprite = idleSprites[randomIndex];

    }
}
