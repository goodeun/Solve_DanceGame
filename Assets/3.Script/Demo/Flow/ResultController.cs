using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 점수 씬. 승패와 점수를 계산해 한 종류씩 순서대로 표시한다.
/// 승리 조건: 기준 점수를 넘기고 보스전에서도 승리.
/// 표시: 승리/패배 → 최종 점수 → 판정 개수 → 최대 콤보 → 랭크 → (자동) 후속 대사
/// → [다음날] 버튼 → 데모 감사 인사·업데이트 안내 → 타이틀로(이후 타이틀에서 종료).
/// </summary>
public class ResultController : MonoBehaviour
{
    [Header("Rules")]
    [Tooltip("승리 기준 점수(일반전 + 보스전 합계)")]
    [SerializeField, Min(0)] private int targetScore = 300000;
    [Tooltip("랭크 기준: 기준 점수 대비 비율 S, A, B, C (그 아래는 D)")]
    [SerializeField] private float[] rankRatios = { 1.5f, 1.0f, 0.7f, 0.4f };

    [Header("Presentation")]
    [SerializeField] private Sprite background;
    [SerializeField, Min(0f)] private float stepDelay = 0.6f;
    [SerializeField] private List<StoryDialogueLine> winLines = new List<StoryDialogueLine>();
    [SerializeField] private List<StoryDialogueLine> loseLines = new List<StoryDialogueLine>();
    [SerializeField, TextArea] private string thanksText =
        "데모를 플레이해 주셔서 감사합니다!\n\n" +
        "다음 업데이트에서는 도시를 돌아다니며 스탯을 키우는 일상 파트,\n" +
        "네임드 NPC와의 호감도, 더 많은 곡과 라이벌이 추가될 예정입니다.\n\n" +
        "- Team sølve -";

    private RectTransform list;
    private Canvas canvas;

    public static string GetRank(int score, int target, float[] ratios)
    {
        string[] ranks = { "S", "A", "B", "C" };
        for (int i = 0; i < ratios.Length && i < ranks.Length; i++)
            if (score >= target * ratios[i]) return ranks[i];
        return "D";
    }

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        DemoUI.EnsureEventSystem();
        EnsureCamera();
        BuildFrame();

        int score = DemoSession.FinalScore;
        bool won = DemoSession.BossWon && score >= targetScore;
        string rank = GetRank(score, targetScore, rankRatios);
        DemoUISettings s = DemoUI.Settings;

        if (winLines == null || winLines.Count == 0)
            winLines = StoryDialogueLine.List(
                StoryDialogueLine.Npc("클럽 매니저", "오늘 밤 최고의 무대였어! 네 이름이 벌써 도시에 돌고 있다고.", DialogueStyle.Emphasis),
                StoryDialogueLine.Npc("클럽 매니저", $"랭크 {rank} 달성! 평판이 올라서 새로운 제안이 들어올지도 몰라."),
                StoryDialogueLine.Npc("라이벌", "…이번 한 번뿐이야. 다음 무대에선 절대 안 져.", DialogueStyle.Angry));
        if (loseLines == null || loseLines.Count == 0)
            loseLines = StoryDialogueLine.List(
                StoryDialogueLine.Npc("클럽 매니저", "아쉽네. 그래도 관객들 몇몇은 네 춤을 기억할 거야.", DialogueStyle.Gloomy),
                StoryDialogueLine.Npc("클럽 매니저", "낮 동안 스탯을 더 키워서 다시 도전해 보자. (일상 파트는 추후 업데이트!)"),
                StoryDialogueLine.Npc("라이벌", "다음에도 이 무대는 내 거야.", DialogueStyle.Emphasis));

        yield return new WaitForSeconds(stepDelay);

