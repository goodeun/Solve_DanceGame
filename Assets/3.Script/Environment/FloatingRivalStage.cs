using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 라이벌이 올라가 춤추는 UFO 같은 공중 무대(Rival 씬).
/// - 모양: 구의 위쪽을 잘라 평평하게 만든 작은 원반(위는 무대 바닥, 아래는 둥근 배). 실행 시 메시를 만든다.
///   에디터에서는 Gizmo로 모양만 미리 보여준다(씬·프리펩에 메시가 저장되지 않음).
/// - 위치: 사각형(QEAD)이면 위쪽 두 객석 사이(가운데), 마름모(WASD)이면 비는 좌상·우상 중 랜덤.
///   객석 앵커에서 플레이어 무대 쪽으로 forwardOffset만큼 당겨 항상 객석 앞에 있다.
///   배치가 바뀌면 객석과 같은 시간 동안 앞쪽으로 살짝 휘는 궤적으로 이동한다(객석과 겹치지 않게).
/// - 회전: 가운데면 정면, 옆이면 플레이어 무대 쪽으로 maxYaw(15~20도)까지만 부드럽게 돈다.
/// - 둥실둥실 떠 있고, 테두리 불빛이 박자에 맞춰 맥동한다.
/// Rival 프리펩은 Visual 아래에 중첩해서 쓴다(Rival 프리펩 자체는 건드리지 않음).
/// </summary>
[ExecuteAlways]
public class FloatingRivalStage : MonoBehaviour
{
    public enum Slot { Center, Left, Right }

    [Header("References")]
    [Tooltip("비우면 StageController.Instance")]
    [SerializeField] private StageController stageController;
    [Tooltip("바라볼 플레이어 무대. 비우면 StageController 위치")]
    [SerializeField] private Transform playerStage;
    [Tooltip("둥실거리고 메시가 붙는 자식(Rival이 이 아래에 있다)")]
    [SerializeField] private Transform visual;

    [Header("Shape (구를 잘라 위가 평평한 원반)")]
    [Tooltip("켜면 위에 선 캐릭터 키에 맞춰 무대 크기를 정한다(반지름 = 키 × 아래 비율). 끄면 Radius 사용")]
    [SerializeField] private bool sizeFromCharacter = true;
    [SerializeField, Range(0.15f, 1.5f)] private float radiusPerCharacterHeight = 0.38f;
    [SerializeField, Min(0.1f)] private float radius = 7f;
    [Tooltip("구 중심에서 자르는 높이 비율. 클수록 평평한 윗면이 작고 배가 둥글다")]
    [SerializeField, Range(0f, 0.9f)] private float cutHeight = 0.45f;
    [Tooltip("세로 납작함(1 = 구, 0.4 = UFO처럼 납작)")]
    [SerializeField, Range(0.15f, 1f)] private float verticalScale = 0.42f;
    [SerializeField, Range(12, 96)] private int segments = 48;
    [SerializeField, Range(4, 32)] private int rings = 12;
    [Tooltip("윗면 가장자리의 빛나는 띠 높이")]
    [SerializeField, Min(0f)] private float rimHeight = 0.35f;

    [Header("Materials (비우면 임시 재질 자동 생성)")]
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material topMaterial;
    [SerializeField] private Material rimMaterial;
    [SerializeField] private Color bodyColor = new Color(0.16f, 0.13f, 0.22f, 1f);
    [SerializeField] private Color topColor = new Color(0.32f, 0.26f, 0.42f, 1f);
    [SerializeField] private Color rimColor = new Color(0.85f, 0.2f, 1f, 1f);
    [SerializeField, Min(0f)] private float rimIntensity = 2.5f;
    [Tooltip("박마다 테두리 불빛이 더 밝아지는 양")]
    [SerializeField, Min(0f)] private float rimBeatPulse = 2f;

