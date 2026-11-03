using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public enum BubbleState
{
    positive,
    negative
}

public class Bubble : MonoBehaviour
{
    [SerializeField] private Sprite positive_img;
    [SerializeField] private Sprite negative_img;

    [SerializeField] private SpriteRenderer bubbleImage;

    [SerializeField] private Transform visualRoot;

    [SerializeField] private TextMeshPro Text;
    [SerializeField] private ParticleSystem effect;


    private BubbleState State;
    private Sequence seq;
    private string Line;

    private void Awake()
    {
        bubbleImage = GetComponentInChildren<SpriteRenderer>();
        effect = GetComponentInChildren<ParticleSystem>();
    }
    private void OnEnable()
    {
        Spawn_Anim();
    }

    private void OnDisable()
    {
        transform.DOKill();
        seq?.Kill();
    }

    public void Initialize(BubbleState _state, string _line)
    {
        State = _state;
        Line = _line;

        Text.text = Line.Replace("<br>", "\n");

        var main = effect.main;

        switch (_state)
        {
            case BubbleState.positive:
                bubbleImage.sprite = positive_img;
                main.startColor = Color.red;
                break;
            case BubbleState.negative:
                bubbleImage.sprite = negative_img;
                main.startColor = Color.green;
                break;
        }
    }

    public void Spawn_Anim()
    {
        Vector3 originPos = visualRoot.position;

        visualRoot.position += new Vector3(0, -0.5f, 0);
        visualRoot.localScale = Vector3.zero;

        visualRoot.DOMove(originPos, 0.2f).SetEase(Ease.OutBack);
        visualRoot.DOScale(new Vector3(1,1,1), 0.2f).SetEase(Ease.OutBack);
    }

    public void Despawn_Anim(System.Action onCompleteCallback)
    {
        seq = DOTween.Sequence();
        effect.gameObject.SetActive(true);

        seq.Append(visualRoot.DOMove(visualRoot.position + new Vector3(0, -0.5f, 0), 0.2f).SetEase(Ease.OutBack))
            .Join(visualRoot.DOPunchScale(new Vector3(0.3f, -0.3f, 0), 0.2f, 10, 1))
            .Append(visualRoot.DOScale(Vector3.zero, 0.2f).SetEase(Ease.OutBack))
            //TODO ÀÌÆåÆ® Àç»ý
            .AppendCallback(effect.Play)
            .AppendInterval(0.2f)
            .OnComplete(() => onCompleteCallback?.Invoke()
            );
    }
}
