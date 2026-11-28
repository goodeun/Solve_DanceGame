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
    private RectTransform rectTransform;
    private Vector2 originPos;
    private Vector2 startPos;
    [SerializeField] private float offsetY = -40f;
    [Tooltip("Spine 말풍선(있으면 등장·꿈틀·터짐을 Spine으로). 비우면 자식에서 찾는다")]
    [SerializeField] private SpineBubbleSkin spineSkin;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        bubble_img = GetComponentInChildren<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();

        originPos = rectTransform != null ? rectTransform.anchoredPosition : (Vector2)transform.localPosition;
        if (spineSkin == null) spineSkin = GetComponentInChildren<SpineBubbleSkin>(true);

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (spineSkin != null) spineSkin.PlayAppear();
        transform.DOKill();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPos;
            rectTransform.DOAnchorPos(originPos, 0.2f);
        }
        else
        {
            transform.localPosition = startPos;
            transform.DOLocalMove(originPos, 0.2f);
        }
    }

    public void DisableBubble()
    {
        // TODO 꺼지는 애니메이션
        transform.DOKill();
        transform.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 커맨드(AAB)로 태클을 물리쳤을 때: 액체(MetaBall)로 터지고 말풍선은 눌렸다가 사라진다.
    /// </summary>
    public void Pop(Keys seatKey)
    {
        if (!gameObject.activeSelf) return;

        // 화면 위치에서 액체 파티클을 터뜨린다(카메라 가까운 고정 깊이 → 화면에서 충분히 크게).
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, transform.position);
        float depth = LiquidBurst.DefaultDepth;
        LiquidBurst.Spawn(screen, depth, LiquidBurst.NegativeChannel);

        transform.DOKill();
        if (spineSkin != null && spineSkin.HasPop)
        {
            // Spine 터짐 애니메이션이 끝나면 꺼진다.
            spineSkin.PlayPop(() =>
            {
                if (this != null) gameObject.SetActive(false);
            });
            return;
        }

        DOTween.Sequence().SetLink(gameObject)
            .Append(transform.DOScale(new Vector3(1.35f, 0.7f, 1f), 0.06f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(new Vector3(0.6f, 1.4f, 1f), 0.06f))
            .Append(transform.DOScale(0f, 0.08f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                transform.localScale = Vector3.one;
                gameObject.SetActive(false);
            });
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

        transform.DOKill();
        if (!gameObject.activeSelf) return;

        if (rectTransform != null)
            rectTransform.DOAnchorPos(originPos, 0.2f).SetEase(Ease.OutBack);
        else
            transform.DOLocalMove(originPos, 0.2f).SetEase(Ease.OutBack);
    }
}
