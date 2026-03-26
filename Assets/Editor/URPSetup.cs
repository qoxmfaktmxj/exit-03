#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP 파이프라인 에셋 생성 + GameScene에 Post Processing Volume 추가
/// 메뉴: Exit03 > Setup URP + Post Processing
/// </summary>
public static class URPSetup
{
    [MenuItem("Exit03/Setup URP + Post Processing")]
    public static void Setup()
    {
        CreateURPAsset();
        AddPostProcessingToScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Exit03] URP + Post Processing 설정 완료!");
        EditorUtility.DisplayDialog("URP 설정 완료",
            "Bloom + Color Grading 적용 완료!\n\nSetup All Scenes 를 다시 실행하거나\nPlay 버튼을 눌러 확인하세요.", "확인");
    }

    // ─── URP Asset 생성 및 Graphics 설정 적용 ────────────────────────────────

    static void CreateURPAsset()
    {
        const string assetPath   = "Assets/Settings/UniversalRenderPipelineAsset.asset";
        const string rendererPath = "Assets/Settings/UniversalRenderPipelineAsset_Renderer.asset";

        // 이미 있으면 스킵
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath) != null)
        {
            Debug.Log("[Exit03] URP Asset 이미 존재 — 스킵");
            return;
        }

        System.IO.Directory.CreateDirectory("Assets/Settings");

        // Renderer 생성 (URP 14+ 에서는 postProcessingEnabled 속성 없음 — 기본값으로 활성화됨)
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(renderer, rendererPath);

        // URP Asset 생성
        var urpAsset = UniversalRenderPipelineAsset.Create(renderer);
        urpAsset.renderScale    = 1f;
        urpAsset.msaaSampleCount = 4;
        AssetDatabase.CreateAsset(urpAsset, assetPath);

        // Graphics Settings 에 연결
        GraphicsSettings.defaultRenderPipeline = urpAsset;
        QualitySettings.renderPipeline         = urpAsset;

        EditorUtility.SetDirty(urpAsset);
        Debug.Log("[Exit03] URP Asset 생성 완료 → " + assetPath);
    }

    // ─── GameScene에 Global Volume 추가 ─────────────────────────────────────

    static void AddPostProcessingToScene()
    {
        const string scenePath    = "Assets/Scenes/GameScene.unity";
        const string profilePath  = "Assets/Settings/PostProcessProfile.asset";

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

        // 이미 Volume 있으면 제거 후 재생성
        var existing = Object.FindFirstObjectByType<Volume>();
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Volume Profile 생성
        System.IO.Directory.CreateDirectory("Assets/Settings");
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // ── Bloom ──────────────────────────────────────────────────────────
        var bloom = profile.Add<Bloom>(true);
        bloom.active              = true;
        bloom.threshold.value     = 0.8f;   // 이 밝기 이상에서 발광
        bloom.intensity.value     = 0.6f;   // 발광 강도
        bloom.scatter.value       = 0.7f;
        bloom.tint.value          = new Color(1f, 0.95f, 0.8f);   // 따뜻한 빛

        // ── Color Adjustments ──────────────────────────────────────────────
        var color = profile.Add<ColorAdjustments>(true);
        color.active              = true;
        color.postExposure.value  = 0.3f;   // 전체 밝기 약간 올리기
        color.contrast.value      = 18f;    // 명암 대비 강화
        color.saturation.value    = 12f;    // 색감 살짝 올리기
        color.colorFilter.value   = new Color(0.95f, 0.97f, 1.05f);  // 차가운 톤

        // ── Vignette (가장자리 어둡게) ──────────────────────────────────────
        var vignette = profile.Add<Vignette>(true);
        vignette.active           = true;
        vignette.intensity.value  = 0.35f;
        vignette.smoothness.value = 0.4f;

        // ── Lift Gamma Gain (섀도우 파란빛) ────────────────────────────────
        var lgg = profile.Add<LiftGammaGain>(true);
        lgg.active = true;
        lgg.lift.value  = new Vector4(0f, 0f, 0.05f, 0f);   // 어두운 곳 파란빛
        lgg.gamma.value = new Vector4(0f, 0f, 0f, 0.02f);

        AssetDatabase.CreateAsset(profile, profilePath);

        // Global Volume GameObject
        var volObj  = new GameObject("Global Volume");
        var vol     = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10;
        vol.profile  = profile;

        // Camera에 Post Processing 활성화
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing         = AntialiasingMode.FastApproximateAntialiasing;
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[Exit03] Post Processing Volume 추가 완료");
    }
}
#endif
