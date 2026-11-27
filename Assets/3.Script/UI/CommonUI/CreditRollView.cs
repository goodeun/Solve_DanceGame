using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pause → Credit 버튼으로만 여는 크레딧 롤(Pause 서브 패널).
/// 영화처럼 새까만 화면에 크레딧이 아래에서 위로 올라가고, 좌측 하단에 메뉴 캐릭터가 작게 춤을 춘다.
/// 다 올라가면 글씨·캐릭터가 사라지고 검은 화면에 우측 하단 "넘어가기" 안내가 뜬다.
/// 이때 아무 키(또는 클릭)나 누르면 크레딧이 닫히고 바로 Pause로 돌아온다. ESC는 언제든 Pause로 돌아간다.
/// 크레딧 내용은 Inspector의 Sections에서 바꾼다(현재 임시 텍스트).
/// </summary>
public class CreditRollView : MonoBehaviour, IPauseSubPanel
{
    [Serializable]
    public class Section
    {
        public string heading;
        [TextArea(1, 6)] public string body;
    }

    [Header("Content (임시 텍스트)")]
    [SerializeField] private string title = "sølve";
    [SerializeField] private string subtitle = "DANCE DEMO";
    [SerializeField] private List<Section> sections = new List<Section>
    {
        new Section { heading = "기획", body = "기획자 이름 1\n기획자 이름 2" },
        new Section { heading = "프로그래밍", body = "프로그래머 이름 1\n프로그래머 이름 2\n프로그래머 이름 3" },
        new Section { heading = "아트", body = "아티스트 이름 1\n아티스트 이름 2" },
        new Section { heading = "캐릭터 · 애니메이션", body = "담당자 이름" },
        new Section { heading = "UI · 모션 그래픽", body = "담당자 이름" },
        new Section { heading = "사운드", body = "담당자 이름" },
        new Section { heading = "음악", body = "RetroVision - Puzzle (2024 Edit) [NCS Release]\nFeel The Rush\n(곡 정보 임시)" },
        new Section { heading = "사용 에셋", body = "DOTween · FMOD · Cinemachine\nPretendard · 뚝상체 · SF레몬빙수" },
        new Section { heading = "Special Thanks", body = "NEOWIZ K-Game Academy 10기\n그리고 플레이해 주신 여러분" },
    };
    [SerializeField] private string endLine = "Thank you for playing!";
    [SerializeField] private string continueText = "아무 키나 눌러 돌아가기";

    [Header("Style")]
    [SerializeField, Min(10f)] private float scrollSpeed = 90f;
    [SerializeField] private Color headingColor = new Color(1f, 0.18f, 0.78f, 1f);
    [SerializeField] private Color bodyColor = Color.white;
    [SerializeField] private Vector2 characterSize = new Vector2(220f, 275f);
    [SerializeField] private Vector2 characterMargin = new Vector2(40f, 30f);
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.5f;

    private RectTransform root;
    private Image black;
    private RectTransform roll;
    private CanvasGroup rollGroup;
    private RawImage character;
    private TextMeshProUGUI continueLabel;
    private Coroutine routine;
    private Tween blink;
    private PausePresentation presentation;
    private bool built;

    // ================= IPauseSubPanel =================

