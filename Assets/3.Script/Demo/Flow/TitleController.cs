using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면. 새로하기 / 이어하기 / 종료.
/// - 새로하기: 기록 초기화 후 튜토리얼(없으면 메인 게임)로 이동
/// - 이어하기: 일상 파트가 없는 데모이므로 데모 범위 안내 팝업
/// 하단에 데모 범위 안내(댄스 파트까지 플레이 가능, 일상 파트는 추후 추가)를 표시한다.
/// UI는 실행 시 생성되며 배경 이미지(키아트)는 Inspector에서 지정할 수 있다.
/// </summary>
public class TitleController : MonoBehaviour
{
    [SerializeField] private Sprite background;
    [SerializeField] private string titleText = "sølve";
    [SerializeField] private string subtitleText = "DANCE DEMO";
    [SerializeField, TextArea] private string demoNotice = "이번 데모는 댄스 파트(일반전 · 라이벌전)까지 플레이할 수 있습니다.\n일상 파트는 추후 업데이트로 추가될 예정입니다.";
    [SerializeField, TextArea] private string continueNotice = "이어하기는 일상 파트가 추가된 뒤 지원됩니다.\n이번 데모에서는 '새로하기'로 플레이해 주세요.";
    [SerializeField] private bool skipTutorialIfMissing = true;
    [Header("Debug")]
    [Tooltip("우측 상단에 라이벌 씬으로 바로 가는 디버그 버튼을 띄운다(배포 전에 끄기)")]
    [SerializeField] private bool showDebugBossButton = true;

    private void Start()
    {
        Time.timeScale = 1f;
        DemoUI.EnsureEventSystem();
        EnsureCamera();
        Build();
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null) return;

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.01f, 0.07f);
    }

    private void Build()
    {
        DemoUISettings s = DemoUI.Settings;
        Canvas canvas = DemoUI.CreateOverlayCanvas("TitleCanvas", 100, transform);

        var bg = DemoUI.CreatePanel("Background", canvas.transform, background != null ? Color.white : new Color(0.06f, 0.02f, 0.1f), background);
        if (background != null) bg.type = Image.Type.Simple;
        DemoUI.Stretch((RectTransform)bg.transform);

        var title = DemoUI.CreateText("Title", canvas.transform, titleText, 220f, TextAlignmentOptions.Center, true);
        DemoUI.Place((RectTransform)title.transform, new Vector2(0.5f, 0.72f), new Vector2(1400f, 300f), Vector2.zero);
        title.color = s.magenta;
        title.transform.DOScale(1.04f, 0.47f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        var subtitle = DemoUI.CreateText("Subtitle", canvas.transform, subtitleText, 60f, TextAlignmentOptions.Center, true);
        DemoUI.Place((RectTransform)subtitle.transform, new Vector2(0.5f, 0.6f), new Vector2(1200f, 90f), Vector2.zero);
        subtitle.color = s.cyan;
        subtitle.characterSpacing = 20f;

        Button newGame = DemoUI.CreateButton("NewGame", canvas.transform, "새로하기", new Vector2(460f, 100f), NewGame);
        DemoUI.Place((RectTransform)newGame.transform, new Vector2(0.5f, 0.42f), new Vector2(460f, 100f), Vector2.zero);

        Button cont = DemoUI.CreateButton("Continue", canvas.transform, "이어하기", new Vector2(460f, 100f), ShowContinueNotice);
        DemoUI.Place((RectTransform)cont.transform, new Vector2(0.5f, 0.42f), new Vector2(460f, 100f), new Vector2(0f, -125f));

        Button quit = DemoUI.CreateButton("Quit", canvas.transform, "종료", new Vector2(460f, 100f), SceneFlow.Quit);
        DemoUI.Place((RectTransform)quit.transform, new Vector2(0.5f, 0.42f), new Vector2(460f, 100f), new Vector2(0f, -250f));

        var notice = DemoUI.CreateText("DemoNotice", canvas.transform, demoNotice, 30f);
        DemoUI.Place((RectTransform)notice.transform, new Vector2(0.5f, 0.06f), new Vector2(1600f, 110f), Vector2.zero);
        notice.color = new Color(1f, 1f, 1f, 0.75f);

        if (showDebugBossButton)
        {
            Button debugBoss = DemoUI.CreateButton("DebugBoss", canvas.transform, "[DEBUG] 라이벌전", new Vector2(320f, 70f), DebugBoss);
            DemoUI.Place((RectTransform)debugBoss.transform, new Vector2(1f, 1f), new Vector2(320f, 70f), new Vector2(-190f, -65f));
        }

        EventSystem.current?.SetSelectedGameObject(newGame.gameObject);
    }

    /// <summary>디버그: 기록 초기화 후 라이벌 씬으로 바로 이동.</summary>
    private void DebugBoss()
    {
        DemoSession.Reset();
        SceneFlow.Load(DemoUI.Settings.bossScene);
    }

    private void NewGame()
    {
        DemoSession.Reset();

        DemoUISettings s = DemoUI.Settings;
        string next = s.tutorialScene;
        if (skipTutorialIfMissing && !Application.CanStreamedLevelBeLoaded(next))
            next = s.mainScene;

        SceneFlow.Load(next);
    }

    private void ShowContinueNotice()
    {
        DemoUI.ShowConfirmPopup(continueNotice, "확인", null);
    }
}
