using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum KeyTextColor { White, Black }

[System.Serializable]
public struct BeatSlot
{
    public RectTransform circleRect;
    public Image keyImage;
    public Image judgementImage;
}

public class RhythmViewUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private RhythmWheelUI rhythmWheel;

    [Header("호선 세팅")]
    [Tooltip("스프라이트에 맞게 조절")]
    [SerializeField] private float radiusX = 300f;
    [SerializeField] private float radiusY = 150f;
    [SerializeField] private float startAngle = 180f;
    [SerializeField] private float endAngle = 360f;

    [Header("UI Elements")]
    [SerializeField] private RectTransform indicatorBar;
    [SerializeField] private BeatSlot[] beatCircles;
    [SerializeField] private Sprite[] judgeSprites;
    [SerializeField] private Sprite[] KeySprites;

    [Header("Key Text (키 이미지 위 글자, 옵션 키 바인딩 반영)")]
    [Tooltip("Keys 순서(W, A, S, D, Q, E)별 글자색. 흰 화살표는 Black, 검은 화살표는 White")]
    [SerializeField] private KeyTextColor[] keyTextColors =
        { KeyTextColor.Black, KeyTextColor.White, KeyTextColor.White, KeyTextColor.White, KeyTextColor.Black, KeyTextColor.Black };
    [SerializeField] private TMP_FontAsset keyTextFont;
    [SerializeField, Min(1f)] private float keyTextSize = 26f;
    [Tooltip("키 이미지 중심에서 글자 위치 보정")]
    [SerializeField] private Vector2 keyTextOffset = Vector2.zero;

    private TextMeshProUGUI[] keyTexts;

    private int curSlotIndex;

    private void Awake()
    {
        musicClock = FindAnyObjectByType<FmodMusicClock>();
        rhythmWheel = FindAnyObjectByType<RhythmWheelUI>();
    }
    private void Start()
    {
        rhythmWheel.OnJudged += HandleJudged;
        musicClock.TimelineBeat += HandleTimeLineBeat;

        ArrangeBeatCircles();
        CreateKeyTexts();
        ClearSlot();
    }
    private void Update()
    {
        if (musicClock == null) return;

        float t = GetBarProgress();
        UpdateIndicator(t);
    }

    private void OnDestroy()
    {
        rhythmWheel.OnJudged -= HandleJudged;
        musicClock.TimelineBeat -= HandleTimeLineBeat;
    }

    private void ArrangeBeatCircles()
    {
        for (int i = 0; i < beatCircles.Length; i++)
        {
            float t = (i + 1) * 0.25f;
            beatCircles[i].circleRect.anchoredPosition = GetEllipsePosition(t);
        }
    }

    private float GetBarProgress()
    {
        // TODO 마디 진행률 계산
        if (!musicClock.TryGetElapsedSeconds(out float elapsed)) return 0f;

        float beatProgress = Mathf.Clamp01((elapsed - musicClock.CurrentBeatStartSeconds) / musicClock.BeatDurationSeconds);
        int beatIndex = musicClock.CurrentBeat % 4;

        return (beatIndex + beatProgress) / 4f;
    }

    private void UpdateIndicator(float t)
    {
        // TODO 타원 접선 각도 적용
        float angleDeg = Mathf.Lerp(startAngle, endAngle, t);
        float rad = angleDeg * Mathf.Deg2Rad;

        float dx = -Mathf.Sin(rad) * radiusX;
        float dy = Mathf.Cos(rad) * radiusY;

        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

        indicatorBar.localRotation = Quaternion.Euler(0f, 0f, angle);
        indicatorBar.anchoredPosition= GetEllipsePosition(t);

    }

    private Vector2 GetEllipsePosition(float t)
    {
        float angleDeg = Mathf.Lerp(startAngle, endAngle, t);
        float rad = angleDeg * Mathf.Deg2Rad;

        float x = Mathf.Cos(rad) * radiusX;
        float y = Mathf.Sin(rad) * radiusY;
        return new Vector2(x, y);
    }

    private void HandleJudged(Keys key, Judge judge, float offset)
    {
        if (curSlotIndex >= 0 && curSlotIndex < 3)
        {
            BeatSlot curCircles = beatCircles[curSlotIndex];

            if (judge == Judge.Miss)
            {
                curCircles.keyImage.enabled = false;
            }
            else
            {
                curCircles.keyImage.sprite = KeySprites[(int)key];
                curCircles.keyImage.enabled = true;
            }

            SetKeyText(curSlotIndex, judge == Judge.Miss ? null : (Keys?)key);

            curCircles.judgementImage.sprite = judgeSprites[(int)judge];
            curCircles.circleRect.gameObject.SetActive(true);
        }

        curSlotIndex++;
    }

    private void CreateKeyTexts()
    {
        keyTexts = new TextMeshProUGUI[beatCircles.Length];
        for (int i = 0; i < beatCircles.Length; i++)
        {
            Image image = beatCircles[i].keyImage;
            if (image == null) continue;

            var go = new GameObject("KeyText", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(image.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = keyTextOffset;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = keyTextFont != null ? keyTextFont : DemoUI.TitleFont;
            text.fontSize = keyTextSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8f;
            text.fontSizeMax = keyTextSize;
            text.text = string.Empty;
            keyTexts[i] = text;
        }
    }

    private void SetKeyText(int slot, Keys? key)
    {
        if (keyTexts == null || slot < 0 || slot >= keyTexts.Length || keyTexts[slot] == null) return;

        TextMeshProUGUI text = keyTexts[slot];
        if (key == null)
        {
            text.text = string.Empty;
            return;
        }

        int index = (int)key.Value;
        KeyTextColor color = keyTextColors != null && index < keyTextColors.Length ? keyTextColors[index] : KeyTextColor.White;
        text.color = color == KeyTextColor.White ? Color.white : Color.black;
        text.text = KeyBindings.DisplayName(key.Value);
    }

    private void HandleTimeLineBeat(int bar, int beat, float tempo, int upper, int lower)
    {
        if (beat == 4)
        {
            ClearSlot();
        }
    }

    private void ClearSlot()
    {
        for (int i = 0; i < beatCircles.Length; i++)
        {
            BeatSlot curSlot = beatCircles[i];
            curSlot.circleRect.gameObject.SetActive(false);
        }

        curSlotIndex = 0;
    }
}
