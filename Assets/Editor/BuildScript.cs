#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/TitleScene.unity",
        "Assets/Scenes/GameScene.unity",
    };

    // ─── Windows 64-bit .exe ─────────────────────────────────────────────────

    [MenuItem("Exit03/Build Windows (.exe)")]
    public static void BuildWindows()
    {
        string outDir = Path.GetFullPath("Build/Windows");
        Directory.CreateDirectory(outDir);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes             = Scenes,
            locationPathName   = Path.Combine(outDir, "Exit03.exe"),
            target             = BuildTarget.StandaloneWindows64,
            options            = BuildOptions.None,
        });

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Exit03] ✓ 빌드 완료 → {outDir}\\Exit03.exe");
            // 빌드 폴더 자동으로 열기
            System.Diagnostics.Process.Start("explorer.exe", outDir);
        }
        else
        {
            Debug.LogError("[Exit03] ✗ 빌드 실패 — Console 오류를 확인하세요.");
        }
    }

    // ─── WebGL (브라우저) ─────────────────────────────────────────────────────

    [MenuItem("Exit03/Build WebGL (브라우저)")]
    public static void BuildWebGL()
    {
        string outDir = Path.GetFullPath("Build/WebGL");
        Directory.CreateDirectory(outDir);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes           = Scenes,
            locationPathName = outDir,
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None,
        });

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Exit03] ✓ WebGL 빌드 완료 → {outDir}");
            System.Diagnostics.Process.Start("explorer.exe", outDir);
        }
        else
        {
            Debug.LogError("[Exit03] ✗ 빌드 실패 — Console 오류를 확인하세요.");
        }
    }
}
#endif
