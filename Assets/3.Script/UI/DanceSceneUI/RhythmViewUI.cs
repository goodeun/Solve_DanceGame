using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

            curCircles.judgementImage.sprite = judgeSprites[(int)judge];
            curCircles.circleRect.gameObject.SetActive(true);
        }

        curSlotIndex++;
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
