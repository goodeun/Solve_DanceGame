using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 진행용 대사창(기본 대사 매니저).
/// 글씨가 하나씩 등장하고, 전부 등장하면 대사창 하단에 삼각형 아이콘이 깜빡인다.
/// ESC를 제외한 모든 키(또는 마우스 클릭)로 대사를 넘긴다. ESC는 Pause로 전달된다.
/// UI 참조를 지정하지 않으면 실행 시 기본 대사창을 만든다.
/// </summary>
public class StoryDialogueView : MonoBehaviour
{
    private static StoryDialogueView instance;

    [Header("UI (비워두면 자동 생성)")]
    [SerializeField] private RectTransform box;
    [SerializeField] private Image boxImage;
    [SerializeField] private Image standingImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Graphic nextIcon;
    [SerializeField] private Image gloomOverlay;
    [SerializeField] private int sortingOrder = 650;

    [Header("Typing")]
    [SerializeField, Min(0.005f)] private float charInterval = 0.035f;
    [SerializeField, Min(10f)] private float baseFontSize = 46f;
    [SerializeField, Min(1f)] private float emphasisScale = 1.35f;

    [Header("Style Colors")]
    [SerializeField] private Color normalBoxColor = new Color(0.08f, 0.03f, 0.12f, 0.92f);
    [SerializeField] private Color angryBoxColor = new Color(0.45f, 0.02f, 0.08f, 0.95f);
    [SerializeField] private Color gloomyBoxColor = new Color(0.05f, 0.06f, 0.14f, 0.95f);
    [SerializeField] private Color gloomyTextColor = new Color(0.65f, 0.72f, 0.9f);

    [Header("Style SFX (FMOD 이벤트 경로, 비워두면 무음)")]
    [SerializeField] private string angrySfxEvent;
    [SerializeField] private string gloomySfxEvent;

    private Coroutine playRoutine;
    private PauseManager pauseManager;
    private Action pendingComplete;

    public bool IsPlaying => playRoutine != null;
    /// <summary>팝업 등이 열려 있을 때 대사 넘김을 막는다.</summary>
    public static int InputBlockCount;

    public static StoryDialogueView GetOrCreate()
    {
        if (instance != null) return instance;

        instance = FindFirstObjectByType<StoryDialogueView>();
        if (instance != null) return instance;

        var go = new GameObject("StoryDialogueView");
        instance = go.AddComponent<StoryDialogueView>();
        return instance;
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        pauseManager = FindFirstObjectByType<PauseManager>();
        Build();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Build()
    {
        if (box != null && bodyText != null) return;

        DemoUISettings s = DemoUI.Settings;
        Canvas canvas = DemoUI.CreateOverlayCanvas("DialogueCanvas", sortingOrder, transform);

        gloomOverlay = DemoUI.CreatePanel("GloomOverlay", canvas.transform, new Color(0.02f, 0.03f, 0.1f, 0f));
        DemoUI.Stretch((RectTransform)gloomOverlay.transform);
        gloomOverlay.raycastTarget = false;

        standingImage = DemoUI.CreatePanel("Standing", canvas.transform, Color.white);
        var standingRect = (RectTransform)standingImage.transform;
        standingRect.anchorMin = standingRect.anchorMax = new Vector2(0f, 0f);
        standingRect.pivot = new Vector2(0.5f, 0f);
        standingRect.sizeDelta = new Vector2(420f, 620f);
        standingRect.anchoredPosition = new Vector2(300f, 40f);
        standingImage.preserveAspect = true;
        standingImage.raycastTarget = false;

        boxImage = DemoUI.CreatePanel("Box", canvas.transform, normalBoxColor, s.dialogueBoxSprite);
        box = (RectTransform)boxImage.transform;
        box.anchorMin = new Vector2(0f, 0f);
        box.anchorMax = new Vector2(1f, 0f);
        box.pivot = new Vector2(0.5f, 0f);
        box.offsetMin = new Vector2(520f, 40f);
        box.offsetMax = new Vector2(-120f, 330f);
        var outline = boxImage.gameObject.AddComponent<Outline>();
        outline.effectColor = s.magenta;
        outline.effectDistance = new Vector2(4f, -4f);

        nameText = DemoUI.CreateText("Name", box, "", 40f, TextAlignmentOptions.Left, true);
        var nameRect = (RectTransform)nameText.transform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 0f);
        nameRect.offsetMin = new Vector2(30f, 6f);
        nameRect.offsetMax = new Vector2(-30f, 66f);
        nameText.color = s.cyan;

        bodyText = DemoUI.CreateText("Body", box, "", baseFontSize, TextAlignmentOptions.TopLeft);
        DemoUI.Stretch((RectTransform)bodyText.transform, 50f, 50f, 40f, 60f);

        if (s.dialogueNextIcon != null)
        {
            var icon = DemoUI.CreatePanel("Next", box, Color.white, s.dialogueNextIcon);
            icon.type = Image.Type.Simple;
            nextIcon = icon;
        }
        else
        {
            var icon = DemoUI.CreateText("Next", box, "▼", 40f);
            icon.color = s.green;
            nextIcon = icon;
        }
        DemoUI.Place((RectTransform)nextIcon.transform, new Vector2(0.5f, 0f), new Vector2(60f, 50f), new Vector2(0f, 30f));
    }

