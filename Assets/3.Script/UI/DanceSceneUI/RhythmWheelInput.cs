using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(PlayerInput), typeof(RhythmWheelTiming), typeof(RhythmWheelJudgement))]
public sealed class RhythmWheelInput : MonoBehaviour
{
    [SerializeField] private GameObject[] keyImages = new GameObject[6];
    [SerializeField] private StarPulseUI starPulse;
    private PlayerInput playerInput;
    private RhythmWheelTiming timing;
    private RhythmWheelJudgement judgement;
    private RhythmWheelFever fever;
    private readonly Dictionary<Keys, KeyImage> keys = new();
    private readonly HashSet<Keys> handledKeysThisFrame = new();
    [SerializeField] private float inputMoveDistance = 30f;
    [SerializeField] private float inputMoveOutDuration = .005f;
    [SerializeField] private float inputMoveReturnDuration = .25f;
    private RectTransform wheel;
    private Vector2 origin;
    private Coroutine wheelMotion;
    [SerializeField] private float cameraInputMoveDistance = .12f;
    private Camera inputCamera;
    private Vector2 cameraDirection;
    private float cameraAmount;
    private Coroutine cameraMotion;
    public event Action<Keys, Judge, float> Judged;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>(); timing = GetComponent<RhythmWheelTiming>(); judgement = GetComponent<RhythmWheelJudgement>(); fever = GetComponent<RhythmWheelFever>(); wheel = transform as RectTransform; if (wheel != null) origin = wheel.anchoredPosition;
        starPulse ??= GetComponentInChildren<StarPulseUI>(true); inputCamera = Camera.main;
        foreach (var item in keyImages) if (item != null && item.TryGetComponent(out KeyImage image)) keys[image.key] = image;
        if (keys.Count == 0)
            foreach (var image in GetComponentsInChildren<KeyImage>(true)) keys[image.key] = image;
        judgement.Judged += (key, result, offset) => Judged?.Invoke(key, result, offset);
    }
    private void OnValidate()
    {
        if (keyImages == null || keyImages.Length == 0 || System.Array.Exists(keyImages, item => item == null)) {
            var images = GetComponentsInChildren<KeyImage>(true);
            keyImages = new GameObject[images.Length];
            for (int i = 0; i < images.Length; i++) keyImages[i] = images[i].gameObject;
        }
        starPulse ??= GetComponentInChildren<StarPulseUI>(true);
    }
    private void OnEnable() { playerInput.onActionTriggered += Handle; CinemachineCore.CameraUpdatedEvent.AddListener(ApplyCameraOffset); }
    private void OnDisable() { playerInput.onActionTriggered -= Handle; CinemachineCore.CameraUpdatedEvent.RemoveListener(ApplyCameraOffset); }

    private void Update()
    {
        // PlayerInput can temporarily stop dispatching an action while its map
        // is being re-enabled.  Poll the physical keyboard as a same-frame
        // fallback so a real key press is never lost during that transition.
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.wKey.wasPressedThisFrame) HandleKey(Keys.W, Time.realtimeSinceStartupAsDouble);
        if (keyboard.aKey.wasPressedThisFrame) HandleKey(Keys.A, Time.realtimeSinceStartupAsDouble);
        if (keyboard.sKey.wasPressedThisFrame) HandleKey(Keys.S, Time.realtimeSinceStartupAsDouble);
        if (keyboard.dKey.wasPressedThisFrame) HandleKey(Keys.D, Time.realtimeSinceStartupAsDouble);
        if (keyboard.qKey.wasPressedThisFrame) HandleKey(Keys.Q, Time.realtimeSinceStartupAsDouble);
        if (keyboard.eKey.wasPressedThisFrame) HandleKey(Keys.E, Time.realtimeSinceStartupAsDouble);
    }

    private void LateUpdate() => handledKeysThisFrame.Clear();

    private void Handle(InputAction.CallbackContext context)
    {
        if (context.performed && Enum.TryParse(context.control.name, true, out Keys key))
            HandleKey(key, context.time);
    }

    private void HandleKey(Keys key, double inputTime)
    {
        if (!handledKeysThisFrame.Add(key) || !keys.TryGetValue(key, out var image) || !image.isActiveAndEnabled) return;
        if (fever != null && fever.IsJudgementLocked) return;
        float elapsed = timing.SampleInputElapsedSeconds(inputTime);
        // Resolve first.  This prevents a visible key/wheel response without a
        // matching score judgement, and lets RhythmWheelJudgement handle its
        // narrow third-beat-to-rest callback grace.
        if (!judgement.JudgeInput(key, elapsed)) return;
        PlayWheelMotion(key); PlayCameraMotion(key); image.PlayPressAnimation();
        starPulse?.Play(Mathf.RoundToInt(60f / timing.BeatDurationSeconds));
    }
    private void PlayWheelMotion(Keys key) { if (wheel == null) return; if (wheelMotion != null) StopCoroutine(wheelMotion); wheelMotion = StartCoroutine(MoveWheel(Direction(key))); }
    private void PlayCameraMotion(Keys key) { inputCamera ??= Camera.main; if (inputCamera == null) return; if (cameraMotion != null) StopCoroutine(cameraMotion); cameraDirection = Direction(key).normalized; cameraMotion = StartCoroutine(MoveCamera()); }
    private System.Collections.IEnumerator MoveCamera() { yield return AnimateCamera(0, 1, inputMoveOutDuration); yield return AnimateCamera(1, 0, inputMoveReturnDuration); cameraAmount = 0; }
    private System.Collections.IEnumerator AnimateCamera(float from, float to, float duration) { for (float time = 0; time < duration; time += Time.unscaledDeltaTime) { cameraAmount = Mathf.SmoothStep(from, to, time / duration); yield return null; } cameraAmount = to; }
    private void ApplyCameraOffset(CinemachineBrain brain) { if (cameraAmount <= 0 || brain == null || brain.OutputCamera != inputCamera) return; var t = inputCamera.transform; t.position += (t.right * cameraDirection.x + t.up * cameraDirection.y) * cameraInputMoveDistance * cameraAmount; }
    private System.Collections.IEnumerator MoveWheel(Vector2 direction) { Vector2 target = origin + direction.normalized * inputMoveDistance; yield return Move(origin, target, inputMoveOutDuration); yield return Move(target, origin, inputMoveReturnDuration); }
    private System.Collections.IEnumerator Move(Vector2 from, Vector2 to, float duration) { for (float time = 0; time < duration; time += Time.unscaledDeltaTime) { wheel.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0, 1, time / duration)); yield return null; } wheel.anchoredPosition = to; }
    private static Vector2 Direction(Keys key) => key switch { Keys.W => Vector2.up, Keys.A => Vector2.left, Keys.S => Vector2.down, Keys.D => Vector2.right, Keys.Q => new Vector2(-1, 1), Keys.E => new Vector2(1, 1), _ => Vector2.zero };
}
