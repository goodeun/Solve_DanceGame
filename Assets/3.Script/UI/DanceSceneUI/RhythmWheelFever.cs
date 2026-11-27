using UnityEngine;

/// <summary>
/// 피버 게이지와 피버 타임 진행.
/// 마디 성공으로 충전하고, 가득 차면 다음 마디 첫 박부터 지정 마디 수만큼 피버가 진행된다.
/// 피버는 마지막 마디의 쉬는 박에서 끝나며, 그때 HUD·입력이 복구되어 다음 마디 첫 박부터 바로 플레이할 수 있다.
/// 피버 길이는 마디 단위로 Inspector에서 조절하며, 시작 시점에 확정되어 진행 중에는 바뀌지 않는다.
/// </summary>
public sealed class RhythmWheelFever : MonoBehaviour
{
    [Tooltip("마디 성공 시 게이지 상승량. 일반전(약 80마디) 기준 0.036이면 피버 1~2회")]
    [SerializeField, Range(.005f, 1f)] private float gain = .036f;
    [Tooltip("마디 실패 시 게이지 감소량")]
    [SerializeField, Range(0f, 1f)] private float loss = .02f;
    [Tooltip("한 판(씬)에서 피버가 발동할 수 있는 최대 횟수. 0이면 제한 없음")]
    [SerializeField, Min(0)] private int maxFeversPerGame = 2;

    [Tooltip("피버 타임 길이(마디). Play Mode 중에 바꾸면 다음 피버부터 반영된다.")]
    [SerializeField, Min(1)] private int durationBars = 3;

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
    private int feverCount;

    public bool IsActive => active;
    public bool IsQueued => queued;
    public int FeverCount => feverCount;
    /// <summary>false면 게이지가 차지 않고 피버도 시작하지 않는다(튜토리얼).</summary>
    public bool FeverEnabled { get; set; } = true;
    private bool ReachedLimit => maxFeversPerGame > 0 && feverCount >= maxFeversPerGame;
    public float Gauge => gauge;
    public int DurationBars { get => durationBars; set => durationBars = Mathf.Max(1, value); }
    /// <summary>현재(또는 마지막) 피버에서 확정된 길이(마디).</summary>
    public int CurrentFeverBars { get; private set; }
    public bool IsJudgementLocked => feverCamera != null && feverCamera.IsCameraSequenceActive;

    /// <summary>피버 시작(마디 첫 박).</summary>
    public event System.Action OnFeverStarted;
    /// <summary>피버 종료(마지막 마디의 쉬는 박). 피니시 연출 시작 시점.</summary>
    public event System.Action OnFeverEnded;
    /// <summary>피버 중 한 마디가 끝날 때마다(자동 득점용). 인자: 피버 내 마디 번호(1부터).</summary>
    public event System.Action<int> OnFeverBar;
    /// <summary>피버 완전 종료(게임플레이 복귀). OnFeverEnded 직후 호출된다.</summary>
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

    private void OnEnable() { if (combo != null) { combo.OnComboSaved += Save; combo.OnComboFailed += Fail; } timing.BeatStarted += HandleBeatStarted; }
    private void OnDisable() { if (combo != null) { combo.OnComboSaved -= Save; combo.OnComboFailed -= Fail; } timing.BeatStarted -= HandleBeatStarted; }

    private void Save(Keys _) { if (active || ReachedLimit || !FeverEnabled) return; gauge = Mathf.Clamp01(gauge + gain); queued |= gauge >= 1; hud?.AddFeverGage(gauge); }
    private void Fail() { if (active) return; gauge = Mathf.Clamp01(gauge - loss); hud?.AddFeverGage(gauge); }

    /// <summary>테스트·튜토리얼용: 게이지를 직접 설정한다.</summary>
    public void SetGauge(float value)
    {
        if (active) return;
        gauge = Mathf.Clamp01(value);
        queued = gauge >= 1f;
        hud?.AddFeverGage(gauge);
    }

    private void HandleBeatStarted(int beat)
    {
        if (active)
        {
            int elapsed = beat - feverStartBeat;
            if (elapsed > 0 && elapsed % 4 == 0)
                OnFeverBar?.Invoke(elapsed / 4);

            if (beat == feverEndBeat - 1)
                EndFever();
            return;
        }

        // Start on the next phrase's first beat after the gauge fills.
        if (!queued || beat % 4 != 0)
            return;

        if (ReachedLimit || !FeverEnabled)
        {
            queued = false;
            return;
        }

        // 피버 구간이 좌석 변경 구간·대사와 하나라도 겹치면 미루고, 겹치지 않는 첫 마디에서 시작한다.
        int bars = Mathf.Max(1, durationBars);
        if (judgement != null)
            for (int b = beat; b < beat + bars * 4; b++)
                if (judgement.IsBeatBlocked(b))
                    return;

        queued = false;
        active = true;
        feverCount++;
        CurrentFeverBars = bars;
        int beatCount = CurrentFeverBars * 4;
        feverStartBeat = beat;
        feverEndBeat = beat + beatCount;

        // 피버 구간은 판정에서 제외한다. 마지막 마디의 쉬는 박에서 피버가 끝나도
        // 그 마디가 '입력 없는 실패 마디'로 처리되지 않게 한다(HP·분위기·콤보 감소 방지).
        judgement?.BlockRange(feverStartBeat, feverEndBeat);

        // Finish on the rest beat immediately before the next phrase begins.
        float activeDuration = Mathf.Max(1, beatCount - 1) * timing.BeatDurationSeconds;
        hud?.RequestFeverTime(true, activeDuration, timing.BeatDurationSeconds);
        OnFeverStarted?.Invoke();
    }

    private void EndFever()
    {
        // 마지막 마디의 자동 득점은 쉬는 박(종료 시점)에 지급한다.
        OnFeverBar?.Invoke(CurrentFeverBars);

        feverEndBeat = -1;
        feverStartBeat = -1;
        gauge = 0f;
        active = false;
        feverCamera?.AllowGameplayInput();
        hud?.AddFeverGage(gauge);
        hud?.EndFeverTime();
        OnFeverEnded?.Invoke();
        OnFeverFinished?.Invoke();
    }
}