    [Header("Placement")]
    [Tooltip("객석 앵커에서 플레이어 무대 쪽(앞)으로 당기는 거리")]
    [SerializeField] private float forwardOffset = 22f;
    [SerializeField] private float heightOffset = 0f;
    [Tooltip("가운데(사각형)에 있을 때 추가 높이. 음수면 아래로(Fever UI에 가리지 않게)")]
    [SerializeField] private float centerHeightOffset = -7f;
    [Tooltip("사각형에서 쓸 위쪽 두 앵커(시계 방향 Seats 순서: 0 = 좌상, 1 = 우상)")]
    [SerializeField] private int topLeftAnchorIndex = 0;
    [SerializeField] private int topRightAnchorIndex = 1;

    [Header("Rotation")]
    [Tooltip("옆에 있을 때 플레이어 무대 쪽으로 도는 최대 각도")]
    [SerializeField, Range(0f, 45f)] private float maxYaw = 18f;
    [SerializeField, Min(0.01f)] private float yawSmoothTime = 0.45f;

    [Header("Move")]
    [Tooltip("0 이하면 객석 이동 시간과 같게")]
    [SerializeField] private float moveDuration = 0f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;
    [Tooltip("이동 중 앞쪽(플레이어 쪽)으로 휘는 정도. 객석과 겹치지 않게")]
    [SerializeField, Min(0f)] private float arcForward = 6f;
    [SerializeField, Min(0f)] private float arcHeight = 2.5f;

    [Header("Hover")]
    [SerializeField, Min(0f)] private float bobHeight = 0.5f;
    [SerializeField, Min(0.1f)] private float bobPeriod = 2.6f;
    [SerializeField, Range(0f, 10f)] private float swayAngle = 2f;

    [Header("Light (은은한 조명)")]
    [Tooltip("URP 실시간 Area Light는 지원되지 않아 넓고 부드러운 Spot Light로 앞 위에서 비춘다")]
    [SerializeField] private bool useLight = true;
    [SerializeField] private Color lightColor = new Color(1f, 0.85f, 0.95f, 1f);
    [Tooltip("캐릭터 크기와 상관없이 같은 밝기가 되도록 거리²를 곱해 쓴다")]
    [SerializeField, Min(0f)] private float lightStrength = 0.8f;
    [Tooltip("캐릭터 앞쪽 거리(키 배수)")]
    [SerializeField, Min(0.1f)] private float lightDistance = 1.1f;
    [Tooltip("캐릭터 위쪽 높이(키 배수)")]
    [SerializeField] private float lightHeight = 1.2f;
    [SerializeField, Range(10f, 170f)] private float lightAngle = 60f;
    [Tooltip("박자에 맞춰 살짝 밝아지는 정도(0 = 고정)")]
    [SerializeField, Range(0f, 1f)] private float lightBeatPulse = 0.15f;

    [Header("Appear")]
    [Tooltip("켜면 시작할 때 숨겨 두고 Appear()로 등장한다(BossBattleDirector가 인트로 컷씬 뒤에 부른다)")]
    [SerializeField] private bool hideOnStart = false;
    [SerializeField, Min(0f)] private float appearHeight = 25f;
    [SerializeField, Min(0.05f)] private float appearDuration = 1.3f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Mesh mesh;
    private GameObject meshObject;
    private readonly List<Material> ownedMaterials = new List<Material>();
    private Material runtimeRim;

    private bool anchorsReady;
    private Vector3 topLeft, topRight;
    private Slot slot = Slot.Center;
    private Vector3 moveFrom, moveTo;
    private float moveTime, moveLength;
    private bool moving;
    private float yaw, yawVelocity;
    private Quaternion baseRotation;
    private Vector3 visualBaseLocal;
    private Quaternion visualBaseRotation;
    private float appearOffset;
    private Tween appearTween;
    private RhythmWheelTiming timing;
    private bool subscribed;
    private bool hidden;
    private float characterHeight;
    private Light stageLight;
    private float lightBaseIntensity;
    private bool measured;

    public Slot CurrentSlot => slot;
    public bool IsShown => visual == null || visual.gameObject.activeSelf;

    // ================= Lifecycle =================

