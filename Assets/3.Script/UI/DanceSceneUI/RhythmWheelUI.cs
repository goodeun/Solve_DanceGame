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

    private PlayerInput playerInput;
    private readonly Dictionary<Keys, KeyImage> keyImagesByKey = new();

    private static readonly int ProgressProperty = Shader.PropertyToID("_Progress");
    private Material circlePulseMaterial;
    private float pulseStartTime;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

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
        playerInput.onActionTriggered += OnActionTriggered;
        pulseStartTime = Time.unscaledTime;
    }

    private void OnDisable()
    {
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
    }

    private void Update()
    {
        if (circlePulseMaterial == null || BPM <= 0)
            return;

        float beatDuration = 60f / BPM;
        float beatProgress = Mathf.Repeat((Time.unscaledTime - pulseStartTime) / beatDuration, 1f);
        circlePulseMaterial.SetFloat(ProgressProperty, beatProgress * 0.5f);
    }

    private void OnDestroy()
    {
        if (circlePulseMaterial != null)
            Destroy(circlePulseMaterial);
    }
}
