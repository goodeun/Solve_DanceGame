using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class MultiplierUI : MonoBehaviour
{
    [SerializeField] private List<KeyImage> uIConfigs;

    // 키 이미지의 키는 무대 배치(사각형/마름모)에 따라 바뀌므로 매번 현재 키로 찾는다.
    public void UpdateUI(Dictionary<Keys, float> curMultipliers, bool isFever)
    {
        if (uIConfigs == null || curMultipliers == null) return;

        foreach (var config in uIConfigs)
        {
            if (config == null || config.MagText == null) continue;
            if (!config.MagText.gameObject.activeInHierarchy) continue;
            if (!curMultipliers.TryGetValue(config.key, out float value)) continue;

            config.MagText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
