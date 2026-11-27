using UnityEngine;

/// <summary>
/// 기존 MetaBall 액체 연출 재사용.
/// 말풍선 프리팹(Bubble)의 액체 파티클(LiquidParticle 레이어)을 복제해 원하는 화면 위치에서 터뜨린다.
/// Particle Camera가 이 파티클을 Tex_MetaBall에 그리고, MasterCanvas의 EffectFilter(M_MetaBall)가
/// R/G 채널을 임계값으로 잘라 끈적한 액체처럼 보이게 한다(초록 = 부정 말풍선 색).
/// </summary>
public static class LiquidBurst
{
    private static GameObject template;
    private static Camera particleCamera;

    public static readonly Color NegativeChannel = Color.green;
    public static readonly Color PositiveChannel = Color.red;

    /// <param name="screenPosition">화면 좌표(픽셀)</param>
    /// <param name="depth">카메라에서의 거리(월드). 기존 말풍선과 비슷한 크기가 되도록 객석 거리 정도</param>
    public static bool Spawn(Vector2 screenPosition, float depth, Color channel, float sizeScale = 1.6f, int count = 26)
    {
        if (!Resolve()) return false;

        Vector3 viewport = new Vector3(screenPosition.x / Mathf.Max(1, Screen.width), screenPosition.y / Mathf.Max(1, Screen.height), depth);
        Vector3 world = particleCamera.ViewportToWorldPoint(viewport);

        GameObject go = Object.Instantiate(template, world, Quaternion.identity);
        go.SetActive(true);
        var ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Object.Destroy(go);
            return false;
        }

        var main = ps.main;
        main.startColor = channel;
        main.startSizeMultiplier *= sizeScale;
        main.startSpeedMultiplier *= 1.2f;
        var emission = ps.emission;
        emission.enabled = false;   // 지속 방출 없이 한 번만 터뜨린다
        ps.Play();
        ps.Emit(count);

        Object.Destroy(go, main.duration + main.startLifetime.constantMax + 0.5f);
        return true;
    }

    private static bool Resolve()
    {
        if (template == null)
        {
            BubbleManager manager = Object.FindFirstObjectByType<BubbleManager>(FindObjectsInactive.Include);
            Bubble prefab = manager != null ? manager.BubblePrefab : null;
            ParticleSystem source = prefab != null ? prefab.GetComponentInChildren<ParticleSystem>(true) : null;
            if (source != null) template = source.gameObject;
        }

        if (particleCamera == null)
        {
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam.targetTexture != null && cam.targetTexture.name.Contains("MetaBall"))
                {
                    particleCamera = cam;
                    break;
                }
            }
            if (particleCamera == null) particleCamera = Camera.main;
        }

        return template != null && particleCamera != null;
    }
}