        // 1. 승패
        var verdict = AddRow(won ? "CLEAR!" : "FAILED...", 120f, won ? s.green : s.magenta, true);
        verdict.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 6, 0.5f);
        string bossLine = DemoSession.BossPlayed
            ? $"라이벌전 {(DemoSession.BossWon ? "승리" : "패배")}  (객석 {DemoSession.PlayerSeats} : {DemoSession.RivalSeats})"
            : "라이벌전 미진행";
        AddRow(bossLine, 40f, s.cyan);
        yield return new WaitForSeconds(stepDelay);

        // 2. 최종 점수(카운트 업)
        var scoreText = AddRow("SCORE 0", 80f, Color.white, true);
        int shown = 0;
        yield return DOTween.To(() => shown, x => { shown = x; scoreText.text = $"SCORE {shown:N0}"; }, score, 1.2f).SetEase(Ease.OutCubic).WaitForCompletion();
        AddRow($"일반전 {DemoSession.NormalScore:N0}  /  라이벌전 {DemoSession.BossScore:N0}  (기준 {targetScore:N0})", 32f, new Color(1f, 1f, 1f, 0.7f));
        yield return new WaitForSeconds(stepDelay);

        // 3. 판정 개수
        int[] j = DemoSession.JudgeCounts;
        AddRow($"JUST {j[(int)Judge.Just]}   GOOD {j[(int)Judge.Good]}   BAD {j[(int)Judge.Bad]}   MISS {j[(int)Judge.Miss]}", 46f, Color.white);
        yield return new WaitForSeconds(stepDelay);

        // 4. 콤보
        AddRow($"MAX COMBO {DemoSession.MaxCombo}", 50f, s.green);
        yield return new WaitForSeconds(stepDelay);

        // 5. 랭크
        var rankText = AddRow($"RANK {rank}", 140f, s.magenta, true);
        rankText.transform.localScale = Vector3.one * 2f;
        yield return rankText.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).WaitForCompletion();
        yield return new WaitForSeconds(stepDelay * 2f);

        // 6. 후속 대사(자동)
        bool done = false;
        StoryDialogueView.GetOrCreate().Play(won ? winLines : loseLines, () => done = true);
        while (!done) yield return null;

        // 7. 다음날 버튼
        Button next = DemoUI.CreateButton("NextDay", canvas.transform, "다음날 ▶", new Vector2(420f, 100f), ShowThanks);
        DemoUI.Place((RectTransform)next.transform, new Vector2(0.5f, 0.07f), new Vector2(420f, 100f), Vector2.zero);
        EventSystem.current?.SetSelectedGameObject(next.gameObject);
    }

    private static void EnsureCamera()
    {
        if (Camera.main != null) return;

        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.01f, 0.07f);
    }

    private void BuildFrame()
    {
        canvas = DemoUI.CreateOverlayCanvas("ResultCanvas", 100, transform);

        var bg = DemoUI.CreatePanel("Background", canvas.transform, background != null ? Color.white : new Color(0.06f, 0.02f, 0.1f), background);
        if (background != null) bg.type = Image.Type.Simple;
        DemoUI.Stretch((RectTransform)bg.transform);

        list = DemoUI.CreateRect("Rows", canvas.transform);
        list.anchorMin = new Vector2(0.5f, 1f);
        list.anchorMax = new Vector2(0.5f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        list.sizeDelta = new Vector2(1600f, 900f);
        list.anchoredPosition = new Vector2(0f, -60f);

        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 12f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
    }

    private TextMeshProUGUI AddRow(string text, float size, Color color, bool title = false)
    {
        var row = DemoUI.CreateText("Row", list, text, size, TextAlignmentOptions.Center, title);
        row.color = color;
        var element = row.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = size * 1.35f;

        row.alpha = 0f;
        row.DOFade(1f, 0.3f);
        return row;
    }

    private void ShowThanks()
    {
        var dim = DemoUI.CreatePanel("Thanks", canvas.transform, new Color(0.03f, 0f, 0.06f, 0.97f));
        DemoUI.Stretch((RectTransform)dim.transform);
        dim.raycastTarget = true;

        var text = DemoUI.CreateText("Message", dim.transform, thanksText, 48f);
        DemoUI.Stretch((RectTransform)text.transform, 200f, 200f, 150f, 250f);
        text.alpha = 0f;
        text.DOFade(1f, 0.6f);

        Button toTitle = DemoUI.CreateButton("ToTitle", dim.transform, "타이틀로", new Vector2(420f, 100f),
            () => SceneFlow.Load(DemoUI.Settings.titleScene));
        DemoUI.Place((RectTransform)toTitle.transform, new Vector2(0.5f, 0.1f), new Vector2(420f, 100f), Vector2.zero);
        EventSystem.current?.SetSelectedGameObject(toTitle.gameObject);
    }
}
