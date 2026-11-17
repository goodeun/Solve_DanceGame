using UnityEngine;
using DG.Tweening;

public class FloorSection : MonoBehaviour
{
    [Header("Tween Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float burstDuration = 0.5f;
    [SerializeField] public float intensity = 3.0f;


    private bool isActivated = false;

    private MeshRenderer floorMesh;
    private Material floorMat;

    private readonly int brightID = Shader.PropertyToID("_Brightness");
    private readonly int scaleID = Shader.PropertyToID("_Scale");

    private Sequence seq;

    private void Awake()
    {
        if (floorMesh == null)
        {
            floorMesh = GetComponent<MeshRenderer>();
        }

        floorMat = floorMesh.material;

    }
    private void OnEnable()
    {
        TempEventManager.OnMeasureEnd_Temp += HandleMeasureEnd;
        TempEventManager.OnBeat_Temp += ChangeScale;
    }
    private void OnDisable()
    {
        TempEventManager.OnMeasureEnd_Temp -= HandleMeasureEnd;
        TempEventManager.OnBeat_Temp -= ChangeScale;
        floorMat?.SetFloat(brightID, 0f);
    }

    public void ActiveLight()
    {
        floorMat.DOKill();
        seq?.Kill();

        // A key press can interrupt the combo-success tween while _Brightness
        // is elevated. Reset it before playing the normal key-light response.
        floorMat.SetFloat(brightID, 0f);

        if (!isActivated)
        {
            isActivated = true;
            floorMat.DOFloat(intensity, brightID, 0.1f);
        }
        else
        {
            seq = DOTween.Sequence();
            seq.SetDelay(0.03f)
                .Append(floorMat.DOFloat(intensity, brightID, 0.07f));
        }
    }

    private void HandleMeasureEnd(bool isComboSuccess)
    {
        isActivated = false;
        floorMat.DOKill();
        floorMat.SetFloat(brightID, 0f);

        if (isComboSuccess)
        {
            seq?.Kill();
            seq = DOTween.Sequence();

            seq.Append(floorMat.DOFloat(intensity, brightID, 0.01f))
                .Append(floorMat.DOFloat(0f, brightID, 0.15f));
        }
        else
        {
            floorMat.DOFloat(0f, brightID, 0.2f);
        }
    }

    private void ChangeScale()
    {
        if (isActivated) return;

        floorMat.SetFloat(scaleID, Random.Range(100f, 1000f));
    }
}
