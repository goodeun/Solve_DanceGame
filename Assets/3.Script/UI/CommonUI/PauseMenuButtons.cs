using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause 메뉴 버튼 연결(Resume / Option / Credit / Main).
/// PauseSystem 프리팹의 버튼 OnClick이 비어 있어 이름으로 찾아 코드에서 연결한다.
/// 버튼·패널을 Inspector에서 직접 지정해도 된다.
/// - Resume : 기존 흐름 유지(메뉴 닫기 → 1박 카운트다운 → 재개)
/// - Option, Credit : 서브 패널을 열고 ESC로 닫는다(PauseManager의 서브 패널 스택 사용)
/// - Main : 확인 팝업 후 타이틀 씬으로 이동
/// </summary>
public class PauseMenuButtons : MonoBehaviour
{
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button creditButton;
    [SerializeField] private Button mainButton;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject creditPanel;
    [SerializeField] private string mainConfirmMessage = "타이틀로 돌아갈까요?\n진행 중인 플레이는 저장되지 않습니다.";

    private void Awake()
    {
        if (pauseManager == null) pauseManager = GetComponentInChildren<PauseManager>(true);
        if (pauseManager == null) pauseManager = FindFirstObjectByType<PauseManager>();
        if (resumeButton == null) resumeButton = FindButton("Resume_b");
        if (optionButton == null) optionButton = FindButton("Option_b");
        if (creditButton == null) creditButton = FindButton("Credit_b");
        if (mainButton == null) mainButton = FindButton("Main_b");
        if (optionPanel == null) optionPanel = FindChild("OptionPanel");
        if (creditPanel == null) creditPanel = FindChild("CreditPanel");
        resumeButton?.onClick.AddListener(Resume);
        optionButton?.onClick.AddListener(() => OpenSubPanel(optionPanel));
        creditButton?.onClick.AddListener(() => OpenSubPanel(creditPanel));
        mainButton?.onClick.AddListener(GoToTitle);
    }

    private Button FindButton(string name)
    {
        GameObject go = FindChild(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private GameObject FindChild(string name)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    public void Resume()
    {
        if (pauseManager != null && pauseManager.isPauseActive)
            pauseManager.ReturnGame();
    }

    private void OpenSubPanel(GameObject panel)
    {
        if (pauseManager == null || panel == null) return;

        var sub = panel.GetComponent<SimplePauseSubPanel>();
        if (sub == null) sub = panel.AddComponent<SimplePauseSubPanel>();
        pauseManager.OpenSubPanel(sub);
    }

    public void GoToTitle()
    {
        StoryDialogueView.InputBlockCount++;
        DemoUI.ShowConfirmPopup(mainConfirmMessage, "타이틀로", () =>
        {
            StoryDialogueView.InputBlockCount = Mathf.Max(0, StoryDialogueView.InputBlockCount - 1);
            SceneFlow.Load(DemoUI.Settings.titleScene);
        }, "취소", () => StoryDialogueView.InputBlockCount = Mathf.Max(0, StoryDialogueView.InputBlockCount - 1), 2000);
    }
}
