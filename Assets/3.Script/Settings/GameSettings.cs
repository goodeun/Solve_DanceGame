using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 옵션 값(사운드·그래픽·게임) 저장과 적용. PlayerPrefs에 저장되고 게임 시작 시 자동 적용된다.
///
/// 사운드(FMOD)
///  - 마스터: bus:/ 볼륨
///  - 음악: bus:/Music이 있으면 그 버스, 없으면 FmodMusicClock 곡 인스턴스 볼륨
///  - 효과음: bus:/SFX가 있으면 그 버스, 없으면 코드에서 재생하는 효과음(대사·메트로놈)에만 적용
/// FMOD Studio에서 Music·SFX 버스를 만들고 이벤트를 연결하면 버스 쪽으로 자동 적용된다.
/// </summary>
public static class GameSettings
{
    private const string Prefix = "Solve.Settings.";

    public static event Action Changed;

    // ---- Sound ----
    public static float MasterVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 0.8f;
    public static float SfxVolume { get; private set; } = 0.8f;

    // ---- Graphics ----
    public static int ScreenModeIndex { get; private set; }      // 0 전체 화면(창), 1 창 모드, 2 전용 전체 화면
    public static int ResolutionIndex { get; private set; } = -1; // -1 = 현재 해상도
    public static bool VSync { get; private set; } = true;
    public static int QualityIndex { get; private set; } = -1;    // -1 = 프로젝트 기본
    public static int FrameRateIndex { get; private set; } = 1;   // FrameRateOptions

    // ---- Game ----
    /// <summary>입력 판정 보정(ms). +면 입력을 그만큼 이르게 본다(늦게 누르는 경향 보정).</summary>
    public static int InputOffsetMs { get; private set; }

    public static readonly string[] ScreenModeNames = { "전체 화면(창)", "창 모드", "전체 화면" };
    public static readonly int[] FrameRateOptions = { 30, 60, 120, 144, 240, -1 };

    public static float InputOffsetSeconds => InputOffsetMs / 1000f;
    public static float EffectiveMusicVolume => MusicVolume;
    public static float EffectiveSfxVolume => SfxVolume;

