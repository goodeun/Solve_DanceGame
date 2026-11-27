using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 페르소나식 Pause 연출. DancePauseHandler가 Enter / BeginExit / Complete를 호출한다.
///
/// 구조 (게임 카메라·실제 플레이어는 건드리지 않는다)
///   [월드: 시간 정지된 게임 화면] → 어둡게 덮는 Dim(정렬 600, 대사창보다 아래라 대사창이 내려가는 게 보임)
///   [FX 캔버스(정렬 1400)] 대각선 배경 패널 → 패턴 → 슬래시 → 캐릭터(RenderTexture) → PAUSE 타이틀
///   [PauseSystem 캔버스(정렬 1500)] 버튼·서브 패널
/// 캐릭터는 실제 플레이어를 복제한 메뉴 전용 리그(스크립트 제거)를 화면 밖 먼 곳에 두고 전용 카메라로 찍어
/// RawImage에 띄운다. 진입 순간 실제 플레이어의 현재 동작을 리그에 복사한 뒤 Pause 진입 → 루프 모션으로 넘어간다.
///
/// 리소스 교체: Inspector의 Sprites 칸에 이미지를 넣으면 코드가 만든 단색 도형 대신 그 이미지를 쓴다.
/// 위치·크기·색·타이밍도 전부 Inspector 값으로 조절한다.
/// </summary>
public class PausePresentation : MonoBehaviour
{
    // ---------------- Inspector ----------------

    [Header("Portrait (캐릭터 촬영)")]
    [Tooltip("복제할 플레이어 Animator. 비우면 PlayerDanceAnimator에서 찾는다")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("메뉴 리그를 둘 위치(게임 화면과 겹치지 않는 먼 곳)")]
    [SerializeField] private Vector3 rigWorldPosition = new Vector3(0f, -3000f, 0f);
    [SerializeField, Range(5f, 60f)] private float portraitFov = 18f;
    [Tooltip("캐릭터를 보는 수평 각도. 0 = 정면(모델 +Z), 모델이 반대를 보면 180")]
    [SerializeField, Range(-180f, 180f)] private float portraitYaw = 24f;
    [Tooltip("내려다보는 각도(+면 위에서)")]
    [SerializeField, Range(-30f, 30f)] private float portraitPitch = 4f;
    [Tooltip("허벅지 위 크롭: 캐릭터 높이 대비 화면 하단 위치(0 = 발, 1 = 머리 끝)")]
    [SerializeField, Range(0f, 0.8f)] private float cropBottom = 0.38f;
    [Tooltip("머리 위 여유(캐릭터 높이 대비). 손·머리 동작이 잘리지 않게")]
    [SerializeField, Range(0f, 0.5f)] private float cropTopMargin = 0.14f;
    [SerializeField] private Vector2Int renderTextureSize = new Vector2Int(1024, 1280);

    [Header("Portrait - Sub Menu (서브 메뉴 진입 시 클로즈업)")]
    [SerializeField, Range(0f, 0.9f)] private float subMenuCropBottom = 0.58f;
    [SerializeField, Range(-180f, 180f)] private float subMenuYaw = -18f;
    [SerializeField, Min(0.05f)] private float subMenuShotDuration = 0.25f;

    [Header("Player Motion")]
    [SerializeField] private string enterState = "Pause_Enter";
    [SerializeField] private string enterStateFallback = "Fever_Finish";
    [SerializeField] private string loopState = "Pause_Loop";
    [SerializeField] private string loopStateFallback = "Idle";
    [SerializeField, Min(0f)] private float motionCrossFade = 0.12f;

    [Header("Layout (1920x1080 기준)")]
    [SerializeField] private Vector2 characterPosition = new Vector2(-430f, -20f);
    [SerializeField] private Vector2 characterSize = new Vector2(820f, 1025f);
    [SerializeField, Range(-20f, 20f)] private float characterTilt = -3f;
    [SerializeField] private Vector2 titlePosition = new Vector2(330f, 360f);
    [SerializeField, Range(-30f, 30f)] private float slashAngle = 24f;
    [SerializeField, Range(-30f, 30f)] private float backPanelAngle = -14f;

