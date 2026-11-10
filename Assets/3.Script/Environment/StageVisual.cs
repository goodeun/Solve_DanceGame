using UnityEngine;
using DG.Tweening;

public class StageVisual : MonoBehaviour
{
    [Header("Key Input Motion")]
    [SerializeField, Min(0f)] private float keyDropDistance = 0.35f;
    [SerializeField, Min(0.01f)] private float keyDropDuration = 0.04f;
    [SerializeField, Min(0.01f)] private float keyReturnDuration = 0.16f;

    private Tween keyInputTween;

    public void PlayKeyInputDrop()
    {
        keyInputTween?.Kill();

        Vector3 startPosition = transform.localPosition;
        keyInputTween = DOTween.Sequence()
            .Append(transform.DOLocalMoveY(startPosition.y - keyDropDistance, keyDropDuration))
            .Append(transform.DOLocalMoveY(startPosition.y, keyReturnDuration).SetEase(Ease.OutQuad))
            .OnComplete(() => keyInputTween = null);
    }

    private void OnDisable()
    {
        keyInputTween?.Kill();
        keyInputTween = null;
    }
}