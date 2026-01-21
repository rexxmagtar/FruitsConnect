using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Editor tool for swapping level node prefabs and connection animation prefabs
/// based on a LevelCreationConfig
/// </summary>
public class LevelSkinSwap : EditorWindow
{
    private LevelConfig selectedLevelConfig;
    private LevelCreationConfig selectedGenerationConfig;
    
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Fruit Connect/Level Skin Swap")]
    public static void ShowWindow()
    {
        LevelSkinSwap window = GetWindow<LevelSkinSwap>("Level Skin Swap");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Level Skin Swap Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool swaps all node prefabs in a level prefab and sets the connection animation prefab " +
            "in the LevelConfig based on the selected LevelCreationConfig.\n\n" +
            "Select a LevelConfig ScriptableObject and a LevelCreationConfig.",
            MessageType.Info
        );
        EditorGUILayout.Space();
        
        // Level Config selection
        GUILayout.Label("Level Config", EditorStyles.boldLabel);
        LevelConfig newLevelConfig = (LevelConfig)EditorGUILayout.ObjectField(
            "Level Config",
            selectedLevelConfig,
            typeof(LevelConfig),
            false
        );
        
        if (newLevelConfig != selectedLevelConfig)
        {
            selectedLevelConfig = newLevelConfig;
            
            // Validate that it has a level prefab
            if (selectedLevelConfig != null && selectedLevelConfig.LevelPrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Invalid Level Config",
                    "Selected LevelConfig does not have a Level Prefab assigned.",
                    "OK"
                );
                selectedLevelConfig = null;
            }
        }
        
        if (selectedLevelConfig != null && selectedLevelConfig.LevelPrefab != null)
        {
            EditorGUILayout.HelpBox(
                $"Level Prefab: {selectedLevelConfig.LevelPrefab.name}",
                MessageType.Info
            );
        }
        
        EditorGUILayout.Space();
        
        // Generation Config selection
        GUILayout.Label("Generation Config", EditorStyles.boldLabel);
        LevelCreationConfig newGenConfig = (LevelCreationConfig)EditorGUILayout.ObjectField(
            "Level Creation Config",
            selectedGenerationConfig,
            typeof(LevelCreationConfig),
            false
        );
        
        if (newGenConfig != selectedGenerationConfig)
        {
            selectedGenerationConfig = newGenConfig;
            
            // Validate config
            if (selectedGenerationConfig != null && !selectedGenerationConfig.IsValid())
            {
                EditorUtility.DisplayDialog(
                    "Invalid Config",
                    "Selected LevelCreationConfig is missing required prefabs. Check the console for details.",
                    "OK"
                );
            }
        }
        
        EditorGUILayout.Space();
        
        // Validation
        bool canSwap = selectedLevelConfig != null && 
                      selectedGenerationConfig != null && 
                      selectedLevelConfig.LevelPrefab != null;
        
        EditorGUI.BeginDisabledGroup(!canSwap);
        
        if (GUILayout.Button("Swap Level Skin", GUILayout.Height(30)))
        {
            PerformSkinSwap();
        }
        
        EditorGUI.EndDisabledGroup();
        
        if (!canSwap)
        {
            EditorGUILayout.HelpBox(
                "Please select both a LevelConfig and a LevelCreationConfig to proceed.",
                MessageType.Warning
            );
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void PerformSkinSwap()
    {
        if (selectedLevelConfig == null || selectedGenerationConfig == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select both a LevelConfig and a LevelCreationConfig.", "OK");
            return;
        }
        
        if (selectedLevelConfig.LevelPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Selected LevelConfig does not have a Level Prefab assigned.", "OK");
            return;
        }
        
        if (!selectedGenerationConfig.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Selected LevelCreationConfig is invalid. Check console for details.", "OK");
            return;
        }
        
        // Get the prefab from LevelConfig
        GameObject levelPrefab = selectedLevelConfig.LevelPrefab;
        string prefabPath = AssetDatabase.GetAssetPath(levelPrefab);
        
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Error", "Could not find asset path for level prefab.", "OK");
            return;
        }
        
        // Confirm action
        if (!EditorUtility.DisplayDialog(
            "Confirm Skin Swap",
            $"This will:\n" +
            $"1. Replace all node prefabs in '{levelPrefab.name}' with prefabs from '{selectedGenerationConfig.name}'\n" +
            $"2. Set the connection animation prefab in '{selectedLevelConfig.name}' to '{selectedGenerationConfig.AnimationPrefab?.name ?? "null"}'\n\n" +
            "This action cannot be undone. Continue?",
            "Yes",
            "Cancel"
        ))
        {
            return;
        }
        
        // Load prefab contents for editing
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load prefab contents.", "OK");
            return;
        }
        
        LevelController levelController = prefabRoot.GetComponent<LevelController>();
        if (levelController == null)
        {
            EditorUtility.DisplayDialog("Error", "Level prefab does not have a LevelController component.", "OK");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }
        
        Undo.RegisterCompleteObjectUndo(selectedLevelConfig, "Level Skin Swap");
        Undo.RegisterCompleteObjectUndo(prefabRoot, "Level Skin Swap");
        
        int nodesSwapped = 0;
        
        // Get all nodes from the level
        List<BaseNode> allNodes = levelController.GetAllNodes();
        
        // Swap each node
        foreach (BaseNode node in allNodes)
        {
            if (node == null) continue;
            
            GameObject newNodePrefab = null;
            NodeType nodeType = GetNodeType(node);
            
            switch (nodeType)
            {
                case NodeType.Producer:
                    newNodePrefab = selectedGenerationConfig.ProducerNodePrefab;
                    break;
                case NodeType.Consumer:
                    newNodePrefab = selectedGenerationConfig.ConsumerNodePrefab;
                    break;
                case NodeType.Neutral:
                    newNodePrefab = selectedGenerationConfig.NeutralNodePrefab;
                    break;
                case NodeType.NeutralDouble:
                    newNodePrefab = selectedGenerationConfig.DoubleNeutralNodePrefab;
                    if (newNodePrefab == null)
                    {
                        newNodePrefab = selectedGenerationConfig.NeutralNodePrefab; // Fallback to regular neutral
                    }
                    break;
            }
            
            if (newNodePrefab == null)
            {
                Debug.LogWarning($"No prefab found for node type {nodeType} in config. Skipping node {node.NodeID}.");
                continue;
            }
            
            // Swap the node
            if (SwapNode(node.gameObject, newNodePrefab, nodeType, prefabRoot))
            {
                nodesSwapped++;
            }
        }
        
        // Save the prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        
        // Set connection animation prefab in LevelConfig
        SerializedObject serializedLevelConfig = new SerializedObject(selectedLevelConfig);
        SerializedProperty animationPrefabProperty = serializedLevelConfig.FindProperty("connectionAnimationPrefab");
        
        if (animationPrefabProperty != null)
        {
            animationPrefabProperty.objectReferenceValue = selectedGenerationConfig.AnimationPrefab;
            serializedLevelConfig.ApplyModifiedProperties();
        }
        
        // Mark config as dirty
        EditorUtility.SetDirty(selectedLevelConfig);
        AssetDatabase.SaveAssets();
        
        // Show completion message
        EditorUtility.DisplayDialog(
            "Skin Swap Complete",
            $"Successfully swapped:\n" +
            $"- {nodesSwapped} nodes in prefab\n" +
            $"- Connection animation prefab set in LevelConfig",
            "OK"
        );
        
        Debug.Log($"Level Skin Swap Complete: Swapped {nodesSwapped} nodes and set connection animation prefab in LevelConfig.");
    }
    
    /// <summary>
    /// Determine the node type from a BaseNode component
    /// </summary>
    private NodeType GetNodeType(BaseNode node)
    {
        if (node is ProducerNode)
        {
            return NodeType.Producer;
        }
        else if (node is ConsumerNode)
        {
            return NodeType.Consumer;
        }
        else if (node is NeutralNode)
        {
            // Check if it's a double neutral by name
            if (node.gameObject.name.StartsWith("NeutralDouble_"))
            {
                return NodeType.NeutralDouble;
            }
            return NodeType.Neutral;
        }
        
        return NodeType.Neutral; // Default fallback
    }
    
    /// <summary>
    /// Swap a node GameObject with a new prefab while preserving all properties
    /// </summary>
    private bool SwapNode(GameObject oldNode, GameObject newPrefab, NodeType nodeType, GameObject levelRoot)
    {
        if (oldNode == null || newPrefab == null)
        {
            return false;
        }
        
        // Check if oldNode is part of a prefab asset - if so, we can't modify it directly
        if (PrefabUtility.IsPartOfPrefabAsset(oldNode))
        {
            Debug.LogWarning($"Node {oldNode.name} is part of a prefab asset and cannot be modified directly. Skipping.");
            return false;
        }
        
        BaseNode oldBaseNode = oldNode.GetComponent<BaseNode>();
        if (oldBaseNode == null)
        {
            Debug.LogWarning($"Node {oldNode.name} does not have a BaseNode component. Skipping.");
            return false;
        }
        
        // Store node properties
        string nodeID = oldBaseNode.NodeID;
        int maxOutgoingConnections = oldBaseNode.MaxOutgoingConnections;
        int weight = oldBaseNode.Weight;
        Vector3 position = oldNode.transform.position;
        Quaternion rotation = oldNode.transform.rotation;
        Transform parent = oldNode.transform.parent;
        
        // Store connection references
        List<Connection> outgoingConnections = new List<Connection>(oldBaseNode.OutgoingConnections);
        List<Connection> incomingConnections = new List<Connection>(oldBaseNode.IncomingConnections);
        
        // Instantiate new node
        GameObject newNode = PrefabUtility.InstantiatePrefab(newPrefab) as GameObject;
        if (newNode == null)
        {
            Debug.LogError($"Failed to instantiate prefab for node type {nodeType}.");
            return false;
        }
        
        // Set transform properties
        newNode.transform.position = position;
        newNode.transform.rotation = rotation;
        
        // Only set parent if it's not part of a prefab asset
        if (parent != null && !PrefabUtility.IsPartOfPrefabAsset(parent))
        {
            newNode.transform.SetParent(parent);
        }
        else if (levelRoot != null)
        {
            // Fallback to level root if parent is a prefab asset
            newNode.transform.SetParent(levelRoot.transform);
        }
        
        // Get BaseNode component from new node
        BaseNode newBaseNode = newNode.GetComponent<BaseNode>();
        if (newBaseNode == null)
        {
            Debug.LogError($"New prefab for {nodeType} does not have a BaseNode component.");
            DestroyImmediate(newNode);
            return false;
        }
        
        // Restore node properties
        newBaseNode.NodeID = nodeID;
        newBaseNode.MaxOutgoingConnections = maxOutgoingConnections;
        newBaseNode.Weight = weight;
        
        // Set name based on node type
        string nodeName = nodeType switch
        {
            NodeType.Producer => $"Producer_{nodeID}",
            NodeType.Consumer => $"Consumer_{nodeID}",
            NodeType.Neutral => $"Neutral_{nodeID}",
            NodeType.NeutralDouble => $"NeutralDouble_{nodeID}",
            _ => $"Node_{nodeID}"
        };
        newNode.name = nodeName;
        
        // Update connection references using reflection
        FieldInfo fromNodeField = typeof(Connection).GetField("fromNode", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo toNodeField = typeof(Connection).GetField("toNode", BindingFlags.NonPublic | BindingFlags.Instance);
        
        foreach (Connection connection in outgoingConnections)
        {
            if (connection != null)
            {
                // Update connection's fromNode reference using reflection
                if (fromNodeField != null)
                {
                    fromNodeField.SetValue(connection, newBaseNode);
                }
                
                // Re-add to new node's outgoing connections
                newBaseNode.AddOutgoingConnection(connection);
                
                // Update connection visual to reflect new node position
                connection.UpdateVisual();
            }
        }
        
        foreach (Connection connection in incomingConnections)
        {
            if (connection != null)
            {
                // Update connection's toNode reference using reflection
                if (toNodeField != null)
                {
                    toNodeField.SetValue(connection, newBaseNode);
                }
                
                // Re-add to new node's incoming connections
                newBaseNode.AddIncomingConnection(connection);
                
                // Update connection visual to reflect new node position
                connection.UpdateVisual();
            }
        }
        
        // Update LevelController's node list
        LevelController levelController = oldNode.GetComponentInParent<LevelController>();
        if (levelController != null)
        {
            // Remove old node and add new node
            // We'll need to update the LevelController's internal list
            // Since GetAllNodes() rebuilds from the list, we need to update the serialized list
            SerializedObject serializedLevel = new SerializedObject(levelController);
            SerializedProperty nodesProperty = serializedLevel.FindProperty("allNodes");
            
            if (nodesProperty != null && nodesProperty.isArray)
            {
                // Find and replace the old node with the new one
                for (int i = 0; i < nodesProperty.arraySize; i++)
                {
                    SerializedProperty element = nodesProperty.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == oldBaseNode)
                    {
                        element.objectReferenceValue = newBaseNode;
                        break;
                    }
                }
                serializedLevel.ApplyModifiedProperties();
            }
        }
        
        // Destroy old node - only if it's not a prefab asset
        if (!PrefabUtility.IsPartOfPrefabAsset(oldNode))
        {
            DestroyImmediate(oldNode);
        }
        else
        {
            Debug.LogWarning($"Cannot destroy node {oldNode.name} as it's part of a prefab asset.");
        }
        
        return true;
    }
}