    private void SetVisible(bool visible)
    {
        if (box != null) box.gameObject.SetActive(visible);
        if (standingImage != null) standingImage.gameObject.SetActive(visible && standingImage.sprite != null);
        if (!visible && gloomOverlay != null) gloomOverlay.color = new Color(gloomOverlay.color.r, gloomOverlay.color.g, gloomOverlay.color.b, 0f);
        if (!visible && nextIcon != null) nextIcon.gameObject.SetActive(false);
    }

    public void Play(IList<StoryDialogueLine> lines, Action onComplete)
    {
        Stop(false);

        if (lines == null || lines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        pendingComplete = onComplete;
        playRoutine = StartCoroutine(PlayRoutine(lines));
    }

    /// <summary>재생 중인 대사를 중단한다. invokeComplete가 true면 완료 콜백을 호출한다.</summary>
    public void Stop(bool invokeComplete)
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = null;
        SetVisible(false);

        Action complete = pendingComplete;
        pendingComplete = null;
        if (invokeComplete) complete?.Invoke();
    }

    private bool IsPaused => (pauseManager != null && pauseManager.isPauseActive) || Time.timeScale == 0f;

    private bool AdvancePressed()
    {
        if (IsPaused || InputBlockCount > 0) return false;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame && !keyboard.escapeKey.wasPressedThisFrame)
            return true;

        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            yield return null;
            if (!IsPaused) elapsed += Time.unscaledDeltaTime;
        }
    }

    private IEnumerator PlayRoutine(IList<StoryDialogueLine> lines)
    {
        // 대사를 시작시킨 키 입력이 첫 대사를 바로 넘기지 않도록 한 프레임 쉰다.
        yield return null;

        foreach (StoryDialogueLine line in lines)
        {
            if (line == null) continue;

            if (line.delayBefore > 0f)
            {
                SetVisible(false);
                yield return WaitUnscaled(line.delayBefore);
            }

            ApplyLine(line);
            SetVisible(true);
            line.onLineStarted?.Invoke();

            // 타이핑
            bodyText.text = line.text;
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            float timer = 0f;
            int visible = 0;
            nextIcon.gameObject.SetActive(false);

            while (visible < total)
            {
                yield return null;
                if (AdvancePressed())
                {
                    visible = total;
                    break;
                }
                if (IsPaused) continue;

                timer += Time.unscaledDeltaTime;
                while (timer >= charInterval && visible < total)
                {
                    timer -= charInterval;
                    visible++;
                }
                bodyText.maxVisibleCharacters = visible;
            }

            bodyText.maxVisibleCharacters = total;
            nextIcon.gameObject.SetActive(true);
            nextIcon.DOKill();
            nextIcon.canvasRenderer.SetAlpha(1f);
            nextIcon.CrossFadeAlpha(0.15f, 0.45f, true);
            float blink = 0f;

            yield return null;
            while (!AdvancePressed())
            {
                blink += Time.unscaledDeltaTime;
                if (blink >= 0.5f)
                {
                    blink = 0f;
                    bool dim = nextIcon.canvasRenderer.GetAlpha() > 0.5f;
                    nextIcon.CrossFadeAlpha(dim ? 0.15f : 1f, 0.45f, true);
                }
                yield return null;
            }

            line.onLineFinished?.Invoke();
        }

        playRoutine = null;
        SetVisible(false);

        Action complete = pendingComplete;
        pendingComplete = null;
        complete?.Invoke();
    }

    private void ApplyLine(StoryDialogueLine line)
    {
        // 스탠딩
        Sprite standing = line.isPlayerLine
            ? (PlayerManager.Instance != null ? PlayerManager.Instance.GetStanding(line.playerStanding) : null)
            : line.standingSprite;
        standingImage.sprite = standing;
        standingImage.gameObject.SetActive(standing != null);

        nameText.text = line.speakerName ?? string.Empty;
        nameText.gameObject.SetActive(!string.IsNullOrEmpty(line.speakerName));

        // 분류별 연출
        box.DOKill(true);
        boxImage.color = normalBoxColor;
        bodyText.color = DemoUI.Settings.text;
        bodyText.fontSize = baseFontSize;
        bodyText.fontStyle = FontStyles.Normal;
        gloomOverlay.DOKill();

        switch (line.style)
        {
            case DialogueStyle.Emphasis:
                bodyText.fontSize = baseFontSize * emphasisScale;
                bodyText.fontStyle = FontStyles.Bold;
                box.DOPunchScale(Vector3.one * 0.04f, 0.3f, 6, 0.5f).SetUpdate(true);
                FadeGloom(0f);
                break;

            case DialogueStyle.Angry:
                boxImage.color = angryBoxColor;
                bodyText.fontStyle = FontStyles.Bold;
                box.DOShakeAnchorPos(0.4f, 18f, 30, 90f, false, true).SetEase(Ease.OutQuad).SetUpdate(true);
                PlaySfx(angrySfxEvent);
                FadeGloom(0f);
                break;

            case DialogueStyle.Gloomy:
                boxImage.color = gloomyBoxColor;
                bodyText.color = gloomyTextColor;
                bodyText.fontStyle = FontStyles.Italic;
                PlaySfx(gloomySfxEvent);
                FadeGloom(0.45f);
                break;

            default:
                FadeGloom(0f);
                break;
        }
    }

    private void FadeGloom(float alpha)
    {
        if (gloomOverlay == null) return;
        gloomOverlay.DOFade(alpha, 0.6f).SetUpdate(true);
    }

    private static void PlaySfx(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return;

        try { FMODUnity.RuntimeManager.PlayOneShot(eventPath); }
        catch (FMODUnity.EventNotFoundException) { Debug.LogWarning($"Dialogue SFX not found: {eventPath}"); }
    }
}
