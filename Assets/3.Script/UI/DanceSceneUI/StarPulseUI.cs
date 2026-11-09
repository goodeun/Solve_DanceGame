using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class StarPulseUI : MonoBehaviour
{
    private static readonly int RadiusProperty = Shader.PropertyToID("_Radius");
    private static readonly int MainColorProperty = Shader.PropertyToID("_Main_Color");
    private static readonly int[] PaletteProperties =
    {
        Shader.PropertyToID("_Color_1"),
        Shader.PropertyToID("_Color_2"),
        Shader.PropertyToID("_Color_3")
    };

    [Tooltip("별이 2에서 0까지 줄어드는 데 걸리는 박 수입니다.")]
    [SerializeField, Min(0.01f)] private float durationBeats = 1f;
    [SerializeField] private float startRadius = 2f;
    [SerializeField] private float endRadius = 0f;

    private Image starImage;
    private Material runtimeMaterial;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        starImage = GetComponent<Image>();
        runtimeMaterial = new Material(starImage.material);
        starImage.material = runtimeMaterial;
        runtimeMaterial.SetFloat(RadiusProperty, endRadius);
    }

    public void Play(int bpm)
    {
        if (runtimeMaterial == null || bpm <= 0)
            return;

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        Color selectedColor = runtimeMaterial.GetColor(PaletteProperties[Random.Range(0, PaletteProperties.Length)]);
        runtimeMaterial.SetColor(MainColorProperty, selectedColor);
        runtimeMaterial.SetFloat(RadiusProperty, startRadius);

        float durationSeconds = durationBeats * 60f / bpm;
        pulseCoroutine = StartCoroutine(ShrinkRoutine(durationSeconds));
    }

    private IEnumerator ShrinkRoutine(float durationSeconds)
    {
        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / durationSeconds);
            runtimeMaterial.SetFloat(RadiusProperty, Mathf.Lerp(startRadius, endRadius, progress));
            yield return null;
        }

        runtimeMaterial.SetFloat(RadiusProperty, endRadius);
        pulseCoroutine = null;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
