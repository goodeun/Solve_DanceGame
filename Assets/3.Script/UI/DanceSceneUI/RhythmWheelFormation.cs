using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RhythmWheelFormation : MonoBehaviour
{
    [SerializeField] private RhythmWheelInput rhythmwheel;
    [SerializeField] private ComboAndMagnification comboAndMagnification;
    [SerializeField] private StageController stageController;
    [SerializeField] private NGBubbleManager ngBubbleManager;
    [SerializeField] private ScoreManager scoreManager;

    [SerializeField] private Stage currentStage;

    [SerializeField] private float[] squareRotations;
    [SerializeField] private float[] rhombusRotations;

    void Awake()
    {
        if (rhythmwheel == null)
            rhythmwheel = FindFirstObjectByType<RhythmWheelInput>();

        if (comboAndMagnification == null) comboAndMagnification = FindFirstObjectByType<ComboAndMagnification>();
        if (stageController == null) stageController = FindFirstObjectByType<StageController>();
        if (ngBubbleManager == null) ngBubbleManager = FindFirstObjectByType<NGBubbleManager>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();

        ApplyStage(Stage.Square, squareRotations);
    }

    public void SetSquare()
    {
        if (stageController != null)
        {
            stageController.ChangePhase(Stage.Square);
            return;
        }

        ApplyStage(Stage.Square, squareRotations);
    }

    public void SetRhombus()
    {
        if (stageController != null)
        {
            stageController.ChangePhase(Stage.Rhombus);
            return;
        }

        ApplyStage(Stage.Rhombus, rhombusRotations);
    }

    public void ApplyStage(Stage stage)
    {
        ApplyStage(stage, stage == Stage.Square ? squareRotations : rhombusRotations);
    }

    private void ApplyStage(Stage stage, float[] rotations)
    {
        if (rhythmwheel == null)
        {
            Debug.LogError("RhythmWheelFormation: RhythmWheelInput was not found.", this);
            return;
        }

        rhythmwheel.SetKeyStage(stage);
        if (rotations != null && rhythmwheel.Keyimages != null && rhythmwheel.Keyimages.Length == rotations.Length)
        {
            for (int i = 0; i < rhythmwheel.Keyimages.Length; i++)
                rhythmwheel.RotateKeyimage(rhythmwheel.Keyimages[i], rotations[i]);
        }

        comboAndMagnification?.SetStage(stage);
        scoreManager?.SetStageMode(stage);
        ngBubbleManager?.MoveStage(stage);
        currentStage = stage;
    }

    public Stage CurrentStage => currentStage;
}
