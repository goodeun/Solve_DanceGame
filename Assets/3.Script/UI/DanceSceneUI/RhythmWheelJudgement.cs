using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RhythmWheelTiming))]
[DefaultExecutionOrder(-100)]
public sealed class RhythmWheelJudgement : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float justWindow = .1f;
    [SerializeField, Range(0f, 1f)] private float goodWindow = .3f;
    [SerializeField, Min(0f)] private float inputCallbackGraceSeconds = .05f;
    [SerializeField] private DanceSceneHUD danceHUD;

    private RhythmWheelTiming timing;
    private RhythmWheelFever fever;

    private readonly HashSet<int> resolvedBeats = new();
    [SerializeField] private ScoreManager scoreManager;

    // 판정 게이트: 대사·전환 구간에서 입력, 자동 Miss, 마디 실패를 함께 막는다.
    private bool suspended;
    private int blockedUntilBeat = int.MinValue;
    private int closedFromBeat = int.MaxValue;
    // [from, to) 박 구간. 전환을 한 마디 앞서 예약해 경계 박의 이른 입력도 막는다.
    private readonly System.Collections.Generic.List<Vector2Int> blockedRanges = new();

    public int Score => scoreManager != null ? scoreManager.TotalScore : 0;
    /// <summary>Just·Good 입력 연속 횟수. 결과용 마디 콤보는 ComboAndMagnification.BarCombo를 사용한다.</summary>
    public int Combo { get; private set; }
    public bool IsSuspended => suspended;
    public int BlockedUntilBeat => blockedUntilBeat;
    public float JustWindowSeconds => justWindow * timing.BeatDurationSeconds;
    public event Action<Keys, Judge, float> Judged;

    private void Awake()
    {
        timing = GetComponent<RhythmWheelTiming>();
        fever = GetComponent<RhythmWheelFever>();
        scoreManager ??= FindFirstObjectByType<ScoreManager>();
        danceHUD ??= FindFirstObjectByType<DanceSceneHUD>();
    }

    private void OnEnable() { Combo = 0; resolvedBeats.Clear(); }

    /// <summary>무기한 판정 중지(대사, 곡 시작 전, 곡 종료 후). false로 풀면 즉시 판정이 열린다.</summary>
    public void SetSuspended(bool value) => suspended = value;

    /// <summary>지정 박 이전의 모든 박을 판정에서 제외한다. 일반전 좌석 변경 구간에 사용.</summary>
    public void BlockUntilBeat(int beat) => blockedUntilBeat = Mathf.Max(blockedUntilBeat, beat);

    /// <summary>판정 중지를 풀되, 지정 박(보통 다음 마디 첫 박)부터 판정을 연다.</summary>
    public void ResumeAtBeat(int beat)
    {
        blockedUntilBeat = beat;
        closedFromBeat = int.MaxValue;
        suspended = false;
    }

    /// <summary>지정 박부터 판정을 닫는다(실습 종료 등). 마디 끝의 쉬는 박에서 다음 마디 첫 박을 미리 누른 입력도 막힌다.</summary>
    public void CloseFromBeat(int beat) => closedFromBeat = beat;

    /// <summary>[fromBeat, toBeat) 구간을 판정에서 제외한다.</summary>
    public void BlockRange(int fromBeat, int toBeat)
    {
        if (toBeat <= fromBeat) return;
        blockedRanges.Add(new Vector2Int(fromBeat, toBeat));
    }

    public bool IsBeatBlocked(int beat)
    {
        if (suspended || beat < blockedUntilBeat || beat >= closedFromBeat) return true;

        for (int i = blockedRanges.Count - 1; i >= 0; i--)
        {
            Vector2Int range = blockedRanges[i];
            // 지나간 구간 정리
            if (timing != null && timing.CurrentBeat >= range.y + 4) { blockedRanges.RemoveAt(i); continue; }
            if (beat >= range.x && beat < range.y) return true;
        }
        return false;
    }

    private void Update()
    {
        // Fever keeps normal key scoring, but missed beats must not break the
        // combo while its bonus window is active.
        if (fever != null && (fever.IsActive || fever.IsJudgementLocked))
            return;

        if (!timing.TryGetCurrentBeatTiming(timing.ElapsedSeconds, out int currentBeat, out float elapsedIntoBeat, out float beatDuration))
            return;
        if (currentBeat < 0) return;
        if (IsBeatBlocked(currentBeat)) return;
        // A press belongs to the nearest beat boundary.  Keep the preceding
        // beat open until the midpoint of this beat so its late Bad/Good side
        // can still be judged before resolving it as a Miss.
        if (elapsedIntoBeat <= beatDuration * .5f + inputCallbackGraceSeconds) return;
        //int previousBeat = currentBeat - 1;
        if (currentBeat % 4 != 3 && resolvedBeats.Add(currentBeat)) Apply(default, Judge.Miss, 0f);
    }

    public bool JudgeInput(Keys key, float elapsed)
    {
        if (!timing.TryGetCurrentBeatTiming(elapsed, out int beat, out float offset, out float beatDuration))
            return false;

        // Judge against the nearest beat boundary.  This produces a symmetric
        // Bad ??Good ??Just ??Good ??Bad timing band around every playable beat.
        if (offset > beatDuration * .5f)
        {
            beat++;
            offset -= beatDuration;
        }

        // The fourth beat in each phrase remains a rest.  The timestamp-based
        // input sampling in RhythmWheelTiming already handles callback delay.
        if (beat % 4 == 3)
            return false;

        if (IsBeatBlocked(beat))
            return false;

        if (!resolvedBeats.Add(beat)) 
            return false;

        float absolute = Mathf.Abs(offset);
        Judge judge = absolute <= justWindow * beatDuration ? Judge.Just : absolute <= goodWindow * beatDuration ? Judge.Good : Judge.Bad;

        Apply(key, judge, offset);

        return true;
    }

    private void Apply(Keys key, Judge judge, float offset)
    {
        if (judge == Judge.Just || judge == Judge.Good)
            Combo++;
        else
            Combo = 0;

        danceHUD?.JudgementEffect(judge);
        Judged?.Invoke(key, judge, offset);
    }
}
