using UnityEngine;

/// <summary>
/// 피버 게이지와 피버 타임 진행.
/// 마디 성공으로 충전하고, 가득 차면 다음 마디 첫 박부터 지정 마디 수만큼 피버가 진행된다.
/// 피버 길이는 마디 단위로 Inspector에서 조절하며, 시작 시점에 확정되어 진행 중에는 바뀌지 않는다.
/// </summary>
public sealed class RhythmWheelFever : MonoBehaviour
{
    [SerializeField, Range(.01f, 1f)] private float gain = .1f;
    [SerializeField, Range(.01f, 1f)] private float loss = .1f;

    [Tooltip("피버 타임 길이(마디). Play Mode 중에 바꾸면 다음 피버부터 반영된다.")]
    [SerializeField, Min(1)] private int durationBars = 3;

    [Tooltip("피니시 애니메이션(OnFeverEnded)을 피버 종료 몇 박 전에 시작할지.")]
    [SerializeField, Min(0)] private int finishLeadBeats = 2;

    [SerializeField] private FeverCamera feverCamera;
    [SerializeField] private ComboAndMagnification combo;
    [SerializeField] private DanceSceneHUD hud;
    private RhythmWheelTiming timing;
    private RhythmWheelJudgement judgement;
    private float gauge;
    private bool queued;
    private bool active;
    private int feverStartBeat = -1;
    private int feverEndBeat = -1;

    public bool IsActive => active;
    public bool IsQueued => queued;
    public float Gauge => gauge;
    public int DurationBars { get => durationBars; set => durationBars = Mathf.Max(1, value); }
    /// <summary>현재(또는 마지막) 피버에서 확정된 길이(마디).</summary>
    public int CurrentFeverBars { get; private set; }
    public bool IsJudgementLocked => feverCamera != null && feverCamera.IsCameraSequenceActive;

    /// <summary>피버 시작(다음 마디 첫 박).</summary>
    public event System.Action OnFeverStarted;
    /// <summary>피니시 연출 시작 시점(종료 finishLeadBeats 박 전).</summary>
    public event System.Action OnFeverEnded;
    /// <summary>피버 중 한 마디가 끝날 때마다(자동 득점용). 인자: 피버 내 마디 번호(1부터).</summary>
    public event System.Action<int> OnFeverBar;
    /// <summary>피버 완전 종료(게임플레이 복귀).</summary>
    public event System.Action OnFeverFinished;

    private void Awake()
    {
        timing = GetComponent<RhythmWheelTiming>();
        judgement = GetComponent<RhythmWheelJudgement>();
        if (combo == null) combo = GetComponent<ComboAndMagnification>();
        if (combo == null) combo = FindFirstObjectByType<ComboAndMagnification>();
        if (feverCamera == null) feverCamera = FindFirstObjectByType<FeverCamera>();
        if (hud == null) hud = FindFirstObjectByType<DanceSceneHUD>();
    }

    private void OnEnable() { if (combo != null) { combo.OnComboSaved += Save; combo.OnComboFailed += Fail; } timing.PhraseCompleted += StartFever; timing.BeatStarted += CheckFeverEnd; }
    private void OnDisable() { if (combo != null) { combo.OnComboSaved -= Save; combo.OnComboFailed -= Fail; } timing.PhraseCompleted -= StartFever; timing.BeatStarted -= CheckFeverEnd; }

    private void Save(Keys _) { if (active) return; gauge = Mathf.Clamp01(gauge + gain); queued |= gauge >= 1; hud?.AddFeverGage(gauge); }
    private void Fail() { if (active) return; gauge = Mathf.Clamp01(gauge - loss); hud?.AddFeverGage(gauge); }

    /// <summary>테스트·튜토리얼용: 게이지를 직접 설정한다.</summary>
    public void SetGauge(float value)
    {
        if (active) return;
        gauge = Mathf.Clamp01(value);
        queued = gauge >= 1f;
        hud?.AddFeverGage(gauge);
    }

    private void StartFever()
    {
        if (!queued || active) return;

        // 좌석 변경 구간·대사 중에는 피버를 미루고, 판정이 열린 첫 마디에서 시작한다.
        if (judgement != null && judgement.IsBeatBlocked(timing.CurrentBeat)) return;

        queued = false;
        active = true;
        CurrentFeverBars = Mathf.Max(1, durationBars);
        int beatCount = CurrentFeverBars * 4;
        feverStartBeat = timing.CurrentBeat;
        feverEndBeat = feverStartBeat + beatCount;

        // 카메라 컷은 종료 박에 맞춰 복귀 블렌드가 끝나도록 블렌드 시간만큼 앞당긴다.
        float blend = timing.BeatDurationSeconds;
        float total = beatCount * timing.BeatDurationSeconds;
        hud?.RequestFeverTime(true, Mathf.Max(blend, total - blend), blend);
        OnFeverStarted?.Invoke();
    }

    private void CheckFeverEnd(int beat)
    {
        if (!active) return;

        if (beat == feverEndBeat - finishLeadBeats) OnFeverEnded?.Invoke();

        int elapsed = beat - feverStartBeat;
        if (elapsed > 0 && elapsed % 4 == 0)
            OnFeverBar?.Invoke(elapsed / 4);

        if (beat < feverEndBeat) return;
        feverEndBeat = -1;
        feverStartBeat = -1;
        gauge = 0f;
        active = false;
        hud?.AddFeverGage(gauge);
        hud?.EndFeverTime();
        OnFeverFinished?.Invoke();
    }
}