    private void Awake()
    {
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        baseRotation = transform.rotation;
        if (visual != null)
        {
            visualBaseLocal = visual.localPosition;
            visualBaseRotation = visual.localRotation;
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        MeasureCharacter();
        BuildMesh();
        BuildLight();
        Subscribe();
        SnapToCurrentStage();
        if (hideOnStart || hidden) HideImmediate();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && meshObject != null) Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
#if UNITY_EDITOR
        OnDisable_Editor();
#endif
    }

    private void OnDestroy()
    {
        Unsubscribe();
        appearTween?.Kill();
        foreach (Material m in ownedMaterials) DestroySafe(m);
        ownedMaterials.Clear();
        DestroySafe(mesh);
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (stageController == null) stageController = StageController.Instance;
        if (stageController == null) stageController = FindFirstObjectByType<StageController>();
        if (stageController == null) return;
        stageController.StageChangeStarted += HandleStageChange;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (stageController != null) stageController.StageChangeStarted -= HandleStageChange;
        subscribed = false;
    }

    // ================= Placement =================

    private void HandleStageChange(Stage target, float duration)
    {
        Slot next = target == Stage.Square ? Slot.Center : (UnityEngine.Random.value < 0.5f ? Slot.Left : Slot.Right);
        MoveTo(next, moveDuration > 0f ? moveDuration : duration);
    }

    /// <summary>현재 객석 배치에 맞는 자리로 바로 옮긴다.</summary>
    public void SnapToCurrentStage()
    {
        CacheAnchors();
        Stage stage = stageController != null ? stageController.currentPhase : Stage.Square;
        slot = stage == Stage.Square ? Slot.Center : (UnityEngine.Random.value < 0.5f ? Slot.Left : Slot.Right);
        moving = false;
        currentBase = SlotPosition(slot);
        baseReady = true;
        transform.position = currentBase + Vector3.up * appearOffset;
        yaw = TargetYaw(slot, transform.position);
        yawVelocity = 0f;
        ApplyRotation();
    }

    /// <summary>지정한 자리로 이동한다(앞쪽으로 휘는 궤적).</summary>
    public void MoveTo(Slot target, float duration)
    {
        CacheAnchors();
        slot = target;
        moveFrom = baseReady ? currentBase : transform.position - Vector3.up * appearOffset;
        moveTo = SlotPosition(target);
        moveLength = Mathf.Max(0.01f, duration);
        moveTime = 0f;
        moving = true;
    }

    private Vector3 currentBase;
    private bool baseReady;

    private void CacheAnchors()
    {
        if (anchorsReady) return;
        if (stageController == null) stageController = StageController.Instance;
        if (stageController == null) return;
        // 앵커는 사각형 기준(시작 시점)으로 한 번만 읽는다.
        if (stageController.TryGetAnchorPosition(Stage.Square, topLeftAnchorIndex, out topLeft)
            && stageController.TryGetAnchorPosition(Stage.Square, topRightAnchorIndex, out topRight))
            anchorsReady = true;
    }

    private Vector3 PlayerStagePosition()
    {
        if (playerStage != null) return playerStage.position;
        if (stageController != null) return stageController.transform.position;
        return Vector3.zero;
    }

    private Vector3 SlotPosition(Slot s)
    {
        if (!anchorsReady) return transform.position;
        Vector3 anchor = s == Slot.Left ? topLeft : s == Slot.Right ? topRight : (topLeft + topRight) * 0.5f;
        Vector3 toPlayer = PlayerStagePosition() - anchor;
        toPlayer.y = 0f;
        Vector3 forward = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.back;
        float height = heightOffset + (s == Slot.Center ? centerHeightOffset : 0f);
        return anchor + forward * forwardOffset + Vector3.up * height;
    }

    private float TargetYaw(Slot s, Vector3 position)
    {
        if (s == Slot.Center) return 0f;
        Vector3 toPlayer = PlayerStagePosition() - position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return 0f;
        Vector3 front = baseRotation * Vector3.back; // 기본 정면 = 카메라(플레이어) 쪽
        front.y = 0f;
        float angle = Vector3.SignedAngle(front, toPlayer, Vector3.up);
        return Mathf.Clamp(angle, -maxYaw, maxYaw);
    }

    // ================= Size =================

