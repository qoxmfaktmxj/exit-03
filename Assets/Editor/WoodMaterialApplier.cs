#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// PolyHaven 나무 텍스처를 장애물에 자동 적용
/// Assets/Textures/Wood/ 폴더의 PNG 파일을 사용합니다.
/// 메뉴: Exit03 > Apply Wood Texture to Traps
/// </summary>
public static class WoodMaterialApplier
{
    const string DIFF_PATH   = "Assets/Textures/Wood/wood_diff.png";
    const string ROUGH_PATH  = "Assets/Textures/Wood/wood_rough.png";
    const string NORMAL_PATH = "Assets/Textures/Wood/wood_normal.png";
    const string MAT_PATH    = "Assets/Materials/TrapWood.mat";

    [MenuItem("Exit03/Apply Wood Texture to Traps")]
    public static void Apply()
    {
        SetupTextureImportSettings();
        AssetDatabase.Refresh();

        var mat = CreateWoodMaterial();
        if (mat == null) { Debug.LogError("[Exit03] 나무 텍스처 파일을 찾지 못했습니다."); return; }

        ApplyToScene(mat);
        Debug.Log("[Exit03] 나무 텍스처 적용 완료!");
    }

    // ─── 텍스처 임포트 설정 ───────────────────────────────────────────────────

    static void SetupTextureImportSettings()
    {
        // 노말맵은 타입 변경 필요
        SetNormalMap(NORMAL_PATH);
        // 러프니스는 Linear 색공간
        SetLinear(ROUGH_PATH);
    }

    static void SetNormalMap(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType == TextureImporterType.NormalMap) return;
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    static void SetLinear(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (!importer.sRGBTexture) return;
        importer.sRGBTexture = false;
        importer.SaveAndReimport();
    }

    // ─── 머티리얼 생성 ────────────────────────────────────────────────────────

    static Material CreateWoodMaterial()
    {
        var diff   = AssetDatabase.LoadAssetAtPath<Texture2D>(DIFF_PATH);
        var rough  = AssetDatabase.LoadAssetAtPath<Texture2D>(ROUGH_PATH);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NORMAL_PATH);

        if (diff == null) return null;

        System.IO.Directory.CreateDirectory("Assets/Materials");

        // URP 또는 Standard 자동 선택
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.name = "TrapWood";

        bool isURP = mat.HasProperty("_BaseMap");

        // Albedo (색상 텍스처)
        if (isURP)
        {
            mat.SetTexture("_BaseMap", diff);
            mat.SetColor("_BaseColor", Color.white);
        }
        else
        {
            mat.SetTexture("_MainTex", diff);
        }

        // Normal Map
        if (normal != null)
        {
            mat.SetTexture(isURP ? "_BumpMap" : "_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1.2f);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Roughness / Smoothness
        if (rough != null)
        {
            // URP: Roughness는 _MetallicGlossMap의 알파로 들어가거나 직접 Smoothness 조절
            mat.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.2f); // 나무는 거칠게
        }

        // UV 타일링 (트랩 arm 스케일 5x0.4x0.4에 맞게)
        mat.SetTextureScale(isURP ? "_BaseMap" : "_MainTex", new Vector2(3f, 1f));

        AssetDatabase.CreateAsset(mat, MAT_PATH);
        return mat;
    }

    // ─── GameScene 장애물에 적용 ─────────────────────────────────────────────

    static void ApplyToScene(Material mat)
    {
        const string scenePath = "Assets/Scenes/GameScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        int count = 0;
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            // "Traps" 하위의 모든 "Arm" 오브젝트에 적용
            var arms = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in arms)
            {
                if (r.gameObject.name == "Arm")
                {
                    r.sharedMaterial = mat;
                    count++;
                }
            }
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Exit03] {count}개 Arm 오브젝트에 나무 텍스처 적용 완료");
    }
}
#endif
