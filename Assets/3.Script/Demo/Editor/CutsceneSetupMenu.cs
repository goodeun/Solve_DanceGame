#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// 메뉴: Solve/Demo/Create Boss Intro Cutscene
/// 지금 열려 있는 씬(DemoBoss)에 보스 등장 컷씬의 기본 틀을 만든다.
///   BossIntroCutscene (PlayableDirector + TimelineCutscene)
///     ├ Cameras / Cam1_Wide, Cam2_Close   (컷씬 카메라, Timeline이 켜고 끈다)
///     ├ RivalPath / P0, P1, P2           (보스가 걸어갈 경로 점)
///     ├ LookTarget                       (도착 후 바라볼 곳 = 플레이어)
///     └ WalkingRival                     (Rival 프리펩, 컷씬 동안만 보임)
///   Timeline 에셋: Assets/5.Animation/Cutscene/BossIntro.playable
/// 그리고 BossBattleDirector의 Intro Cutscene에 연결한다. 이후 작업 방법은 CUTSCENE_GUIDE.md 참고.
/// </summary>
public static class CutsceneSetupMenu
{
    private const string TimelinePath = "Assets/5.Animation/Cutscene/BossIntro.playable";
    private const string RivalPrefabPath = "Assets/2.Model/Prefabs/Rival.prefab";
    private const string WalkClipPath = "Assets/5.Animation/Cutscene/Rival_Walk_InPlace.anim";
    private const string ReadyClipPath = "Assets/5.Animation/Rival/Rival_Ready.anim";

