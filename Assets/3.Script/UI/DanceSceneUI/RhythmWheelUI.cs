using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class RhythmWheelUI : MonoBehaviour
{
    [SerializeField] private int BPM = 120;

    [Header("Pulse Color Settings")]
    [SerializeField] private Color normalPulseColor = Color.cyan;
    [SerializeField] private Color fourthBeatPulseColor = Color.red;

    [SerializeField] private GameObject[] Key_Images = new GameObject[6];

    [SerializeField] private Image circlePulse;

    [Header("Wheel Input Motion")]
    [Tooltip("키 입력 시 Rhythm Wheel이 키 방향으로 이동할 거리입니다.")]
    [SerializeField, Min(0f)] private float inputMoveDistance = 18f;
    [SerializeField, Min(0.005f)] private float inputMoveOutDuration = 0.015f;
    [SerializeField, Min(0.01f)] private float inputMoveReturnDuration = 0.16f;
    [Tooltip("키 입력 시 카메라가 화면 방향으로 이동할 월드 거리입니다.")]
    [SerializeField, Min(0f)] private float cameraInputMoveDistance = 0.25f;

    [Header("Key Settings")]
    [SerializeField] private bool isSquare = true;
    [SerializeField] private bool isRhombus;
    [SerializeField] private bool isSixKeys;

    [Header("Judgement Settings")]
    [SerializeField] private DanceSceneHUD danceHUD;
    [Tooltip("한 박 길이 대비 Just 판정 창의 비율입니다. 0.1은 한 박의 10%입니다.")]
    [SerializeField, Range(0f, 1f)] private float justWindow = 0.1f;
    [Tooltip("한 박 길이 대비 Good 판정 창의 비율입니다. 0.3은 한 박의 30%입니다.")]
    [SerializeField, Range(0f, 1f)] private float goodWindow = 0.3f;

    [Header("Fever Settings")]
    [Tooltip("3입력 콤보 1회 완성으로 채울 피버 게이지 비율입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float feverGainPerCombo = 0.1f;
    [Tooltip("3입력 콤보를 완성하지 못했을 때 줄어들 피버 게이지 비율입니다.")]
    [SerializeField, Range(0.01f, 1f)] private float feverLossPerFailedCombo = 0.1f;
    [Tooltip("피버 지속 시간입니다. 초가 아니라 박(beat) 수로 지정하며, 항상 4박 배수로 적용됩니다.")]
    [SerializeField, Min(4f)] private float feverDuration = 12f;

    [Header("Music Sync")]
    [SerializeField] private FmodMusicClock musicClock;
    [SerializeField] private StarPulseUI starPulse;
    [SerializeField] private FeverCamera feverCamera;
    [SerializeField] private ComboAndMagnification comboAndMagnification;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public float BeatDurationSeconds => BPM > 0 ? 60f / BPM : 0f;
    public event System.Action<Keys, Judge, float> OnJudged;
    public event System.Action<int> OnBeatStarted;
    public event System.Action OnFourthBeatStarted;
    public event System.Action OnFourthPulsePeak;
    public event System.Action OnPhraseCompleted;

    private PlayerInput playerInput;
    private RectTransform wheelRectTransform;
    private Vector2 defaultWheelAnchoredPosition;
    private Coroutine inputMoveCoroutine;
    private Camera inputCamera;
    private Coroutine cameraInputMoveCoroutine;
    private Vector2 cameraInputDirection;
    private float cameraInputAmount;
    private readonly Dictionary<Keys, KeyImage> keyImagesByKey = new();

    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
    private Material circlePulseMaterial;
    private float pulseStartTime;
    private float lastMusicElapsed;
    private double lastMusicSampleTime;
    private bool hasMusicTimeSample;
    private bool isFourthBeat;
    private readonly HashSet<int> resolvedBeats = new();
    private int lastObservedBeat = -1;
    private int lastObservedPulseCycle = int.MinValue;
    private float feverGauge;
    private bool isFeverActive;
    private bool isFeverQueued;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        wheelRectTransform = transform as RectTransform;
        if (wheelRectTransform != null)
            defaultWheelAnchoredPosition = wheelRectTransform.anchoredPosition;

        inputCamera = Camera.main;

        if (musicClock == null)
            musicClock = FindFirstObjectByType<FmodMusicClock>();

        if (starPulse == null)
            starPulse = GetComponentInChildren<StarPulseUI>(true);

        if (feverCamera == null)
            feverCamera = FindFirstObjectByType<FeverCamera>();

        if (comboAndMagnification == null)
            comboAndMagnification = FindFirstObjectByType<ComboAndMagnification>();

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

        if (comboAndMagnification != null)
        {
            comboAndMagnification.OnComboSaved += HandleComboSaved;
            comboAndMagnification.OnComboFailed += HandleComboFailed;
        }
        OnPhraseCompleted += StartQueuedFever;
        CinemachineCore.CameraUpdatedEvent.AddListener(ApplyCameraInputOffset);

        pulseStartTime = Time.unscaledTime;
        isFourthBeat = false;
        resolvedBeats.Clear();
        lastObservedBeat = -1;
        lastObservedPulseCycle = int.MinValue;
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

        if (inputMoveCoroutine != null)
        {
            StopCoroutine(inputMoveCoroutine);
            inputMoveCoroutine = null;
        }

        if (wheelRectTransform != null)
            wheelRectTransform.anchoredPosition = defaultWheelAnchoredPosition;

        if (cameraInputMoveCoroutine != null)
        {
            StopCoroutine(cameraInputMoveCoroutine);
            cameraInputMoveCoroutine = null;
        }

        cameraInputDirection = Vector2.zero;
        cameraInputAmount = 0f;

        if (comboAndMagnification != null)
        {
            comboAndMagnification.OnComboSaved -= HandleComboSaved;
            comboAndMagnification.OnComboFailed -= HandleComboFailed;
        }
        OnPhraseCompleted -= StartQueuedFever;
        CinemachineCore.CameraUpdatedEvent.RemoveListener(ApplyCameraInputOffset);

    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!System.Enum.TryParse(context.control.name, true, out Keys pressedKey))
            return;

        float inputElapsed = GetInputElapsedTime(context.time);

        // The fourth beat is a rest. Ignore it before any wheel or key visual
        // feedback is played, while leaving the judgement logic unchanged.
        if (IsRestBeat(inputElapsed))
            return;

        PlayWheelInputMotion(pressedKey);

        if (IsJudgementLocked)
            return;

        PlayCameraInputMotion(pressedKey);

        if (!IsKeyInputEnabled(pressedKey))
            return;

        KeyImage keyImage = keyImagesByKey[pressedKey];
        keyImage.PlayPressAnimation();

        // Capture the music time once.  In particular, do not reject a press just
        // because FMOD advanced into the rest beat between the input update and
        // this callback.
        if (JudgeInput(pressedKey, inputElapsed))
            starPulse?.Play(BPM);
    }

    private void PlayWheelInputMotion(Keys pressedKey)
    {
        if (wheelRectTransform == null || !Application.isPlaying)
            return;

        if (inputMoveCoroutine != null)
            StopCoroutine(inputMoveCoroutine);

        wheelRectTransform.anchoredPosition = defaultWheelAnchoredPosition;
        inputMoveCoroutine = StartCoroutine(PlayWheelInputMotionCoroutine(GetKeyDirection(pressedKey)));
    }

    private IEnumerator PlayWheelInputMotionCoroutine(Vector2 direction)
    {
        Vector2 targetPosition = defaultWheelAnchoredPosition + (direction.normalized * inputMoveDistance);
        yield return MoveWheelAnchoredPosition(defaultWheelAnchoredPosition, targetPosition, inputMoveOutDuration);
        yield return MoveWheelAnchoredPosition(targetPosition, defaultWheelAnchoredPosition, inputMoveReturnDuration);
        inputMoveCoroutine = null;
    }

    private IEnumerator MoveWheelAnchoredPosition(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            wheelRectTransform.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        wheelRectTransform.anchoredPosition = to;
    }

    private static Vector2 GetKeyDirection(Keys key)
    {
        return key switch
        {
            Keys.W => Vector2.up,
            Keys.A => Vector2.left,
            Keys.S => Vector2.down,
            Keys.D => Vector2.right,
            Keys.Q => new Vector2(-1f, 1f),
            Keys.E => new Vector2(1f, 1f),
            _ => Vector2.zero
        };
    }

    private void PlayCameraInputMotion(Keys pressedKey)
    {
        if (inputCamera == null)
            inputCamera = Camera.main;

        if (inputCamera == null || !Application.isPlaying)
            return;

        if (cameraInputMoveCoroutine != null)
            StopCoroutine(cameraInputMoveCoroutine);

        cameraInputDirection = GetKeyDirection(pressedKey).normalized;
        cameraInputMoveCoroutine = StartCoroutine(PlayCameraInputMotionCoroutine());
    }

    private IEnumerator PlayCameraInputMotionCoroutine()
    {
        yield return AnimateCameraInputAmount(0f, 1f, inputMoveOutDuration);
        yield return AnimateCameraInputAmount(1f, 0f, inputMoveReturnDuration);
        cameraInputAmount = 0f;
        cameraInputDirection = Vector2.zero;
        cameraInputMoveCoroutine = null;
    }

    private IEnumerator AnimateCameraInputAmount(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            cameraInputAmount = Mathf.SmoothStep(from, to, progress);
            yield return null;
        }
    }

    private void ApplyCameraInputOffset(CinemachineBrain brain)
    {
        if (cameraInputAmount <= 0f || inputCamera == null || brain == null || brain.OutputCamera != inputCamera)
            return;

        Transform cameraTransform = inputCamera.transform;
        Vector3 worldDirection = (cameraTransform.right * cameraInputDirection.x)
            + (cameraTransform.up * cameraInputDirection.y);
        cameraTransform.position += worldDirection * (cameraInputMoveDistance * cameraInputAmount);
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

        // The beat marker is the instant a new beat begins.  A key press at
        // that downbeat is Just; presses later in the beat are increasingly late.
        float offset = pulseOffset;

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

    private bool IsRestBeat(float elapsed)
    {
        if (BPM <= 0)
            return false;

        int currentBeat = Mathf.FloorToInt(elapsed / (60f / BPM));
        return currentBeat % 4 == 3;
    }

    private Judge GetJudge(float absoluteOffset)
    {
        // Judgement windows are stored as a fraction of one beat, so their
        // relative size remains identical at every BPM.
        float beatDuration = 60f / BPM;
        float scaledJustWindow = justWindow * beatDuration;
        float scaledGoodWindow = goodWindow * beatDuration;

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
        return BPM > 0 ? justWindow * (60f / BPM) : 0f;
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

    private void HandleComboSaved(Keys lastPressedKey)
    {
        if (isFeverActive)
            return;

        feverGauge = Mathf.Clamp01(feverGauge + feverGainPerCombo);
        UpdateFeverGaugeUI();

        if (feverGauge < 1f)
            return;

        isFeverQueued = true;
    }

    private void HandleComboFailed()
    {
        if (isFeverActive)
            return;

        feverGauge = Mathf.Clamp01(feverGauge - feverLossPerFailedCombo);
        UpdateFeverGaugeUI();
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
        lastMusicElapsed = elapsed;
        lastMusicSampleTime = Time.realtimeSinceStartupAsDouble;
        hasMusicTimeSample = true;
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

        // Keep the pulse's 0 -> 0.5 motion intact, but delay only its visual
        // timeline by the Just window. This lets the preceding pulse reach 0.5
        // during the current beat's Just window without changing judgement time.
        float pulseElapsed = elapsed - GetScaledJustWindow();
        float beatProgress = Mathf.Repeat(pulseElapsed / beatDuration, 1f);
        NotifyPulsePeaks(Mathf.FloorToInt(pulseElapsed / beatDuration));
        // This pulse completes on the following beat, so use that destination
        // beat for its color. In particular, the first beat remains normal
        // rather than inheriting the previous phrase's fourth-beat red.
        int pulseTargetBeat = Mathf.FloorToInt(pulseElapsed / beatDuration) + 1;
        bool isPulseFourthBeat = ((pulseTargetBeat % 4) + 4) % 4 == 3;

        circlePulseMaterial.SetColor(
            "_Ring_Color",
            isPulseFourthBeat ? fourthBeatPulseColor : normalPulseColor);

        circlePulseMaterial.SetFloat(
            ProgressProperty,
            beatProgress * 0.5f);
    }

    private void NotifyBeatTransitions(int currentBeat)
    {
        for (int beat = lastObservedBeat + 1; beat <= currentBeat; beat++)
        {
            if (beat >= 0)
                OnBeatStarted?.Invoke(beat);

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

    private void NotifyPulsePeaks(int currentPulseCycle)
    {
        if (lastObservedPulseCycle == int.MinValue)
        {
            lastObservedPulseCycle = currentPulseCycle;
            return;
        }

        for (int cycle = lastObservedPulseCycle + 1; cycle <= currentPulseCycle; cycle++)
        {
            // A cycle boundary is the moment its pulse reaches 0.5. The fourth
            // target beat uses the red pulse and is the combo confirmation cue.
            if (cycle >= 0 && cycle % 4 == 3)
                OnFourthPulsePeak?.Invoke();
        }

        lastObservedPulseCycle = currentPulseCycle;
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

    private float GetInputElapsedTime(double inputTime)
    {
        if (!hasMusicTimeSample)
            return GetElapsedTime();

        // Input System timestamps the physical key event. Estimate the FMOD
        // timeline at that instant instead of using the later callback time.
        double secondsSinceLastMusicSample = inputTime - lastMusicSampleTime;
        return Mathf.Max(0f, lastMusicElapsed + (float)secondsSinceLastMusicSample);
    }

    private void OnDestroy()
    {
        if (circlePulseMaterial != null)
            Destroy(circlePulseMaterial);

    }

}
