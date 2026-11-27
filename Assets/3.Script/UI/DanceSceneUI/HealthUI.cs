using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider HPSlider;
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int damage = 30;
    [SerializeField] private int heal = 5;
    [SerializeField] private ComboAndMagnification comboAndMagnification;

    private float cashedHP;

    private void OnEnable()
    {
        comboAndMagnification ??= FindFirstObjectByType<ComboAndMagnification>();
        if (comboAndMagnification != null)
        {
            comboAndMagnification.OnComboFailed += HandleComboFailed;
            comboAndMagnification.OnComboSaved += HandleComboSaved;
        }

        SetHealth(maxHP);
    }

    private void OnDisable()
    {
        if (comboAndMagnification == null)
            return;

        comboAndMagnification.OnComboFailed -= HandleComboFailed;
        comboAndMagnification.OnComboSaved -= HandleComboSaved;
    }

    private void HandleComboFailed() => Damage(damage);
    private void HandleComboSaved(Keys _) => Heal(heal);

    public void SetHealth(int HP)
    {
        maxHP = Mathf.Max(0, HP);
        HPSlider.maxValue = maxHP;
        cashedHP = maxHP;
        HPSlider.value = cashedHP;
    }

    public void Damage(int damage)
    {
        cashedHP = Mathf.Clamp(cashedHP - damage, 0, maxHP);
        HPSlider.DOValue(cashedHP, 0.3f);
    }

    public void Heal(int healValue)
    {
        cashedHP = Mathf.Clamp(cashedHP + healValue, 0, maxHP);
        HPSlider.DOValue(cashedHP, 0.3f);
    }

    public void DEBUG_OnClickDemage(int damage) => Damage(damage);
    public void DEBUG_OnClickHealed(int heal) => Heal(heal);
}