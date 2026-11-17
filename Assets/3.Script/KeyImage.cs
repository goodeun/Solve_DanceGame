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

public class KeyImage : MonoBehaviour
{
    [SerializeField] public Keys key;

    [Space(10)]
    [SerializeField] private Image fill;
    [SerializeField] private Image line;
    [SerializeField] private TextMeshProUGUI keyInfo;
    [SerializeField] private TextMeshProUGUI magnificationText;

    private const float PressScale = 1.1f;
    private const float GrowDuration = 0.01f;
    private const float ReturnDuration = 0.4f;

    private Vector3 defaultScale;
    private Sequence pressSequence;

    public TextMeshProUGUI KeyInfo => keyInfo;
    public TextMeshProUGUI MagText => magnificationText;

    private void Awake()
    {
        defaultScale = transform.localScale;
        RefreshVisuals();
    }

    private void OnValidate()
    {
        // Keep the prefab preview correct without running a Canvas/TMP update
        // every editor frame.  ExecuteAlways previously made these assignments
        // continuously while the Game view was open, which kept producing editor
        // UI rebuild work even outside Play Mode.
        RefreshVisuals();
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
        if (!Application.isPlaying)
            return;

        SetTextRotation();
        SetTextPosition();
    }

    private void RefreshVisuals()
    {
        SetTextRotation();
        SetTextPosition();
        SetKeyText();
    }

    private void SetTextRotation()
    {
        if (keyInfo == null)
            return;

        keyInfo.transform.localEulerAngles = new Vector3(0, 0, -transform.localEulerAngles.z);
    }

    private void SetTextPosition()
    {
        if (fill == null || keyInfo == null || fill.material == null)
            return;

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
        if (keyInfo == null)
            return;

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
