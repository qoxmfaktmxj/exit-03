#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Standard 셰이더 → URP/Lit 셰이더로 일괄 변환
/// URP 설치 후 핑크로 변한 머티리얼을 고칩니다.
/// 메뉴: Exit03 > Upgrade Materials to URP
/// </summary>
public static class UpgradeMaterials
{
    [MenuItem("Exit03/Upgrade Materials to URP")]
    public static void Upgrade()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("URP 셰이더 없음",
                "URP가 아직 설치되지 않았습니다.\nPackage Manager에서 URP 로딩이 끝난 뒤 다시 시도하세요.", "확인");
            return;
        }

        int count = 0;

        // Assets 폴더 내 머티리얼 전체 업그레이드
        var matGuids = AssetDatabase.FindAssets("t:Material");
        foreach (var guid in matGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Standard 셰이더 또는 Sprites/Default 사용 중인 것만 변환
            if (mat.shader.name.StartsWith("Standard") || mat.shader.name == "Sprites/Default")
            {
                Color col = mat.HasProperty("_Color")    ? mat.GetColor("_Color")
                          : mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                          : Color.white;

                bool hasEmission = mat.IsKeywordEnabled("_EMISSION");
                Color emissionColor = hasEmission && mat.HasProperty("_EmissionColor")
                    ? mat.GetColor("_EmissionColor") : Color.black;

                mat.shader = urpLit;

                mat.SetColor("_BaseColor", col);
                mat.color = col;

                if (hasEmission && emissionColor != Color.black)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        // 열린 씬에서 직접 생성된 (Assets에 없는) 임시 머티리얼도 변환
        count += UpgradeSceneMaterials(urpLit, "Assets/Scenes/GameScene.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("머티리얼 업그레이드 완료",
            $"{count}개 머티리얼을 URP/Lit으로 변환했습니다!\n\n이제 Exit03 > Setup All Scenes 를 실행하세요.", "확인");
    }

    static int UpgradeSceneMaterials(Shader urpLit, string scenePath)
    {
        int count = 0;
        if (!System.IO.File.Exists(scenePath)) return 0;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].shader.name.StartsWith("Standard") || mats[i].shader.name == "Sprites/Default")
                    {
                        Color col = mats[i].HasProperty("_Color") ? mats[i].GetColor("_Color")
                                  : mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor")
                                  : Color.white;
                        mats[i].shader = urpLit;
                        mats[i].SetColor("_BaseColor", col);
                        mats[i].color = col;
                        changed = true;
                        count++;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
        return count;
    }

    // ─── Setup All Scenes 후 자동 실행 가능하도록 public static 제공 ──────────
    public static void AutoRun()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null) Upgrade();
    }
}
#endif
