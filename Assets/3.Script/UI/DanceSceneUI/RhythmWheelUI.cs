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

    [Header("Fever Settings")]
    [Tooltip("Just 판정 1회로 채울 피버 게이지 비율입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float feverGainPerJust = 0.1f;
    [Tooltip("피버 지속 시간입니다. 초가 아니라 박(beat) 수로 지정하며, 항상 4박 배수로 적용됩니다.")]
    [SerializeField, Min(4f)] private float feverDuration = 12f;

    [Header("Music Sync")]
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private StarPulseUI starPulse;
    [SerializeField] private FeverCamera feverCamera;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public event System.Action<Keys, Judge, float> OnJudged;
    public event System.Action OnFourthBeatStarted;
    public event System.Action OnPhraseCompleted;

    private PlayerInput playerInput;
    private readonly Dictionary<Keys, KeyImage> keyImagesByKey = new();

    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
    private Material circlePulseMaterial;
    private float pulseStartTime;
    private bool isFourthBeat;
    private readonly HashSet<int> resolvedBeats = new();
    private int lastObservedBeat = -1;
    private float feverGauge;
    private bool isFeverActive;
    private bool isFeverQueued;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        if (musicClock == null)
            musicClock = FindFirstObjectByType<FmodMusicClock>();

        if (starPulse == null)
            starPulse = GetComponentInChildren<StarPulseUI>(true);

        if (feverCamera == null)
            feverCamera = FindFirstObjectByType<FeverCamera>();

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
        OnPhraseCompleted += StartQueuedFever;

        pulseStartTime = Time.unscaledTime;
        isFourthBeat = false;
        resolvedBeats.Clear();
        lastObservedBeat = -1;
        Score = 0;
        Combo = 0;
        feverGauge = 0f;
        isFeverActive = false;
        isFeverQueued = false;
        UpdateFeverGaugeUI();
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.onActionTriggered -= OnActionTriggered;

        OnJudged -= HandleFeverJudgement;
        OnPhraseCompleted -= StartQueuedFever;

    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!System.Enum.TryParse(context.control.name, true, out Keys pressedKey))
            return;

        if (IsJudgementLocked)
            return;

        if (!IsKeyInputEnabled(pressedKey))
            return;

        KeyImage keyImage = keyImagesByKey[pressedKey];
        keyImage.PlayPressAnimation();

        // Capture the music time once.  In particular, do not reject a press just
        // because FMOD advanced into the rest beat between the input update and
        // this callback.
        if (JudgeInput(pressedKey, GetElapsedTime()))
            starPulse?.Play(BPM);
    }

    /// <summary>
    /// Evaluates one valid dance-key press against the closest BPM beat.
    /// A chart/note generator can also call this method when it has selected a note.
    /// </summary>
    public void JudgeInput(Keys pressedKey)
    {
        JudgeInput(pressedKey, GetElapsedTime());
    }

    private bool JudgeInput(Keys pressedKey, float elapsed)
    {
        if (BPM <= 0 || !IsKeyInputEnabled(pressedKey))
            return false;

        float beatDuration = 60f / BPM;
        int currentBeat = Mathf.FloorToInt(elapsed / beatDuration);
        float pulseOffset = elapsed - (currentBeat * beatDuration);

        // The fourth beat is always a rest. Do not turn a rest-beat key press
        // into a late third-beat input; it must not receive a judgement or play
        // the associated star effect.
        if (currentBeat % 4 == 3)
            return false;

        // During the Just grace at the start of a new beat, finish the previous
        // unresolved input beat first. This keeps a key press at a beat boundary
        // from being recorded as the new beat while the prior beat becomes Miss.
        int previousBeat = currentBeat - 1;
        bool canResolvePreviousBeat = previousBeat >= 0
            && previousBeat % 4 != 3
            && !resolvedBeats.Contains(previousBeat);

        if (pulseOffset <= GetScaledJustWindow() && canResolvePreviousBeat)
        {
            currentBeat = previousBeat;
        }
        float offset = pulseOffset <= beatDuration * 0.5f
            ? pulseOffset
            : pulseOffset - beatDuration;

        // A beat can only be resolved once.
        if (resolvedBeats.Contains(currentBeat))
            return false;

        Judge judge = GetJudge(Mathf.Abs(offset));
        resolvedBeats.Add(currentBeat);
        ApplyJudgement(pressedKey, judge, offset);
        return true;
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

        if (absoluteOffset <= scaledJustWindow)
            return Judge.Just;
        if (absoluteOffset <= scaledGoodWindow)
            return Judge.Good;

        // A pressed key is always a Bad at worst. Miss is reserved for the
        // beat-resolution path where the player supplied no input at all.
        return Judge.Bad;
    }

    private float GetScaledJustWindow()
    {
        return justWindow * ((float)judgementReferenceBPM / BPM);
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

        isFeverQueued = true;
    }

    private void StartQueuedFever()
    {
        if (!isFeverQueued || isFeverActive || BPM <= 0)
            return;

        isFeverQueued = false;
        isFeverActive = true;
        float beatDuration = 60f / BPM;
        float feverDurationSeconds = GetFeverDurationBeats() * beatDuration;
        danceHUD?.RequestFeverTime(true, feverDurationSeconds, beatDuration);
        StartCoroutine(ResetFeverAfterDuration(feverDurationSeconds));
    }

    private IEnumerator ResetFeverAfterDuration(float feverDurationSeconds)
    {
        yield return new WaitForSeconds(feverDurationSeconds);

        feverGauge = 0f;
        isFeverActive = false;
        UpdateFeverGaugeUI();
    }

    private float GetFeverDurationBeats()
    {
        // A fever always finishes on a full four-beat phrase. Round upward so a
        // designer-entered value never makes the effect shorter than requested.
        return Mathf.Max(4f, Mathf.Ceil(feverDuration / 4f) * 4f);
    }

    private void OnValidate()
    {
        feverDuration = GetFeverDurationBeats();
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
        float elapsed = GetElapsedTime();
        int currentBeat = Mathf.FloorToInt(elapsed / beatDuration);
        isFourthBeat = currentBeat % 4 == 3;

        NotifyBeatTransitions(currentBeat);
        if (IsJudgementLocked)
        {
            MarkBeatAsExcluded(currentBeat);
            MarkBeatAsExcluded(currentBeat - 1);
        }
        else
        {
            ResolvePreviousBeatAfterGrace(currentBeat, elapsed, beatDuration);
        }

        if (circlePulseMaterial == null)
            return;

        float beatProgress = Mathf.Repeat(elapsed / beatDuration, 1f);

        circlePulseMaterial.SetColor(
            "_Ring_Color",
            isFourthBeat ? fourthBeatPulseColor : normalPulseColor);

        circlePulseMaterial.SetFloat(ProgressProperty, beatProgress * 0.5f);
    }

    private void NotifyBeatTransitions(int currentBeat)
    {
        for (int beat = lastObservedBeat + 1; beat <= currentBeat; beat++)
        {
            // A phrase finishes only after its fourth (rest) beat has elapsed.
            // This is the first beat of the next four-beat loop.
            if (beat > 0 && beat % 4 == 0)
                OnPhraseCompleted?.Invoke();

            if (beat >= 0 && beat % 4 == 3)
            {
                OnFourthBeatStarted?.Invoke();
            }
        }

        lastObservedBeat = currentBeat;
    }

    private void ResolvePreviousBeatAfterGrace(int currentBeat, float elapsed, float beatDuration)
    {
        if (currentBeat <= 0)
            return;

        float currentBeatStart = currentBeat * beatDuration;
        if (elapsed - currentBeatStart <= GetScaledJustWindow())
            return;

        int previousBeat = currentBeat - 1;
        // The fourth beat is a rest and does not have an input to resolve.
        if (previousBeat % 4 == 3)
            return;

        if (resolvedBeats.Add(previousBeat))
            ApplyJudgement(default, Judge.Miss, 0f);
    }

    public bool IsJudgementLocked
    {
        get { return feverCamera != null && feverCamera.IsCameraSequenceActive; }
    }

    private void MarkBeatAsExcluded(int beat)
    {
        if (beat >= 0 && beat % 4 != 3)
            resolvedBeats.Add(beat);
    }

    private float GetElapsedTime()
    {
        if (musicClock != null && musicClock.TryGetElapsedSeconds(out float elapsed))
            return elapsed;

        return Time.unscaledTime - pulseStartTime;
    }

    private void OnDestroy()
    {
        if (circlePulseMaterial != null)
            Destroy(circlePulseMaterial);

    }

}