    public void PlayOpen(Action onComplete)
    {
        gameObject.SetActive(true);
        if (!built) Build();
        if (presentation == null) presentation = FindFirstObjectByType<PausePresentation>();

        // 초기 상태
        black.color = new Color(0f, 0f, 0f, 0f);
        rollGroup.alpha = 1f;
        roll.anchoredPosition = new Vector2(0f, -roll.sizeDelta.y);
        continueLabel.gameObject.SetActive(false);
        blink?.Kill();

        presentation?.BeginCreditsPose();
        character.texture = presentation != null ? presentation.PortraitTexture : null;
        character.enabled = character.texture != null;
        character.color = new Color(1f, 1f, 1f, 0f);

        black.DOKill();
        black.DOFade(1f, fadeDuration).SetUpdate(true).OnComplete(() =>
        {
            character.DOFade(1f, fadeDuration).SetUpdate(true);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Roll());
            onComplete?.Invoke();
        });
    }

    public void PlayClose(Action onComplete)
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        blink?.Kill();

        var group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.DOKill();
        group.alpha = 1f;
        group.DOFade(0f, 0.25f).SetUpdate(true).OnComplete(() =>
        {
            presentation?.EndCreditsPose();
            gameObject.SetActive(false);
            group.alpha = 1f;
            onComplete?.Invoke();
        });
    }

    // ================= Roll =================

    private IEnumerator Roll()
    {
        float screenHeight = ((RectTransform)root).rect.height;
        if (screenHeight <= 0f) screenHeight = 1080f;

        // 화면 아래에서 시작해 마지막 줄이 화면 위로 다 사라질 때까지 올린다.
        float startY = -roll.sizeDelta.y - screenHeight * 0.5f;
        float endY = screenHeight * 0.5f;
        roll.anchoredPosition = new Vector2(0f, startY);

        while (roll.anchoredPosition.y < endY)
        {
            roll.anchoredPosition += new Vector2(0f, scrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        // 글씨·캐릭터가 사라지고 검은 화면만 남는다.
        rollGroup.DOFade(0f, fadeDuration).SetUpdate(true);
        character.DOFade(0f, fadeDuration).SetUpdate(true);
        yield return new WaitForSecondsRealtime(fadeDuration + 0.4f);

        // 우측 하단 넘어가기 안내 → 입력하면 Pause로 돌아간다.
        continueLabel.gameObject.SetActive(true);
        continueLabel.alpha = 0f;
        blink = DOTween.Sequence()
            .Append(continueLabel.DOFade(1f, 0.5f))
            .Append(continueLabel.DOFade(0.35f, 0.6f))
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetLink(gameObject);

        yield return null;
        while (!AnyInput()) yield return null;

        routine = null;
        PauseManager pm = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        pm?.ReturnPause();
    }

    private static bool AnyInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame && !keyboard.escapeKey.wasPressedThisFrame) return true;
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    // ================= Build =================

    private void Build()
    {
        built = true;
        root = (RectTransform)transform;

        // 패널 자체 배경은 쓰지 않는다(검은 화면을 따로 깐다).
        Image panelImage = GetComponent<Image>();
        if (panelImage != null) panelImage.color = new Color(0f, 0f, 0f, 0f);

        black = DemoUI.CreatePanel("Black", root, Color.black);
        DemoUI.Stretch((RectTransform)black.transform);
        black.raycastTarget = true;

        // 롤(세로 목록)
        roll = DemoUI.CreateRect("Roll", root);
        roll.anchorMin = roll.anchorMax = new Vector2(0.5f, 0.5f);
        roll.pivot = new Vector2(0.5f, 1f);
        roll.sizeDelta = new Vector2(1100f, 100f);
        rollGroup = roll.gameObject.AddComponent<CanvasGroup>();
        var layout = roll.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 18f;
        var fitter = roll.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddLine(title, 120f, headingColor, true);
        AddLine(subtitle, 44f, bodyColor, true);
        AddSpacer(160f);
        foreach (Section section in sections)
        {
            if (section == null) continue;
            AddLine(section.heading, 40f, headingColor, true);
            AddLine(section.body, 34f, bodyColor, false);
            AddSpacer(90f);
        }
        AddSpacer(120f);
        AddLine(endLine, 52f, bodyColor, true);

        LayoutRebuilder.ForceRebuildLayoutImmediate(roll);

        // 좌측 하단 작은 캐릭터(메뉴 캐릭터 이미지, 춤 루프)
        var characterRect = DemoUI.CreateRect("Character", root);
        characterRect.anchorMin = characterRect.anchorMax = new Vector2(0f, 0f);
        characterRect.pivot = new Vector2(0f, 0f);
        characterRect.sizeDelta = characterSize;
        characterRect.anchoredPosition = characterMargin;
        character = characterRect.gameObject.AddComponent<RawImage>();
        character.raycastTarget = false;

        // 우측 하단 넘어가기 안내
        continueLabel = DemoUI.CreateText("Continue", root, continueText, 28f, TextAlignmentOptions.Right);
        var continueRect = (RectTransform)continueLabel.transform;
        continueRect.anchorMin = continueRect.anchorMax = new Vector2(1f, 0f);
        continueRect.pivot = new Vector2(1f, 0f);
        continueRect.sizeDelta = new Vector2(600f, 50f);
        continueRect.anchoredPosition = new Vector2(-50f, 40f);
        continueLabel.color = new Color(1f, 1f, 1f, 0.8f);
        continueLabel.gameObject.SetActive(false);
    }

    private void AddLine(string text, float size, Color color, bool title)
    {
        if (string.IsNullOrEmpty(text)) return;
        var line = DemoUI.CreateText("Line", roll, text, size, TextAlignmentOptions.Center, title);
        line.color = color;
        line.lineSpacing = 18f;
    }

    private void AddSpacer(float height)
    {
        var spacer = DemoUI.CreateRect("Space", roll);
        var element = spacer.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        element.minHeight = height;
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        blink?.Kill();
    }
}
