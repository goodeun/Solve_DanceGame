using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MainFloorController : MonoBehaviour
{
    // 메인플로어 이동 및 비주얼 관리
    [SerializeField] private FloorSection[] floorSections;
    [Header("Beat Settings")]
    [SerializeField] private RhythmWheelUI rhythmWheel;
    [SerializeField] private ComboAndMagnification comboAndMagnification;
    [SerializeField] private StageController stageController;

    private bool isCombo = false;

    private void Awake()
    {
        if (rhythmWheel == null)
            rhythmWheel = FindFirstObjectByType<RhythmWheelUI>();

        if (comboAndMagnification == null)
            comboAndMagnification = FindFirstObjectByType<ComboAndMagnification>();

        if (stageController == null)
            stageController = GetComponent<StageController>();
    }

    private void OnEnable()
    {
        if (rhythmWheel != null)
        {
            rhythmWheel.OnJudged += HandleRhythmJudgement;
            rhythmWheel.OnBeatStarted += HandleBeatStarted;
            rhythmWheel.OnFourthPulsePeak += HandleFourthPulsePeak;
        }

        if (comboAndMagnification != null)
            comboAndMagnification.OnComboSaved += HandleComboSaved;
    }

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
        TempEventManager.OnBeat_Temp?.Invoke();
    }

    private void HandleBeatStarted(int beat)
    {
        TempEventManager.OnBeat_Temp?.Invoke();
    }

    private void HandleFourthPulsePeak()
    {
        TempEventManager.OnMeasureEnd_Temp?.Invoke(isCombo);
        isCombo = false;
    }

    private void OnDisable()
    {
        if (rhythmWheel != null)
        {
            rhythmWheel.OnJudged -= HandleRhythmJudgement;
            rhythmWheel.OnBeatStarted -= HandleBeatStarted;
            rhythmWheel.OnFourthPulsePeak -= HandleFourthPulsePeak;
        }

        if (comboAndMagnification != null)
            comboAndMagnification.OnComboSaved -= HandleComboSaved;

    }

    private void HandleComboSaved(Keys lastPressedKey)
    {
        isCombo = true;
        stageController?.ClearBubblesForComboKey(lastPressedKey);
    }

    private void HandleRhythmJudgement(Keys pressedKey, Judge judge, float offset)
    {
        if (judge == Judge.Miss)
            return;

        int floorIndex = pressedKey switch
        {
            Keys.Q => 0,
            Keys.E => 1,
            Keys.A => 2,
            Keys.D => 3,
            Keys.W => 4,
            Keys.S => 5,
            _ => -1
        };

        if (floorIndex >= 0)
        {
            OnNoteHit(floorIndex);
            stageController?.PlayKeyInputDrop(pressedKey);
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
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.green*3f);
                colorIndex_temp++;
                break;
            case 1:
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.aliceBlue * 3f);
                colorIndex_temp++;
                break;
            case 2:
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.deepPink * 3f);
                colorIndex_temp++;
                break;
            case 3:
                TempEventManager.OnThemaColorChange_Temp.Invoke(Color.yellow * 3f);
                colorIndex_temp = 0;
                break;
        }
    }


    #endregion
}
