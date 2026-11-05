using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider HPSlider;

    private float cashedHP;

    private void OnEnable()
    {
        //TODO. 임시 HP
        SetHealth((int)HPSlider.maxValue);
    }

    //TODO 생성될 때 맥스 HP값 계산해 연결
    public void SetHealth(int HP)
    {
        HPSlider.maxValue = HP;
        cashedHP = HP;
    }

    public void Damage(int damage)
    {
        cashedHP -= damage;
        HPSlider.DOValue(cashedHP, 0.3f);
    }

    public void Heal(int healValue)
    {
        cashedHP += healValue;
        HPSlider.DOValue(cashedHP, 0.3f);
    }


    #region 디버그

    public void DEBUG_OnClickDemage(int damage)
    {
        Damage(damage);
    }

    public void DEBUG_OnClickHealed(int heal)
    {
        Heal(heal);
    }

    #endregion
}
