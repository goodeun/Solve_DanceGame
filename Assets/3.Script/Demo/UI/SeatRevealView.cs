using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과 화면의 라이벌전 땅따먹기 공개 연출.
/// 객석 카드가 뒤집힌 채 놓여 있고, 한 차례마다 플레이어와 라이벌이 자기 객석을 하나씩 공개한다.
/// 더 공개할 객석이 없는 쪽은 가만히 있고, 남은 쪽만 공개한다. 모두 공개하면 최종 승자를 표시한다.
/// 각 카드에는 플레이어·라이벌 분위기 게이지(막대)와 비율이 표시된다.
/// </summary>
public class SeatRevealView : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float roundDelay = 1.0f;
    [SerializeField] private Color playerColor = new Color(0.25f, 0.75f, 1f);
    [SerializeField] private Color rivalColor = new Color(1f, 0.27f, 0.33f);
    [SerializeField] private Color cardBackColor = new Color(0.13f, 0.06f, 0.2f, 0.95f);

    private const float CardWidth = 250f;
    private const float CardHeight = 330f;
    private const float BarMaxHeight = 190f;

    private class Card
    {
        public RectTransform root;
        public Image frame;
        public Outline outline;
        public GameObject back;
        public GameObject face;
        public RectTransform playerBar;
        public RectTransform rivalBar;
        public TextMeshProUGUI playerPercent;
        public TextMeshProUGUI rivalPercent;
        public TextMeshProUGUI ownerLabel;
        public DemoSession.SeatResult data;
    }

    private RectTransform root;
    private TextMeshProUGUI playerTally;
    private TextMeshProUGUI rivalTally;
    private TextMeshProUGUI headline;
    private readonly List<Card> cards = new List<Card>();
    private int playerCount;
    private int rivalCount;

    public RectTransform Root => root;

    /// <summary>parent 아래에 화면을 만들고 공개 연출을 재생한다. playerWins는 최종 판정 결과.</summary>
    public IEnumerator Play(RectTransform parent, IList<DemoSession.SeatResult> seats, bool playerWins)
    {
        Build(parent, seats);

        yield return new WaitForSeconds(0.8f);

        var playerQueue = new Queue<Card>();
        var rivalQueue = new Queue<Card>();
        var tieQueue = new List<Card>();

        // 차이가 작은 객석부터 공개해 긴장감을 만든다.
        var ordered = new List<Card>(cards);
        ordered.Sort((a, b) => Mathf.Abs(a.data.playerPoints - a.data.RivalPoints).CompareTo(Mathf.Abs(b.data.playerPoints - b.data.RivalPoints)));
        foreach (Card card in ordered)
        {
            if (card.data.Owner > 0) playerQueue.Enqueue(card);
            else if (card.data.Owner < 0) rivalQueue.Enqueue(card);
            else tieQueue.Add(card);
        }

        while (playerQueue.Count > 0 || rivalQueue.Count > 0)
        {
            if (playerQueue.Count > 0)
            {
                Reveal(playerQueue.Dequeue());
                playerCount++;
                Bump(playerTally, $"플레이어  {playerCount}");
            }

            if (rivalQueue.Count > 0)
            {
                Reveal(rivalQueue.Dequeue());
                rivalCount++;
                Bump(rivalTally, $"{rivalCount}  라이벌");
            }

            yield return new WaitForSeconds(roundDelay);
        }

        if (tieQueue.Count > 0)
        {
            foreach (Card card in tieQueue) Reveal(card);
            yield return new WaitForSeconds(roundDelay);
        }

        // 객석 수가 같으면 총 분위기로 비교한다.
        if (playerCount == rivalCount)
        {
            float player = 0f, rival = 0f;
            foreach (Card card in cards)
            {
                player += card.data.playerPoints;
                rival += card.data.RivalPoints;
            }
            float total = Mathf.Max(1f, player + rival);
            ShowHeadline($"객석 동점!  총 분위기  {Mathf.RoundToInt(player / total * 100f)}% : {Mathf.RoundToInt(rival / total * 100f)}%", Color.white, 54f);
            yield return new WaitForSeconds(roundDelay * 1.6f);
        }

        ShowHeadline(playerWins ? "플레이어 승리!" : "라이벌 승리...", playerWins ? playerColor : rivalColor, 110f);
        yield return new WaitForSeconds(roundDelay * 2f);
    }

    private void Build(RectTransform parent, IList<DemoSession.SeatResult> seats)
    {
        var bg = DemoUI.CreatePanel("SeatReveal", parent, new Color(0.04f, 0.01f, 0.08f, 0.97f));
        root = (RectTransform)bg.transform;
        DemoUI.Stretch(root);
        bg.raycastTarget = true;

        var title = DemoUI.CreateText("Title", root, "라이벌전 결과", 72f, TextAlignmentOptions.Center, true);
        DemoUI.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(1200f, 110f), new Vector2(0f, -110f));
        title.color = DemoUI.Settings.green;

        playerTally = DemoUI.CreateText("PlayerTally", root, "플레이어  0", 64f, TextAlignmentOptions.Left, true);
        DemoUI.Place((RectTransform)playerTally.transform, new Vector2(0f, 1f), new Vector2(600f, 100f), new Vector2(380f, -240f));
        playerTally.color = playerColor;

        rivalTally = DemoUI.CreateText("RivalTally", root, "0  라이벌", 64f, TextAlignmentOptions.Right, true);
        DemoUI.Place((RectTransform)rivalTally.transform, new Vector2(1f, 1f), new Vector2(600f, 100f), new Vector2(-380f, -240f));
        rivalTally.color = rivalColor;

        headline = DemoUI.CreateText("Headline", root, "", 110f, TextAlignmentOptions.Center, true);
        DemoUI.Place((RectTransform)headline.transform, new Vector2(0.5f, 0f), new Vector2(1600f, 160f), new Vector2(0f, 150f));
        headline.gameObject.SetActive(false);

        cards.Clear();
        int count = seats.Count;
        float spacing = 40f;
        float totalWidth = count * CardWidth + (count - 1) * spacing;
        for (int i = 0; i < count; i++)
        {
            float x = -totalWidth / 2f + CardWidth / 2f + i * (CardWidth + spacing);
            cards.Add(CreateCard(seats[i], new Vector2(x, -20f)));
        }
    }

    private Card CreateCard(DemoSession.SeatResult data, Vector2 position)
    {
        var card = new Card { data = data };

        card.frame = DemoUI.CreatePanel("Seat " + data.key, root, cardBackColor);
        card.root = (RectTransform)card.frame.transform;
        DemoUI.Place(card.root, new Vector2(0.5f, 0.5f), new Vector2(CardWidth, CardHeight), position);
        card.outline = card.frame.gameObject.AddComponent<Outline>();
        card.outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
        card.outline.effectDistance = new Vector2(4f, -4f);

        // 뒷면: 키 + ?
        card.back = DemoUI.CreateRect("Back", card.root).gameObject;
        DemoUI.Stretch((RectTransform)card.back.transform);
        var backKey = DemoUI.CreateText("Key", card.back.transform, data.key, 120f, TextAlignmentOptions.Center, true);
        DemoUI.Stretch((RectTransform)backKey.transform, 0f, 0f, 20f, 90f);
        backKey.color = new Color(1f, 1f, 1f, 0.5f);
        var question = DemoUI.CreateText("Q", card.back.transform, "?", 70f, TextAlignmentOptions.Center, true);
        DemoUI.Stretch((RectTransform)question.transform, 0f, 0f, 200f, 10f);
        question.color = DemoUI.Settings.green;

        // 앞면: 키, 두 게이지, 비율, 소유자
        card.face = DemoUI.CreateRect("Face", card.root).gameObject;
        DemoUI.Stretch((RectTransform)card.face.transform);
        var key = DemoUI.CreateText("Key", card.face.transform, data.key, 56f, TextAlignmentOptions.Left, true);
        DemoUI.Place((RectTransform)key.transform, new Vector2(0f, 1f), new Vector2(120f, 70f), new Vector2(70f, -45f));

        card.playerBar = CreateBar(card.face.transform, new Vector2(-45f, 55f), playerColor);
        card.rivalBar = CreateBar(card.face.transform, new Vector2(45f, 55f), rivalColor);

        card.playerPercent = DemoUI.CreateText("P%", card.face.transform, "", 30f);
        DemoUI.Place((RectTransform)card.playerPercent.transform, new Vector2(0.5f, 0f), new Vector2(110f, 40f), new Vector2(-45f, 30f));
        card.playerPercent.color = playerColor;
        card.rivalPercent = DemoUI.CreateText("R%", card.face.transform, "", 30f);
        DemoUI.Place((RectTransform)card.rivalPercent.transform, new Vector2(0.5f, 0f), new Vector2(110f, 40f), new Vector2(45f, 30f));
        card.rivalPercent.color = rivalColor;

        card.ownerLabel = DemoUI.CreateText("Owner", card.face.transform, "", 34f, TextAlignmentOptions.Right, true);
        DemoUI.Place((RectTransform)card.ownerLabel.transform, new Vector2(1f, 1f), new Vector2(150f, 60f), new Vector2(-85f, -45f));

        card.face.SetActive(false);
        return card;
    }

    private static RectTransform CreateBar(Transform parent, Vector2 bottomCenter, Color color)
    {
        var track = DemoUI.CreatePanel("Track", parent, new Color(1f, 1f, 1f, 0.08f));
        var trackRect = (RectTransform)track.transform;
        trackRect.anchorMin = trackRect.anchorMax = new Vector2(0.5f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.sizeDelta = new Vector2(56f, BarMaxHeight);
        trackRect.anchoredPosition = bottomCenter;

        var bar = DemoUI.CreatePanel("Bar", trackRect, color);
        var rect = (RectTransform)bar.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(56f, 0f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private void Reveal(Card card)
    {
        Color owner = card.data.Owner > 0 ? playerColor : card.data.Owner < 0 ? rivalColor : Color.white;

        // 카드를 뒤집는다(가로로 접혔다 펴지며 앞면 표시).
        Sequence flip = DOTween.Sequence();
        flip.Append(card.root.DOScaleX(0f, 0.15f).SetEase(Ease.InQuad));
        flip.AppendCallback(() =>
        {
            card.back.SetActive(false);
            card.face.SetActive(true);
            card.frame.color = new Color(owner.r * 0.25f, owner.g * 0.25f, owner.b * 0.25f, 0.95f);
            card.outline.effectColor = owner;
            card.ownerLabel.text = card.data.Owner > 0 ? "플레이어" : card.data.Owner < 0 ? "라이벌" : "무승부";
            card.ownerLabel.color = owner;

            float player = card.data.PlayerShare;
            card.playerPercent.text = $"{Mathf.RoundToInt(player * 100f)}%";
            card.rivalPercent.text = $"{Mathf.RoundToInt((1f - player) * 100f)}%";
            card.playerBar.DOSizeDelta(new Vector2(56f, BarMaxHeight * player), 0.45f).SetEase(Ease.OutCubic);
            card.rivalBar.DOSizeDelta(new Vector2(56f, BarMaxHeight * (1f - player)), 0.45f).SetEase(Ease.OutCubic);
        });
        flip.Append(card.root.DOScaleX(1f, 0.18f).SetEase(Ease.OutBack));
        flip.Append(card.root.DOPunchScale(Vector3.one * 0.12f, 0.35f, 6, 0.6f));
    }

    private static void Bump(TextMeshProUGUI text, string value)
    {
        text.text = value;
        text.transform.DOKill(true);
        text.transform.DOPunchScale(Vector3.one * 0.35f, 0.4f, 6, 0.6f);
    }

    private void ShowHeadline(string text, Color color, float size)
    {
        headline.gameObject.SetActive(true);
        headline.text = text;
        headline.fontSize = size;
        headline.color = color;
        headline.transform.localScale = Vector3.one * 1.6f;
        headline.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
    }
}
