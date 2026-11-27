using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pause → Option 화면. 탭(조작 / 사운드 / 그래픽 / 게임)으로 나뉜 설정 메뉴.
/// - 조작: 게임 키 6개 바인딩(겹치면 맞바꿈), 기본값 복원. HUD(리듬 휠·리듬 UI)에 바로 반영.
/// - 사운드: 마스터·음악·효과음(FMOD 버스) 볼륨
/// - 그래픽: 화면 모드, 해상도, 수직 동기화, 프레임 제한, 품질
/// - 게임: 입력 보정(ms)
/// 키보드: 방향키로 이동, 좌우로 값 변경, Enter로 실행, Q/E(또는 Tab)로 탭 전환, ESC로 뒤로.
/// UI는 OptionPanel 안에 실행 시 만들어진다. 색·크기는 Inspector에서 조정.
/// </summary>
public class OptionsMenuView : MonoBehaviour
{
    [Header("Layout (1920x1080 기준)")]
    [SerializeField] private Vector2 windowSize = new Vector2(1060f, 800f);
    [SerializeField] private Vector2 windowPosition = new Vector2(330f, -10f);
    [SerializeField, Range(-10f, 10f)] private float windowTilt = -2f;
    [SerializeField] private float rowHeight = 74f;

    [Header("Colors")]
    [SerializeField] private Color backdropColor = new Color(0.02f, 0f, 0.05f, 0.45f);
    [SerializeField] private Color windowColor = new Color(0.07f, 0.02f, 0.1f, 0.96f);
    [SerializeField] private Color accentColor = new Color(1f, 0.18f, 0.78f, 1f);
    [SerializeField] private Color subAccentColor = new Color(0.25f, 0.9f, 1f, 1f);
    [SerializeField] private Color rowIdleColor = new Color(1f, 1f, 1f, 0.04f);
    [SerializeField] private Color rowSelectedColor = new Color(1f, 0.18f, 0.78f, 0.28f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color dimTextColor = new Color(1f, 1f, 1f, 0.55f);

    private enum Tab { Controls, Sound, Graphics, Game }
    private static readonly string[] TabNames = { "조작", "사운드", "그래픽", "게임" };

    private static readonly Dictionary<Keys, string> SlotRoles = new Dictionary<Keys, string>
    {
        [Keys.Q] = "왼쪽 위 · 사각형 왼팔",
        [Keys.E] = "오른쪽 위 · 사각형 오른팔",
        [Keys.A] = "왼쪽 · 사각형 왼다리 / 마름모 왼팔",
        [Keys.D] = "오른쪽 · 사각형 오른다리 / 마름모 오른팔",
        [Keys.W] = "위 · 마름모 머리",
        [Keys.S] = "아래 · 마름모 스텝",
    };

    private RectTransform window;
    private RectTransform content;
    private RectTransform tabIndicator;
    private TextMeshProUGUI hintText;
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<Selectable> rows = new List<Selectable>();
    private readonly List<Button> footerButtons = new List<Button>();
    private readonly Dictionary<Keys, TextMeshProUGUI> keyLabels = new Dictionary<Keys, TextMeshProUGUI>();
    private Tab currentTab = Tab.Controls;
    private bool built;
    private Coroutine rebindRoutine;

    // ================= Lifecycle =================

    private void OnEnable()
    {
        GameSettings.Load();
        if (!built) Build();
        ShowTab(currentTab, false);
        KeyBindings.Changed += RefreshKeyLabels;
        StartCoroutine(SelectNextFrame(tabButtons[(int)currentTab].gameObject));
    }

    private void OnDisable()
    {
        KeyBindings.Changed -= RefreshKeyLabels;
        CancelRebind();
    }

    private IEnumerator SelectNextFrame(GameObject target)
    {
        yield return null;
        if (EventSystem.current != null && target != null) EventSystem.current.SetSelectedGameObject(target);
    }

    private void Update()
    {
        if (KeyBindings.IsRebinding) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // 페르소나·다른 게임처럼 Q/E(또는 Tab)로 탭 전환
        if (keyboard.qKey.wasPressedThisFrame) SwitchTab(-1);
        else if (keyboard.eKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame) SwitchTab(1);
    }

    // ================= Build =================

    private void Build()
    {
        built = true;
        var root = (RectTransform)transform;

        // 패널 자체 배경(프리팹의 반투명 흰색)은 화면을 희게 덮으므로 어두운 막으로 바꾼다.
        Image backdrop = GetComponent<Image>();
        if (backdrop != null) backdrop.color = backdropColor;

        var bg = DemoUI.CreatePanel("Window", root, windowColor);
        window = (RectTransform)bg.transform;
        DemoUI.Place(window, new Vector2(0.5f, 0.5f), windowSize, windowPosition);
        window.localRotation = Quaternion.Euler(0f, 0f, windowTilt);
        bg.raycastTarget = true;
        var outline = bg.gameObject.AddComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(6f, -6f);

        // 머리띠(비스듬한 제목 바)
        var header = DemoUI.CreatePanel("Header", window, accentColor);
        var headerRect = (RectTransform)header.transform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(0f, 1f);
        headerRect.pivot = new Vector2(0f, 0.5f);
        headerRect.sizeDelta = new Vector2(420f, 96f);
        headerRect.anchoredPosition = new Vector2(-30f, 8f);
        headerRect.localRotation = Quaternion.Euler(0f, 0f, 4f);
        var title = DemoUI.CreateText("Title", headerRect, "OPTION", 72f, TextAlignmentOptions.Center, true);
        DemoUI.Stretch((RectTransform)title.transform);

        // 탭
        var tabRow = DemoUI.CreateRect("Tabs", window);
        tabRow.anchorMin = new Vector2(0f, 1f);
        tabRow.anchorMax = new Vector2(1f, 1f);
        tabRow.pivot = new Vector2(0.5f, 1f);
        tabRow.sizeDelta = new Vector2(-80f, 70f);
        tabRow.anchoredPosition = new Vector2(0f, -80f);

        var indicator = DemoUI.CreatePanel("TabIndicator", tabRow, accentColor);
        tabIndicator = (RectTransform)indicator.transform;
        tabIndicator.anchorMin = tabIndicator.anchorMax = new Vector2(0f, 0.5f);
        tabIndicator.pivot = new Vector2(0.5f, 0.5f);
        tabIndicator.sizeDelta = new Vector2(210f, 64f);
        tabIndicator.localRotation = Quaternion.Euler(0f, 0f, -3f);

        float tabWidth = (windowSize.x - 80f) / TabNames.Length;
        for (int i = 0; i < TabNames.Length; i++)
        {
            int index = i;
            var image = DemoUI.CreatePanel("Tab " + TabNames[i], tabRow, new Color(0f, 0f, 0f, 0f));
            var rect = (RectTransform)image.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(tabWidth - 10f, 64f);
            rect.anchoredPosition = new Vector2(tabWidth * (i + 0.5f), 0f);
            var button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ShowTab((Tab)index, true));
            var label = DemoUI.CreateText("Label", rect, TabNames[i], 40f, TextAlignmentOptions.Center, true);
            DemoUI.Stretch((RectTransform)label.transform);
            image.gameObject.AddComponent<OptionTabFx>().Bind(this, index);
            tabButtons.Add(button);
        }

        var hint = DemoUI.CreateText("TabHint", window, "Q ◀   ▶ E", 26f, TextAlignmentOptions.Right);
        DemoUI.Place((RectTransform)hint.transform, new Vector2(1f, 1f), new Vector2(300f, 40f), new Vector2(-170f, -40f));
        hint.color = dimTextColor;

        // 내용
        content = DemoUI.CreateRect("Content", window);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 1f);
        content.offsetMin = new Vector2(50f, 130f);
        content.offsetMax = new Vector2(-50f, -170f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        // 하단 버튼 + 안내
        var reset = DemoUI.CreateButton("Reset", window, "기본값", new Vector2(220f, 70f), ResetCurrentTab);
        DemoUI.Place((RectTransform)reset.transform, new Vector2(0f, 0f), new Vector2(220f, 70f), new Vector2(170f, 60f));
        var back = DemoUI.CreateButton("Back", window, "돌아가기", new Vector2(220f, 70f), Back);
        DemoUI.Place((RectTransform)back.transform, new Vector2(0f, 0f), new Vector2(220f, 70f), new Vector2(410f, 60f));
        footerButtons.Add(reset);
        footerButtons.Add(back);

        hintText = DemoUI.CreateText("Hint", window, "", 24f, TextAlignmentOptions.Right);
        DemoUI.Place((RectTransform)hintText.transform, new Vector2(1f, 0f), new Vector2(520f, 70f), new Vector2(-300f, 60f));
        hintText.color = dimTextColor;
    }