    /// <summary>Visual 아래 캐릭터(무대 메시 제외)의 렌더러 높이를 잰다.</summary>
    private void MeasureCharacter()
    {
        if (Application.isPlaying && measured) return;
        measured = true;
        characterHeight = 0f;
        Transform root = visual != null ? visual : transform;
        bool any = false;
        Bounds bounds = default;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (meshObject != null && r.gameObject == meshObject) continue;
            if (r is ParticleSystemRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (any) characterHeight = bounds.size.y;
    }

    private float EffectiveRadius() =>
        sizeFromCharacter && characterHeight > 0.01f ? characterHeight * radiusPerCharacterHeight : radius;

    // ================= Light =================

    private void BuildLight()
    {
        if (!useLight || stageLight != null) return;
        float h = characterHeight > 0.01f ? characterHeight : EffectiveRadius() * 2.5f;

        var go = new GameObject("RivalStageLight");
        go.transform.SetParent(transform, false);
        // 정면 = 로컬 -Z(플레이어 쪽). 앞 위에서 가슴 높이를 비춘다.
        Vector3 localPos = new Vector3(0f, h * lightHeight, -h * lightDistance);
        Vector3 aim = new Vector3(0f, h * 0.55f, 0f);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.LookRotation(aim - localPos, Vector3.up);

        stageLight = go.AddComponent<Light>();
        stageLight.type = LightType.Spot;
        stageLight.color = lightColor;
        float distance = (aim - localPos).magnitude;
        stageLight.range = distance * 2.2f;
        stageLight.spotAngle = lightAngle;
        stageLight.innerSpotAngle = lightAngle * 0.35f;  // 가장자리를 넓게 퍼지게(Area 느낌)
        lightBaseIntensity = lightStrength * distance * distance;
        stageLight.intensity = lightBaseIntensity;
        stageLight.shadows = LightShadows.None;
    }

    /// <summary>라이벌 조명 켜기/끄기.</summary>
    public void SetLightEnabled(bool on)
    {
        useLight = on;
        if (on) BuildLight();
        if (stageLight != null) stageLight.enabled = on;
    }

    private void ApplyRotation() => transform.rotation = baseRotation * Quaternion.Euler(0f, yaw, 0f);

    // ================= Update =================

    private void Update()
    {
        if (!Application.isPlaying) return;

        // 이동
        if (moving)
        {
            moveTime += Time.deltaTime;
            float t = Mathf.Clamp01(moveTime / moveLength);
            float e = DOVirtual.EasedValue(0f, 1f, t, moveEase);
            float bulge = Mathf.Sin(t * Mathf.PI);

            Vector3 mid = (moveFrom + moveTo) * 0.5f;
            Vector3 toPlayer = PlayerStagePosition() - mid;
            toPlayer.y = 0f;
            Vector3 forward = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.back;

            currentBase = Vector3.Lerp(moveFrom, moveTo, e) + forward * arcForward * bulge + Vector3.up * arcHeight * bulge;
            if (t >= 1f) { moving = false; currentBase = moveTo; }
        }
        else if (!baseReady)
        {
            currentBase = transform.position;
            baseReady = true;
        }

        transform.position = currentBase + Vector3.up * appearOffset;

        // 회전(가운데면 정면, 옆이면 플레이어 무대 쪽으로 살짝)
        float targetYaw = TargetYaw(slot, moving ? moveTo : currentBase);
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, yawSmoothTime);
        ApplyRotation();

        // 둥실둥실
        if (visual != null)
        {
            float time = Time.time * Mathf.PI * 2f / bobPeriod;
            visual.localPosition = visualBaseLocal + Vector3.up * Mathf.Sin(time) * bobHeight;
            visual.localRotation = visualBaseRotation * Quaternion.Euler(Mathf.Sin(time * 0.5f) * swayAngle, 0f, Mathf.Cos(time * 0.7f) * swayAngle);
        }

        UpdateRimPulse();
    }

