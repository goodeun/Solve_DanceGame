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
            floorSprite.color = transparent;
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
            Color whiteAlpha = Color.white;
            whiteAlpha.a = 0.8f;
            floorSprite.color = whiteAlpha;
        }
        else
        {
            Color blinkColor = Color.white * secondaryColor;
            blinkColor.a = 1f;

            floorSprite.color = transparent;
            floorSprite.DOColor(blinkColor, 0.05f).SetLoops(1, LoopType.Yoyo);
        }
    }

    private void HandleMeasureEnd(bool isComboSuccess)
    {
        isActivated = false;

        floorSprite.DOKill();

        if (isComboSuccess)
        {
            Color burstColor = floorSprite.color;
            burstColor.a = 1f;

            Sequence burstSeq = DOTween.Sequence();
            burstSeq.Append(floorSprite.DOColor(burstColor, 0.05f))
                .Append(floorSprite.DOColor(transparent, burstDuration).SetEase(Ease.InExpo));
        }
        else
        {
            floorSprite.DOColor(transparent, fadeDuration);
        }
    }

    private void ChangeIdleSprite()
    {
        if (isActivated) return;

        int randomIndex = Random.Range(0, idleSprites.Length);
        floorSprite.sprite = idleSprites[randomIndex];

    }
}