    // ================= Tabs =================

    public void SwitchTab(int direction)
    {
        int next = ((int)currentTab + direction + TabNames.Length) % TabNames.Length;
        ShowTab((Tab)next, true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(tabButtons[next].gameObject);
    }

    public void OnTabHighlighted(int index)
    {
        // 키보드로 탭에 포커스가 가면 그 탭을 바로 보여준다.
        if ((int)currentTab != index) ShowTab((Tab)index, true);
    }

    private void ShowTab(Tab tab, bool animate)
    {
        CancelRebind();
        currentTab = tab;

        for (int i = 0; i < tabButtons.Count; i++)
        {
            var label = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = i == (int)tab ? Color.white : dimTextColor;
        }

        var target = (RectTransform)tabButtons[(int)tab].transform;
        tabIndicator.DOKill();
        if (animate)
        {
            tabIndicator.DOAnchorPos(target.anchoredPosition, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
            tabIndicator.DOPunchScale(new Vector3(0.12f, -0.1f, 0f), 0.22f, 6, 0.5f).SetUpdate(true);
        }
        else
        {
            tabIndicator.anchoredPosition = target.anchoredPosition;
        }

        // 레이아웃에서 바로 빠지도록 부모를 끊고 지운다.
        var old = new List<Transform>();
        foreach (Transform child in content) old.Add(child);
        foreach (Transform child in old)
        {
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
        rows.Clear();
        keyLabels.Clear();

        switch (tab)
        {
            case Tab.Controls: BuildControls(); break;
            case Tab.Sound: BuildSound(); break;
            case Tab.Graphics: BuildGraphics(); break;
            case Tab.Game: BuildGame(); break;
        }

        WireNavigation();
        hintText.text = "↑↓ 이동   ←→ 변경   Enter 선택   ESC 뒤로";

        if (animate)
        {
            // 새 탭 항목이 순서대로 미끄러져 들어온다
            for (int i = 0; i < rows.Count; i++)
            {
                var row = (RectTransform)rows[i].transform;
                var group = row.GetComponent<CanvasGroup>();
                if (group == null) group = row.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.DOFade(1f, 0.16f).SetDelay(i * 0.025f).SetUpdate(true);
            }
        }
    }

    private void WireNavigation()
    {
        Selectable firstRow = rows.Count > 0 ? rows[0] : footerButtons[0];
        Selectable lastRow = rows.Count > 0 ? rows[rows.Count - 1] : null;

        for (int i = 0; i < tabButtons.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnLeft = tabButtons[(i - 1 + tabButtons.Count) % tabButtons.Count];
            nav.selectOnRight = tabButtons[(i + 1) % tabButtons.Count];
            nav.selectOnDown = firstRow;
            tabButtons[i].navigation = nav;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = i > 0 ? rows[i - 1] : tabButtons[(int)currentTab];
            nav.selectOnDown = i < rows.Count - 1 ? rows[i + 1] : footerButtons[0];
            rows[i].navigation = nav;
        }

        for (int i = 0; i < footerButtons.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnLeft = footerButtons[(i - 1 + footerButtons.Count) % footerButtons.Count];
            nav.selectOnRight = footerButtons[(i + 1) % footerButtons.Count];
            nav.selectOnUp = lastRow != null ? lastRow : tabButtons[(int)currentTab];
            footerButtons[i].navigation = nav;
        }
    }

    private void ResetCurrentTab()
    {
        switch (currentTab)
        {
            case Tab.Controls: KeyBindings.ResetToDefaults(); break;
            case Tab.Sound: GameSettings.ResetSound(); break;
            case Tab.Graphics: GameSettings.ResetGraphics(); break;
            case Tab.Game: GameSettings.ResetGame(); break;
        }
        ShowTab(currentTab, true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(footerButtons[0].gameObject);
    }

    private void Back()
    {
        if (KeyBindings.IsRebinding) return;
        PauseManager pm = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        pm?.ReturnPause();
    }

    // ================= Rows =================

    private RectTransform CreateRow(string label, out RectTransform shift)
    {
        var bg = DemoUI.CreatePanel("Row " + label, content, rowIdleColor);
        var row = (RectTransform)bg.transform;
        row.sizeDelta = new Vector2(0f, rowHeight);

        var marker = DemoUI.CreatePanel("Marker", row, accentColor);
        var markerRect = (RectTransform)marker.transform;
        markerRect.anchorMin = new Vector2(0f, 0f);
        markerRect.anchorMax = new Vector2(0f, 1f);
        markerRect.pivot = new Vector2(0f, 0.5f);
        markerRect.sizeDelta = new Vector2(10f, 0f);

        shift = DemoUI.CreateRect("Shift", row);
        DemoUI.Stretch(shift);

        var text = DemoUI.CreateText("Label", shift, label, 34f, TextAlignmentOptions.Left);
        var textRect = (RectTransform)text.transform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.offsetMin = new Vector2(30f, 0f);
        textRect.offsetMax = Vector2.zero;
        text.color = textColor;

        row.gameObject.AddComponent<OptionRowFx>().Bind(bg, markerRect, shift, rowIdleColor, rowSelectedColor);
        return row;
    }

    private void AddKeyRow(Keys slot)
    {
        RectTransform row = CreateRow(SlotRoles[slot], out RectTransform shift);
        var button = row.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;

        var keyBox = DemoUI.CreatePanel("KeyBox", shift, new Color(1f, 1f, 1f, 0.1f));
        var keyRect = (RectTransform)keyBox.transform;
        keyRect.anchorMin = keyRect.anchorMax = new Vector2(1f, 0.5f);
        keyRect.pivot = new Vector2(1f, 0.5f);
        keyRect.sizeDelta = new Vector2(300f, rowHeight - 18f);
        keyRect.anchoredPosition = new Vector2(-20f, 0f);
        var keyOutline = keyBox.gameObject.AddComponent<Outline>();
        keyOutline.effectColor = subAccentColor;
        keyOutline.effectDistance = new Vector2(2f, -2f);

        var keyText = DemoUI.CreateText("Key", keyRect, KeyBindings.DisplayName(slot), 36f, TextAlignmentOptions.Center, true);
        DemoUI.Stretch((RectTransform)keyText.transform);
        keyLabels[slot] = keyText;

        button.onClick.AddListener(() => StartRebind(slot, keyText, keyRect));
        rows.Add(button);
    }

    private void AddStepper(string label, Func<string[]> options, Func<int> get, Action<int> set)
    {
        RectTransform row = CreateRow(label, out RectTransform shift);
        var stepper = row.gameObject.AddComponent<OptionStepper>();
        stepper.transition = Selectable.Transition.None;

        var value = DemoUI.CreateText("Value", shift, "", 34f, TextAlignmentOptions.Center);
        var valueRect = (RectTransform)value.transform;
        valueRect.anchorMin = new Vector2(0.5f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(60f, 0f);
        valueRect.offsetMax = new Vector2(-60f, 0f);

        var left = DemoUI.CreateText("Left", shift, "◀", 30f, TextAlignmentOptions.Center);
        DemoUI.Place((RectTransform)left.transform, new Vector2(0.5f, 0.5f), new Vector2(50f, 50f), new Vector2(35f, 0f));
        left.color = subAccentColor;
        var right = DemoUI.CreateText("Right", shift, "▶", 30f, TextAlignmentOptions.Center);
        DemoUI.Place((RectTransform)right.transform, new Vector2(1f, 0.5f), new Vector2(50f, 50f), new Vector2(-35f, 0f));
        right.color = subAccentColor;

        stepper.Bind(value, options, get, set);
        rows.Add(stepper);
    }

    private void AddSlider(string label, Func<float> get, Action<float> set)
    {
        RectTransform row = CreateRow(label, out RectTransform shift);
        var slider = row.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;

        var area = DemoUI.CreateRect("Area", shift);
        area.anchorMin = new Vector2(0.5f, 0.5f);
        area.anchorMax = new Vector2(1f, 0.5f);
        area.offsetMin = new Vector2(40f, -8f);
        area.offsetMax = new Vector2(-150f, 8f);

        var track = DemoUI.CreatePanel("Track", area, new Color(1f, 1f, 1f, 0.15f));
        DemoUI.Stretch((RectTransform)track.transform);

        var fillArea = DemoUI.CreateRect("FillArea", area);
        DemoUI.Stretch(fillArea);
        var fill = DemoUI.CreatePanel("Fill", fillArea, accentColor);
        var fillRect = (RectTransform)fill.transform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

        var handleArea = DemoUI.CreateRect("HandleArea", area);
        DemoUI.Stretch(handleArea);
        var handle = DemoUI.CreatePanel("Handle", handleArea, Color.white);
        var handleRect = (RectTransform)handle.transform;
        handleRect.sizeDelta = new Vector2(22f, 40f);
        handleRect.localRotation = Quaternion.Euler(0f, 0f, -12f);

        var valueText = DemoUI.CreateText("Value", shift, "", 32f, TextAlignmentOptions.Right);
        var valueRect = (RectTransform)valueText.transform;
        valueRect.anchorMin = valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.sizeDelta = new Vector2(120f, rowHeight);
        valueRect.anchoredPosition = new Vector2(-20f, 0f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 20f; // 5% 단위
        slider.wholeNumbers = true;
        slider.value = Mathf.Round(get() * 20f);
        valueText.text = $"{Mathf.RoundToInt(get() * 100f)}";

        slider.onValueChanged.AddListener(v =>
        {
            set(v / 20f);
            valueText.text = $"{Mathf.RoundToInt(v * 5f)}";
            handleRect.DOKill(true);
            handleRect.DOPunchScale(new Vector3(0.3f, 0.3f, 0f), 0.15f, 6, 0.5f).SetUpdate(true);
        });
        rows.Add(slider);
    }

    private void AddInfo(string text)
    {
        var info = DemoUI.CreateText("Info", content, text, 24f, TextAlignmentOptions.Left);
        ((RectTransform)info.transform).sizeDelta = new Vector2(0f, 44f);
        info.color = dimTextColor;
        info.margin = new Vector4(30f, 0f, 0f, 0f);
    }

    // ================= Tab contents =================

    private void BuildControls()
    {
        foreach (Keys slot in KeyBindings.Slots) AddKeyRow(slot);
        AddInfo("항목을 선택하고 Enter → 새 키 입력 (ESC 취소). 겹치는 키는 서로 바뀝니다. Pause는 ESC 고정.");
    }

    private void BuildSound()
    {
        AddSlider("마스터 볼륨", () => GameSettings.MasterVolume, GameSettings.SetMasterVolume);
        AddSlider("음악", () => GameSettings.MusicVolume, GameSettings.SetMusicVolume);
        AddSlider("효과음", () => GameSettings.SfxVolume, GameSettings.SetSfxVolume);
        if (!GameSettings.HasSfxBus)
            AddInfo("FMOD에 SFX 버스가 없어 효과음 볼륨은 코드에서 재생하는 효과음에만 적용됩니다.");
    }

    private void BuildGraphics()
    {
        AddStepper("화면 모드", () => GameSettings.ScreenModeNames, () => GameSettings.ScreenModeIndex, GameSettings.SetScreenMode);
        AddStepper("해상도", ResolutionNames, () => GameSettings.CurrentResolutionIndex, GameSettings.SetResolution);
        AddStepper("수직 동기화", () => new[] { "끄기", "켜기" }, () => GameSettings.VSync ? 1 : 0, i => GameSettings.SetVSync(i == 1));
        AddStepper("프레임 제한", FrameRateNames, () => GameSettings.FrameRateIndex, GameSettings.SetFrameRate);
        AddStepper("그래픽 품질", () => QualitySettings.names, () => GameSettings.QualityIndex >= 0 ? GameSettings.QualityIndex : QualitySettings.GetQualityLevel(), GameSettings.SetQuality);
        AddInfo("프레임 제한은 수직 동기화를 끈 경우에만 적용됩니다.");
    }

    private void BuildGame()
    {
        AddStepper("입력 보정", OffsetNames, () => (GameSettings.InputOffsetMs + 200) / 5, i => GameSettings.SetInputOffsetMs(i * 5 - 200));
        AddInfo("판정이 늦게 나오면 + 방향, 이르게 나오면 − 방향으로 맞춥니다.");
    }

    private static string[] ResolutionNames()
    {
        var list = GameSettings.Resolutions;
        var names = new string[list.Count];
        for (int i = 0; i < list.Count; i++) names[i] = $"{list[i].x} × {list[i].y}";
        return names;
    }

    private static string[] FrameRateNames()
    {
        var names = new string[GameSettings.FrameRateOptions.Length];
        for (int i = 0; i < names.Length; i++)
            names[i] = GameSettings.FrameRateOptions[i] < 0 ? "제한 없음" : $"{GameSettings.FrameRateOptions[i]} FPS";
        return names;
    }

    private static string[] OffsetNames()
    {
        var names = new string[81];
        for (int i = 0; i < names.Length; i++)
        {
            int ms = i * 5 - 200;
            names[i] = ms > 0 ? $"+{ms} ms" : $"{ms} ms";
        }
        return names;
    }

    // ================= Rebinding =================

    private void StartRebind(Keys slot, TextMeshProUGUI keyText, RectTransform keyRect)
    {
        if (KeyBindings.IsRebinding) return;
        rebindRoutine = StartCoroutine(Rebind(slot, keyText, keyRect));
    }

    private IEnumerator Rebind(Keys slot, TextMeshProUGUI keyText, RectTransform keyRect)
    {
        KeyBindings.IsRebinding = true;
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;

        keyText.text = "키 입력…";
        keyText.color = subAccentColor;
        keyRect.DOKill(true);
        Tween pulse = keyRect.DOScale(1.06f, 0.25f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        hintText.text = "새 키를 누르세요   ESC 취소";

        // 이 버튼을 누른 Enter가 바로 입력되지 않게 한 프레임 쉰다.
        yield return null;

        bool finished = false;
        while (!finished)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) break;

                foreach (var control in keyboard.allKeys)
                {
                    if (control == null || !control.wasPressedThisFrame) continue;
                    Key key = control.keyCode;
                    if (KeyBindings.IsReserved(key))
                    {
                        hintText.text = $"{KeyBindings.DisplayName(key)} 키는 쓸 수 없습니다";
                        keyRect.DOShakeAnchorPos(0.25f, 12f, 20, 90f, false, true).SetUpdate(true);
                        continue;
                    }

                    KeyBindings.Set(slot, key);
                    finished = true;
                    break;
                }
            }
            if (!finished) yield return null;
        }

        pulse.Kill();
        keyRect.localScale = Vector3.one;
        keyRect.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.25f, 6, 0.5f).SetUpdate(true);
        RefreshKeyLabels();
        hintText.text = "↑↓ 이동   ←→ 변경   Enter 선택   ESC 뒤로";

        // ESC가 이번 프레임의 '뒤로' 입력으로 처리되지 않도록 한 프레임 뒤에 푼다.
        yield return null;
        KeyBindings.IsRebinding = false;
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        rebindRoutine = null;
    }

    private void CancelRebind()
    {
        if (rebindRoutine == null) return;
        StopCoroutine(rebindRoutine);
        rebindRoutine = null;
        KeyBindings.IsRebinding = false;
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        RefreshKeyLabels();
    }

    private void RefreshKeyLabels()
    {
        foreach (var pair in keyLabels)
        {
            if (pair.Value == null) continue;
            pair.Value.text = KeyBindings.DisplayName(pair.Key);
            pair.Value.color = Color.white;
        }
    }
}
