using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class RhythmWheelUI : MonoBehaviour
{
    [SerializeField] private int BPM = 120;

    [Header("Pulse Color Settings")]
    [SerializeField] private Color normalPulseColor = Color.cyan;
    [SerializeField] private Color fourthBeatPulseColor = Color.red;

    [SerializeField] private GameObject[] Key_Images = new GameObject[6];

    [SerializeField] private Image circlePulse;

    [Header("Key Settings")]
    [SerializeField] private bool isSquare = true;
    [SerializeField] private bool isRhombus;
    [SerializeField] private bool isSixKeys;

    [Header("Judgement Settings")]
    [SerializeField] private DanceSceneHUD danceHUD;
    [Tooltip("판정 창 수치의 기준 BPM입니다. 실제 판정 창은 현재 BPM에 맞춰 비례 조정됩니다.")]
    [SerializeField, Min(1)] private int judgementReferenceBPM = 120;
    [SerializeField, Min(0f)] private float justWindow = 0.05f;
    [SerializeField, Min(0f)] private float goodWindow = 0.10f;
    [SerializeField, Min(0f)] private float badWindow = 0.18f;

    [Header("Fever Settings")]
    [Tooltip("Just 판정 1회로 채울 피버 게이지 비율입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float feverGainPerJust = 0.1f;
    [SerializeField, Min(0.1f)] private float feverDuration = 10f;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public event System.Action<Keys, Judge, float> OnJudged;
    public event System.Action OnFourthBeatStarted;

    private PlayerInput playerInput;
    private readonly Dictionary<Keys, KeyImage> keyImagesByKey = new();

    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
    private Material circlePulseMaterial;
    private float pulseStartTime;
    private bool isFourthBeat;
    private int lastJudgedBeat = int.MinValue;
    private int lastAutoMissedBeat = -1;
    private int lastObservedBeat = -1;
    private float feverGauge;
    private bool isFeverActive;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        if (danceHUD == null)
            danceHUD = FindFirstObjectByType<DanceSceneHUD>();

        if (circlePulse != null && circlePulse.material != null)
        {
            // Image마다 전용 Material 인스턴스를 만들어 다른 UI와 값을 공유하지 않는다.
            circlePulseMaterial = new Material(circlePulse.material);
            circlePulse.material = circlePulseMaterial;
        }

        foreach (GameObject keyImageObject in Key_Images)
        {
            if (keyImageObject == null)
                continue;

            KeyImage keyImage = keyImageObject.GetComponent<KeyImage>();
            if (keyImage == null)
            {
                Debug.LogWarning($"{keyImageObject.name}에 KeyImage 컴포넌트가 없습니다.", keyImageObject);
                continue;
            }

            keyImagesByKey[keyImage.key] = keyImage;
        }
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.onActionTriggered += OnActionTriggered;

        OnJudged += HandleFeverJudgement;

        pulseStartTime = Time.unscaledTime;
        isFourthBeat = false;
        lastJudgedBeat = int.MinValue;
        lastAutoMissedBeat = -1;
        lastObservedBeat = -1;
        Score = 0;
        Combo = 0;
        feverGauge = 0f;
        isFeverActive = false;
        UpdateFeverGaugeUI();
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.onActionTriggered -= OnActionTriggered;

        OnJudged -= HandleFeverJudgement;
    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!System.Enum.TryParse(context.control.name, true, out Keys pressedKey))
            return;

        if (IsFourthBeat())
            return;

        if (!IsKeyInputEnabled(pressedKey))
            return;

        KeyImage keyImage = keyImagesByKey[pressedKey];
        keyImage.PlayPressAnimation();

        JudgeInput(pressedKey);
    }

    /// <summary>
    /// Evaluates one valid dance-key press against the closest BPM beat.
    /// A chart/note generator can also call this method when it has selected a note.
    /// </summary>
    public void JudgeInput(Keys pressedKey)
    {
        if (BPM <= 0 || IsFourthBeat() || !IsKeyInputEnabled(pressedKey))
            return;

        float beatDuration = 60f / BPM;
        float elapsed = Time.unscaledTime - pulseStartTime;
        int closestBeat = Mathf.RoundToInt(elapsed / beatDuration);
        float offset = elapsed - (closestBeat * beatDuration);

        // Only one input can consume a beat. This prevents key mashing from scoring.
        if (closestBeat == lastJudgedBeat)
            return;

        Judge judge = GetJudge(Mathf.Abs(offset));
        lastJudgedBeat = closestBeat;
        ApplyJudgement(pressedKey, judge, offset);
    }

    private bool IsKeyInputEnabled(Keys pressedKey)
    {
        // A disabled key button must not be judged or score.
        return keyImagesByKey.TryGetValue(pressedKey, out KeyImage keyImage)
            && keyImage.gameObject.activeInHierarchy
            && keyImage.enabled;
    }

    private Judge GetJudge(float absoluteOffset)
    {
        // Window values are authored at the reference BPM so the judgement difficulty
        // remains the same relative to each beat when BPM changes.
        float bpmScale = (float)judgementReferenceBPM / BPM;
        float scaledJustWindow = justWindow * bpmScale;
        float scaledGoodWindow = goodWindow * bpmScale;
        float scaledBadWindow = badWindow * bpmScale;

        if (absoluteOffset <= scaledJustWindow)
            return Judge.Just;
        if (absoluteOffset <= scaledGoodWindow)
            return Judge.Good;
        if (absoluteOffset <= scaledBadWindow)
            return Judge.Bad;
        return Judge.Miss;
    }

    private float GetScaledBadWindow()
    {
        return badWindow * ((float)judgementReferenceBPM / BPM);
    }

    private void ApplyJudgement(Keys pressedKey, Judge judge, float offset)
    {
        switch (judge)
        {
            case Judge.Just:
                Score += 1000;
                Combo++;
                break;
            case Judge.Good:
                Score += 700;
                Combo++;
                break;
            case Judge.Bad:
                Score += 300;
                Combo = 0;
                break;
            case Judge.Miss:
                Combo = 0;
                break;
        }

        if (danceHUD == null)
            danceHUD = FindFirstObjectByType<DanceSceneHUD>();

        if (danceHUD != null)
        {
            danceHUD.JudgementEffect(judge);
            danceHUD.AddScore(Score);
        }

        OnJudged?.Invoke(pressedKey, judge, offset);
    }

    /// <summary>
    /// 판정 이벤트에서 피버를 처리한다. Just 이외의 판정은 게이지에 영향을 주지 않는다.
    /// </summary>
    private void HandleFeverJudgement(Keys pressedKey, Judge judge, float offset)
    {
        if (judge != Judge.Just || isFeverActive)
            return;

        feverGauge = Mathf.Clamp01(feverGauge + feverGainPerJust);
        UpdateFeverGaugeUI();

        if (feverGauge < 1f)
            return;

        isFeverActive = true;
        danceHUD?.RequestFeverTime(true, feverDuration);
        StartCoroutine(ResetFeverAfterDuration());
    }

    private IEnumerator ResetFeverAfterDuration()
    {
        yield return new WaitForSeconds(feverDuration);

        feverGauge = 0f;
        isFeverActive = false;
        UpdateFeverGaugeUI();
    }

    private void UpdateFeverGaugeUI()
    {
        if (danceHUD == null)
            danceHUD = FindFirstObjectByType<DanceSceneHUD>();

        danceHUD?.AddFeverGage(feverGauge);
    }

    private void Update()
    {
        if (BPM <= 0)
            return;

        float beatDuration = 60f / BPM;
        float elapsed = Time.unscaledTime - pulseStartTime;
        int currentBeat = Mathf.FloorToInt(elapsed / beatDuration);

        NotifyFourthBeatStart(currentBeat);
        TryApplyMissedBeats(elapsed, beatDuration);

        if (circlePulseMaterial == null)
            return;

        float beatProgress = Mathf.Repeat(elapsed / beatDuration, 1f);

        isFourthBeat = IsFourthBeat();

        circlePulseMaterial.SetColor(
            "_Ring_Color",
            isFourthBeat ? fourthBeatPulseColor : normalPulseColor);

        circlePulseMaterial.SetFloat(ProgressProperty, beatProgress * 0.5f);
    }

    private void TryApplyMissedBeats(float elapsed, float beatDuration)
    {
        // Wait until the Bad window has passed before resolving a beat with no input.
        int latestExpiredBeat = Mathf.FloorToInt((elapsed - GetScaledBadWindow()) / beatDuration);

        for (int beat = lastAutoMissedBeat + 1; beat <= latestExpiredBeat; beat++)
        {
            lastAutoMissedBeat = beat;

            // The fourth beat is a rest, so it never produces a Miss.
            if (beat < 0 || beat % 4 == 3 || beat == lastJudgedBeat)
                continue;

            ApplyJudgement(default, Judge.Miss, elapsed - (beat * beatDuration));
        }
    }

    private void NotifyFourthBeatStart(int currentBeat)
    {
        for (int beat = lastObservedBeat + 1; beat <= currentBeat; beat++)
        {
            if (beat >= 0 && beat % 4 == 3)
                OnFourthBeatStarted?.Invoke();
        }

        lastObservedBeat = currentBeat;
    }

    private bool IsFourthBeat()
    {
        if (BPM <= 0)
            return false;

        float beatDuration = 60f / BPM;
        int currentBeat = Mathf.FloorToInt((Time.unscaledTime - pulseStartTime) / beatDuration);
        return currentBeat % 4 == 3;
    }

    private void OnDestroy()
    {
        if (circlePulseMaterial != null)
            Destroy(circlePulseMaterial);

    }

}
