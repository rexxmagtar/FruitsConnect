using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using JigsawSystem;
using UnityEditor.U2D;
using UnityEngine.U2D;
using UnityEngine.UI;

public class JigsawPieceGenerator : EditorWindow
{
    private Object sourceObject;
    private JigsawPuzzleData targetData;
    private string savePath = "Assets/Sprites/JigsawPieces";
    
    [Header("Generator Settings")]
    [Range(0.1f, 0.5f)]
    private float tabSizeRatio = 0.25f;
    [Range(0.0f, 0.5f)]
    private float tabBulgeOffset = 0.15f;

    [MenuItem("Tools/Fruit Connect/Jigsaw Piece Generator")]
    public static void ShowWindow()
    {
        GetWindow<JigsawPieceGenerator>("Jigsaw Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Jigsaw Piece Generator", EditorStyles.boldLabel);

        sourceObject = EditorGUILayout.ObjectField("Source Image/Sprite", sourceObject, typeof(Object), false);
        targetData = (JigsawPuzzleData)EditorGUILayout.ObjectField("Target JigsawPuzzleData", targetData, typeof(JigsawPuzzleData), false);
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        
        GUILayout.Space(10);
        GUILayout.Label("Shape Settings", EditorStyles.boldLabel);
        tabSizeRatio = EditorGUILayout.Slider("Tab Size Ratio", tabSizeRatio, 0.1f, 0.5f);
        tabBulgeOffset = EditorGUILayout.Slider("Tab Bulge Offset", tabBulgeOffset, 0.0f, 0.5f);

        GUILayout.Space(20);
        if (GUILayout.Button("Generate Pieces"))
        {
            GeneratePieces();
        }
    }

    private Texture2D GetTexture(Object obj)
    {
        if (obj == null) return null;
        if (obj is Texture2D tex) return tex;
        if (obj is Sprite sprite) return sprite.texture;
        return null;
    }

    private void GeneratePieces()
    {
        Texture2D sourceImage = GetTexture(sourceObject);
        if (sourceImage == null || targetData == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign source image/sprite and target data.", "OK");
            return;
        }

        // Check readability
        try
        {
            sourceImage.GetPixel(0, 0);
        }
        catch (UnityException)
        {
            EditorUtility.DisplayDialog("Error", $"Source image '{sourceImage.name}' must be marked as 'Read/Write' in Import Settings.", "OK");
            return;
        }

        // Folder logic: check if occupied
        string finalSavePath = savePath;
        if (Directory.Exists(finalSavePath) && Directory.GetFileSystemEntries(finalSavePath).Length > 0)
        {
            int choice = EditorUtility.DisplayDialogComplex("Folder Occupied", 
                $"The folder '{savePath}' is not empty. Do you want to override existing files or create a new subfolder?", 
                "Override", "Create New Subfolder", "Cancel");

            if (choice == 2) return; // Cancel
            
            if (choice == 1) // Create New Subfolder
            {
                finalSavePath = Path.Combine(finalSavePath, targetData.puzzleId);
                if (!Directory.Exists(finalSavePath))
                {
                    Directory.CreateDirectory(finalSavePath);
                }
            }
            // choice == 0 is Override, so we stick with finalSavePath = savePath
        }
        else if (!Directory.Exists(finalSavePath))
        {
            Directory.CreateDirectory(finalSavePath);
        }

        // If sourceObject is a sprite, we should use its rect for slicing
        Rect sourceRect = new Rect(0, 0, sourceImage.width, sourceImage.height);
        if (sourceObject is Sprite s)
        {
            sourceRect = s.rect;
        }

        int pieceBaseWidth = Mathf.RoundToInt(sourceRect.width / 3);
        int pieceBaseHeight = Mathf.RoundToInt(sourceRect.height / 3);
        
        // Define tab properties
        float radius = pieceBaseWidth * tabSizeRatio;
        float bulgeOffset = radius * tabBulgeOffset;
        
        // Generate random internal edges
        // hEdges[row][col_boundary] -> vertical line between columns
        int[,] hEdges = new int[3, 2];
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 2; c++)
                hEdges[r, c] = Random.value > 0.5f ? 1 : -1;
                
        // vEdges[row_boundary][col] -> horizontal line between rows
        int[,] vEdges = new int[2, 3];
        for (int r = 0; r < 2; r++)
            for (int c = 0; c < 3; c++)
                vEdges[r, c] = Random.value > 0.5f ? 1 : -1;

        // Uniform padding for all pieces to ensure same image sizes
        int padding = Mathf.CeilToInt(radius + bulgeOffset + 2); // +2 for antialiasing safety
        int texWidth = pieceBaseWidth + padding * 2;
        int texHeight = pieceBaseHeight + padding * 2;

        List<Sprite> generatedSprites = new List<Sprite>();