    private void UpdateRimPulse()
    {
        if (runtimeRim == null && stageLight == null) return;
        float env = 0f;
        if (timing == null) timing = FindFirstObjectByType<RhythmWheelTiming>();
        if (timing != null && timing.TryGetCurrentBeatTiming(timing.ElapsedSeconds, out int beat, out float offset, out float duration) && beat >= 0 && duration > 0f)
        {
            float phase = Mathf.Clamp01(offset / duration);
            env = Mathf.Exp(-6f * phase) * (beat % 4 == 0 ? 1.4f : beat % 4 == 3 ? 0.5f : 1f);
        }
        if (stageLight != null && stageLight.enabled)
            stageLight.intensity = lightBaseIntensity * (1f + lightBeatPulse * env);
        if (runtimeRim != null) runtimeRim.SetColor(EmissionColorId, rimColor * (rimIntensity + rimBeatPulse * env));
    }

    // ================= Appear / Hide =================

    /// <summary>바로 숨긴다(인트로 컷씬 동안).</summary>
    public void HideImmediate()
    {
        // 꺼진 렌더러는 크기를 잴 수 없으므로 숨기기 전에 캐릭터 키를 재 둔다.
        MeasureCharacter();
        hidden = true;
        appearTween?.Kill();
        if (visual != null) visual.gameObject.SetActive(false);
        if (meshObject != null) meshObject.SetActive(false);
        if (stageLight != null) stageLight.gameObject.SetActive(false);
    }

    /// <summary>위에서 내려와 등장한다.</summary>
    public void Appear(Action onComplete = null)
    {
        hidden = false;
        appearTween?.Kill();
        if (visual != null) visual.gameObject.SetActive(true);
        if (meshObject != null) meshObject.SetActive(true);
        if (stageLight != null) stageLight.gameObject.SetActive(true);
        appearOffset = appearHeight;
        appearTween = DOTween.To(() => appearOffset, v => appearOffset = v, 0f, appearDuration)
            .SetEase(Ease.OutBack, 1.1f)
            .SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    // ================= Mesh =================

    private void BuildMesh()
    {
        if (meshObject != null) return;
        Transform parent = visual != null ? visual : transform;

        mesh = CreateMesh();
        meshObject = new GameObject("FloatingStageMesh");
        meshObject.layer = gameObject.layer;
        meshObject.transform.SetParent(parent, false);
        meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = meshObject.AddComponent<MeshRenderer>();

        Material body = bodyMaterial != null ? bodyMaterial : MakeMaterial(bodyColor, Color.black, 0.75f, 0.55f);
        Material top = topMaterial != null ? topMaterial : MakeMaterial(topColor, Color.black, 0.3f, 0.7f);
        runtimeRim = rimMaterial != null ? new Material(rimMaterial) : MakeMaterial(Color.black, rimColor * rimIntensity, 0f, 0.5f);
        if (rimMaterial != null) ownedMaterials.Add(runtimeRim);
        renderer.sharedMaterials = new[] { body, top, runtimeRim };
    }

    private Material MakeMaterial(Color color, Color emission, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new Material(shader) { name = "FloatingStage (temp)" };
        if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);
        if (m.HasProperty(ColorId)) m.SetColor(ColorId, color);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (m.HasProperty(EmissionColorId)) m.SetColor(EmissionColorId, emission);
        ownedMaterials.Add(m);
        return m;
    }

