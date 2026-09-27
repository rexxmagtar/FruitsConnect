using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lowers DefaultTexturePlatform maxTextureSize for WebGL/Playables size budgets.
/// Paths mapped per FruitsConnect handoff / PuzzleTask practice.
/// </summary>
public static class OptimizeTexturesForWebGL
{
    struct Rule
    {
        public string PathPrefix;
        public int MaxSize;
    }

    static readonly Rule[] Rules =
    {
        new Rule { PathPrefix = "Assets/Art/Backgrounds", MaxSize = 512 },
        new Rule { PathPrefix = "Assets/Art/Ui", MaxSize = 512 },
        new Rule { PathPrefix = "Assets/Sprites/JigsawPieces", MaxSize = 1024 },
        new Rule { PathPrefix = "Assets/Art/PuzlePeaces", MaxSize = 1024 },
        new Rule { PathPrefix = "Assets/Sprites", MaxSize = 512 },
        new Rule { PathPrefix = "Assets/Art", MaxSize = 1024 },
    };

    [MenuItem("Tools/WebGL/Optimize Texture Max Sizes")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || path.StartsWith("Assets/Plugins") || path.StartsWith("Assets/Modules"))
                    continue;

                int target = ResolveMaxSize(path);
                if (target <= 0)
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                var settings = importer.GetDefaultPlatformTextureSettings();
                bool dirty = false;

                if (settings.maxTextureSize > target)
                {
                    settings.maxTextureSize = target;
                    importer.SetPlatformTextureSettings(settings);
                    dirty = true;
                }

                if (importer.isReadable)
                {
                    importer.isReadable = false;
                    dirty = true;
                }

                // UI sprites: prefer no mipmaps
                if (importer.textureType == TextureImporterType.Sprite && importer.mipmapEnabled)
                {
                    bool looksUi = path.IndexOf("/Ui", System.StringComparison.OrdinalIgnoreCase) >= 0
                                   || path.IndexOf("/UI", System.StringComparison.OrdinalIgnoreCase) >= 0
                                   || path.IndexOf("/Sprites/", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    if (looksUi)
                    {
                        importer.mipmapEnabled = false;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    changed++;
                }

                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Optimize Textures", path, (float)i / guids.Length);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[OptimizeTexturesForWebGL] Updated {changed} texture importers.");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    static int ResolveMaxSize(string path)
    {
        // Most specific prefix wins (longest match)
        int bestLen = -1;
        int bestSize = -1;
        foreach (var rule in Rules)
        {
            if (path.StartsWith(rule.PathPrefix, System.StringComparison.OrdinalIgnoreCase)
                && rule.PathPrefix.Length > bestLen)
            {
                bestLen = rule.PathPrefix.Length;
                bestSize = rule.MaxSize;
            }
        }
        return bestSize;
    }
}