    [Header("Colors")]
    [SerializeField] private Color dimColor = new Color(0.06f, 0.0f, 0.08f, 0.68f);
    [SerializeField] private Color backPanelColor = new Color(0.55f, 0.02f, 0.38f, 0.92f);
    [SerializeField] private Color slashColor = new Color(1f, 0.18f, 0.78f, 1f);
    [SerializeField] private Color accentColor = new Color(0.25f, 0.9f, 1f, 1f);
    [SerializeField] private Color patternColor = new Color(1f, 1f, 1f, 0.07f);
    [SerializeField] private Color titleColor = Color.white;

    [Header("Sprites (리소스 교체용, 비우면 임시 도형)")]
    [SerializeField] private Sprite backPanelSprite;
    [SerializeField] private Sprite slashSprite;
    [SerializeField] private Sprite titleSprite;
    [Tooltip("캐릭터 위에 덮을 일러스트(프레임·장식)")]
    [SerializeField] private Sprite characterOverlaySprite;
    [SerializeField] private Texture patternTexture;

    [Header("Timing (초, 실시간)")]
    [SerializeField, Range(0.2f, 1.2f)] private float enterDuration = 0.5f;
    [SerializeField, Range(0.1f, 0.6f)] private float exitDuration = 0.2f;
    [SerializeField] private string titleText = "PAUSE";
    [Tooltip("음악이 멈춰도 마지막 BPM으로 패널이 숨쉰다")]
    [SerializeField] private bool breatheOnLastBpm = true;

    [Header("Buttons (임시 연출, 직접 만들면 끈다)")]
    [SerializeField] private bool animateButtons = true;
    [SerializeField] private float buttonStagger = 0.045f;

    // ---------------- Runtime ----------------

    private FmodMusicClock musicClock;
    private Transform rig;
    private Transform rigCharacter;
    private Animator rigAnimator;
    private Camera portraitCamera;
    private RenderTexture portraitTexture;
    private Bounds rigBounds;
    private Renderer[] playerRenderers;
    private bool[] playerRendererStates;

    private Canvas dimCanvas;
    private Image dim;
    private Canvas fxCanvas;
    private RectTransform fxRoot;
    private RectTransform backPanel;
    private RawImage pattern;
    private RectTransform slash;
    private RectTransform slashLine;
    private Image flash;
    private RawImage character;
    private RawImage characterGhost;
    private Image characterOverlay;
    private readonly List<TextMeshProUGUI> titleLetters = new List<TextMeshProUGUI>();
    private Image titleImage;

    private readonly List<RectTransform> buttons = new List<RectTransform>();
    private readonly List<Vector2> buttonOrigins = new List<Vector2>();

    private Sequence sequence;
    private Tween breathe;
    private Coroutine motionRoutine;
    private PauseManager pauseManager;
    private bool active;
    private bool exiting;
    private int subDepth;

    public bool IsActive => active;
    /// <summary>카운트다운 없는 복귀에서 쓰던 값(호환용). 이제 게임 카메라를 움직이지 않으므로 퇴장 시간과 같다.</summary>
    public float ExitBlendWithoutCountdown => exitDuration;

    private void Awake()
    {
        musicClock = FindFirstObjectByType<FmodMusicClock>();
        if (playerAnimator == null)
        {
            PlayerDanceAnimator dance = FindFirstObjectByType<PlayerDanceAnimator>();
            if (dance != null) playerAnimator = dance.GetComponent<Animator>();
        }
    }

    private void Start()
    {
        BuildRig();
        BuildUi();
        CollectButtons();

        pauseManager = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        if (pauseManager != null) pauseManager.SubPanelDepthChanged += HandleSubPanelDepth;
    }

    private void OnDestroy()
    {
        if (pauseManager != null) pauseManager.SubPanelDepthChanged -= HandleSubPanelDepth;
        sequence?.Kill();
        breathe?.Kill();
        if (portraitTexture != null)
        {
            portraitTexture.Release();
            Destroy(portraitTexture);
        }
    }

