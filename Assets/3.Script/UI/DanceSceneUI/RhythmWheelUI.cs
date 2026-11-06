using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class RhythmWheelUI : MonoBehaviour
{
    [SerializeField] private int BPM = 120;

    [SerializeField] private GameObject[] Key_Images = new GameObject[6];

    [SerializeField] private Image circlePulse;

    [Header("Key Settings")]
    [SerializeField] private bool isSquare = true;
    [SerializeField] private bool isRhombus;
    [SerializeField] private bool isSixKeys;

    [Header("Combined Outline")]
    [Tooltip("Rhythm Wheel의 자식인 Combined Outline Image를 비워 두면 이름으로 자동 검색합니다.")]
    [SerializeField] private bool enableCombinedOutline;
    [SerializeField] private Image combinedOutline;
    [SerializeField, Min(64)] private int outlineMaskResolution = 512;
    [SerializeField, Min(1f)] private float centerMaskDiameter = 60f;

    [Header("Judgement Settings")]
    [SerializeField] private DanceSceneHUD danceHUD;
    [SerializeField, Min(0f)] private float justWindow = 0.05f;
    [SerializeField, Min(0f)] private float goodWindow = 0.10f;
    [SerializeField, Min(0f)] private float badWindow = 0.18f;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public event System.Action<Keys, Judge, float> OnJudged;

    private PlayerInput playerInput;
    private readonly Dictionary<Keys, KeyImage> keyImagesByKey = new();

    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
    private Material circlePulseMaterial;
    private float pulseStartTime;
    private int lastJudgedBeat = int.MinValue;

    private readonly List<RectTransform> outlineMaskKeys = new();
    private readonly List<Material> outlineMaskMaterials = new();
    private RenderTexture outlineMaskTexture;
    private GameObject outlineMaskCanvasObject;
    private Camera outlineMaskCamera;
    private GameObject outlineMaskCameraObject;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (danceHUD == null)
            danceHUD = FindFirstObjectByType<DanceSceneHUD>();

        if (enableCombinedOutline)
            SetupCombinedOutline();

        if (circlePulse != null && circlePulse.material != null)
        {
            // Image마다 전용 Material 인스턴스를 만들어 다른 UI와 값을 공유하지 않는다.
            circlePulseMaterial = new Material(circlePulse.material);
            circlePulse.material = circlePulseMaterial;
        }

        foreach (GameObject keyImageObject in Key_Images)
        {
            if (keyImageObject == null)
                continue;

            KeyImage keyImage = keyImageObject.GetComponent<KeyImage>();
            if (keyImage == null)
            {
                Debug.LogWarning($"{keyImageObject.name}에 KeyImage 컴포넌트가 없습니다.", keyImageObject);
                continue;
            }

            keyImagesByKey[keyImage.key] = keyImage;
        }
    }

    private void OnEnable()
    {
        if (playerInput != null)
            playerInput.onActionTriggered += OnActionTriggered;

        pulseStartTime = Time.unscaledTime;
        lastJudgedBeat = int.MinValue;
        Score = 0;
        Combo = 0;
    }

    private void OnDisable()
    {
        if (playerInput != null)
            playerInput.onActionTriggered -= OnActionTriggered;
    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!System.Enum.TryParse(context.control.name, true, out Keys pressedKey))
            return;

        if (keyImagesByKey.TryGetValue(pressedKey, out KeyImage keyImage))
            keyImage.PlayPressAnimation();

        JudgeInput(pressedKey);
    }

    /// <summary>
    /// Evaluates one valid dance-key press against the closest BPM beat.
    /// A chart/note generator can also call this method when it has selected a note.
    /// </summary>
    public void JudgeInput(Keys pressedKey)
    {
        if (BPM <= 0)
            return;

        float beatDuration = 60f / BPM;
        float elapsed = Time.unscaledTime - pulseStartTime;
        int closestBeat = Mathf.RoundToInt(elapsed / beatDuration);
        float offset = elapsed - (closestBeat * beatDuration);

        // Only one input can consume a beat. This prevents key mashing from scoring.
        if (closestBeat == lastJudgedBeat)
            return;

        Judge judge = GetJudge(Mathf.Abs(offset));
        lastJudgedBeat = closestBeat;
        ApplyJudgement(pressedKey, judge, offset);
    }

    private Judge GetJudge(float absoluteOffset)
    {
        if (absoluteOffset <= justWindow)
            return Judge.Just;
        if (absoluteOffset <= goodWindow)
            return Judge.Good;
        if (absoluteOffset <= badWindow)
            return Judge.Bad;
        return Judge.Miss;
    }

    private void ApplyJudgement(Keys pressedKey, Judge judge, float offset)
    {
        switch (judge)
        {
            case Judge.Just:
                Score += 1000;
                Combo++;
                break;
            case Judge.Good:
                Score += 700;
                Combo++;
                break;
            case Judge.Bad:
                Score += 300;
                Combo = 0;
                break;
            case Judge.Miss:
                Combo = 0;
                break;
        }

        if (danceHUD == null)
            danceHUD = FindFirstObjectByType<DanceSceneHUD>();

        if (danceHUD != null)
        {
            danceHUD.JudgementEffect(judge);
            danceHUD.AddScore(Score);
        }

        OnJudged?.Invoke(pressedKey, judge, offset);
    }

    private void Update()
    {
        if (circlePulseMaterial == null || BPM <= 0)
            return;

        float beatDuration = 60f / BPM;
        float beatProgress = Mathf.Repeat((Time.unscaledTime - pulseStartTime) / beatDuration, 1f);
        circlePulseMaterial.SetFloat(ProgressProperty, (1f - beatProgress) * 0.5f);
    }

    private void OnDestroy()
    {
        if (circlePulseMaterial != null)
            Destroy(circlePulseMaterial);

        foreach (Material material in outlineMaskMaterials)
            Destroy(material);

        if (outlineMaskTexture != null)
            outlineMaskTexture.Release();

        if (outlineMaskCanvasObject != null)
            Destroy(outlineMaskCanvasObject);

        if (outlineMaskCameraObject != null)
            Destroy(outlineMaskCameraObject);
    }

    private void LateUpdate()
    {
        if (!enableCombinedOutline)
            return;

        SyncOutlineMaskLayout();

        if (outlineMaskCamera != null)
        {
            Canvas.ForceUpdateCanvases();
            outlineMaskCamera.Render();

        }
    }

    private void SetupCombinedOutline()
    {
        if (!Application.isPlaying)
            return;


        if (combinedOutline == null)
        {
            Transform outlineTransform = transform.Find("Combined Outline");
            if (outlineTransform != null)
                combinedOutline = outlineTransform.GetComponent<Image>();
        }

        Image sourceFill = null;
        foreach (GameObject keyImageObject in Key_Images)
        {
            if (keyImageObject == null)
                continue;

            sourceFill = keyImageObject.GetComponentInChildren<Image>();
            if (sourceFill != null)
                break;
        }

        if (combinedOutline == null)
        {
            Debug.LogWarning("Combined Outline Image를 찾지 못했습니다.", this);
            return;
        }

        // Canvas Image가 Sprite 없이 컬링되면 Shader Graph 자체가 실행되지 않는다.
        // 전체 RT UV를 받는 기본 UI Sprite를 명시적으로 사용한다.
        if (combinedOutline.sprite == null)
            combinedOutline.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        outlineMaskTexture = new RenderTexture(outlineMaskResolution, outlineMaskResolution, 0, RenderTextureFormat.ARGB32)
        {
            name = "Runtime Rhythm Wheel Outline Mask",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        outlineMaskTexture.Create();

        // 기존 Canvas Shader Graph가 쓰는 실제 프로퍼티에 런타임 마스크를 연결한다.
        // Inspector의 Outline with Pixel 값도 이 머티리얼 하나에서 계속 조절된다.
        Material outlineMaterial = combinedOutline.material;
        outlineMaterial.SetTexture("_Mask_Texture", outlineMaskTexture);

        const int MaskLayer = 31;
        outlineMaskCanvasObject = new GameObject("Runtime Rhythm Wheel Outline Mask", typeof(Canvas));
        SetLayerRecursively(outlineMaskCanvasObject, MaskLayer);

        Canvas maskCanvas = outlineMaskCanvasObject.GetComponent<Canvas>();
        maskCanvas.renderMode = RenderMode.WorldSpace;
        maskCanvas.sortingOrder = short.MinValue;

        RectTransform canvasRect = outlineMaskCanvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = ((RectTransform)transform).sizeDelta;
        canvasRect.localScale = Vector3.one * 0.01f;
        canvasRect.position = transform.position;

        outlineMaskCameraObject = new GameObject("Runtime Rhythm Wheel Outline Camera", typeof(Camera));
        SetLayerRecursively(outlineMaskCameraObject, MaskLayer);

        Camera maskCamera = outlineMaskCameraObject.GetComponent<Camera>();
        maskCamera.transform.position = transform.position + new Vector3(0f, 0f, -10f);
        maskCamera.orthographic = true;
        maskCamera.orthographicSize = 0.5f;
        maskCamera.clearFlags = CameraClearFlags.SolidColor;
        maskCamera.backgroundColor = Color.clear;
        maskCamera.cullingMask = 1 << MaskLayer;
        maskCamera.targetTexture = outlineMaskTexture;
        maskCamera.enabled = false;
        maskCanvas.worldCamera = maskCamera;
        outlineMaskCamera = maskCamera;

        RectTransform maskRoot = new GameObject("Mask Shapes", typeof(RectTransform)).GetComponent<RectTransform>();
        maskRoot.SetParent(outlineMaskCanvasObject.transform, false);
        maskRoot.anchorMin = maskRoot.anchorMax = new Vector2(0.5f, 0.5f);
        maskRoot.anchoredPosition = Vector2.zero;
        maskRoot.sizeDelta = ((RectTransform)transform).sizeDelta;
        maskRoot.localScale = Vector3.one;
        SetLayerRecursively(maskRoot.gameObject, MaskLayer);

        foreach (GameObject keyImageObject in Key_Images)
        {
            if (keyImageObject == null)
                continue;

            GameObject clone = Instantiate(keyImageObject, maskRoot);
            clone.name = $"Mask {keyImageObject.name}";
            SetLayerRecursively(clone, MaskLayer);

            KeyImage clonedKeyImage = clone.GetComponent<KeyImage>();
            if (clonedKeyImage != null)
                clonedKeyImage.enabled = false;

            foreach (TMPro.TextMeshProUGUI text in clone.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                text.gameObject.SetActive(false);

            foreach (Image image in clone.GetComponentsInChildren<Image>(true))
            {
                Material maskMaterial = new Material(image.material);
                if (maskMaterial.HasProperty("_Fill_Color"))
                    maskMaterial.SetColor("_Fill_Color", Color.white);
                if (maskMaterial.HasProperty("_Line_Color"))
                    maskMaterial.SetColor("_Line_Color", Color.white);

                image.material = maskMaterial;
                image.color = Color.white;
                outlineMaskMaterials.Add(maskMaterial);
            }

            outlineMaskKeys.Add(clone.GetComponent<RectTransform>());
        }

        CreateCenterMask(maskRoot, MaskLayer);
        Transform circleLine = transform.Find("Circle Line");
        if (circleLine != null)
            circleLine.gameObject.SetActive(false);

        combinedOutline.gameObject.SetActive(true);
        combinedOutline.transform.SetAsLastSibling();
        SyncOutlineMaskLayout();
    }

    private void CreateCenterMask(RectTransform maskRoot, int maskLayer)
    {
        GameObject centerObject = new GameObject("Center Mask", typeof(RectTransform), typeof(Image));
        centerObject.transform.SetParent(maskRoot, false);
        SetLayerRecursively(centerObject, maskLayer);

        RectTransform centerTransform = centerObject.GetComponent<RectTransform>();
        centerTransform.anchorMin = centerTransform.anchorMax = new Vector2(0.5f, 0.5f);
        centerTransform.anchoredPosition = Vector2.zero;
        centerTransform.sizeDelta = Vector2.one * centerMaskDiameter;

        Image centerImage = centerObject.GetComponent<Image>();
        centerImage.sprite = CreateCircleSprite(128);
        centerImage.color = Color.white;
        centerImage.raycastTarget = false;
    }

    private void SyncOutlineMaskLayout()
    {
        if (!Application.isPlaying || outlineMaskKeys.Count != Key_Images.Length)
            return;

        for (int i = 0; i < Key_Images.Length; i++)
        {
            if (Key_Images[i] == null || outlineMaskKeys[i] == null)
                continue;

            RectTransform source = Key_Images[i].GetComponent<RectTransform>();
            RectTransform target = outlineMaskKeys[i];
            target.gameObject.SetActive(Key_Images[i].activeInHierarchy);
            target.anchoredPosition = source.anchoredPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
            target.sizeDelta = source.sizeDelta;
        }
    }

    private static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius - 0.5f, radius - 0.5f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float alpha = Vector2.Distance(new Vector2(x, y), center) <= radius ? 1f : 0f;
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

}
