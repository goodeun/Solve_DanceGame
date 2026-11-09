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
        CinemachineCore.CameraUpdatedEvent.AddListener(ApplyCameraInputOffset);

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

        OnJudged -= HandleFeverJudgement;
        OnPhraseCompleted -= StartQueuedFever;
        CinemachineCore.CameraUpdatedEvent.RemoveListener(ApplyCameraInputOffset);

    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!System.Enum.TryParse(context.control.name, true, out Keys pressedKey))
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
        if (JudgeInput(pressedKey, GetInputElapsedTime(context.time)))
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

        // The visible pulse moves from 0 to 0.5 during one beat.  Its 0.5
        // endpoint is the beat marker and therefore the Just point.
        float offset = beatDuration - pulseOffset;

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

        // The visual pulse always travels smoothly from 0 to 0.5.
        float beatProgress = Mathf.Repeat(elapsed / beatDuration, 1f);

        circlePulseMaterial.SetColor(
            "_Ring_Color",
            isFourthBeat ? fourthBeatPulseColor : normalPulseColor);

        // Just is evaluated when this value reaches 0.5, immediately before
        // the following pulse starts at zero.
        circlePulseMaterial.SetFloat(
            ProgressProperty,
            beatProgress * 0.5f);
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
