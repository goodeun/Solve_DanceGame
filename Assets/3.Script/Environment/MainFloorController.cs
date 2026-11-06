using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MainFloorController : MonoBehaviour
{
    // 메인플로어 이동 및 비주얼 관리
    [SerializeField] private FloorSection[] floorSections;

    private bool isCombo = false;

    public void OnNoteHit(int floorIndex)
    {
        if (floorSections == null || floorIndex < 0 || floorIndex >= floorSections.Length)
        {
            return;
        }

        floorSections[floorIndex].ActiveLight();
    }

    #region 디버그

    int colorIndex_temp = 0;

    public void DEBUG_MusicOn()
    {
        StartCoroutine(DEBUG_Beat_co());
    }

    private IEnumerator DEBUG_Beat_co()
    {
        int turn=0;

        while (true)
        {
            TempEventManager.OnBeat_Temp.Invoke();
            if(turn!=0)
            {
                if (turn % 3 == 0)
                {
                    TempEventManager.OnMeasureEnd_Temp.Invoke(isCombo);
                    isCombo = false;
                }
            }
            turn++;
            yield return new WaitForSeconds(1f);
        }

    }

    public void DEBUG_Combo_test()
    {
        OnNoteHit(1);
        OnNoteHit(2);
        OnNoteHit(3);
        isCombo = true;
    }

    public void DEBUG_ThemaColorChange()
    {
        switch (colorIndex_temp)
        {
            case 0:
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.green);
                colorIndex_temp++;
                break;
            case 1:
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.aliceBlue);
                colorIndex_temp++;
                break;
            case 2:
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.deepPink);
                colorIndex_temp++;
                break;
            case 3:
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.yellow);
                colorIndex_temp = 0;
                break;
        }
    }


    #endregion
}