    [MenuItem("Solve/Demo/Create Boss Intro Cutscene")]
    private static void Create()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("컷씬 틀 만들기", "플레이 모드를 끄고 실행해 주세요.", "확인");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.name.Contains("Boss") &&
            !EditorUtility.DisplayDialog("컷씬 틀 만들기", $"지금 열린 씬은 '{scene.name}'입니다. 보스 씬(DemoBoss)이 아닌데 계속할까요?", "계속"))
            return;

        TimelineCutscene existing = Object.FindFirstObjectByType<TimelineCutscene>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeObject = existing.gameObject;
            EditorUtility.DisplayDialog("컷씬 틀 만들기", "이미 컷씬(TimelineCutscene)이 있습니다. 선택해 두었어요.", "확인");
            return;
        }

        // ---------- 오브젝트 ----------
        var root = new GameObject("BossIntroCutscene");
        Undo.RegisterCreatedObjectUndo(root, "Create Boss Intro Cutscene");
        var director = root.AddComponent<PlayableDirector>();
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.Hold;
        var cutscene = root.AddComponent<TimelineCutscene>();

        // 경로(플레이어 무대 뒤에서 앞으로). 씬에서 점을 옮겨 조정한다.
        float floorY = -1.4f;
        Transform pathRoot = Child("RivalPath", root.transform, Vector3.zero);
        Transform p0 = Child("P0_Start", pathRoot, new Vector3(0f, floorY, 16f));
        Transform p1 = Child("P1", pathRoot, new Vector3(1.5f, floorY, 10f));
        Transform p2 = Child("P2_End", pathRoot, new Vector3(0f, floorY, 6f));
        Transform lookTarget = Child("LookTarget", root.transform, new Vector3(0f, floorY, 0f));

        // 컷씬 카메라(꺼 둔 채로 두고 Timeline Activation 트랙이 켠다. 켜지면 우선순위가 높아 게임 카메라를 대신한다)
        Transform cameras = Child("Cameras", root.transform, Vector3.zero);
        CinemachineCamera wide = Camera("Cam1_Wide", cameras, new Vector3(0f, 4f, -4f), p1.position + Vector3.up * 2.5f, 45f);
        CinemachineCamera close = Camera("Cam2_Close", cameras, p2.position + new Vector3(2f, 3f, -6f), p2.position + Vector3.up * 3f, 32f);

        // 걸어나오는 보스(Rival 프리펩)
        GameObject rival = null;
        var rivalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RivalPrefabPath);
        if (rivalPrefab != null)
        {
            rival = (GameObject)PrefabUtility.InstantiatePrefab(rivalPrefab, scene);
            rival.name = "WalkingRival";
            rival.transform.SetParent(root.transform, true);
            rival.transform.position = p0.position;
            rival.transform.rotation = Quaternion.LookRotation(p1.position - p0.position);
        }

        // ---------- Timeline ----------
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, AssetDatabase.GenerateUniqueAssetPath(TimelinePath));

        var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
        var ready = AssetDatabase.LoadAssetAtPath<AnimationClip>(ReadyClipPath);

        AnimationTrack animTrack = timeline.CreateTrack<AnimationTrack>(null, "보스 애니메이션");
        animTrack.trackOffset = TrackOffset.ApplySceneOffsets;
        if (walk != null)
        {
            TimelineClip c = animTrack.CreateClip(walk);
            c.start = 0;
            c.duration = 4;
            c.displayName = "걷기";
        }
        if (ready != null)
        {
            TimelineClip c = animTrack.CreateClip(ready);
            c.start = 4;
            c.duration = 2;
            c.displayName = "멈춰 서기";
        }

        ActivationTrack cam1Track = timeline.CreateTrack<ActivationTrack>(null, "카메라1 와이드");
        TimelineClip cam1Clip = cam1Track.CreateDefaultClip();
        cam1Clip.start = 0;
        cam1Clip.duration = 3.5;

        ActivationTrack cam2Track = timeline.CreateTrack<ActivationTrack>(null, "카메라2 클로즈업");
        TimelineClip cam2Clip = cam2Track.CreateDefaultClip();
        cam2Clip.start = 3.5;
        cam2Clip.duration = 2.5;

        director.playableAsset = timeline;
        if (rival != null) director.SetGenericBinding(animTrack, rival.GetComponent<Animator>());
        director.SetGenericBinding(cam1Track, wide.gameObject);
        director.SetGenericBinding(cam2Track, close.gameObject);
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();

        // ---------- 재생기 설정 ----------
        var onlyObjects = new List<GameObject> { cameras.gameObject };
        if (rival != null) onlyObjects.Add(rival);

        var moves = new List<TimelineCutscene.PathMove>();
        if (rival != null)
        {
            moves.Add(new TimelineCutscene.PathMove
            {
                memo = "보스가 걸어나온다",
                target = rival.transform,
                points = new[] { p0, p1, p2 },
                startTime = 0f,
                duration = 4f,
                faceMoveDirection = true,
                lookAtWhenArrived = lookTarget,
            });
        }

        var cues = new List<TimelineCutscene.DialogueCue>
        {
            new TimelineCutscene.DialogueCue
            {
                memo = "도착해서 한마디(필요 없으면 지우기)",
                time = 5f,
                lines = StoryDialogueLine.List(StoryDialogueLine.Npc("라이벌", "여기가 오늘 내 무대인가?")),
            },
        };

        cutscene.EditorSetup(director, onlyObjects, moves, cues);
        EditorUtility.SetDirty(cutscene);

        // 에디터에서는 켜 두어 Timeline 미리보기로 볼 수 있게 한다. 플레이하면 TimelineCutscene이 시작 시 끄고, 재생할 때 켠다.

        // ---------- 보스 흐름에 연결 ----------
        BossBattleDirector boss = Object.FindFirstObjectByType<BossBattleDirector>(FindObjectsInactive.Include);
        if (boss != null)
        {
            Undo.RecordObject(boss, "Link Boss Intro Cutscene");
            boss.EditorSetIntroCutscene(cutscene);
            EditorUtility.SetDirty(boss);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("컷씬 틀 만들기",
            "BossIntroCutscene을 만들었어요.\n\n" +
            "Window > Sequencing > Timeline 창을 열고 BossIntroCutscene을 선택하면 편집할 수 있어요.\n" +
            (boss != null ? "BossBattleDirector에 연결했어요." : "BossBattleDirector를 찾지 못했어요. Intro Cutscene에 직접 연결해 주세요.") +
            "\n\n씬을 저장(Ctrl+S)하는 것 잊지 마세요!", "확인");
    }

    private static Transform Child(string name, Transform parent, Vector3 worldPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPosition;
        return go.transform;
    }

    private static CinemachineCamera Camera(string name, Transform parent, Vector3 position, Vector3 lookAt, float fov)
    {
        Transform t = Child(name, parent, position);
        t.rotation = Quaternion.LookRotation(lookAt - position);
        var cam = t.gameObject.AddComponent<CinemachineCamera>();
        cam.Priority = 100;
        LensSettings lens = cam.Lens;
        lens.FieldOfView = fov;
        cam.Lens = lens;
        t.gameObject.SetActive(false);
        return cam;
    }
}
#endif
