using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal adaptive UI setup (matches intent of Expand scaler):
/// - CanvasScaler → Scale With Screen Size + Expand @ 1080x1920
/// - Full-screen roots ONLY (explicit allowlist) → stretch (0,0)-(1,1), sizeDelta 0
/// - Children / item widgets are left untouched
/// </summary>
public static class FixWebGLUiAdaptiveScale
{
    const float RefWidth = 1080f;
    const float RefHeight = 1920f;

    static readonly string[] ScreenPrefabFolders = { "Assets/Prefabs" };

    // Explicit allowlist only — do NOT match every *Ui / *UI (SkinButtonUi, CircleUi, etc.).
    static readonly HashSet<string> ScreenRootNameHints = new HashSet<string>
    {
        "MainMenuUi", "LevelCompleteUi", "LevelFailedUi", "PauseUI", "SettingsUi",
        "LoadingScreen"
    };

    public static void Run()
    {
        try
        {
            FixOpenScenesCanvasScalers();
            FixScreenRootAnchorsOnly();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[FixWebGLUiAdaptiveScale] Done (roots + scaler only; children unchanged).");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FixWebGLUiAdaptiveScale] Failed: {e}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            return;
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    [MenuItem("Tools/UI/Fix WebGL Adaptive Scale")]
    public static void RunFromMenu() => Run();

    static void FixOpenScenesCanvasScalers()
    {
        var sample = "Assets/Scenes/SampleScene.unity";
        if (Application.isBatchMode || EditorSceneManager.GetActiveScene().path != sample)
            EditorSceneManager.OpenScene(sample, OpenSceneMode.Single);

        foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;
            EditorUtility.SetDirty(scaler);
            Debug.Log($"[FixWebGLUiAdaptiveScale] CanvasScaler Expand on '{scaler.gameObject.name}'");
        }
    }

    static void FixScreenRootAnchorsOnly()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", ScreenPrefabFolders);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var rt = root.GetComponent<RectTransform>();
                if (rt == null)
                    continue;

                if (!ScreenRootNameHints.Contains(root.name))
                    continue;

                // Only the top container stretches. Do not modify any children.
                if (!IsFullStretch(rt) || rt.sizeDelta != Vector2.zero)
                {
                    StretchFullRoot(rt);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[FixWebGLUiAdaptiveScale] Root stretch only: {path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    static bool IsFullStretch(RectTransform rt)
    {
        return Mathf.Approximately(rt.anchorMin.x, 0f) && Mathf.Approximately(rt.anchorMin.y, 0f)
               && Mathf.Approximately(rt.anchorMax.x, 1f) && Mathf.Approximately(rt.anchorMax.y, 1f);
    }

    static void StretchFullRoot(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(rt);
    }
}
