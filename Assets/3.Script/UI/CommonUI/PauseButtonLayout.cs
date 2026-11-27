using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause 메뉴 버튼(ButtonGroup 아래)의 원래 배치를 기억한다.
/// 진입·서브 메뉴 전환 연출이 버튼을 움직여도 항상 같은 자리로 돌아오게 하는 기준.
/// </summary>
public static class PauseButtonLayout
{
    private static readonly Dictionary<RectTransform, Vector2> origins = new Dictionary<RectTransform, Vector2>();

    public static void Register(RectTransform button)
    {
        if (button != null && !origins.ContainsKey(button))
            origins[button] = button.anchoredPosition;
    }

    public static Vector2 Origin(RectTransform button)
    {
        Register(button);
        return origins[button];
    }

    /// <summary>ButtonGroup 아래 버튼을 위에서부터 순서대로 돌려준다.</summary>
    public static List<RectTransform> Collect(Transform root)
    {
        var list = new List<RectTransform>();
        if (root == null) return list;

        foreach (Button b in root.GetComponentsInChildren<Button>(true))
        {
            if (b.transform.parent == null || b.transform.parent.name != "ButtonGroup") continue;
            var rect = (RectTransform)b.transform;
            Register(rect);
            list.Add(rect);
        }

        list.Sort((a, b) => Origin(b).y.CompareTo(Origin(a).y));
        RemoveDestroyed();
        return list;
    }

    private static void RemoveDestroyed()
    {
        var dead = new List<RectTransform>();
        foreach (var key in origins.Keys) if (key == null) dead.Add(key);
        foreach (var key in dead) origins.Remove(key);
    }
}
