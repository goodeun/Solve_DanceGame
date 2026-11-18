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

    private readonly int brightID = Shader.PropertyToID("_Brightness");
    private readonly int scaleID = Shader.PropertyToID("_Scale");

    private Sequence seq;

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

    }
    private void OnEnable()
    {
        floorMat?.SetFloat(brightID, normalIntensity);
        TempEventManager.OnMeasureEnd_Temp += HandleMeasureEnd;
        TempEventManager.OnBeat_Temp += ChangeScale;
    }
    private void OnDisable()
    {
        TempEventManager.OnMeasureEnd_Temp -= HandleMeasureEnd;
        TempEventManager.OnBeat_Temp -= ChangeScale;
        floorMat?.SetFloat(brightID, normalIntensity);
    }

    public void ActiveLight()
    {
        floorMat.DOKill();
        seq?.Kill();

        // Start the key-light response from the floor's normal brightness.
        floorMat.SetFloat(brightID, normalIntensity);

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
}
