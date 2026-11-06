using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class RhythmWheelUI : MonoBehaviour
{
    [SerializeField] private int BPM = 120;

    [SerializeField] private GameObject[] Key_Images = new GameObject[6];

    [SerializeField] private Image circlePulse;

    [Header("Key Settings")]
    [SerializeField] private bool isSquare = true;
    [SerializeField] private bool isRhombus;
    [SerializeField] private bool isSixKeys;

    [Header("Judgement Settings")]
    [SerializeField] private DanceSceneHUD danceHUD;
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

    private PlayerInput playerInput;
    private readonly Dictionary<Keys, KeyImage> keyImagesByKey = new();

    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
    private Material circlePulseMaterial;
    private float pulseStartTime;
    private int lastJudgedBeat = int.MinValue;
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
        lastJudgedBeat = int.MinValue;
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

        if (keyImagesByKey.TryGetValue(pressedKey, out KeyImage keyImage))
            keyImage.PlayPressAnimation();

        JudgeInput(pressedKey);
    }

    /// <summary>
    /// Evaluates one valid dance-key press against the closest BPM beat.
    /// A chart/note generator can also call this method when it has selected a note.
    /// </summary>
    public void JudgeInput(Keys pressedKey)
    {
        if (BPM <= 0)
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

    private Judge GetJudge(float absoluteOffset)
    {
        if (absoluteOffset <= justWindow)
            return Judge.Just;
        if (absoluteOffset <= goodWindow)
            return Judge.Good;
        if (absoluteOffset <= badWindow)
            return Judge.Bad;
        return Judge.Miss;
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
        if (circlePulseMaterial == null || BPM <= 0)
            return;

        float beatDuration = 60f / BPM;
        float beatProgress = Mathf.Repeat((Time.unscaledTime - pulseStartTime) / beatDuration, 1f);
        circlePulseMaterial.SetFloat(ProgressProperty, beatProgress * 0.5f);
    }

    private void OnDestroy()
    {
        if (circlePulseMaterial != null)
            Destroy(circlePulseMaterial);

    }

}
