using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 데모 흐름 설치 메뉴. Unity 상단 메뉴 Solve/Demo.
/// 1) 메인(DanceScene_1)·보스(DanceScene_Rival) 씬에 흐름 컴포넌트와 Pause 시스템을 설치
/// 2) 타이틀·결과 씬을 새로 만들고, 메인 씬을 복사해 튜토리얼 씬을 만든다
/// 3) Build Settings에 Title → Tutorial → Main → Boss → Result 순서로 등록
/// 이미 설치된 항목은 건너뛰므로 여러 번 실행해도 된다.
/// </summary>
public static class DemoSetupMenu
{
    private const string DemoSceneFolder = "Assets/1.Scene/Demo";
    private const string PauseSystemPrefab = "Assets/2.Model/Prefabs/PauseSystem.prefab";
    private const string FlowObjectName = "DemoFlow";

    [MenuItem("Solve/Demo/Setup All (전체 설치)", priority = 0)]
    public static void SetupAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string previous = SceneManager.GetActiveScene().path;

        InstallInScene(MainScenePath(), InstallMain);
        InstallInScene(BossScenePath(), InstallBoss);
        CreateTitleScene();
        CreateResultScene();
        CreateTutorialScene();
        RegisterBuildSettings();

        if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
            EditorSceneManager.OpenScene(previous);

        EditorUtility.DisplayDialog("Solve Demo", "데모 흐름 설치가 끝났습니다.\nBuild Settings의 첫 씬(DemoTitle)부터 플레이하세요.", "확인");
    }

    [MenuItem("Solve/Demo/Install Main Flow In Open Scene")]
    public static void InstallMainInOpenScene() => InstallMain(SceneManager.GetActiveScene());

    [MenuItem("Solve/Demo/Install Boss Flow In Open Scene")]
    public static void InstallBossInOpenScene() => InstallBoss(SceneManager.GetActiveScene());

    [MenuItem("Solve/Demo/Register Demo Scenes In Build Settings")]
    public static void RegisterBuildSettings()
    {
        DemoUISettings s = LoadSettings();
        var ordered = new List<string>
        {
            $"{DemoSceneFolder}/{s.titleScene}.unity",
            $"{DemoSceneFolder}/{s.tutorialScene}.unity",
            MainScenePath(),
            BossScenePath(),
            $"{DemoSceneFolder}/{s.resultScene}.unity",
        }.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();

        var scenes = ordered.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
        foreach (var existing in EditorBuildSettings.scenes)
            if (!ordered.Contains(existing.path))
                scenes.Add(existing);

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[Solve Demo] Build Settings: " + string.Join(" → ", ordered.Select(Path.GetFileNameWithoutExtension)));
    }

    // ---------------- Scene install ----------------

    private static void InstallInScene(string path, System.Action<Scene> install)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[Solve Demo] Scene not found; skipped.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        install(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void InstallMain(Scene scene)
    {
        GameObject flow = GetOrCreateRoot(scene, FlowObjectName);
        AddIfMissing<CountDownController>(flow);
        AddIfMissing<MainGameFlow>(flow);
        AddIfMissing<NormalBattleDirector>(flow);
        InstallPause(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[Solve Demo] Main flow installed in {scene.name}");
    }

    private static void InstallBoss(Scene scene)
    {
        GameObject flow = GetOrCreateRoot(scene, FlowObjectName);
        AddIfMissing<CountDownController>(flow);
        AddIfMissing<BossBattleDirector>(flow);
        InstallPause(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[Solve Demo] Boss flow installed in {scene.name}");
    }

    private static void InstallPause(Scene scene)
    {
        PauseManager pause = FindInScene<PauseManager>(scene);
        if (pause == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PauseSystemPrefab);
            if (prefab == null)
            {
                Debug.LogWarning($"[Solve Demo] {PauseSystemPrefab} not found; pause not installed.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            pause = instance.GetComponentInChildren<PauseManager>(true);
        }

        DancePauseHandler handler = FindInScene<DancePauseHandler>(scene);
        if (handler == null)
        {
            var go = new GameObject("DancePauseHandler");
            SceneManager.MoveGameObjectToScene(go, scene);
            handler = go.AddComponent<DancePauseHandler>();
        }

        if (pause != null)
        {
            var so = new SerializedObject(pause);
            SerializedProperty prop = so.FindProperty("sceneHandlerObject");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = handler;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject root = pause.transform.root.gameObject;
            AddIfMissing<PauseMenuButtons>(root);
        }

        if (FindInScene<UnityEngine.EventSystems.EventSystem>(scene) == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(es, scene);
        }
    }

    // ---------------- New scenes ----------------

    private static void CreateTitleScene() => CreateSimpleScene(LoadSettings().titleScene, typeof(TitleController));
    private static void CreateResultScene() => CreateSimpleScene(LoadSettings().resultScene, typeof(ResultController));

    private static void CreateSimpleScene(string sceneName, System.Type controller)
    {
        string path = $"{DemoSceneFolder}/{sceneName}.unity";
        if (File.Exists(path)) return;

        EnsureFolder(DemoSceneFolder);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.01f, 0.07f);
        }

        var go = new GameObject(controller.Name);
        go.AddComponent(controller);

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[Solve Demo] Created {path}");
    }

    private static void CreateTutorialScene()
    {
        DemoUISettings s = LoadSettings();
        string path = $"{DemoSceneFolder}/{s.tutorialScene}.unity";
        if (File.Exists(path)) return;

        string main = MainScenePath();
        if (string.IsNullOrEmpty(main)) return;

        EnsureFolder(DemoSceneFolder);
        if (!AssetDatabase.CopyAsset(main, path))
        {
            Debug.LogError($"[Solve Demo] Failed to copy {main} → {path}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        foreach (var flow in FindAllInScene<MainGameFlow>(scene)) Object.DestroyImmediate(flow);
        foreach (var director in FindAllInScene<NormalBattleDirector>(scene)) Object.DestroyImmediate(director);

        GameObject root = GetOrCreateRoot(scene, FlowObjectName);
        AddIfMissing<CountDownController>(root);
        AddIfMissing<TutorialDirector>(root);
        InstallPause(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Solve Demo] Created {path} (튜토리얼 BGM은 FmodMusicClock의 Music Event Path에서 지정)");
    }

    // ---------------- Helpers ----------------

    private static DemoUISettings LoadSettings()
    {
        var settings = Resources.Load<DemoUISettings>("DemoUISettings");
        if (settings != null) return settings;

        EnsureFolder("Assets/Resources");
        settings = ScriptableObject.CreateInstance<DemoUISettings>();
        AssetDatabase.CreateAsset(settings, "Assets/Resources/DemoUISettings.asset");
        AssetDatabase.SaveAssets();
        return settings;
    }

    private static string MainScenePath() => FindScenePath(LoadSettings().mainScene);
    private static string BossScenePath() => FindScenePath(LoadSettings().bossScene);

    private static string FindScenePath(string sceneName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:Scene {sceneName}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName && !path.Contains("_Recovery"))
                return path;
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static GameObject GetOrCreateRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;

        var created = new GameObject(name);
        SceneManager.MoveGameObjectToScene(created, scene);
        return created;
    }

    private static T AddIfMissing<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    private static T FindInScene<T>(Scene scene) where T : Component => FindAllInScene<T>(scene).FirstOrDefault();

    private static IEnumerable<T> FindAllInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (T c in root.GetComponentsInChildren<T>(true))
                yield return c;
    }
}
