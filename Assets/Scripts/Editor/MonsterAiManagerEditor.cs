using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonsterAiManager))]
public class MonsterAiManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();

        MonsterAiManager manager = (MonsterAiManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Helpers", EditorStyles.boldLabel);

        if (GUILayout.Button("Auto-Fit to Level Bounds"))
        {
            AutoFitToBounds(manager);
            EditorUtility.SetDirty(manager);
        }
        
        EditorGUILayout.HelpBox("The red lines in the Scene view show the spawn zone grid. The center is automatically derived from the Level Controller's Terrain or Nodes.", MessageType.Info);
    }

    private void AutoFitToBounds(MonsterAiManager manager)
    {
        // Use reflection or serializable fields to get currentLevel if it's private
        // Actually currentLevel is serialized but private. We can access it via serializedObject.
        SerializedProperty levelProp = serializedObject.FindProperty("currentLevel");
        LevelController level = levelProp.objectReferenceValue as LevelController;

        if (level == null)
        {
            // Try to find one in the scene if not assigned
            level = FindFirstObjectByType<LevelController>();
            if (level != null)
            {
                levelProp.objectReferenceValue = level;
                serializedObject.ApplyModifiedProperties();
            }
        }

        if (level != null)
        {
            Bounds bounds = new Bounds();
            bool boundsSet = false;

            if (level.TerrainMeshRenderer != null)
            {
                bounds = level.TerrainMeshRenderer.bounds;
                boundsSet = true;
            }
            else
            {
                var nodes = level.GetAllNodes();
                if (nodes.Count > 0)
                {
                    bounds = new Bounds(nodes[0].transform.position, Vector3.zero);
                    foreach (var node in nodes)
                    {
                        bounds.Encapsulate(node.transform.position);
                    }
                    bounds.Expand(2f);
                    boundsSet = true;
                }
            }

            if (boundsSet)
            {
                SerializedProperty widthProp = serializedObject.FindProperty("spawnZoneWidth");
                SerializedProperty heightProp = serializedObject.FindProperty("spawnZoneHeight");
                
                widthProp.floatValue = bounds.size.x;
                heightProp.floatValue = bounds.size.z;
                
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"Auto-fitted spawn zone to: {bounds.size.x}x{bounds.size.z}");
            }
        }
        else
        {
            Debug.LogWarning("No LevelController found to fit bounds to!");
        }
    }
}