    // ================= Rig =================

    private void BuildRig()
    {
        if (playerAnimator == null) return;

        // 비활성 부모 아래에 복제해 Awake가 돌기 전에 게임 스크립트를 제거한다.
        var holder = new GameObject("PausePortraitRig");
        holder.SetActive(false);
        holder.transform.SetParent(transform, false);
        holder.transform.position = rigWorldPosition;
        rig = holder.transform;

        GameObject clone = Instantiate(playerAnimator.gameObject, rig);
        clone.name = "MenuCharacter";
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        // 원본의 월드 크기를 유지한다(부모 스케일 반영)
        clone.transform.localScale = playerAnimator.transform.lossyScale;
        rigCharacter = clone.transform;
        StripGameplay(clone);

        rigAnimator = clone.GetComponent<Animator>();
        if (rigAnimator != null)
        {
            rigAnimator.applyRootMotion = false;
            rigAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            rigAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        rigBounds = ComputeLocalBounds(clone);

        var camGo = new GameObject("PortraitCamera");
        camGo.transform.SetParent(rig, false);
        portraitCamera = camGo.AddComponent<Camera>();
        portraitCamera.clearFlags = CameraClearFlags.SolidColor;
        portraitCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        portraitCamera.nearClipPlane = 0.05f;
        portraitCamera.farClipPlane = 30f;
        portraitCamera.allowHDR = false;
        portraitCamera.allowMSAA = true;
        portraitCamera.depth = -10f;

        portraitTexture = new RenderTexture(renderTextureSize.x, renderTextureSize.y, 24, RenderTextureFormat.ARGB32)
        {
            name = "PausePortraitRT",
            antiAliasing = 4,
        };
        portraitCamera.targetTexture = portraitTexture;
        portraitCamera.enabled = false;

        ApplyShot(cropBottom, portraitYaw, 0f);
    }

    private static void StripGameplay(GameObject clone)
    {
        foreach (Transform t in clone.GetComponentsInChildren<Transform>(true))
            t.gameObject.tag = "Untagged";

        // 의존 관계 때문에 한 번에 안 지워지는 경우가 있어 몇 차례 반복한다.
        for (int pass = 0; pass < 3; pass++)
        {
            foreach (MonoBehaviour mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null) DestroyImmediate(mb);
            foreach (Collider c in clone.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (Rigidbody r in clone.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(r);
            foreach (AudioSource a in clone.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(a);
        }
    }

    private Bounds ComputeLocalBounds(GameObject clone)
    {
        // 원본 플레이어 렌더러 경계를 원본 기준 로컬로 변환해 쓴다(복제본은 아직 렌더되지 않아 경계가 불안정).
        Renderer[] source = playerAnimator.GetComponentsInChildren<Renderer>();
        playerRenderers = source;
        var bounds = new Bounds(Vector3.up, Vector3.one * 0.1f);
        bool has = false;
        foreach (Renderer r in source)
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            Bounds b = r.bounds;
            Vector3 min = playerAnimator.transform.InverseTransformPoint(b.min);
            Vector3 max = playerAnimator.transform.InverseTransformPoint(b.max);
            var local = new Bounds((min + max) * 0.5f, new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z)));
            if (!has) { bounds = local; has = true; }
            else bounds.Encapsulate(local);
        }
        return bounds;
    }

    /// <summary>허벅지 위 크롭으로 카메라 위치를 계산한다(duration > 0이면 트윈).</summary>
    private void ApplyShot(float bottomRatio, float yaw, float duration)
    {
        if (portraitCamera == null || rig == null) return;

        Transform basis = rigCharacter != null ? rigCharacter : rig;
        float height = rigBounds.size.y;
        float bottom = rigBounds.min.y + height * bottomRatio;
        float top = rigBounds.max.y + height * cropTopMargin;
        Vector3 bottomWorld = basis.TransformPoint(new Vector3(rigBounds.center.x, bottom, rigBounds.center.z));
        Vector3 topWorld = basis.TransformPoint(new Vector3(rigBounds.center.x, top, rigBounds.center.z));
        float frameHeight = Mathf.Max(0.05f, Vector3.Distance(bottomWorld, topWorld));
        float distance = frameHeight * 0.5f / Mathf.Tan(portraitFov * 0.5f * Mathf.Deg2Rad);
        portraitCamera.farClipPlane = distance + 10f;

        Vector3 look = (bottomWorld + topWorld) * 0.5f;
        Vector3 dir = rig.rotation * (Quaternion.Euler(portraitPitch, yaw, 0f) * Vector3.forward);
        Vector3 position = look + dir * distance;
        Quaternion rotation = Quaternion.LookRotation(look - position, Vector3.up);

        Transform cam = portraitCamera.transform;
        cam.DOKill();
        portraitCamera.fieldOfView = portraitFov;
        if (duration <= 0f)
        {
            cam.SetPositionAndRotation(position, rotation);
            return;
        }

        cam.DOMove(position, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        cam.DORotateQuaternion(rotation, duration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    // ================= UI =================

    private void BuildUi()
    {
        // 어둡게 덮는 층은 대사창(650)보다 아래에 둬서 대사창이 내려가는 게 보이게 한다.
        dimCanvas = DemoUI.CreateOverlayCanvas("PauseDimCanvas", 600, transform);
        dim = DemoUI.CreatePanel("Dim", dimCanvas.transform, dimColor);
        DemoUI.Stretch((RectTransform)dim.transform);
        dimCanvas.gameObject.SetActive(false);

        // 연출 층은 PauseSystem 캔버스(1500)보다 아래(버튼이 위로 오게)
        fxCanvas = DemoUI.CreateOverlayCanvas("PauseFxCanvas", 1400, transform);
        fxRoot = DemoUI.CreateRect("Root", fxCanvas.transform);
        DemoUI.Stretch(fxRoot);

        // 대각선 배경 패널(캐릭터 뒤)
        var back = DemoUI.CreatePanel("BackPanel", fxRoot, backPanelColor, backPanelSprite);
        backPanel = (RectTransform)back.transform;
        backPanel.anchorMin = backPanel.anchorMax = new Vector2(0.5f, 0.5f);
        backPanel.pivot = new Vector2(0.5f, 0.5f);
        backPanel.sizeDelta = new Vector2(1250f, 2400f);
        backPanel.anchoredPosition = new Vector2(-520f, 0f);
        backPanel.localRotation = Quaternion.Euler(0f, 0f, backPanelAngle);

        // 흐르는 사선 패턴
        var patternGo = DemoUI.CreateRect("Pattern", backPanel);
        DemoUI.Stretch(patternGo);
        pattern = patternGo.gameObject.AddComponent<RawImage>();
        pattern.texture = patternTexture != null ? patternTexture : CreateStripeTexture();
        pattern.color = patternColor;
        pattern.uvRect = new Rect(0f, 0f, 6f, 12f);
        pattern.raycastTarget = false;

        // 슬래시(굵은 띠 + 얇은 시안 선)
        slash = CreateSlash("Slash", 230f, slashColor, slashSprite);
        slashLine = CreateSlash("SlashLine", 18f, accentColor, null);
        slashLine.anchoredPosition += new Vector2(0f, -150f);

        // 캐릭터(잔상 → 본체 → 덮개 일러스트)
        characterGhost = CreateCharacterImage("CharacterGhost", accentColor);
        character = CreateCharacterImage("Character", Color.white);
        if (characterOverlaySprite != null)
        {
            characterOverlay = DemoUI.CreatePanel("CharacterOverlay", character.transform, Color.white, characterOverlaySprite);
            characterOverlay.type = Image.Type.Simple;
            DemoUI.Stretch((RectTransform)characterOverlay.transform);
            characterOverlay.raycastTarget = false;
        }

        // 타이틀(이미지가 있으면 이미지, 없으면 글자별 텍스트)
        var titleRoot = DemoUI.CreateRect("Title", fxRoot);
        DemoUI.Place(titleRoot, new Vector2(0.5f, 0.5f), new Vector2(900f, 220f), titlePosition);
        titleRoot.localRotation = Quaternion.Euler(0f, 0f, 6f);
        if (titleSprite != null)
        {
            titleImage = DemoUI.CreatePanel("TitleImage", titleRoot, Color.white, titleSprite);
            titleImage.type = Image.Type.Simple;
            titleImage.preserveAspect = true;
            DemoUI.Stretch((RectTransform)titleImage.transform);
        }
        else
        {
            float x = -(titleText.Length - 1) * 0.5f * 150f;
            for (int i = 0; i < titleText.Length; i++)
            {
                var letter = DemoUI.CreateText("L" + i, titleRoot, titleText[i].ToString(), 190f, TextAlignmentOptions.Center, true);
                DemoUI.Place((RectTransform)letter.transform, new Vector2(0.5f, 0.5f), new Vector2(170f, 230f), new Vector2(x + i * 150f, (i % 2 == 0 ? 8f : -8f)));
                letter.color = titleColor;
                letter.outlineWidth = 0.22f;
                letter.outlineColor = new Color32(30, 0, 40, 255);
                titleLetters.Add(letter);
            }
        }

        // 화면 번쩍임
        flash = DemoUI.CreatePanel("Flash", fxRoot, new Color(1f, 1f, 1f, 0f));
        DemoUI.Stretch((RectTransform)flash.transform);

        fxCanvas.gameObject.SetActive(false);
    }

    private RectTransform CreateSlash(string name, float thickness, Color color, Sprite sprite)
    {
        var image = DemoUI.CreatePanel(name, fxRoot, color, sprite);
        var rect = (RectTransform)image.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(3200f, thickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, slashAngle);
        rect.anchoredPosition = new Vector2(-1500f, -620f);
        return rect;
    }

    private RawImage CreateCharacterImage(string name, Color color)
    {
        var rect = DemoUI.CreateRect(name, fxRoot);
        DemoUI.Place(rect, new Vector2(0.5f, 0.5f), characterSize, characterPosition);
        var image = rect.gameObject.AddComponent<RawImage>();
        image.texture = portraitTexture;
        image.color = color;
        image.enabled = portraitTexture != null;
        image.raycastTarget = false;
        return image;
    }

    private static Texture2D CreateStripeTexture()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = ((x + y) % size) < size / 3 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    private void CollectButtons()
    {
        buttons.Clear();
        buttonOrigins.Clear();

        PauseManager pm = FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
        if (pm == null) return;

        // 화면 위쪽 버튼부터(PauseButtonLayout이 원래 배치를 기억한다)
        foreach (RectTransform rect in PauseButtonLayout.Collect(pm.transform.root))
        {
            buttons.Add(rect);
            buttonOrigins.Add(PauseButtonLayout.Origin(rect));
            if (rect.GetComponent<PauseButtonFx>() == null) rect.gameObject.AddComponent<PauseButtonFx>().Bind(this);
        }
    }

    // ================= Enter =================

    public void Enter()
    {
        if (active) return;
        if (exiting) FinishVisuals();
        active = true;
        exiting = false;
        subDepth = 0;

        HidePlayer(true);
        StartRig();
        PlayEnterSequence();
    }

    private void HidePlayer(bool hide)
    {
        if (playerRenderers == null) return;

        if (hide)
        {
            playerRendererStates = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] == null) continue;
                playerRendererStates[i] = playerRenderers[i].enabled;
                playerRenderers[i].enabled = false;
            }
        }
        else if (playerRendererStates != null)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
                if (playerRenderers[i] != null) playerRenderers[i].enabled = playerRendererStates[i];
        }
    }

