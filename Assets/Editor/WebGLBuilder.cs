using System;
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
        bool changedDefines = false;

        // Leave .data/.wasm uncompressed so you can zip them yourself for the YT template.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        Debug.Log("WebGLBuilder: WebGL compression Disabled");

        // Smaller .wasm; longer link. Matches Editor "Code Optimization: Disk Size with LTO".
        UnityEditor.WebGL.UserBuildSettings.codeOptimization =
            UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        Debug.Log("WebGLBuilder: Code Optimization = DiskSizeLTO");

        if (stripYtDefine)
        {
            activeDefines = RemoveDefine(originalDefines, YtDefine);
            if (!string.Equals(activeDefines, originalDefines, StringComparison.Ordinal))
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, activeDefines);
                changedDefines = true;
            }
            Debug.Log($"WebGLBuilder: stripped {YtDefine}. Defines: '{activeDefines}' (was '{originalDefines}')");
        }
        else
        {
            activeDefines = EnsureDefine(originalDefines, YtDefine);
            if (!string.Equals(activeDefines, originalDefines, StringComparison.Ordinal))
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, activeDefines);
                changedDefines = true;
                Debug.Log($"WebGLBuilder: ensured {YtDefine}. Defines: '{activeDefines}' (was '{originalDefines}')");
            }
            else
            {
                Debug.Log($"WebGLBuilder: {YtDefine} already set. Defines: '{activeDefines}'");
            }
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
                options = BuildOptions.None // Release (not Development)
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"WebGLBuilder: succeeded in {summary.totalTime} → {outputPath}");
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError($"WebGLBuilder: failed ({summary.result}). Errors: {summary.totalErrors}");
            EditorApplication.Exit(1);
        }
        finally
        {
            if (changedDefines)
            {
                PlayerSettings.SetScriptingDefineSymbols(namedTarget, originalDefines);
                Debug.Log($"WebGLBuilder: restored defines → '{originalDefines}'");
            }
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

    private static string EnsureDefine(string defines, string define)
    {
        if (string.IsNullOrEmpty(defines))
        {
            return define;
        }

        bool alreadyPresent = defines
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(symbol => string.Equals(symbol.Trim(), define, StringComparison.OrdinalIgnoreCase));

        return alreadyPresent ? defines : $"{defines};{define}";
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
