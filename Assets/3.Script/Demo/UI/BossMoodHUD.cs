using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스전 객석별 분위기 비교 표시(플레이어 vs 라이벌 막대).
/// 기존 객석 색상(임계값 기반)과 별개로 최종 승패 판정에 쓰이는 값을 그대로 보여준다.
/// </summary>
public class BossMoodHUD : MonoBehaviour
{
    [SerializeField] private int sortingOrder = 500;
    [SerializeField] private float barMaxHeight = 120f;

    private BossBattleDirector director;
    private GameObject root;
    private TextMeshProUGUI[] keyLabels;
    private RectTransform[] playerBars;
    private RectTransform[] rivalBars;
    private TextMeshProUGUI remainingText;

    public void Bind(BossBattleDirector target)
    {
        director = target;
        Build();
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }

    private void Build()
    {
        if (root != null || director == null) return;

        DemoUISettings s = DemoUI.Settings;
        Canvas canvas = DemoUI.CreateOverlayCanvas("BossMoodCanvas", sortingOrder, transform);
        root = canvas.gameObject;

        int count = director.SeatCount;
        keyLabels = new TextMeshProUGUI[count];
        playerBars = new RectTransform[count];
        rivalBars = new RectTransform[count];

        var panel = DemoUI.CreatePanel("Panel", canvas.transform, new Color(0f, 0f, 0f, 0.55f));
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.sizeDelta = new Vector2(120f * count + 40f, barMaxHeight + 130f);
        panelRect.anchoredPosition = new Vector2(-30f, -150f);

        remainingText = DemoUI.CreateText("Remaining", panelRect, "", 30f);
        var remRect = (RectTransform)remainingText.transform;
        remRect.anchorMin = new Vector2(0f, 1f);
        remRect.anchorMax = new Vector2(1f, 1f);
        remRect.pivot = new Vector2(0.5f, 1f);
        remRect.sizeDelta = new Vector2(0f, 40f);
        remRect.anchoredPosition = new Vector2(0f, -6f);

        for (int i = 0; i < count; i++)
        {
            float x = 20f + 120f * i + 60f;

            playerBars[i] = CreateBar(panelRect, new Vector2(x - 22f, 50f), s.magenta);
            rivalBars[i] = CreateBar(panelRect, new Vector2(x + 22f, 50f), s.cyan);

            keyLabels[i] = DemoUI.CreateText("Key", panelRect, "", 34f, TextAlignmentOptions.Center, true);
            var labelRect = (RectTransform)keyLabels[i].transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0f);
            labelRect.sizeDelta = new Vector2(100f, 40f);
            labelRect.anchoredPosition = new Vector2(x, 25f);
        }
    }

    private static RectTransform CreateBar(RectTransform parent, Vector2 bottomCenter, Color color)
    {
        var bar = DemoUI.CreatePanel("Bar", parent, color);
        var rect = (RectTransform)bar.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(34f, 0f);
        rect.anchoredPosition = bottomCenter;
        return rect;
    }

    private void Update()
    {
        if (director == null || root == null || !root.activeSelf) return;

        float max = 100f;
        for (int i = 0; i < director.SeatCount; i++)
            max = Mathf.Max(max, director.GetPlayerMood(i), director.GetRivalMood(i));

        for (int i = 0; i < director.SeatCount; i++)
        {
            float p = director.GetPlayerMood(i);
            float r = director.GetRivalMood(i);
            playerBars[i].sizeDelta = new Vector2(34f, barMaxHeight * p / max);
            rivalBars[i].sizeDelta = new Vector2(34f, barMaxHeight * r / max);

            keyLabels[i].text = director.GetSeatKey(i).ToString();
            keyLabels[i].color = p > r ? DemoUI.Settings.magenta : r > p ? DemoUI.Settings.cyan : Color.white;
        }

        remainingText.text = director.IsBattling ? $"남은 마디 {director.RemainingBars}" : "";
    }
}
