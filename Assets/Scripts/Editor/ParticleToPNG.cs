using UnityEngine;
using UnityEditor;
using System.IO;

public class ParticleToPNG : EditorWindow
{
    private GameObject particlePrefab;
    private float simulationTime = 0.5f; 
    private int resolution = 512;
    private float cameraZoom = 2.0f;
    private bool transparentBackground = true;
    private Color backgroundColor = Color.black;

    [MenuItem("Tools/Particle to PNG Converter")]
    public static void ShowWindow()
    {
        GetWindow<ParticleToPNG>("Particle to PNG");
    }

    private void OnGUI()
    {
        GUILayout.Label("Particle Preview Generator", EditorStyles.boldLabel);
        
        particlePrefab = (GameObject)EditorGUILayout.ObjectField("Particle Prefab", particlePrefab, typeof(GameObject), false);
        simulationTime = EditorGUILayout.FloatField("Capture Time (sec)", simulationTime);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 128, 2048);
        cameraZoom = EditorGUILayout.FloatField("Camera Zoom", cameraZoom);
        transparentBackground = EditorGUILayout.Toggle("Transparent BG", transparentBackground);
        
        if (!transparentBackground)
            backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Preview PNG", GUILayout.Height(40)))
        {
            Generate();
        }
    }

    private void Generate()
    {
        if (particlePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a Particle Prefab first!", "OK");
            return;
        }

        // 1. Setup temporary Camera
        GameObject camGo = new GameObject("TempCaptureCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        
        // For additive particles, capturing against pure black (0,0,0,1) is most accurate
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = cameraZoom;

        // 2. Instantiate and Simulate
        GameObject instance = Instantiate(particlePrefab, Vector3.zero, Quaternion.identity);
        instance.layer = 0;
        foreach (Transform t in instance.GetComponentsInChildren<Transform>()) t.gameObject.layer = 0;

        ParticleSystem[] allPS = instance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in allPS)
        {
            ps.useAutoRandomSeed = false;
            ps.randomSeed = 1; 
            ps.Simulate(simulationTime, true, true);
        }

        // 3. Render to Texture
        RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        // 4. Convert and Save
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        
        if (transparentBackground)
        {
            Color[] pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                // Un-premultiply logic:
                // Additive shaders add color to black. To get transparency,
                // we treat the brightness as the alpha, and then 'normalize' the color
                // so it doesn't look washed out or shifted toward white/yellow.
                float maxChannel = Mathf.Max(pixels[i].r, Mathf.Max(pixels[i].g, pixels[i].b));
                
                if (maxChannel > 0.001f)
                {
                    pixels[i].r = Mathf.Clamp01(pixels[i].r / maxChannel);
                    pixels[i].g = Mathf.Clamp01(pixels[i].g / maxChannel);
                    pixels[i].b = Mathf.Clamp01(pixels[i].b / maxChannel);
                    pixels[i].a = maxChannel;
                }
                else
                {
                    pixels[i] = new Color(0, 0, 0, 0);
                }
            }
            tex.SetPixels(pixels);
        }
        
        tex.Apply();

        string path = EditorUtility.SaveFilePanel("Save Particle Preview", "Assets", particlePrefab.name + "_preview", "png");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            
            string relativePath = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            Debug.Log($"Successfully saved preview to {path}");
        }

        // 5. Cleanup
        RenderTexture.active = null;
        DestroyImmediate(camGo);
        DestroyImmediate(instance);
        DestroyImmediate(rt);
        DestroyImmediate(tex);
    }
}
