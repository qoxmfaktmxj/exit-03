#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exit03 자동 씬 생성기
/// GameScene.unity가 없으면 프로젝트 오픈 시 자동 실행됩니다.
/// 수동 실행: 메뉴 Exit03 > Setup All Scenes
/// </summary>
[InitializeOnLoad]
public static class AutoSetup
{
    static AutoSetup()
    {
        if (!File.Exists("Assets/Scenes/GameScene.unity"))
            EditorApplication.delayCall += RunSetup;
    }

    [MenuItem("Exit03/Setup All Scenes")]
    public static void RunSetup()
    {
        ImportTMPIfNeeded();
        AddTag("Player");
        AddTag("Exit");

        Directory.CreateDirectory("Assets/Scenes");
        BuildTitleScene();
        BuildGameScene();
        RegisterBuildScenes();

        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");

        Debug.Log("[Exit03] Setup 완료! ▶ Play 버튼을 눌러 시작하세요.");
        EditorUtility.DisplayDialog("Exit03 Setup 완료",
            "씬 생성 완료!\n\n▶ Play 버튼을 눌러 게임을 시작하세요.", "확인");
    }

    // ──────────────────────────────── TMP ────────────────────────────────────

    static void ImportTMPIfNeeded()
    {
        if (Directory.Exists("Assets/TextMesh Pro")) return;
        try { TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false); }
        catch { /* Unity 6: com.unity.ugui에 포함되어 있으면 자동 처리됨 */ }
    }

    // ──────────────────────────────── Tags ───────────────────────────────────

    static void AddTag(string tag)
    {
        const string path = "ProjectSettings/TagManager.asset";
        if (!File.Exists(path)) return;
        var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(path));
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.arraySize++;
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }

    // ─────────────────────────── Title Scene ─────────────────────────────────

    static void BuildTitleScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SpawnLight(Quaternion.Euler(50, -30, 0));

        var sl = new GameObject("SceneLoader").AddComponent<SceneLoader>();

        var canvas = MakeCanvas();

        var bg = MakeImage(canvas.transform, "Background", new Color(0.04f, 0.04f, 0.10f));
        StretchFull(bg);

        MakeTMP(bg.transform, "TitleText", "EXIT 03",
            new Vector2(0, 160), new Vector2(700, 140), 90, Color.cyan, FontStyles.Bold);
        MakeTMP(bg.transform, "SubText", "Escape the maze",
            new Vector2(0, 60), new Vector2(500, 70), 28, Color.white);
        MakeTMP(bg.transform, "ControlsText", "Move :  W   A   S   D",
            new Vector2(0, -20), new Vector2(500, 50), 24, new Color(0.7f, 0.7f, 0.7f));

        var startBtn = MakeButton(bg.transform, "StartButton", "START",
            new Vector2(0, -115), new Vector2(220, 65));
        UnityEventTools.AddVoidPersistentListener(startBtn.onClick, sl.LoadGame);

        var quitBtn = MakeButton(bg.transform, "QuitButton", "QUIT",
            new Vector2(0, -200), new Vector2(220, 65));
        UnityEventTools.AddVoidPersistentListener(quitBtn.onClick, sl.QuitGame);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TitleScene.unity");
    }

    // ─────────────────────────── Game Scene ──────────────────────────────────

    static void BuildGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var dirLight = SpawnLight(Quaternion.Euler(50, -30, 0));
        dirLight.intensity = 1.4f;
        dirLight.color = new Color(1f, 0.95f, 0.85f);  // 따뜻한 흰색 조명

        // 어두운 분위기 앰비언트
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.15f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.12f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 15f;
        RenderSettings.fogEndDistance = 35f;

        new GameObject("GameManager").AddComponent<GameManager>();
        var uiMgr = new GameObject("UIManager").AddComponent<UIManager>();
        var sl    = new GameObject("SceneLoader").AddComponent<SceneLoader>();

        var player = SpawnPlayer();

        var camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f);  // 어두운 남색 배경
        var follow = camObj.AddComponent<CameraFollow>();
        follow.target = player.transform;
        camObj.transform.position = new Vector3(-9, 9, -17);
        camObj.transform.LookAt(player.transform.position + Vector3.up * 1.5f);

        var map = new GameObject("Map").transform;
        BuildMap(map);
        BuildExitDoor(map);

        var items = new GameObject("Items").transform;
        SpawnBattery(items, "Battery_01", new Vector3(-8, 1, 2));
        SpawnBattery(items, "Battery_02", new Vector3( 7, 1, -6));
        SpawnBattery(items, "Battery_03", new Vector3( 8, 1,  8));

        var traps = new GameObject("Traps").transform;
        SpawnTrap(traps, "Trap_01", new Vector3(5.5f, 1.5f,  3f),  130f);
        SpawnTrap(traps, "Trap_02", new Vector3(3f,   1.5f, -5f), -100f);

        BuildGameUI(uiMgr, sl);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
    }

    // ─────────────────────────── Player ──────────────────────────────────────

    static GameObject SpawnPlayer()
    {
        // Assets/Models/ 에 FBX 있으면 자동으로 Mixamo 캐릭터 사용
        var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models" });
        if (guids.Length > 0)
        {
            string modelPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab != null)
            {
                SetupFBXImport(modelPath);
                return SpawnMixamoPlayer(prefab);
            }
        }
        return SpawnCapsulePlayer();
    }

    static GameObject SpawnCapsulePlayer()
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.name = "Player";
        obj.tag  = "Player";
        obj.transform.position = new Vector3(-9, 1, -9);
        PaintObj(obj, new Color(0.25f, 0.55f, 1f));
        Object.DestroyImmediate(obj.GetComponent<CapsuleCollider>());
        obj.AddComponent<CharacterController>();
        obj.AddComponent<PlayerController>();
        return obj;
    }

    static GameObject SpawnMixamoPlayer(GameObject prefab)
    {
        // Object.Instantiate 사용 — 프리팹 링크 없이 순수 복사본 생성 (AddComponent 가능)
        var obj = Object.Instantiate(prefab);
        obj.name = "Player";
        obj.tag  = "Player";
        obj.transform.position = new Vector3(-9, 0, -9);

        // 높이 계산해서 1.8m 로 스케일 맞추기
        float height = GetApproxHeight(obj);
        float scale  = height > 0.1f ? 1.8f / height : 0.01f;
        obj.transform.localScale = Vector3.one * scale;

        // CharacterController — 루트에 없으면 추가
        var cc = obj.GetComponent<CharacterController>();
        if (cc == null) cc = obj.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.3f;
        cc.height = 1.8f;

        // PlayerController
        if (obj.GetComponent<PlayerController>() == null) obj.AddComponent<PlayerController>();

        // Animator + 기본 컨트롤러
        var anim = obj.GetComponent<Animator>() ?? obj.AddComponent<Animator>();
        anim.applyRootMotion = false;
        var ctrl = GetOrCreateAnimatorController();
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;

        // URP 머티리얼 업그레이드 (nonPBR 대응)
        UpgradeRendererMatsToURP(obj);

        return obj;
    }

    static void SetupFBXImport(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) return;
        bool changed = false;
        if (imp.animationType != ModelImporterAnimationType.Human)
        { imp.animationType = ModelImporterAnimationType.Human; changed = true; }
        if (imp.isReadable)
        { imp.isReadable = false; changed = true; }
        if (changed) imp.SaveAndReimport();
    }

    static float GetApproxHeight(GameObject go)
    {
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(r.bounds);
        return bounds.size.y;
    }

    static void UpgradeRendererMatsToURP(GameObject go)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpLit == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                if (!mats[i].shader.name.StartsWith("Universal"))
                {
                    Color col = mats[i].HasProperty("_Color")     ? mats[i].GetColor("_Color")
                              : mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor")
                              : Color.white;
                    mats[i] = new Material(urpLit);
                    mats[i].SetColor("_BaseColor", col);
                }
            }
            r.sharedMaterials = mats;
        }
    }

    static UnityEditor.Animations.AnimatorController GetOrCreateAnimatorController()
    {
        const string path = "Assets/Animations/PlayerAnimator.controller";
        System.IO.Directory.CreateDirectory("Assets/Animations");
        var existing = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        if (existing != null) return existing;

        var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var root  = ctrl.layers[0].stateMachine;
        var idle  = root.AddState("Idle");
        var walk  = root.AddState("Walk");
        root.defaultState = idle;

        var i2w = idle.AddTransition(walk);
        i2w.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        i2w.hasExitTime = false; i2w.duration = 0.15f;

        var w2i = walk.AddTransition(idle);
        w2i.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        w2i.hasExitTime = false; w2i.duration = 0.15f;

        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // ─────────────────────────── Map / Maze ──────────────────────────────────

    /*  Maze layout (top-down, 22×22 interior)
     *
     *  N  ████████████████████████
     *     █ B3           ██ EXIT █
     *     █      ████████        █
     *     █ B1   █     gap → T1  █
     *  W  █   ████      █████████ E
     *     █      █  B2           █
     *     █      █████████       █
     *     █ P              T2    █
     *  S  ████████████████████████
     *
     *  P  = Player start (-9, -9)
     *  EXIT = ExitDoor (9, 9)
     *  Gap in Inner_H1: x > 5 (east side) — where T1 guards the crossing
     */

    static void BuildMap(Transform p)
    {
        var wall  = MakeMat(new Color(0.22f, 0.22f, 0.28f));   // 어두운 청회색 벽
        var floor = MakeMat(new Color(0.30f, 0.30f, 0.36f));   // 살짝 밝은 바닥

        Box(p, "Floor",  new Vector3(0,      -0.5f,  0),     new Vector3(24, 1, 24), floor);

        Box(p, "Wall_N", new Vector3(0,       1.5f,  11.5f), new Vector3(24, 3,  1), wall);
        Box(p, "Wall_S", new Vector3(0,       1.5f, -11.5f), new Vector3(24, 3,  1), wall);
        Box(p, "Wall_E", new Vector3(11.5f,   1.5f,  0),     new Vector3( 1, 3, 24), wall);
        Box(p, "Wall_W", new Vector3(-11.5f,  1.5f,  0),     new Vector3( 1, 3, 24), wall);

        // H-wall at z=4: x from -9 to 5 → gap at x=5..11 (upper-right passage, guarded by T1)
        Box(p, "Inner_H1", new Vector3(-2f,   1.5f,  4f),  new Vector3(14, 3, 1), wall);
        // H-wall at z=-2: x from -1 to 9 → lower area split
        Box(p, "Inner_H2", new Vector3(4f,    1.5f, -2f),  new Vector3(10, 3, 1), wall);
        // V-wall at x=-1: z from -2 to 4 → blocks shortcut
        Box(p, "Inner_V1", new Vector3(-1f,   1.5f,  1f),  new Vector3( 1, 3,  6), wall);
        // V-wall at x=-7: z from -6 to 5 → creates left corridor where B1 hides
        Box(p, "Inner_V2", new Vector3(-7f,   1.5f, -0.5f),new Vector3( 1, 3, 11), wall);
    }

    // ─────────────────────────── Exit Door ───────────────────────────────────

    static void BuildExitDoor(Transform parent)
    {
        var doorRoot = new GameObject("ExitDoor");
        doorRoot.transform.SetParent(parent);
        doorRoot.transform.position = new Vector3(9f, 0f, 9.5f);
        var dc = doorRoot.AddComponent<DoorController>();

        // Visual door mesh (child — slides up when opened)
        var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = "DoorMesh";
        mesh.transform.SetParent(doorRoot.transform);
        mesh.transform.localPosition = new Vector3(0, 1.5f, 0);
        mesh.transform.localScale    = new Vector3(3, 3, 1);
        dc.doorRenderer = mesh.GetComponent<Renderer>();
        dc.closedColor  = new Color(0.28f, 0.28f, 0.28f);
        dc.openColor    = new Color(0f, 1f, 0.4f);
        PaintObj(mesh, dc.closedColor);

        // Invisible exit trigger (enabled by DoorController after door opens)
        var trig = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trig.name = "ExitTrigger";
        trig.transform.SetParent(parent);
        trig.transform.position   = new Vector3(9f, 1.5f, 10.5f);
        trig.transform.localScale = new Vector3(3, 3, 1);
        trig.tag = "Exit";
        trig.GetComponent<Renderer>().enabled = false;
        var tc = trig.GetComponent<Collider>();
        tc.isTrigger = true;
        tc.enabled   = false;   // DoorController activates this when door fully opens
        dc.exitTrigger = tc;
    }

    // ─────────────────────────── Battery ─────────────────────────────────────

    static void SpawnBattery(Transform parent, string name, Vector3 pos)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position   = pos;
        obj.transform.localScale = Vector3.one * 0.5f;

        // 발광하는 노란 배터리
        var mat = MakeMat(new Color(1f, 0.88f, 0f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0f) * 1.5f);
        obj.GetComponent<Renderer>().sharedMaterial = mat;

        // 배터리 주변 포인트 라이트
        var light = new GameObject("BatteryLight").AddComponent<Light>();
        light.type      = LightType.Point;
        light.color     = new Color(1f, 0.85f, 0.2f);
        light.intensity = 1.2f;
        light.range     = 3f;
        light.transform.SetParent(obj.transform);
        light.transform.localPosition = Vector3.zero;

        obj.GetComponent<SphereCollider>().isTrigger = true;
        obj.AddComponent<Collectible>();
    }

    // ─────────────────────────── Trap ────────────────────────────────────────

    static void SpawnTrap(Transform parent, string name, Vector3 pos, float speed)
    {
        var pivot = new GameObject(name);
        pivot.transform.SetParent(parent);
        pivot.transform.position = pos;
        var ts = pivot.AddComponent<TrapSpinner>();
        ts.rotationAxis  = Vector3.up;
        ts.rotationSpeed = speed;

        var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.SetParent(pivot.transform);
        arm.transform.localPosition = Vector3.zero;
        arm.transform.localScale    = new Vector3(5f, 0.4f, 0.4f);

        // 나무 텍스처 있으면 적용, 없으면 붉은색 폴백
        var woodMat = GetOrCreateWoodMaterial();
        arm.GetComponent<Renderer>().sharedMaterial = woodMat ?? NewMat(new Color(0.9f, 0.18f, 0.1f));
        arm.GetComponent<BoxCollider>().isTrigger = true;
    }

    // ─────────────────────────── Wood Material ───────────────────────────────

    static Material GetOrCreateWoodMaterial()
    {
        const string DIFF_PATH   = "Assets/Textures/Wood/wood_diff.png";
        const string ROUGH_PATH  = "Assets/Textures/Wood/wood_rough.png";
        const string NORMAL_PATH = "Assets/Textures/Wood/wood_normal.png";
        const string MAT_PATH    = "Assets/Materials/TrapWood.mat";

        // 캐시된 머티리얼 있으면 재사용
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (existing != null) return existing;

        var diff = AssetDatabase.LoadAssetAtPath<Texture2D>(DIFF_PATH);
        if (diff == null) return null;   // 텍스처 없으면 폴백

        // 노말맵 임포트 타입 설정
        SetTextureAsNormalMap(NORMAL_PATH);
        SetTextureLinear(ROUGH_PATH);
        AssetDatabase.Refresh();

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = "TrapWood" };
        bool isURP = mat.HasProperty("_BaseMap");

        if (isURP) { mat.SetTexture("_BaseMap", diff); mat.SetColor("_BaseColor", Color.white); }
        else        { mat.SetTexture("_MainTex", diff); }

        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NORMAL_PATH);
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", 1.2f); }

        mat.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.15f);  // 거친 나무

        // UV 타일링 (arm 크기 5×0.4 비율에 맞게)
        mat.SetTextureScale(isURP ? "_BaseMap" : "_MainTex", new Vector2(4f, 1f));

        System.IO.Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.CreateAsset(mat, MAT_PATH);
        return mat;
    }

    static void SetTextureAsNormalMap(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null || imp.textureType == TextureImporterType.NormalMap) return;
        imp.textureType = TextureImporterType.NormalMap;
        imp.SaveAndReimport();
    }

    static void SetTextureLinear(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null || !imp.sRGBTexture) return;
        imp.sRGBTexture = false;
        imp.SaveAndReimport();
    }

    static Material NewMat(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        ApplyColorToMat(mat, color);
        return mat;
    }

    // ─────────────────────────── Game UI ─────────────────────────────────────

    static void BuildGameUI(UIManager uiMgr, SceneLoader sl)
    {
        var canvas = MakeCanvas();

        // Timer — top-left
        var timerGO = MakeTMPGO(canvas.transform, "TimerText", "01:30",
            Vector2.zero, new Vector2(200, 60), 36, Color.white);
        var timerRect = timerGO.GetComponent<RectTransform>();
        timerRect.anchorMin = timerRect.anchorMax = new Vector2(0, 1);
        timerRect.pivot = new Vector2(0, 1);
        timerRect.anchoredPosition = new Vector2(20, -20);

        // Count — top-right
        var countGO = MakeTMPGO(canvas.transform, "CountText", "BAT  0 / 3",
            Vector2.zero, new Vector2(250, 60), 28, Color.white);
        var countRect = countGO.GetComponent<RectTransform>();
        countRect.anchorMin = countRect.anchorMax = new Vector2(1, 1);
        countRect.pivot = new Vector2(1, 1);
        countRect.anchoredPosition = new Vector2(-20, -20);

        // Clear Panel
        var clearPanel = MakeResultPanel(canvas.transform, "ClearPanel",
            new Color(0f, 0.38f, 0.12f, 0.93f),
            "CLEAR!", Color.yellow, sl);
        clearPanel.SetActive(false);

        // GameOver Panel
        var goPanel = MakeResultPanel(canvas.transform, "GameOverPanel",
            new Color(0.48f, 0.05f, 0.05f, 0.93f),
            "GAME OVER", Color.red, sl);
        goPanel.SetActive(false);

        uiMgr.timerText     = timerGO.GetComponent<TMP_Text>();
        uiMgr.countText     = countGO.GetComponent<TMP_Text>();
        uiMgr.clearPanel    = clearPanel;
        uiMgr.gameOverPanel = goPanel;
    }

    static GameObject MakeResultPanel(Transform parent, string name, Color bg,
        string title, Color titleColor, SceneLoader sl)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var img = panel.AddComponent<Image>();
        img.color = bg;
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.25f);
        rect.anchorMax = new Vector2(0.75f, 0.75f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        MakeTMP(panel.transform, "Title", title,
            new Vector2(0, 80), new Vector2(500, 100), 64, titleColor, FontStyles.Bold);

        var restart = MakeButton(panel.transform, "RestartButton", "RETRY",
            new Vector2(0, -30), new Vector2(210, 60));
        UnityEventTools.AddVoidPersistentListener(restart.onClick, sl.RestartGame);

        var toTitle = MakeButton(panel.transform, "TitleButton", "TITLE",
            new Vector2(0, -110), new Vector2(210, 60));
        UnityEventTools.AddVoidPersistentListener(toTitle.onClick, sl.LoadTitle);

        return panel;
    }

    // ─────────────────────────── Build Settings ───────────────────────────────

    static void RegisterBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity",  true),
        };
    }

    // ─────────────────────────── Primitive helpers ────────────────────────────

    static Light SpawnLight(Quaternion rot)
    {
        var obj = new GameObject("Directional Light");
        var l = obj.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        obj.transform.rotation = rot;
        return l;
    }

    static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position   = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material MakeMat(Color color)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                 ?? Shader.Find("Standard")
                 ?? Shader.Find("Diffuse");
        var mat = new Material(sh ?? Shader.Find("Sprites/Default"));
        ApplyColorToMat(mat, color);
        return mat;
    }

    static void PaintObj(GameObject obj, Color color)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(r.sharedMaterial);
        ApplyColorToMat(mat, color);
        r.sharedMaterial = mat;
    }

    static void ApplyColorToMat(Material mat, Color color)
    {
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
    }

    // ─────────────────────────── UI helpers ──────────────────────────────────

    static GameObject MakeCanvas()
    {
        var obj = new GameObject("Canvas");
        var c = obj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = obj.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        obj.AddComponent<GraphicRaycaster>();

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        return obj;
    }

    static GameObject MakeImage(Transform parent, string name, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    static TMP_Text MakeTMP(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, float fontSize, Color color,
        FontStyles style = FontStyles.Normal)
    {
        return MakeTMPGO(parent, name, text, pos, size, fontSize, color, style)
               .GetComponent<TMP_Text>();
    }

    static GameObject MakeTMPGO(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, float fontSize, Color color,
        FontStyles style = FontStyles.Normal)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return obj;
    }

    static Button MakeButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>().color = new Color(0.18f, 0.20f, 0.32f);
        var btn  = obj.AddComponent<Button>();
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        MakeTMP(obj.transform, "Label", label, Vector2.zero, size, 26, Color.white);
        return btn;
    }

    static void StretchFull(GameObject obj)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
}
#endif