        for (int i = 0; i < 9; i++)
        {
            int row = i / 3;
            int col = i % 3;

            // Determine edges for this piece (1=Tab, -1=Hole, 0=Flat)
            int left = (col > 0) ? -hEdges[row, col - 1] : 0;
            int right = (col < 2) ? hEdges[row, col] : 0;
            int top = (row > 0) ? -vEdges[row - 1, col] : 0;
            int bottom = (row < 2) ? vEdges[row, col] : 0;

            Texture2D pieceTex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            
            // Source coordinates (accounting for padding)
            int sourceStartX = Mathf.RoundToInt(sourceRect.x + col * pieceBaseWidth - padding);
            int sourceStartY = Mathf.RoundToInt(sourceRect.y + (2 - row) * pieceBaseHeight - padding);

            // Super-sampling for antialiasing
            int ssFactor = 3; // 3x3 sampling
            float ssStep = 1f / ssFactor;
            float bleed = 1.0f; // 1 pixel overlap to prevent alpha seams at connections

            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    float coverage = 0;
                    
                    for (int sy = 0; sy < ssFactor; sy++)
                    {
                        for (int sx = 0; sx < ssFactor; sx++)
                        {
                            float px = (x - padding) + (sx + 0.5f) * ssStep;
                            float py = (y - padding) + (sy + 0.5f) * ssStep;

                            // Expand the base rect for connections to ensure overlap
                            float minX = (col > 0) ? -bleed : 0;
                            float maxX = (col < 2) ? pieceBaseWidth + bleed : pieceBaseWidth;
                            float minY = (row < 2) ? -bleed : 0; // row 2 is bottom in our logic
                            float maxY = (row > 0) ? pieceBaseHeight + bleed : pieceBaseHeight; // row 0 is top

                            bool subInside = px >= minX && px < maxX && py >= minY && py < maxY;
                            
                            // Check Left
                            if (left != 0) {
                                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(-left * bulgeOffset, pieceBaseHeight / 2f));
                                // Expand piece: increase tab radius, decrease hole radius
                                float r = radius + (left == 1 ? bleed : -bleed);
                                if (dist < r) subInside = (left == 1);
                            }
                            // Check Right
                            if (right != 0) {
                                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(pieceBaseWidth + right * bulgeOffset, pieceBaseHeight / 2f));
                                float r = radius + (right == 1 ? bleed : -bleed);
                                if (dist < r) subInside = (right == 1);
                            }
                            // Check Top
                            if (top != 0) {
                                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(pieceBaseWidth / 2f, pieceBaseHeight + top * bulgeOffset));
                                float r = radius + (top == 1 ? bleed : -bleed);
                                if (dist < r) subInside = (top == 1);
                            }
                            // Check Bottom
                            if (bottom != 0) {
                                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(pieceBaseWidth / 2f, -bottom * bulgeOffset));
                                float r = radius + (bottom == 1 ? bleed : -bleed);
                                if (dist < r) subInside = (bottom == 1);
                            }

                            if (subInside) coverage += 1;
                        }
                    }

                    float alpha = coverage / (ssFactor * ssFactor);

                    if (alpha > 0)
                    {
                        int sx = sourceStartX + x;
                        int sy = sourceStartY + y;
                        
                        if (sx >= 0 && sx < sourceImage.width && sy >= 0 && sy < sourceImage.height)
                        {
                            Color c = sourceImage.GetPixel(sx, sy);
                            c.a *= alpha;
                            pieceTex.SetPixel(x, y, c);
                        }
                        else
                        {
                            pieceTex.SetPixel(x, y, Color.clear);
                        }
                    }
                    else
                    {
                        pieceTex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            pieceTex.Apply();

            // Calculate pivot to maintain original center alignment (always 0.5, 0.5 for uniform size)
            float pivotX = 0.5f;
            float pivotY = 0.5f;

            byte[] bytes = pieceTex.EncodeToPNG();
            string fileName = $"{targetData.puzzleId}_piece_{i}.png";
            string fullPath = Path.Combine(finalSavePath, fileName);
            File.WriteAllBytes(fullPath, bytes);
            
            AssetDatabase.ImportAsset(fullPath);
            
            // Set as Sprite
            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(pivotX, pivotY);
                importer.SetTextureSettings(settings);
                
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Sprite pieceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            targetData.pieces[i] = pieceSprite;
            generatedSprites.Add(pieceSprite);
        }

        // Pack in Atlas
        string atlasPath = Path.Combine(finalSavePath, $"{targetData.puzzleId}_Atlas.spriteatlas");
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        // Configure Atlas
        SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings()
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 2
        };
        atlas.SetPackingSettings(packingSettings);

        SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings()
        {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear
        };
        atlas.SetTextureSettings(textureSettings);

        // Add sprites to atlas
        SpriteAtlasExtensions.Add(atlas, generatedSprites.ToArray());

        EditorUtility.SetDirty(targetData);
        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();

        CreateDebugCanvas(generatedSprites, pieceBaseWidth, pieceBaseHeight);

        EditorUtility.DisplayDialog("Success", "Generated 9 programmatic pieces, packed into atlas, and assigned to JigsawPuzzleData.", "OK");
    }

    private void CreateDebugCanvas(List<Sprite> sprites, int baseWidth, int baseHeight)
    {
        string canvasName = "JigsawDebugCanvas";
        GameObject existing = GameObject.Find(canvasName);
        if (existing != null) DestroyImmediate(existing);

        GameObject canvasGo = new GameObject(canvasName);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject rootGo = new GameObject("Puzzle_Preview_" + targetData.puzzleId);
        rootGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rootRect = rootGo.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(baseWidth * 3, baseHeight * 3);
        rootRect.anchoredPosition = Vector2.zero;

        for (int i = 0; i < sprites.Count; i++)
        {
            int row = i / 3;
            int col = i % 3;

            GameObject pieceGo = new GameObject($"Piece_{i}");
            pieceGo.transform.SetParent(rootGo.transform, false);
            Image img = pieceGo.AddComponent<Image>();
            img.sprite = sprites[i];
            img.SetNativeSize();

            RectTransform rt = pieceGo.GetComponent<RectTransform>();
            // Position relative to top-left of the 3x3 grid
            // Grid center is (0,0). 
            // col 0 -> -baseWidth, col 1 -> 0, col 2 -> baseWidth
            // row 0 -> baseHeight, row 1 -> 0, row 2 -> -baseHeight
            float xPos = (col - 1) * baseWidth;
            float yPos = (1 - row) * baseHeight;
            rt.anchoredPosition = new Vector2(xPos, yPos);
        }

        Selection.activeGameObject = canvasGo;
        EditorGUIUtility.PingObject(canvasGo);
    }
}
