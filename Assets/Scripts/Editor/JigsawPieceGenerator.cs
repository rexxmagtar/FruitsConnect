using UnityEngine;
using UnityEditor;
using System.IO;
using JigsawSystem;

public class JigsawPieceGenerator : EditorWindow
{
    private Object sourceObject;
    private JigsawSystem.JigsawPuzzleData targetData;
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
        targetData = (JigsawSystem.JigsawPuzzleData)EditorGUILayout.ObjectField("Target JigsawPuzzleData", targetData, typeof(JigsawSystem.JigsawPuzzleData), false);
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

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
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

        // Padding to accommodate tabs
        int padding = Mathf.CeilToInt(radius * 2);
        int texWidth = pieceBaseWidth + padding * 2;
        int texHeight = pieceBaseHeight + padding * 2;

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

            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    float px = x - padding;
                    float py = y - padding;

                    bool inside = px >= 0 && px < pieceBaseWidth && py >= 0 && py < pieceBaseHeight;
                    
                    // Check Left
                    if (left != 0) {
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(-left * bulgeOffset, pieceBaseHeight / 2f));
                        if (dist < radius) inside = (left == 1);
                    }
                    // Check Right
                    if (right != 0) {
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(pieceBaseWidth + right * bulgeOffset, pieceBaseHeight / 2f));
                        if (dist < radius) inside = (right == 1);
                    }
                    // Check Top
                    if (top != 0) {
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(pieceBaseWidth / 2f, pieceBaseHeight + top * bulgeOffset));
                        if (dist < radius) inside = (top == 1);
                    }
                    // Check Bottom
                    if (bottom != 0) {
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(pieceBaseWidth / 2f, -bottom * bulgeOffset));
                        if (dist < radius) inside = (bottom == 1);
                    }

                    if (inside)
                    {
                        int sx = sourceStartX + x;
                        int sy = sourceStartY + y;
                        
                        // Handle source image bounds
                        if (sx >= 0 && sx < sourceImage.width && sy >= 0 && sy < sourceImage.height)
                        {
                            pieceTex.SetPixel(x, y, sourceImage.GetPixel(sx, sy));
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

            byte[] bytes = pieceTex.EncodeToPNG();
            string fileName = $"{targetData.puzzleId}_piece_{i}.png";
            string fullPath = Path.Combine(savePath, fileName);
            File.WriteAllBytes(fullPath, bytes);
            
            AssetDatabase.ImportAsset(fullPath);
            
            // Set as Sprite
            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            Sprite pieceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            targetData.pieces[i] = pieceSprite;
        }

        EditorUtility.SetDirty(targetData);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Success", "Generated 9 programmatic pieces and assigned to JigsawPuzzleData.", "OK");
    }
}
