using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom editor window for creating and editing fruit connection levels
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    private LevelController currentLevel;
    private GameObject currentLevelObject;
    
    private Vector2 scrollPosition;
    private Vector2 nodeListScrollPosition;
    private Vector2 connectionScrollPosition;
    
    private int selectedNodeIndex = -1;
    private NodeType nodeTypeToAdd = NodeType.Neutral;
    private int maxOutgoingConnections = 2;
    
    private enum NodeType
    {
        Producer,
        Consumer,
        Neutral
    }
    
    [MenuItem("Tools/Fruit Connect Level Editor")]
    public static void ShowWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Fruit Connect Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        DrawLevelSection();
        EditorGUILayout.Space();
        
        if (currentLevel != null)
        {
            DrawNodeCreationSection();
            EditorGUILayout.Space();
            
            DrawNodeListSection();
            EditorGUILayout.Space();
            
            DrawConnectionMappingSection();
            EditorGUILayout.Space();
            
            DrawValidationSection();
            EditorGUILayout.Space();
            
            DrawSaveSection();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawLevelSection()
    {
        GUILayout.Label("Level Management", EditorStyles.boldLabel);
        
        if (GUILayout.Button("New Level", GUILayout.Height(30)))
        {
            CreateNewLevel();
        }
        
        if (GUILayout.Button("Load Selected Level", GUILayout.Height(30)))
        {
            LoadSelectedLevel();
        }
        
        if (currentLevel != null)
        {
            EditorGUILayout.HelpBox($"Current Level: {currentLevel.gameObject.name}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No level loaded. Create new or select a level GameObject in hierarchy.", MessageType.Warning);
        }
    }
    
    private void DrawNodeCreationSection()
    {
        GUILayout.Label("Add Node", EditorStyles.boldLabel);
        
        nodeTypeToAdd = (NodeType)EditorGUILayout.EnumPopup("Node Type", nodeTypeToAdd);
        maxOutgoingConnections = EditorGUILayout.IntField("Max Outgoing Connections", maxOutgoingConnections);
        
        if (GUILayout.Button("Add Node to Scene", GUILayout.Height(30)))
        {
            AddNode();
        }
        
        EditorGUILayout.HelpBox("Click 'Add Node' then position it in Scene view", MessageType.Info);
    }
    
    private void DrawNodeListSection()
    {
        GUILayout.Label("Nodes in Level", EditorStyles.boldLabel);
        
        List<BaseNode> nodes = currentLevel.GetAllNodes();
        
        if (nodes.Count == 0)
        {
            EditorGUILayout.HelpBox("No nodes in level yet", MessageType.Info);
            return;
        }
        
        nodeListScrollPosition = EditorGUILayout.BeginScrollView(nodeListScrollPosition, GUILayout.Height(200));
        
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            
            EditorGUILayout.BeginHorizontal("box");
            
            bool isSelected = selectedNodeIndex == i;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = Color.cyan;
            }
            
            string nodeTypeName = nodes[i].GetType().Name;
            string label = $"{nodes[i].NodeID} ({nodeTypeName}) - Max Out: {nodes[i].MaxOutgoingConnections}";
            
            if (GUILayout.Button(label, GUILayout.Width(300)))
            {
                selectedNodeIndex = i;
                Selection.activeGameObject = nodes[i].gameObject;
                SceneView.FrameLastActiveSceneView();
            }
            
            GUI.backgroundColor = originalColor;
            
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                DeleteNode(nodes[i]);
                selectedNodeIndex = -1;
                break;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawConnectionMappingSection()
    {
        GUILayout.Label("Connection Mappings", EditorStyles.boldLabel);
        
        if (selectedNodeIndex < 0)
        {
            EditorGUILayout.HelpBox("Select a node from the list above to edit its connections", MessageType.Info);
            return;
        }
        
        List<BaseNode> nodes = currentLevel.GetAllNodes();
        if (selectedNodeIndex >= nodes.Count) return;
        
        BaseNode selectedNode = nodes[selectedNodeIndex];
        if (selectedNode == null) return;
        
        // Check if this is a consumer node (consumers cannot have outgoing connections)
        if (selectedNode is ConsumerNode)
        {
            EditorGUILayout.HelpBox($"{selectedNode.NodeID} is a Consumer node.\n\nConsumers are endpoints and CANNOT have outgoing connections.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.HelpBox($"Editing connections for: {selectedNode.NodeID}", MessageType.Info);
        
        List<string> currentMappings = currentLevel.GetConnectionMapping(selectedNode.NodeID);
        
        connectionScrollPosition = EditorGUILayout.BeginScrollView(connectionScrollPosition, GUILayout.Height(250));
        
        bool changed = false;
        foreach (BaseNode targetNode in nodes)
        {
            if (targetNode == null || targetNode == selectedNode) continue;
            
            bool isConnected = currentMappings.Contains(targetNode.NodeID);
            
            // Check if reverse connection exists (bidirectional warning)
            List<string> reverseMappings = currentLevel.GetConnectionMapping(targetNode.NodeID);
            bool reverseExists = reverseMappings.Contains(selectedNode.NodeID);
            
            string label = $"{targetNode.NodeID} ({targetNode.GetType().Name})";
            if (reverseExists && isConnected)
            {
                label += " ⚠ BIDIRECTIONAL";
            }
            
            bool newValue = EditorGUILayout.ToggleLeft(label, isConnected);
            
            if (newValue != isConnected)
            {
                if (newValue)
                {
                    currentMappings.Add(targetNode.NodeID);
                    
                    // Warn about bidirectional connection
                    if (reverseExists)
                    {
                        EditorUtility.DisplayDialog(
                            "Bidirectional Connection",
                            $"Warning: Both {selectedNode.NodeID} → {targetNode.NodeID} and {targetNode.NodeID} → {selectedNode.NodeID} are now defined.\n\nConnections are treated as bidirectional during gameplay, so this creates redundancy.",
                            "OK"
                        );
                    }
                }
                else
                {
                    currentMappings.Remove(targetNode.NodeID);
                }
                changed = true;
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        // Show info about bidirectional connections
        EditorGUILayout.HelpBox("Note: Connections are bidirectional during gameplay. A→B prevents creating B→A at runtime.", MessageType.Info);
        
        if (changed)
        {
            currentLevel.UpdateConnectionMapping(selectedNode.NodeID, currentMappings);
            EditorUtility.SetDirty(currentLevel);
        }
    }
    
    private void DrawValidationSection()
    {
        GUILayout.Label("Level Validation", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Validate Level", GUILayout.Height(30)))
        {
            ValidateLevel();
        }
    }
    
    private void DrawSaveSection()
    {
        GUILayout.Label("Save Level", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Save Level (Creates Config + Adds to LevelsConfig)", GUILayout.Height(40)))
        {
            SaveLevelComplete();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Save Prefab Only", GUILayout.Height(25)))
        {
            SaveLevelPrefab();
        }
        
        EditorGUILayout.HelpBox("Use 'Save Level' to automatically create LevelConfig and add to LevelsConfig. Or use 'Save Prefab Only' for manual setup.", MessageType.Info);
    }
    
    private void CreateNewLevel()
    {
        GameObject levelObj = new GameObject("New_Level");
        currentLevelObject = levelObj;
        currentLevel = levelObj.AddComponent<LevelController>();
        
        Selection.activeGameObject = levelObj;
        
        Debug.Log("Created new level. Add nodes and configure connections.");
    }
    
    private void LoadSelectedLevel()
    {
        if (Selection.activeGameObject != null)
        {
            LevelController level = Selection.activeGameObject.GetComponent<LevelController>();
            if (level != null)
            {
                currentLevel = level;
                currentLevelObject = level.gameObject;
                selectedNodeIndex = -1;
                Debug.Log($"Loaded level: {level.gameObject.name}");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Selected GameObject doesn't have a LevelController component", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "No GameObject selected", "OK");
        }
    }
    
    private void AddNode()
    {
        if (currentLevel == null) return;
        
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nodeObj.transform.SetParent(currentLevel.transform);
        nodeObj.transform.localPosition = Vector3.zero;
        
        BaseNode node = null;
        string nodeID = $"Node_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        
        switch (nodeTypeToAdd)
        {
            case NodeType.Producer:
                node = nodeObj.AddComponent<ProducerNode>();
                nodeObj.name = $"Producer_{nodeID}";
                break;
            case NodeType.Consumer:
                node = nodeObj.AddComponent<ConsumerNode>();
                nodeObj.name = $"Consumer_{nodeID}";
                break;
            case NodeType.Neutral:
                node = nodeObj.AddComponent<NeutralNode>();
                nodeObj.name = $"Neutral_{nodeID}";
                break;
        }
        
        if (node != null)
        {
            node.NodeID = nodeID;
            node.MaxOutgoingConnections = maxOutgoingConnections;
            currentLevel.AddNode(node);
            
            Selection.activeGameObject = nodeObj;
            SceneView.FrameLastActiveSceneView();
            
            EditorUtility.SetDirty(currentLevel);
        }
    }
    
    private void DeleteNode(BaseNode node)
    {
        if (node == null || currentLevel == null) return;
        
        bool confirm = EditorUtility.DisplayDialog(
            "Delete Node",
            $"Are you sure you want to delete node {node.NodeID}?",
            "Yes",
            "No"
        );
        
        if (confirm)
        {
            currentLevel.RemoveNode(node);
            DestroyImmediate(node.gameObject);
            EditorUtility.SetDirty(currentLevel);
        }
    }
    
    private List<string> ValidateLevelInternal()
    {
        List<string> errors = new List<string>();
        
        if (currentLevel == null)
        {
            errors.Add("No level loaded");
            return errors;
        }
        
        // Check for producers
        var producers = currentLevel.GetProducerNodes();
        if (producers.Count == 0)
        {
            errors.Add("No Producer nodes found! Level needs at least one producer.");
        }
        
        // Check for consumers
        var consumers = currentLevel.GetConsumerNodes();
        if (consumers.Count == 0)
        {
            errors.Add("No Consumer nodes found! Level needs at least one consumer.");
        }
        
        // Check if all consumers can potentially reach a producer
        if (producers.Count > 0 && consumers.Count > 0)
        {
            foreach (var consumer in consumers)
            {
                if (!CanReachProducer(consumer))
                {
                    errors.Add($"Consumer {consumer.NodeID} cannot reach any producer!");
                }
            }
        }
        
        return errors;
    }
    
    private void ValidateLevel()
    {
        if (currentLevel == null) return;
        
        List<string> errors = ValidateLevelInternal();
        List<string> warnings = new List<string>();
        
        // Check for nodes without connections (warnings only)
        foreach (var node in currentLevel.GetAllNodes())
        {
            // Consumer nodes should NOT have outgoing connections
            if (node is ConsumerNode)
            {
                List<string> mappings = currentLevel.GetConnectionMapping(node.NodeID);
                if (mappings.Count > 0)
                {
                    errors.Add($"Consumer {node.NodeID} has outgoing connections defined - consumers cannot have outputs!");
                }
            }
            else
            {
                List<string> mappings = currentLevel.GetConnectionMapping(node.NodeID);
                if (mappings.Count == 0)
                {
                    warnings.Add($"Node {node.NodeID} has no outgoing connections defined");
                }
            }
        }
        
        // Display results
        string message = "";
        
        if (errors.Count == 0 && warnings.Count == 0)
        {
            message = "✓ Level validation passed! Level is solvable.";
            EditorUtility.DisplayDialog("Validation Success", message, "OK");
        }
        else
        {
            if (errors.Count > 0)
            {
                message += "ERRORS:\n" + string.Join("\n", errors) + "\n\n";
            }
            if (warnings.Count > 0)
            {
                message += "WARNINGS:\n" + string.Join("\n", warnings);
            }
            
            EditorUtility.DisplayDialog("Validation Results", message, "OK");
        }
        
        Debug.Log($"Level Validation Results:\n{message}");
    }
    
    private bool CanReachProducer(ConsumerNode consumer)
    {
        // Use BFS to check if consumer can potentially reach any producer
        // This considers all possible connection paths based on mappings
        
        HashSet<string> visited = new HashSet<string>();
        Queue<string> queue = new Queue<string>();
        
        queue.Enqueue(consumer.NodeID);
        visited.Add(consumer.NodeID);
        
        var allNodes = currentLevel.GetAllNodes();
        var nodeDict = allNodes.ToDictionary(n => n.NodeID, n => n);
        
        while (queue.Count > 0)
        {
            string currentID = queue.Dequeue();
            
            // Check all nodes that can connect TO this node
            foreach (var node in allNodes)
            {
                List<string> targets = currentLevel.GetConnectionMapping(node.NodeID);
                
                if (targets.Contains(currentID) && !visited.Contains(node.NodeID))
                {
                    // This node can connect to current node
                    if (node is ProducerNode)
                    {
                        return true; // Found a path to producer!
                    }
                    
                    visited.Add(node.NodeID);
                    queue.Enqueue(node.NodeID);
                }
            }
        }
        
        return false;
    }
    
    private void SaveLevelPrefab()
    {
        if (currentLevel == null) return;
        
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Level Prefab",
            currentLevel.gameObject.name,
            "prefab",
            "Save level as prefab"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(currentLevel.gameObject, path);
            Debug.Log($"Level prefab saved to: {path}");
            EditorUtility.DisplayDialog("Success", $"Level prefab saved to:\n{path}\n\nNote: LevelConfig was not created. Use 'Save Level' button to auto-create config.", "OK");
        }
    }
    
    private void SaveLevelComplete()
    {
        if (currentLevel == null)
        {
            EditorUtility.DisplayDialog("Error", "No level loaded to save", "OK");
            return;
        }
        
        // First, validate the level
        List<string> errors = ValidateLevelInternal();
        if (errors.Count > 0)
        {
            string errorMsg = "Cannot save level with errors:\n\n" + string.Join("\n", errors);
            EditorUtility.DisplayDialog("Validation Failed", errorMsg, "OK");
            return;
        }
        
        // Find LevelsConfig to determine level number
        LevelsConfig levelsConfig = FindLevelsConfig();
        int levelNumber = 1;
        
        if (levelsConfig != null)
        {
            levelNumber = levelsConfig.GetTotalLevels() + 1;
        }
        
        // Generate automatic name and path
        string levelName = $"Level{levelNumber}";
        string prefabDirectory = "Assets/Prefabs/Levels";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(prefabDirectory))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Levels");
        }
        
        string prefabPath = $"{prefabDirectory}/{levelName}.prefab";
        
        // Check if file already exists
        if (System.IO.File.Exists(prefabPath))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "File Exists",
                $"{levelName}.prefab already exists.\n\nDo you want to overwrite it?",
                "Overwrite",
                "Cancel"
            );
            
            if (!overwrite)
            {
                return;
            }
        }
        
        // Update the level GameObject name before saving
        currentLevel.gameObject.name = levelName;
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(currentLevel.gameObject, prefabPath);
        Debug.Log($"Level prefab saved to: {prefabPath}");
        
        // Step 2: Create LevelConfig in ScriptableObjects/Levels directory
        string configDirectory = "Assets/ScriptableObjects/Levels";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder(configDirectory))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Levels");
        }
        
        string configPath = $"{configDirectory}/{levelName}_Config.asset";
        
        // Check if config already exists
        LevelConfig existingConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>(configPath);
        LevelConfig levelConfig;
        
        if (existingConfig != null)
        {
            // Update existing config
            levelConfig = existingConfig;
            Debug.Log($"Updating existing LevelConfig at: {configPath}");
        }
        else
        {
            // Create new config
            levelConfig = ScriptableObject.CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(levelConfig, configPath);
            Debug.Log($"Created new LevelConfig at: {configPath}");
        }
        
        // Set config properties using reflection
        var prefabField = typeof(LevelConfig).GetField("levelPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var coinField = typeof(LevelConfig).GetField("coinReward", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = typeof(LevelConfig).GetField("levelName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (prefabField != null) prefabField.SetValue(levelConfig, prefab);
        if (coinField != null) coinField.SetValue(levelConfig, 10); // Default coin reward
        if (nameField != null) nameField.SetValue(levelConfig, levelName);
        
        EditorUtility.SetDirty(levelConfig);
        AssetDatabase.SaveAssets();
        
        // Step 3: Automatically add to LevelsConfig (if found)
        if (levelsConfig != null)
        {
            AddLevelToConfig(levelsConfig, levelConfig);
        }
        else
        {
            // No LevelsConfig found, ask to create
            bool createConfig = EditorUtility.DisplayDialog(
                "Create LevelsConfig?",
                $"Level saved successfully!\n\nPrefab: {levelName}.prefab\nConfig: {levelName}_Config.asset\n\nNo LevelsConfig found. Create one now?",
                "Create",
                "Skip"
            );
            
            if (createConfig)
            {
                CreateLevelsConfig(levelConfig);
            }
        }
        
        // Select the config in project
        Selection.activeObject = levelConfig;
        EditorGUIUtility.PingObject(levelConfig);
        
        EditorUtility.DisplayDialog(
            "Success!",
            $"Level {levelNumber} saved successfully!\n\n✓ Prefab: {prefabPath}\n✓ Config: {configPath}\n✓ Added to LevelsConfig\n\nYou can edit coin reward in the Inspector.",
            "OK"
        );
    }
    
    private LevelsConfig FindLevelsConfig()
    {
        // Find first LevelsConfig in project
        string[] guids = AssetDatabase.FindAssets("t:LevelsConfig");
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LevelsConfig>(path);
        }
        
        return null;
    }
    
    private void AddToLevelsConfig(LevelConfig newLevelConfig)
    {
        // Find all LevelsConfig assets
        string[] guids = AssetDatabase.FindAssets("t:LevelsConfig");
        
        if (guids.Length == 0)
        {
            bool create = EditorUtility.DisplayDialog(
                "No LevelsConfig Found",
                "No LevelsConfig found in project. Create one now?",
                "Create",
                "Cancel"
            );
            
            if (create)
            {
                CreateLevelsConfig(newLevelConfig);
            }
            return;
        }
        
        // If only one, use it
        if (guids.Length == 1)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            LevelsConfig levelsConfig = AssetDatabase.LoadAssetAtPath<LevelsConfig>(path);
            AddLevelToConfig(levelsConfig, newLevelConfig);
            return;
        }
        
        // Multiple found - let user choose
        GenericMenu menu = new GenericMenu();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelsConfig config = AssetDatabase.LoadAssetAtPath<LevelsConfig>(path);
            string displayName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            menu.AddItem(new GUIContent(displayName), false, () => AddLevelToConfig(config, newLevelConfig));
        }
        menu.ShowAsContext();
    }
    
    private void AddLevelToConfig(LevelsConfig levelsConfig, LevelConfig newLevelConfig)
    {
        if (levelsConfig == null) return;
        
        // Use reflection to access private field
        var levelsField = typeof(LevelsConfig).GetField("levels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (levelsField != null)
        {
            LevelConfig[] currentLevels = (LevelConfig[])levelsField.GetValue(levelsConfig);
            
            // Check if already in array
            if (currentLevels != null && System.Array.IndexOf(currentLevels, newLevelConfig) >= 0)
            {
                EditorUtility.DisplayDialog("Already Added", "This level is already in the LevelsConfig", "OK");
                return;
            }
            
            // Add to array
            LevelConfig[] newLevels;
            if (currentLevels == null || currentLevels.Length == 0)
            {
                newLevels = new LevelConfig[] { newLevelConfig };
            }
            else
            {
                newLevels = new LevelConfig[currentLevels.Length + 1];
                currentLevels.CopyTo(newLevels, 0);
                newLevels[currentLevels.Length] = newLevelConfig;
            }
            
            levelsField.SetValue(levelsConfig, newLevels);
            EditorUtility.SetDirty(levelsConfig);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Added level to {levelsConfig.name} at index {newLevels.Length - 1}");
            EditorUtility.DisplayDialog("Success", $"Level added to {levelsConfig.name}!\n\nPosition: Level {newLevels.Length}", "OK");
        }
    }
    
    private void CreateLevelsConfig(LevelConfig firstLevel)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create LevelsConfig",
            "LevelsConfig",
            "asset",
            "Create new LevelsConfig asset"
        );
        
        if (string.IsNullOrEmpty(path)) return;
        
        LevelsConfig levelsConfig = ScriptableObject.CreateInstance<LevelsConfig>();
        
        // Use reflection to set the levels array
        var levelsField = typeof(LevelsConfig).GetField("levels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (levelsField != null)
        {
            levelsField.SetValue(levelsConfig, new LevelConfig[] { firstLevel });
        }
        
        AssetDatabase.CreateAsset(levelsConfig, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Created LevelsConfig at: {path}");
        EditorUtility.DisplayDialog("Success", $"LevelsConfig created with first level!\n\n{path}", "OK");
        
        Selection.activeObject = levelsConfig;
        EditorGUIUtility.PingObject(levelsConfig);
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (currentLevel == null) return;
        
        // Draw handles for moving nodes
        foreach (BaseNode node in currentLevel.GetAllNodes())
        {
            if (node == null) continue;
            
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(node.transform.position, Quaternion.identity);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(node.transform, "Move Node");
                node.transform.position = newPos;
                EditorUtility.SetDirty(node.transform);
            }
            
            // Draw connection lines in editor
            List<string> targets = currentLevel.GetConnectionMapping(node.NodeID);
            foreach (string targetID in targets)
            {
                BaseNode targetNode = currentLevel.GetAllNodes().Find(n => n != null && n.NodeID == targetID);
                if (targetNode != null)
                {
                    Handles.color = Color.green;
                    Handles.DrawLine(node.transform.position, targetNode.transform.position);
                }
            }
        }
    }
}

