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
    [Tooltip("Pause 캔버스 정렬 순서. 대사창(650)·배너·카운트다운보다 앞, 확인 팝업(2000)보다 뒤")]
    [SerializeField] private int pauseSortingOrder = 1500;
    [SerializeField] private string mainConfirmMessage = "타이틀로 돌아갈까요?\n진행 중인 플레이는 저장되지 않습니다.";

    private Transform pauseRoot;
    // 같은 PauseManager에 버튼이 두 번 연결되면 팝업이 두 개 뜨므로 하나만 연결한다.
    private static readonly System.Collections.Generic.HashSet<PauseManager> wired = new System.Collections.Generic.HashSet<PauseManager>();
    private PauseManager wiredManager;

    private void Awake()
    {
        if (pauseManager == null) pauseManager = GetComponentInChildren<PauseManager>(true);
        if (pauseManager == null) pauseManager = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        if (pauseManager == null) return;
        if (!wired.Add(pauseManager)) return;
        wiredManager = pauseManager;

        // 버튼·패널은 PauseSystem 아래에서 찾는다(이 컴포넌트가 다른 오브젝트에 있어도 동작).
        pauseRoot = pauseManager.transform.root;

        // Pause 패널이 대사창보다 앞에 보이도록 PauseSystem의 Canvas 정렬을 올린다.
        foreach (Canvas canvas in pauseRoot.GetComponentsInChildren<Canvas>(true))
        {
            if (!canvas.isRootCanvas && !canvas.overrideSorting) continue;
            canvas.overrideSorting = true;
            canvas.sortingOrder = pauseSortingOrder;
        }

        if (resumeButton == null) resumeButton = FindButton("Resume_b");
        if (optionButton == null) optionButton = FindButton("Option_b");
        if (creditButton == null) creditButton = FindButton("Credit_b");
        if (mainButton == null) mainButton = FindButton("Main_b");
        if (optionPanel == null) optionPanel = FindChild("OptionPanel");
        if (creditPanel == null) creditPanel = FindChild("CreditPanel");

        // 서브 패널·팝업은 처음에 닫아 둔다(반투명 배경이 겹쳐 화면이 희게 덮이던 문제).
        // PausePanel의 자식이면 서브 메뉴 진입 때 PausePanel과 함께 꺼지므로 PausePanel 옆(형제)으로 옮긴다.
        PrepareSubPanel(optionPanel);
        PrepareSubPanel(creditPanel);
        PrepareSubPanel(FindChild("MainConfirmPopup"));
        resumeButton?.onClick.AddListener(Resume);
        optionButton?.onClick.AddListener(() => OpenSubPanel(optionPanel));
        creditButton?.onClick.AddListener(() => OpenSubPanel(creditPanel));
        mainButton?.onClick.AddListener(GoToTitle);
    }

    private void OnDestroy()
    {
        if (wiredManager != null) wired.Remove(wiredManager);
        wired.RemoveWhere(m => m == null);
    }

    private static void PrepareSubPanel(GameObject panel)
    {
        if (panel == null) return;

        Transform parent = panel.transform.parent;
        if (parent != null && parent.GetComponent<PausePanelView>() != null && parent.parent != null)
            panel.transform.SetParent(parent.parent, false);

        panel.SetActive(false);
    }

    private Button FindButton(string name)
    {
        GameObject go = FindChild(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private GameObject FindChild(string name)
    {
        foreach (Transform t in pauseRoot.GetComponentsInChildren<Transform>(true))
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
