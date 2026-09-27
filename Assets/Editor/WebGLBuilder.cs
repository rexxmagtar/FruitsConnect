using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuilder
{
    private const string YtDefine = "YT_PLYABLE";

    /// <summary>
    /// Headless WebGL player build. Invoked via:
    /// unity build . --target WebGL --execute-method WebGLBuilder.Build --output-path Builds/WebGL
    /// </summary>
    public static void Build()
    {
        BuildInternal(stripYtDefine: HasArg("-stripYtDefine"));
    }

    /// <summary>
    /// WebGL build with YT_PLYABLE temporarily removed from scripting defines.
    /// </summary>
    public static void BuildWithoutYt()
    {
        BuildInternal(stripYtDefine: true);
    }

    private static void BuildInternal(bool stripYtDefine)
    {
        string outputPath = GetArg("-buildOutput")
            ?? (stripYtDefine ? "Builds/WebGL_NoYT" : "Builds/WebGL");

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("WebGLBuilder: no enabled scenes in EditorBuildSettings.");
            EditorApplication.Exit(1);
            return;
        }

        NamedBuildTarget namedTarget = NamedBuildTarget.WebGL;
        string originalDefines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
        string activeDefines = originalDefines;

        // Template loadResources expects uncompressed Unity output, then manual .data/.wasm zips.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Debug.Log("WebGLBuilder: WebGL compression Disabled (manual .data/.wasm zip after build)");

        if (stripYtDefine)
        {
            activeDefines = RemoveDefine(originalDefines, YtDefine);
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, activeDefines);
            Debug.Log($"WebGLBuilder: stripped {YtDefine}. Defines: '{activeDefines}' (was '{originalDefines}')");
        }

        try
        {
            Debug.Log($"WebGLBuilder: building {scenes.Length} scene(s) → {outputPath}");
            foreach (string scene in scenes)
            {
                Debug.Log($"  scene: {scene}");
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                ZipDataAndWasm(outputPath);
                Debug.Log($"WebGLBuilder: succeeded in {summary.totalTime} → {outputPath}");
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError($"WebGLBuilder: failed ({summary.result}). Errors: {summary.totalErrors}");
            EditorApplication.Exit(1);
        }
        finally
        {
            if (stripYtDefine)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, originalDefines);
                Debug.Log($"WebGLBuilder: restored defines → '{originalDefines}'");
            }
        }
    }

    /// <summary>
    /// Zip uncompressed .data and .wasm for the YT template loadResources path.
    /// Creates File.data.zip / File.wasm.zip (entry = original filename), then deletes originals.
    /// </summary>
    private static void ZipDataAndWasm(string buildRoot)
    {
        string buildFolder = Path.Combine(buildRoot, "Build");
        if (!Directory.Exists(buildFolder))
        {
            Debug.LogWarning($"WebGLBuilder: Build folder not found at {buildFolder}; skip zip.");
            return;
        }

        string[] targets = Directory.GetFiles(buildFolder)
            .Where(path =>
            {
                string name = Path.GetFileName(path);
                return name.EndsWith(".data", StringComparison.OrdinalIgnoreCase)
                       || name.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        if (targets.Length == 0)
        {
            Debug.LogWarning($"WebGLBuilder: no uncompressed .data/.wasm found in {buildFolder}");
            return;
        }

        foreach (string filePath in targets)
        {
            string zipPath = filePath + ".zip";
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath), System.IO.Compression.CompressionLevel.Optimal);
            }

            File.Delete(filePath);
            Debug.Log($"WebGLBuilder: zipped {Path.GetFileName(filePath)} → {Path.GetFileName(zipPath)}");
        }
    }

    private static string RemoveDefine(string defines, string define)
    {
        if (string.IsNullOrEmpty(defines))
        {
            return string.Empty;
        }

        return string.Join(";", defines
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(symbol => !string.Equals(symbol.Trim(), define, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
