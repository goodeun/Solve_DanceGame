using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[ExecuteAlways]
public class KeyImage : MonoBehaviour
{
    [SerializeField] private Keys Sqarekey;
    [SerializeField] private Keys Rhombuskey;

    [SerializeField] private Keys CurrentKey;

    public Keys key => CurrentKey;

    [Space(10)]
    [SerializeField] private Image fill;
    [SerializeField] private TextMeshProUGUI keyInfo;
    [SerializeField] private TextMeshProUGUI magnificationText;
    [SerializeField] private float offsetX;

    private const float PressScale = 1.1f;
    private const float GrowDuration = 0.01f;
    private const float ReturnDuration = 0.4f;

    private Vector3 defaultScale;
    private Sequence pressSequence;

    public TextMeshProUGUI KeyInfo => keyInfo;
    public TextMeshProUGUI MagText => magnificationText;

    private void Awake()
    {
        CurrentKey = Sqarekey;

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

    private void OnEnable()
    {
        KeyBindings.Changed += SetKeyText;
        SetKeyText();
    }

    private void OnDisable()
    {
        KeyBindings.Changed -= SetKeyText;
        pressSequence?.Kill();
        pressSequence = null;

        if (Application.isPlaying)
            transform.localScale = defaultScale;
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        SetKeyTextRotation();
        SetKeyTextPosition();

        SetMagnificationTextRotation();
        SetMagnificationTextPosition();
    }

    private void RefreshVisuals()
    {
        SetKeyTextRotation();
        SetKeyTextPosition();
        SetKeyText();

        SetMagnificationTextRotation();
        SetMagnificationTextPosition();
    }

    private void SetKeyTextRotation()
    {
        if (keyInfo == null)
            return;

        keyInfo.transform.localEulerAngles = new Vector3(0, 0, -transform.localEulerAngles.z);
    }

    private void SetKeyTextPosition()
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

    private void SetMagnificationTextRotation()
    {
        if (magnificationText == null)
            return;

        magnificationText.transform.localEulerAngles = new Vector3(0, 0, -transform.localEulerAngles.z);
    }

    private void SetMagnificationTextPosition()
    {
        if (magnificationText == null || keyInfo == null)
            return;

        Vector3 basePosition = keyInfo.rectTransform.position;

        magnificationText.rectTransform.position =
            basePosition + Vector3.right * offsetX;
    }

    private void SetKeyText()
    {
        if (keyInfo == null)
            return;

        // 옵션 키 바인딩에 연결된 실제 키 이름을 표시한다.
        keyInfo.text = KeyBindings.DisplayName(CurrentKey);
    }

    public void SetStageKey(Stage stage)
    {
        CurrentKey = stage == Stage.Square ? Sqarekey : Rhombuskey;
        SetKeyText();
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
