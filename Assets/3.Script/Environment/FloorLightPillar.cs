using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 댄스 플로어 구역의 빛기둥. 구역 메시(쪼개진 바닥 모양)의 외곽선을 위로 밀어 올린 벽을
/// 가산 셰이더(Solve/LightPillar)로 그린다. FloorSection이 자동으로 붙인다.
/// - Rise: 구역이 켜질 때 바닥에서 위로 '팡' 솟구치고, 꺼질 때까지 유지(흐르는 줄무늬)
/// - Fall: 마디가 끝나 꺼질 때 사라진다. 콤보 성공이면 한 번 더 높이 번쩍인 뒤 사라진다
/// 바닥이 회전(사각형 ↔ 마름모)해도 구역의 자식이라 같이 돈다.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class FloorLightPillar : MonoBehaviour
{
    [Header("Shape")]
    [Tooltip("기둥 높이 = 구역 폭 × 이 값")]
    [SerializeField, Min(0.1f)] private float heightMultiplier = 3.2f;
    [Tooltip("구역 경계보다 살짝 안쪽(이웃 구역 기둥과 겹치지 않게, 쪼개진 느낌)")]
    [SerializeField, Range(0.5f, 1f)] private float inset = 0.92f;

    [Header("Look")]
    [SerializeField, Min(0f)] private float intensity = 1.6f;
    [SerializeField, Min(0f)] private float burstIntensity = 3.6f;
    [Tooltip("페이즈 색을 기둥 색으로 쓸 때 밝기를 맞추기 위해 가장 밝은 채널을 1로 정규화")]
    [SerializeField] private bool normalizeColor = true;

    [Header("Timing (초)")]
    [SerializeField, Min(0.01f)] private float riseDuration = 0.14f;
    [SerializeField, Min(0.01f)] private float fallDuration = 0.18f;
    [SerializeField, Range(0f, 1f)] private float successExtraRise = 0.3f;

    private static Shader shader;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int RiseId = Shader.PropertyToID("_Rise");

    private MeshRenderer pillarRenderer;
    private Material material;
    private Sequence sequence;
    private bool built;
    private bool active;

    public bool IsActive => active;

    private void Awake() => Build();

    private void OnDisable() => Hide();

    private void OnDestroy()
    {
        sequence?.Kill();
        if (material != null) Destroy(material);
    }

    // ================= Control =================

    /// <summary>구역이 켜질 때. 처음이면 솟구치고, 이미 켜져 있으면 짧게 맥동한다.</summary>
    public void Rise(Color color)
    {
        if (!Build()) return;

        SetColor(color);
        sequence?.Kill();
        pillarRenderer.enabled = true;

        if (!active)
        {
            active = true;
            material.SetFloat(RiseId, 0f);
            material.SetFloat(IntensityId, burstIntensity);
            sequence = DOTween.Sequence().SetLink(gameObject)
                .Append(DOTween.To(() => material.GetFloat(RiseId), v => material.SetFloat(RiseId, v), 1f, riseDuration).SetEase(Ease.OutBack, 2.2f))
                .Join(DOTween.To(() => material.GetFloat(IntensityId), v => material.SetFloat(IntensityId, v), intensity, riseDuration * 2.2f).SetEase(Ease.OutCubic));
        }
        else
        {
            // 같은 마디에서 다시 눌리면 살짝 다시 밝아지고 높아진다
            sequence = DOTween.Sequence().SetLink(gameObject)
                .Append(DOTween.To(() => material.GetFloat(IntensityId), v => material.SetFloat(IntensityId, v), burstIntensity * 0.8f, 0.04f))
                .Join(DOTween.To(() => material.GetFloat(RiseId), v => material.SetFloat(RiseId, v), 1.1f, 0.06f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => material.GetFloat(IntensityId), v => material.SetFloat(IntensityId, v), intensity, 0.2f))
                .Join(DOTween.To(() => material.GetFloat(RiseId), v => material.SetFloat(RiseId, v), 1f, 0.2f).SetEase(Ease.OutCubic));
        }
    }

    /// <summary>구역이 꺼질 때(마디 끝). 콤보 성공이면 한 번 더 높이 번쩍인 뒤 사라진다.</summary>
    public void Fall(bool success)
    {
        if (!active || material == null) return;
        active = false;
        sequence?.Kill();

        var s = DOTween.Sequence().SetLink(gameObject);
        if (success)
        {
            s.Append(DOTween.To(() => material.GetFloat(RiseId), v => material.SetFloat(RiseId, v), 1f + successExtraRise, 0.06f).SetEase(Ease.OutQuad))
             .Join(DOTween.To(() => material.GetFloat(IntensityId), v => material.SetFloat(IntensityId, v), burstIntensity, 0.06f));
        }
        s.Append(DOTween.To(() => material.GetFloat(IntensityId), v => material.SetFloat(IntensityId, v), 0f, fallDuration).SetEase(Ease.InQuad))
         .OnComplete(() => { if (pillarRenderer != null) pillarRenderer.enabled = false; });
        sequence = s;
    }

    public void Hide()
    {
        active = false;
        sequence?.Kill();
        if (pillarRenderer != null) pillarRenderer.enabled = false;
    }

    public void SetColor(Color color)
    {
        if (material == null) return;
        if (normalizeColor)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (max > 0.0001f) color = new Color(color.r / max, color.g / max, color.b / max, 1f);
        }
        material.SetColor(ColorId, color);
    }

    // ================= Build =================

    private bool Build()
    {
        if (built) return pillarRenderer != null;
        built = true;

        if (shader == null) shader = Resources.Load<Shader>("Shaders/SolveLightPillar");
        if (shader == null) shader = Shader.Find("Solve/LightPillar");
        Mesh source = GetComponent<MeshFilter>().sharedMesh;
        if (shader == null || source == null || !source.isReadable) return false;

        Mesh mesh = BuildPillarMesh(source);
        if (mesh == null) return false;

        var go = new GameObject("LightPillar");
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        pillarRenderer = go.AddComponent<MeshRenderer>();
        pillarRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pillarRenderer.receiveShadows = false;
        material = new Material(shader);
        pillarRenderer.sharedMaterial = material;
        pillarRenderer.enabled = false;
        return true;
    }

    /// <summary>원본 메시의 외곽 모서리(삼각형 하나에만 속한 변)를 위로 밀어 올려 벽을 만든다.</summary>
    private Mesh BuildPillarMesh(Mesh source)
    {
        Vector3[] verts = source.vertices;
        int[] tris = source.triangles;
        if (verts.Length == 0 || tris.Length == 0) return null;

        // 외곽 모서리 찾기
        var edgeCount = new Dictionary<(int, int), int>();
        var edgeDir = new Dictionary<(int, int), (int, int)>();
        for (int i = 0; i < tris.Length; i += 3)
        {
            for (int k = 0; k < 3; k++)
            {
                int a = tris[i + k], b = tris[i + (k + 1) % 3];
                var key = a < b ? (a, b) : (b, a);
                edgeCount[key] = edgeCount.TryGetValue(key, out int c) ? c + 1 : 1;
                edgeDir[key] = (a, b);
            }
        }

        // 중심(안쪽으로 줄이기용)
        Vector3 center = Vector3.zero;
        foreach (Vector3 v in verts) center += v;
        center /= verts.Length;

        // 구역 폭(월드)과 위 방향(로컬)
        Vector3 wMin = Vector3.positiveInfinity, wMax = Vector3.negativeInfinity;
        foreach (Vector3 v in verts)
        {
            Vector3 w = transform.TransformPoint(v);
            wMin = Vector3.Min(wMin, w);
            wMax = Vector3.Max(wMax, w);
        }
        float footprint = Mathf.Max(wMax.x - wMin.x, wMax.z - wMin.z);
        Vector3 localUp = transform.InverseTransformVector(Vector3.up * footprint * heightMultiplier);

        var outVerts = new List<Vector3>();
        var outUv = new List<Vector2>();
        var outTris = new List<int>();

        // 둘레 길이(줄무늬 UV용)
        float perimeter = 0f;
        foreach (var pair in edgeCount)
            if (pair.Value == 1) perimeter += Vector3.Distance(verts[pair.Key.Item1], verts[pair.Key.Item2]);
        perimeter = Mathf.Max(0.0001f, perimeter);

        float u = 0f;
        foreach (var pair in edgeCount)
        {
            if (pair.Value != 1) continue;
            (int a, int b) = edgeDir[pair.Key];
            Vector3 p0 = center + (verts[a] - center) * inset;
            Vector3 p1 = center + (verts[b] - center) * inset;
            float u1 = u + Vector3.Distance(verts[a], verts[b]) / perimeter;

            int start = outVerts.Count;
            outVerts.Add(p0); outUv.Add(new Vector2(u, 0f));
            outVerts.Add(p1); outUv.Add(new Vector2(u1, 0f));
            outVerts.Add(p1 + localUp); outUv.Add(new Vector2(u1, 1f));
            outVerts.Add(p0 + localUp); outUv.Add(new Vector2(u, 1f));
            outTris.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            u = u1;
        }

        if (outVerts.Count == 0) return null;

        var mesh = new Mesh { name = "LightPillar" };
        mesh.SetVertices(outVerts);
        mesh.SetUVs(0, outUv);
        mesh.SetTriangles(outTris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
