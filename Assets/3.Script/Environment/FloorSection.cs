using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

public class FloorSection : MonoBehaviour
{
    [Header("Tween Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float burstDuration = 0.5f;
    [Header("Brightness")]
    [SerializeField, Min(0f)] private float normalIntensity = 3f;
    [SerializeField, Min(0f)] private float inputIntensity = 5f;

    [FormerlySerializedAs("intensity")]
    [SerializeField, HideInInspector] private float legacyIntensity;


    private bool isActivated = false;

    private MeshRenderer floorMesh;
    private Material floorMat;
    private PhaseController phaseController;
    private Color baseEmissionColor;
    private Color baseFloorColor;
    private Color currentPhaseEmissionColor;
    private bool hasSeatEmissionColor;

    private readonly int brightID = Shader.PropertyToID("_Brightness");
    private readonly int scaleID = Shader.PropertyToID("_Scale");

    private Sequence seq;
    private FloorLightPillar pillar;

    private void Awake()
    {
        // Preserve existing prefab input brightness when migrating the old field.
        if (legacyIntensity > 0f)
            inputIntensity = legacyIntensity;

        if (floorMesh == null)
        {
            floorMesh = GetComponent<MeshRenderer>();
        }

        floorMat = floorMesh.material;

        // 켜질 때 구역 모양 그대로 위로 솟구치는 빛기둥
        pillar = GetComponent<FloorLightPillar>();
        if (pillar == null) pillar = gameObject.AddComponent<FloorLightPillar>();
        phaseController = FindFirstObjectByType<PhaseController>();
        baseEmissionColor = floorMat.HasProperty(Shader.PropertyToID("_EmissionColor"))
            ? floorMat.GetColor(Shader.PropertyToID("_EmissionColor"))
            : Color.white;
        baseFloorColor = floorMat.HasProperty(Shader.PropertyToID("_Color"))
            ? floorMat.GetColor(Shader.PropertyToID("_Color"))
            : Color.white;
        UpdatePhaseColor();

    }
    private void OnEnable()
    {
        floorMat?.SetFloat(brightID, normalIntensity);
        UpdatePhaseColor();
        TempEventManager.OnMeasureEnd_Temp += HandleMeasureEnd;
        TempEventManager.OnBeat_Temp += ChangeScale;
    }
    private void OnDisable()
    {
        TempEventManager.OnMeasureEnd_Temp -= HandleMeasureEnd;
        TempEventManager.OnBeat_Temp -= ChangeScale;
        floorMat?.SetFloat(brightID, normalIntensity);
        pillar?.Hide();
    }

    public void ActiveLight()
    {
        UpdatePhaseColor();
        floorMat.DOKill();
        seq?.Kill();

        // Start the key-light response from the floor's normal brightness.
        floorMat.SetFloat(brightID, normalIntensity);

        pillar?.Rise(currentPhaseEmissionColor);

        if (!isActivated)
        {
            isActivated = true;
            floorMat.DOFloat(inputIntensity, brightID, 0.1f);
        }
        else
        {
            seq = DOTween.Sequence();
            seq.SetDelay(0.03f)
                .Append(floorMat.DOFloat(inputIntensity, brightID, 0.07f));
        }
    }

    private void HandleMeasureEnd(bool isComboSuccess)
    {
        UpdatePhaseColor();
        pillar?.Fall(isComboSuccess);
        isActivated = false;
        floorMat.DOKill();
        floorMat.SetFloat(brightID, normalIntensity);

        if (isComboSuccess)
        {
            seq?.Kill();
            seq = DOTween.Sequence();

            seq.Append(floorMat.DOFloat(inputIntensity, brightID, 0.01f))
                .Append(floorMat.DOFloat(normalIntensity, brightID, 0.15f));
        }
        else
        {
            floorMat.DOFloat(normalIntensity, brightID, 0.2f);
        }
    }

    private void ChangeScale()
    {
        if (isActivated) return;

        floorMat.SetFloat(scaleID, Random.Range(100f, 1000f));
    }

    private void UpdatePhaseColor()
    {
        if (phaseController == null)
            phaseController = FindFirstObjectByType<PhaseController>();

        if (phaseController == null || floorMat == null)
            return;

        if (!hasSeatEmissionColor)
            currentPhaseEmissionColor = phaseController.CurrentEmissionColor;
        int emissionColorId = Shader.PropertyToID("_EmissionColor");
        if (floorMat.HasProperty(emissionColorId))
            floorMat.SetColor(emissionColorId, currentPhaseEmissionColor);
        int floorColorId = Shader.PropertyToID("_Color");
        if (floorMat.HasProperty(floorColorId))
            floorMat.SetColor(floorColorId, currentPhaseEmissionColor);
    }

    public void SetSeatEmissionColor(Color color)
    {
        currentPhaseEmissionColor = color;
        hasSeatEmissionColor = true;
        int emissionColorId = Shader.PropertyToID("_EmissionColor");
        if (floorMat != null && floorMat.HasProperty(emissionColorId))
            floorMat.SetColor(emissionColorId, currentPhaseEmissionColor);
        int floorColorId = Shader.PropertyToID("_Color");
        if (floorMat != null && floorMat.HasProperty(floorColorId))
            floorMat.SetColor(floorColorId, currentPhaseEmissionColor);
    }

    private void OnDestroy()
    {
        if (floorMat == null)
            return;

        int emissionColorId = Shader.PropertyToID("_EmissionColor");
        if (floorMat.HasProperty(emissionColorId))
            floorMat.SetColor(emissionColorId, baseEmissionColor);
        int floorColorId = Shader.PropertyToID("_Color");
        if (floorMat.HasProperty(floorColorId))
            floorMat.SetColor(floorColorId, baseFloorColor);
    }
}