    /// <summary>윗면이 y = 0인 잘린 구(서브메시 0: 배, 1: 윗면, 2: 테두리 띠).</summary>
    private Mesh CreateMesh()
    {
        float radius = EffectiveRadius();
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var body = new List<int>();
        var top = new List<int>();
        var rim = new List<int>();

        float c = Mathf.Clamp(cutHeight, 0f, 0.9f) * radius;    // 자르는 평면의 높이(구 중심 기준)
        float theta0 = Mathf.Acos(c / radius);                  // 위에서 잘린 각도
        float topRadius = Mathf.Sqrt(radius * radius - c * c);
        float s = verticalScale;
        int seg = Mathf.Max(12, segments);
        int ringCount = Mathf.Max(4, rings);

        // 배(아래 둥근 부분)
        for (int r = 0; r <= ringCount; r++)
        {
            float theta = Mathf.Lerp(theta0, Mathf.PI, r / (float)ringCount);
            float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);
            for (int i = 0; i <= seg; i++)
            {
                float phi = i / (float)seg * Mathf.PI * 2f;
                float cp = Mathf.Cos(phi), sp = Mathf.Sin(phi);
                verts.Add(new Vector3(radius * sinT * cp, s * (radius * cosT - c), radius * sinT * sp));
                normals.Add(new Vector3(sinT * cp, cosT / s, sinT * sp).normalized);
                uvs.Add(new Vector2(i / (float)seg, r / (float)ringCount));
            }
        }
        for (int r = 0; r < ringCount; r++)
        for (int i = 0; i < seg; i++)
        {
            int a = r * (seg + 1) + i, b = a + 1, d = a + seg + 1, e = d + 1;
            body.AddRange(new[] { a, b, d, b, e, d });
        }

        // 윗면(평평한 무대 바닥)
        int center = verts.Count;
        verts.Add(Vector3.zero); normals.Add(Vector3.up); uvs.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i <= seg; i++)
        {
            float phi = i / (float)seg * Mathf.PI * 2f;
            float cp = Mathf.Cos(phi), sp = Mathf.Sin(phi);
            verts.Add(new Vector3(topRadius * cp, 0f, topRadius * sp));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f + cp * 0.5f, 0.5f + sp * 0.5f));
        }
        for (int i = 0; i < seg; i++) top.AddRange(new[] { center, center + i + 2, center + i + 1 });

        // 테두리 빛 띠(윗면 가장자리 바로 아래, 바깥을 향함)
        if (rimHeight > 0f)
        {
            int start = verts.Count;
            float rr = topRadius * 1.01f;
            for (int i = 0; i <= seg; i++)
            {
                float phi = i / (float)seg * Mathf.PI * 2f;
                Vector3 n = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
                verts.Add(n * rr + Vector3.up * 0.02f); normals.Add(n); uvs.Add(new Vector2(i / (float)seg, 1f));
                verts.Add(n * rr * 1.0f - Vector3.up * rimHeight); normals.Add(n); uvs.Add(new Vector2(i / (float)seg, 0f));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = start + i * 2, b = a + 1, d = a + 2, e = a + 3;
                rim.AddRange(new[] { a, d, b, b, d, e });
            }
        }

        var m = new Mesh { name = "FloatingRivalStage" };
        if (!Application.isPlaying) m.hideFlags = HideFlags.DontSave;
        m.SetVertices(verts);
        m.SetNormals(normals);
        m.SetUVs(0, uvs);
        m.subMeshCount = 3;
        m.SetTriangles(body, 0);
        m.SetTriangles(top, 1);
        m.SetTriangles(rim, 2);
        m.RecalculateBounds();
        return m;
    }

    private static void DestroySafe(UnityEngine.Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }

    // ================= Editor preview =================

#if UNITY_EDITOR
    private Mesh previewMesh;
    private string previewKey;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        MeasureCharacter();
        string key = $"{EffectiveRadius()}|{cutHeight}|{verticalScale}|{segments}|{rings}|{rimHeight}";
        if (previewMesh == null || previewKey != key)
        {
            if (previewMesh != null) DestroyImmediate(previewMesh);
            previewMesh = CreateMesh();
            previewKey = key;
        }

        Transform t = visual != null ? visual : transform;
        Gizmos.matrix = t.localToWorldMatrix;
        Gizmos.color = new Color(bodyColor.r + 0.2f, bodyColor.g + 0.2f, bodyColor.b + 0.25f, 1f);
        Gizmos.DrawMesh(previewMesh, 0);
        Gizmos.color = topColor + new Color(0.15f, 0.15f, 0.15f, 0f);
        Gizmos.DrawMesh(previewMesh, 1);
        Gizmos.color = rimColor;
        Gizmos.DrawMesh(previewMesh, 2);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void OnValidate() => previewKey = null;

    private void OnDisable_Editor()
    {
        if (previewMesh != null) DestroyImmediate(previewMesh);
        previewMesh = null;
    }
#endif
}
