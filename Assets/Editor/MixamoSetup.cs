#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Mixamo 캐릭터를 Player로 교체하는 자동 설정 도구
///
/// 사용법:
///   1. mixamo.com 접속 → 원하는 캐릭터 선택
///   2. Animations 탭 → "T-pose" 검색 → Download
///      Format: FBX for Unity  /  Pose: T-pose  /  Skin: With Skin
///   3. 다운받은 FBX → Assets/Models/ 폴더에 넣기
///   4. 메뉴: Exit03 > Apply Mixamo Character
/// </summary>
public static class MixamoSetup
{
    [MenuItem("Exit03/Apply Mixamo Character")]
    public static void ApplyCharacter()
    {
        // Assets/Models/ 에서 FBX 자동 탐색
        var guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Models" });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Mixamo 캐릭터 없음",
                "Assets/Models/ 폴더에 FBX 파일이 없습니다.\n\n" +
                "1. mixamo.com → 캐릭터 선택\n" +
                "2. Download → FBX for Unity / T-pose / With Skin\n" +
                "3. 다운받은 .fbx 파일을 Assets/Models/ 폴더에 넣기\n" +
                "4. 이 메뉴를 다시 실행", "확인");
            return;
        }

        // 첫 번째 FBX 사용
        string modelPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (prefab == null) { Debug.LogError("[Exit03] FBX 로드 실패: " + modelPath); return; }

        // FBX 임포트 설정 최적화
        OptimizeFBXImport(modelPath);

        // AnimatorController 생성
        var controller = CreateAnimatorController();

        // GameScene Player 교체
        ReplacePlayerInScene(prefab, controller);

        Debug.Log($"[Exit03] Mixamo 캐릭터 적용 완료: {prefab.name}");
        EditorUtility.DisplayDialog("캐릭터 교체 완료",
            $"{prefab.name} 캐릭터가 적용됐습니다!\nPlay 버튼을 눌러 확인하세요.", "확인");
    }

    // ─── FBX 임포트 최적화 ────────────────────────────────────────────────────

    static void OptimizeFBXImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        // 리그 설정 (Humanoid → Mixamo 표준 리그)
        importer.animationType = ModelImporterAnimationType.Human;

        // 메시 최적화
        importer.meshCompression       = ModelImporterMeshCompression.Low;
        importer.isReadable            = false;
        importer.optimizeMeshPolygons  = true;
        importer.optimizeMeshVertices  = true;

        // 머티리얼 임포트
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation    = ModelImporterMaterialLocation.InPrefab;

        importer.SaveAndReimport();
    }

    // ─── AnimatorController 생성 ─────────────────────────────────────────────

    static AnimatorController CreateAnimatorController()
    {
        const string controllerPath = "Assets/Animations/PlayerAnimator.controller";
        System.IO.Directory.CreateDirectory("Assets/Animations");

        // 이미 있으면 재사용
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null) return existing;

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // 파라미터: Speed (이동속도), Dead (사망)
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Dead",  AnimatorControllerParameterType.Bool);

        var root = controller.layers[0].stateMachine;

        // Idle 상태
        var idle = root.AddState("Idle");
        idle.motion = null;   // 나중에 애니메이션 클립 연결 가능
        root.defaultState = idle;

        // Walk 상태
        var walk = root.AddState("Walk");
        walk.motion = null;

        // Dead 상태
        var dead = root.AddState("Dead");
        dead.motion = null;

        // 트랜지션: Idle → Walk (Speed > 0.1)
        var idleToWalk = idle.AddTransition(walk);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime  = false;
        idleToWalk.duration     = 0.1f;

        // 트랜지션: Walk → Idle (Speed < 0.1)
        var walkToIdle = walk.AddTransition(idle);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime  = false;
        walkToIdle.duration     = 0.1f;

        // 트랜지션: Any → Dead
        var toDead = root.AddAnyStateTransition(dead);
        toDead.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        toDead.hasExitTime = false;
        toDead.duration    = 0.2f;

        AssetDatabase.SaveAssets();
        return controller;
    }

    // ─── GameScene Player 오브젝트 교체 ──────────────────────────────────────

    static void ReplacePlayerInScene(GameObject characterPrefab, AnimatorController controller)
    {
        const string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        // 기존 Player 찾기
        GameObject oldPlayer = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Player") { oldPlayer = root; break; }

        Vector3 pos = oldPlayer != null ? oldPlayer.transform.position : new Vector3(-9, 0, -9);

        // 기존 Player 삭제
        if (oldPlayer != null) Object.DestroyImmediate(oldPlayer);

        // 새 캐릭터 인스턴스 생성
        var newPlayer = Object.Instantiate(characterPrefab);
        newPlayer.name = "Player";
        newPlayer.tag  = "Player";
        newPlayer.transform.position = pos;
        newPlayer.transform.rotation = Quaternion.identity;

        // 크기 자동 조정 (Mixamo 캐릭터는 보통 1.8m 정도)
        var bounds = GetModelBounds(newPlayer);
        float scale = bounds.size.y > 0 ? 1.8f / bounds.size.y : 1f;
        newPlayer.transform.localScale = Vector3.one * scale;

        // CharacterController 추가
        var cc = newPlayer.GetComponent<CharacterController>();
        if (cc == null) cc = newPlayer.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.3f;
        cc.height = 1.8f;

        // PlayerController 추가 (기존 스크립트)
        if (newPlayer.GetComponent<PlayerController>() == null)
            newPlayer.AddComponent<PlayerController>();

        // Animator 설정
        var animator = newPlayer.GetComponent<Animator>();
        if (animator == null) animator = newPlayer.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        // CameraFollow 타겟 업데이트
        var camFollow = Object.FindFirstObjectByType<CameraFollow>();
        if (camFollow != null) camFollow.target = newPlayer.transform;

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Exit03] Player 교체 완료: {characterPrefab.name} (scale={scale:F2})");
    }

    static Bounds GetModelBounds(GameObject go)
    {
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);
        return bounds;
    }
}
#endif
