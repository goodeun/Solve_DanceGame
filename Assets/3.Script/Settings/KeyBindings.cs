using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// 게임 입력 키 바인딩. 게임 로직은 위치(슬롯)를 나타내는 Keys(W·A·S·D·Q·E)를 그대로 쓰고,
/// 각 슬롯에 실제로 누를 키보드 키를 연결한다. PlayerPrefs에 저장된다.
/// HUD(리듬 휠 키, 리듬 UI)는 DisplayName으로 표시하고 Changed 이벤트로 갱신한다.
/// </summary>
public static class KeyBindings
{
    private const string PrefKey = "Solve.KeyBinding.";

    public static readonly Keys[] Slots = { Keys.Q, Keys.E, Keys.A, Keys.D, Keys.W, Keys.S };

    private static readonly Dictionary<Keys, Key> defaults = new Dictionary<Keys, Key>
    {
        [Keys.W] = Key.W,
        [Keys.A] = Key.A,
        [Keys.S] = Key.S,
        [Keys.D] = Key.D,
        [Keys.Q] = Key.Q,
        [Keys.E] = Key.E,
    };

    // 게임 키로 쓸 수 없는 키(시스템·메뉴용)
    private static readonly HashSet<Key> reserved = new HashSet<Key>
    {
        Key.Escape, Key.Enter, Key.NumpadEnter, Key.Tab, Key.LeftWindows, Key.RightWindows, Key.ContextMenu, Key.PrintScreen,
    };

    private static Dictionary<Keys, Key> current;

    /// <summary>바인딩이 바뀌었을 때.</summary>
    public static event Action Changed;

    /// <summary>옵션에서 새 키를 기다리는 중. 이 동안 ESC 등 다른 UI 입력을 막는다.</summary>
    public static bool IsRebinding { get; set; }

    private static Dictionary<Keys, Key> Current
    {
        get
        {
            if (current == null) Load();
            return current;
        }
    }

    public static Key Get(Keys slot) => Current.TryGetValue(slot, out Key key) ? key : defaults[slot];

    public static bool TryGetSlot(Key key, out Keys slot)
    {
        foreach (var pair in Current)
        {
            if (pair.Value == key)
            {
                slot = pair.Key;
                return true;
            }
        }
        slot = Keys.Q;
        return false;
    }

    public static bool IsReserved(Key key) => key == Key.None || reserved.Contains(key);

    /// <summary>키를 슬롯에 연결한다. 다른 슬롯이 이미 쓰는 키면 서로 맞바꾼다.</summary>
    public static void Set(Keys slot, Key key)
    {
        if (IsReserved(key)) return;

        Key previous = Get(slot);
        if (TryGetSlot(key, out Keys other) && other != slot)
            Current[other] = previous;

        Current[slot] = key;
        Save();
        Changed?.Invoke();
    }

    public static void ResetToDefaults()
    {
        current = new Dictionary<Keys, Key>(defaults);
        Save();
        Changed?.Invoke();
    }

    /// <summary>화면에 보일 키 이름(키보드 배열 반영).</summary>
    public static string DisplayName(Keys slot) => DisplayName(Get(slot));

    public static string DisplayName(Key key)
    {
        KeyControl control = Keyboard.current != null ? Keyboard.current[key] : null;
        string name = control != null && !string.IsNullOrEmpty(control.displayName) ? control.displayName : key.ToString();
        return name.Length == 1 ? name.ToUpperInvariant() : name;
    }

    /// <summary>Input System 바인딩 경로("&lt;Keyboard&gt;/q").</summary>
    public static string ControlPath(Keys slot)
    {
        Key key = Get(slot);
        KeyControl control = Keyboard.current != null ? Keyboard.current[key] : null;
        string name = control != null ? control.name : key.ToString().ToLowerInvariant();
        return "<Keyboard>/" + name;
    }

    public static bool WasPressedThisFrame(Keys slot)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[Get(slot)].wasPressedThisFrame;
    }

    private static void Load()
    {
        current = new Dictionary<Keys, Key>(defaults);
        foreach (Keys slot in Slots)
        {
            string saved = PlayerPrefs.GetString(PrefKey + slot, string.Empty);
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out Key key) && !IsReserved(key))
                current[slot] = key;
        }
    }

    private static void Save()
    {
        foreach (var pair in Current)
            PlayerPrefs.SetString(PrefKey + pair.Key, pair.Value.ToString());
        PlayerPrefs.Save();
    }
}