    private static bool loaded;
    private static List<Vector2Int> resolutions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Load();
        ApplyGraphics();
        ApplyAudio();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => ApplyAudio();
    }

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        MasterVolume = PlayerPrefs.GetFloat(Prefix + "Master", MasterVolume);
        MusicVolume = PlayerPrefs.GetFloat(Prefix + "Music", MusicVolume);
        SfxVolume = PlayerPrefs.GetFloat(Prefix + "Sfx", SfxVolume);
        ScreenModeIndex = PlayerPrefs.GetInt(Prefix + "ScreenMode", ScreenModeIndex);
        ResolutionIndex = PlayerPrefs.GetInt(Prefix + "Resolution", ResolutionIndex);
        VSync = PlayerPrefs.GetInt(Prefix + "VSync", VSync ? 1 : 0) == 1;
        QualityIndex = PlayerPrefs.GetInt(Prefix + "Quality", QualityIndex);
        FrameRateIndex = PlayerPrefs.GetInt(Prefix + "FrameRate", FrameRateIndex);
        InputOffsetMs = PlayerPrefs.GetInt(Prefix + "InputOffset", InputOffsetMs);
    }

    // ---------------- Sound ----------------

    public static void SetMasterVolume(float value) { MasterVolume = Mathf.Clamp01(value); SaveFloat("Master", MasterVolume); ApplyAudio(); }
    public static void SetMusicVolume(float value) { MusicVolume = Mathf.Clamp01(value); SaveFloat("Music", MusicVolume); ApplyAudio(); }
    public static void SetSfxVolume(float value) { SfxVolume = Mathf.Clamp01(value); SaveFloat("Sfx", SfxVolume); ApplyAudio(); }

    public static bool HasMusicBus { get; private set; }
    public static bool HasSfxBus { get; private set; }

    public static void ApplyAudio()
    {
        SetBus("bus:/", MasterVolume, out _);
        SetBus("bus:/Music", MusicVolume, out bool music);
        SetBus("bus:/SFX", SfxVolume, out bool sfx);
        HasMusicBus = music;
        HasSfxBus = sfx;

        // 버스가 없을 때의 대체 적용(곡 인스턴스·Unity 오디오)
        AudioListener.volume = MasterVolume;
        Changed?.Invoke();
    }

    private static void SetBus(string path, float volume, out bool found)
    {
        found = false;
        try
        {
            FMOD.Studio.Bus bus = FMODUnity.RuntimeManager.GetBus(path);
            if (bus.isValid())
            {
                bus.setVolume(volume);
                found = true;
            }
        }
        catch (Exception)
        {
            // 버스가 없거나 FMOD가 아직 준비되지 않음
        }
    }

    /// <summary>코드에서 재생하는 FMOD 효과음. 효과음 볼륨을 적용한다.</summary>
    public static void PlaySfx(string eventPath)
    {
        if (string.IsNullOrEmpty(eventPath)) return;

        try
        {
            FMOD.Studio.EventInstance instance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
            if (!HasSfxBus) instance.setVolume(SfxVolume);
            instance.start();
            instance.release();
        }
        catch (FMODUnity.EventNotFoundException)
        {
            Debug.LogWarning($"SFX not found: {eventPath}");
        }
    }

    // ---------------- Graphics ----------------

    public static IReadOnlyList<Vector2Int> Resolutions
    {
        get
        {
            if (resolutions == null)
            {
                resolutions = new List<Vector2Int>();
                foreach (Resolution r in Screen.resolutions)
                {
                    var size = new Vector2Int(r.width, r.height);
                    if (!resolutions.Contains(size)) resolutions.Add(size);
                }
                if (resolutions.Count == 0) resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            }
            return resolutions;
        }
    }

    public static int CurrentResolutionIndex
    {
        get
        {
            if (ResolutionIndex >= 0 && ResolutionIndex < Resolutions.Count) return ResolutionIndex;
            var current = new Vector2Int(Screen.width, Screen.height);
            int index = 0;
            for (int i = 0; i < Resolutions.Count; i++)
                if (Resolutions[i] == current) index = i;
            return index;
        }
    }

    public static void SetScreenMode(int index) { ScreenModeIndex = Mathf.Clamp(index, 0, ScreenModeNames.Length - 1); SaveInt("ScreenMode", ScreenModeIndex); ApplyGraphics(); }
    public static void SetResolution(int index) { ResolutionIndex = Mathf.Clamp(index, 0, Resolutions.Count - 1); SaveInt("Resolution", ResolutionIndex); ApplyGraphics(); }
    public static void SetVSync(bool value) { VSync = value; SaveInt("VSync", value ? 1 : 0); ApplyGraphics(); }
    public static void SetQuality(int index) { QualityIndex = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1); SaveInt("Quality", QualityIndex); ApplyGraphics(); }
    public static void SetFrameRate(int index) { FrameRateIndex = Mathf.Clamp(index, 0, FrameRateOptions.Length - 1); SaveInt("FrameRate", FrameRateIndex); ApplyGraphics(); }

    public static void ApplyGraphics()
    {
        if (QualityIndex >= 0 && QualityIndex < QualitySettings.names.Length)
            QualitySettings.SetQualityLevel(QualityIndex, true);

        QualitySettings.vSyncCount = VSync ? 1 : 0;
        int fps = FrameRateOptions[Mathf.Clamp(FrameRateIndex, 0, FrameRateOptions.Length - 1)];
        Application.targetFrameRate = VSync ? -1 : fps;

#if !UNITY_EDITOR
        FullScreenMode mode = ScreenModeIndex switch
        {
            1 => FullScreenMode.Windowed,
            2 => FullScreenMode.ExclusiveFullScreen,
            _ => FullScreenMode.FullScreenWindow,
        };
        Vector2Int size = Resolutions[CurrentResolutionIndex];
        Screen.SetResolution(size.x, size.y, mode);
#endif
        Changed?.Invoke();
    }

    // ---------------- Game ----------------

    public static void SetInputOffsetMs(int value) { InputOffsetMs = Mathf.Clamp(value, -200, 200); SaveInt("InputOffset", InputOffsetMs); Changed?.Invoke(); }

    // ---------------- Reset ----------------

    public static void ResetSound() { SetMasterVolume(1f); SetMusicVolume(0.8f); SetSfxVolume(0.8f); }

    public static void ResetGraphics()
    {
        ScreenModeIndex = 0; ResolutionIndex = -1; VSync = true; QualityIndex = -1; FrameRateIndex = 1;
        SaveInt("ScreenMode", 0); SaveInt("Resolution", -1); SaveInt("VSync", 1); SaveInt("Quality", -1); SaveInt("FrameRate", 1);
        ApplyGraphics();
    }

    public static void ResetGame() => SetInputOffsetMs(0);

    private static void SaveFloat(string key, float value) { PlayerPrefs.SetFloat(Prefix + key, value); PlayerPrefs.Save(); }
    private static void SaveInt(string key, int value) { PlayerPrefs.SetInt(Prefix + key, value); PlayerPrefs.Save(); }
}