    private void StartRig()
    {
        if (rig == null) return;

        rig.gameObject.SetActive(true);
        portraitCamera.enabled = true;
        ApplyShot(cropBottom, portraitYaw, 0f);

        if (rigAnimator == null || playerAnimator == null) return;

        // 실제 플레이어가 추던 동작을 그대로 복사한 뒤 Pause 모션으로 넘어간다.
        rigAnimator.Rebind();
        for (int layer = 0; layer < playerAnimator.layerCount && layer < rigAnimator.layerCount; layer++)
        {
            AnimatorStateInfo info = playerAnimator.IsInTransition(layer)
                ? playerAnimator.GetNextAnimatorStateInfo(layer)
                : playerAnimator.GetCurrentAnimatorStateInfo(layer);
            float t = info.normalizedTime > 1f ? Mathf.Repeat(info.normalizedTime, 1f) : info.normalizedTime;
            rigAnimator.Play(info.fullPathHash, layer, t);
            rigAnimator.SetLayerWeight(layer, playerAnimator.GetLayerWeight(layer));
        }
        rigAnimator.speed = 1f;
        rigAnimator.Update(0f);

        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = StartCoroutine(PlayPauseMotion());
    }

    private IEnumerator PlayPauseMotion()
    {
        int enterHash = ResolveState(enterState, enterStateFallback);
        int loopHash = ResolveState(loopState, loopStateFallback);

        if (enterHash != 0)
        {
            rigAnimator.CrossFadeInFixedTime(enterHash, motionCrossFade, 0);
            yield return null;
            yield return null;

            float guard = 0f;
            while (active && guard < 5f)
            {
                AnimatorStateInfo info = rigAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.shortNameHash != enterHash && !rigAnimator.IsInTransition(0)) break;
                if (info.shortNameHash == enterHash && info.normalizedTime >= 0.98f) break;
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (active && loopHash != 0)
            rigAnimator.CrossFadeInFixedTime(loopHash, motionCrossFade, 0);

        motionRoutine = null;
    }

    private int ResolveState(string preferred, string fallback)
    {
        int hash = Animator.StringToHash(preferred);
        if (!string.IsNullOrEmpty(preferred) && rigAnimator.HasState(0, hash)) return hash;
        hash = Animator.StringToHash(fallback);
        if (!string.IsNullOrEmpty(fallback) && rigAnimator.HasState(0, hash)) return hash;
        return 0;
    }

    private void PlayEnterSequence()
    {
        sequence?.Kill();
        breathe?.Kill();

        float d = enterDuration;
        dimCanvas.gameObject.SetActive(true);
        fxCanvas.gameObject.SetActive(true);

        // 초기 상태
        dim.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);
        flash.color = new Color(1f, 1f, 1f, 0.32f);
        slash.localScale = new Vector3(0f, 1f, 1f);
        slashLine.localScale = new Vector3(0f, 1f, 1f);
        backPanel.localScale = new Vector3(0f, 1f, 1f);
        var charRect = character.rectTransform;
        var ghostRect = characterGhost.rectTransform;
        charRect.anchoredPosition = characterPosition + new Vector2(-900f, -60f);
        charRect.localScale = Vector3.one * 1.15f;
        charRect.localRotation = Quaternion.Euler(0f, 0f, characterTilt - 8f);
        ghostRect.anchoredPosition = charRect.anchoredPosition;
        ghostRect.localScale = charRect.localScale;
        ghostRect.localRotation = charRect.localRotation;
        characterGhost.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.6f);
        foreach (var letter in titleLetters)
        {
            letter.transform.localScale = Vector3.zero;
            letter.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));
        }
        if (titleImage != null) titleImage.transform.localScale = Vector3.zero;

        var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        // 0.00 정지 프레임: 약한 번쩍임 + 어둡게
        s.Insert(0f, flash.DOFade(0f, d * 0.18f));
        s.Insert(0f, dim.DOFade(dimColor.a, d * 0.24f));

        // 0.00~0.12 슬래시가 찢듯이 지나가고 시안 선이 살짝 늦게 따라온다
        s.Insert(0f, slash.DOScaleX(1f, d * 0.24f).SetEase(Ease.OutExpo));
        s.Insert(d * 0.06f, slashLine.DOScaleX(1f, d * 0.24f).SetEase(Ease.OutExpo));
        s.Insert(d * 0.04f, backPanel.DOScaleX(1f, d * 0.3f).SetEase(Ease.OutQuart));

        // 0.06~0.30 캐릭터가 밀고 들어와 튕기며 정착, 잔상은 늦게 따라오다 사라짐
        s.Insert(d * 0.12f, charRect.DOAnchorPos(characterPosition, d * 0.48f).SetEase(Ease.OutBack, 1.4f));
        s.Insert(d * 0.12f, charRect.DOScale(1f, d * 0.48f).SetEase(Ease.OutBack));
        s.Insert(d * 0.12f, charRect.DOLocalRotate(new Vector3(0f, 0f, characterTilt), d * 0.48f).SetEase(Ease.OutBack));
        s.Insert(d * 0.2f, ghostRect.DOAnchorPos(characterPosition + new Vector2(14f, -10f), d * 0.48f).SetEase(Ease.OutCubic));
        s.Insert(d * 0.2f, ghostRect.DOScale(1.03f, d * 0.48f));
        s.Insert(d * 0.2f, ghostRect.DOLocalRotate(new Vector3(0f, 0f, characterTilt), d * 0.48f));
        s.Insert(d * 0.5f, characterGhost.DOFade(0f, d * 0.4f));

        // 0.12~0.40 타이틀 글자가 차례로 튀어나온다
        for (int i = 0; i < titleLetters.Count; i++)
        {
            Transform letter = titleLetters[i].transform;
            float at = d * 0.24f + i * 0.03f;
            s.Insert(at, letter.DOScale(1f, d * 0.3f).SetEase(Ease.OutBack, 2.2f));
        }
        if (titleImage != null)
            s.Insert(d * 0.24f, titleImage.transform.DOScale(1f, d * 0.4f).SetEase(Ease.OutBack, 2f));

        // 0.20~ 버튼(임시): 가운데에서 오른쪽 제자리로, 버튼마다 다른 궤적
        if (animateButtons)
        {
            Ease[] eases = { Ease.OutBack, Ease.OutExpo, Ease.OutElastic, Ease.OutQuart };
            for (int i = 0; i < buttons.Count; i++)
            {
                RectTransform b = buttons[i];
                if (b == null) continue;
                Vector2 target = buttonOrigins[i];
                CanvasGroup group = b.GetComponent<CanvasGroup>();
                if (group != null) { group.DOKill(); group.alpha = 1f; }
                b.anchoredPosition = new Vector2(-120f, target.y * 0.2f);
                b.localScale = Vector3.zero;
                b.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 25f : -25f);
                float at = d * 0.4f + i * buttonStagger;
                s.Insert(at, b.DOAnchorPos(target, d * 0.5f).SetEase(eases[i % eases.Length]));
                s.Insert(at, b.DOScale(1f, d * 0.35f).SetEase(Ease.OutBack, 2f));
                s.Insert(at, b.DOLocalRotate(Vector3.zero, d * 0.5f).SetEase(Ease.OutBack));
            }
        }

        s.OnComplete(StartIdle);
        sequence = s;
    }

    private void StartIdle()
    {
        breathe?.Kill();
        if (!active) return;

        float beat = musicClock != null ? musicClock.BeatDurationSeconds : 0.5f;
        if (!breatheOnLastBpm) beat = 0.8f;

        // 마지막 BPM으로 배경 패널이 숨쉰다(음악은 멈췄지만 화면은 살아 있게).
        breathe = DOTween.Sequence()
            .Append(backPanel.DOScale(new Vector3(1.012f, 1.012f, 1f), beat * 0.15f).SetEase(Ease.OutQuad))
            .Append(backPanel.DOScale(Vector3.one, beat * 0.85f).SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void Update()
    {
        if (!active || pattern == null) return;

        // 사선 패턴이 천천히 흐른다(실시간)
        Rect uv = pattern.uvRect;
        uv.x += Time.unscaledDeltaTime * 0.35f;
        pattern.uvRect = uv;
    }

    // ================= Sub menu =================

    private void HandleSubPanelDepth(int depth)
    {
        if (!active) return;
        subDepth = depth;

        // 메뉴 깊이가 바뀌면 인물 구도가 바뀐다: 서브 메뉴에서는 상반신 클로즈업 + 반대편 각도
        bool deep = depth > 0;
        ApplyShot(deep ? subMenuCropBottom : cropBottom, deep ? subMenuYaw : portraitYaw, subMenuShotDuration);

        var charRect = character.rectTransform;
        charRect.DOKill(true);
        charRect.DOPunchAnchorPos(new Vector2(deep ? 30f : -30f, 0f), subMenuShotDuration, 8, 0.5f).SetUpdate(true);
        pattern.DOKill();
        pattern.DOColor(deep ? new Color(accentColor.r, accentColor.g, accentColor.b, patternColor.a * 1.6f) : patternColor, subMenuShotDuration).SetUpdate(true);
    }

    /// <summary>버튼 선택·호버 시 작은 반응(PauseButtonFx가 호출).</summary>
    public void OnButtonHighlighted(RectTransform button)
    {
        if (!active || character == null) return;

        var charRect = character.rectTransform;
        charRect.DOKill(true);
        charRect.DOPunchRotation(new Vector3(0f, 0f, 2.5f), 0.25f, 6, 0.5f).SetUpdate(true);
        backPanel.DOKill(true);
        backPanel.DOLocalRotate(new Vector3(0f, 0f, backPanelAngle + (button.anchoredPosition.y > 0f ? 2f : -2f)), 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    // ================= Exit =================

    /// <summary>Pause UI가 닫힌 직후. 연출을 빠르게 역순으로 치운다(게임 카메라는 움직이지 않았으므로 블렌드 없음).</summary>
    public void BeginExit(float _)
    {
        if (!active) return;

        sequence?.Kill();
        breathe?.Kill();
        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = null;
        exiting = true;

        float d = exitDuration;
        var charRect = character.rectTransform;
        var s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        s.Insert(0f, charRect.DOAnchorPos(characterPosition + new Vector2(-1000f, 40f), d).SetEase(Ease.InBack));
        foreach (var letter in titleLetters)
            s.Insert(0f, letter.transform.DOScale(0f, d * 0.6f).SetEase(Ease.InBack));
        if (titleImage != null) s.Insert(0f, titleImage.transform.DOScale(0f, d * 0.6f).SetEase(Ease.InBack));
        s.Insert(d * 0.1f, slash.DOScaleX(0f, d * 0.7f).SetEase(Ease.InExpo));
        s.Insert(d * 0.1f, slashLine.DOScaleX(0f, d * 0.6f).SetEase(Ease.InExpo));
        s.Insert(d * 0.2f, backPanel.DOScaleX(0f, d * 0.7f).SetEase(Ease.InQuart));
        s.Insert(d * 0.3f, dim.DOFade(0f, d * 0.7f));
        s.OnComplete(FinishVisuals);
        sequence = s;
    }

    private void FinishVisuals()
    {
        exiting = false;
        if (fxCanvas != null) fxCanvas.gameObject.SetActive(false);
        if (dimCanvas != null) dimCanvas.gameObject.SetActive(false);
        if (portraitCamera != null) portraitCamera.enabled = false;
        if (rig != null) rig.gameObject.SetActive(false);
        HidePlayer(false);
        RestoreButtons();
    }

    private void RestoreButtons()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].DOKill();
            buttons[i].anchoredPosition = buttonOrigins[i];
            buttons[i].localScale = Vector3.one;
            buttons[i].localRotation = Quaternion.identity;
        }
    }

    /// <summary>게임 재개 직전. 퇴장 연출이 덜 끝났어도 모든 상태를 정리한다(여러 번 불러도 안전).</summary>
    public void Complete()
    {
        if (!active) return;
        active = false;

        // 퇴장 연출이 진행 중이면(대사 중 바로 복귀 등) 끝까지 보여주고 OnComplete에서 정리한다.
        if (exiting) return;

        sequence?.Kill();
        breathe?.Kill();
        if (motionRoutine != null) StopCoroutine(motionRoutine);
        motionRoutine = null;
        FinishVisuals();
    }

    private void OnDisable()
    {
        sequence?.Kill();
        breathe?.Kill();
        if (active || exiting)
        {
            active = false;
            FinishVisuals();
        }
    }
}
