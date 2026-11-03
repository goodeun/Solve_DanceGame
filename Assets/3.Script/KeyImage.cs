using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum Keys
{
    W, A, S, D, Q, E
}

[ExecuteAlways]
public class KeyImage : MonoBehaviour
{
    [SerializeField] public Keys key;

    [Space(10)]
    [SerializeField] private Image fill;
    [SerializeField] private Image line;
    [SerializeField] private TextMeshProUGUI keyInfo;

    private const float PressScale = 1.1f;
    private const float GrowDuration = 0.01f;
    private const float ReturnDuration = 0.4f;

    private Vector3 defaultScale;
    private Sequence pressSequence;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    private void OnDisable()
    {
        pressSequence?.Kill();
        pressSequence = null;

        if (Application.isPlaying)
            transform.localScale = defaultScale;
    }

    private void Update()
    {
        SetTextRotation();
        SetTextPosition();
        SetKeyText();
    }

    private void SetTextRotation()
    {
        keyInfo.transform.localEulerAngles = new Vector3(0, 0, -transform.localEulerAngles.z);
    }

    private void SetTextPosition()
    {
        Material mat = fill.material;

        keyInfo.transform.localPosition = 
            new Vector3(
                0,
                (mat.GetFloat("_Inner_Radius") + (mat.GetFloat("_Button_Thickness") / 2.3f)) * 100, 
                0
            );
    }

    private void SetKeyText()
    {
        keyInfo.text = key.ToString();
    }

    public void PlayPressAnimation()
    {
        if (!Application.isPlaying)
            return;

        pressSequence?.Kill();
        transform.localScale = defaultScale;

        pressSequence = DOTween.Sequence()
            .Append(transform.DOScale(defaultScale * PressScale, GrowDuration).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(defaultScale, ReturnDuration).SetEase(Ease.OutQuad));
    }
}
