using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class MultiplierUI : MonoBehaviour
{
    [SerializeField] private List<KeyImage> uIConfigs;

    private Dictionary<Keys, TextMeshProUGUI> uiDict = new Dictionary<Keys, TextMeshProUGUI>();

    private void Awake()
    {
        foreach(var config in uIConfigs)
        {
            if (config.MagText != null)
            {
                uiDict[config.key] = config.MagText;
            }
        }
    }

    public void UpdateUI(Dictionary<Keys, float> curMultipliers, bool isFever)
    {
        foreach (var kvp in curMultipliers)
        {
            if (uiDict.TryGetValue(kvp.Key, out TextMeshProUGUI textUI))
            {
                if (!textUI.gameObject.activeInHierarchy) continue;

                textUI.text = $"{Mathf.RoundToInt(kvp.Value * 100f)}%";
            }
        }
    }
}
