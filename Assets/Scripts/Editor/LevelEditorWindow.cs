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
    private int nodeWeight = 0;
    
    // Configuration
    private LevelCreationConfig levelCreationConfig;
    
    // Auto-generation settings
    private int autoGenProducerCount = 2;
    private int autoGenConsumerCount = 3;
    private int autoGenNeutralCount = 10;
    private DifficultyTier autoGenDifficulty = DifficultyTier.Medium;
    private GraphPattern autoGenPattern = GraphPattern.Mixed;
    private bool showAutoGenSection = false;
    
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
        
        DrawConfigSection();
        EditorGUILayout.Space();
        
        DrawAutoGenerationSection();
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
    
    private void DrawConfigSection()
    {
        GUILayout.Label("Level Creation Configuration", EditorStyles.boldLabel);
        
        LevelCreationConfig newConfig = (LevelCreationConfig)EditorGUILayout.ObjectField(
            "Creation Config",
            levelCreationConfig,
            typeof(LevelCreationConfig),
            false
        );
        
        if (newConfig != levelCreationConfig)
        {
            levelCreationConfig = newConfig;
            
            // Validate config when assigned
            if (levelCreationConfig != null)
            {
                if (!levelCreationConfig.IsValid())
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Config",
                        "The LevelCreationConfig is missing required prefab assignments. Please assign all node prefabs in the config asset.",
                        "OK"
                    );
                }
            }
        }
        
        if (levelCreationConfig == null)
        {
            EditorGUILayout.HelpBox("No LevelCreationConfig assigned. Assign one to use prefabs for node creation, or create nodes from primitives.", MessageType.Warning);
            
            if (GUILayout.Button("Find or Create LevelCreationConfig"))
            {
                FindOrCreateLevelCreationConfig();
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"Using: {levelCreationConfig.name}", MessageType.Info);
        }
    }
    
    private void DrawAutoGenerationSection()
    {
        showAutoGenSection = EditorGUILayout.Foldout(showAutoGenSection, "Automatic Level Generation", true, EditorStyles.foldoutHeader);
        
        if (!showAutoGenSection) return;
        
        EditorGUILayout.BeginVertical("box");
        
        GUILayout.Label("Node Counts", EditorStyles.boldLabel);
        autoGenProducerCount = EditorGUILayout.IntSlider("Producers", autoGenProducerCount, 1, 10);
        autoGenConsumerCount = EditorGUILayout.IntSlider("Consumers", autoGenConsumerCount, 1, 10);
        autoGenNeutralCount = EditorGUILayout.IntSlider("Neutral Nodes", autoGenNeutralCount, 5, 30);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Layout Pattern", EditorStyles.boldLabel);
        autoGenPattern = (GraphPattern)EditorGUILayout.EnumPopup("Graph Pattern", autoGenPattern);
        EditorGUILayout.HelpBox(GetPatternDescription(autoGenPattern), MessageType.Info);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Difficulty", EditorStyles.boldLabel);
        autoGenDifficulty = (DifficultyTier)EditorGUILayout.EnumPopup("Difficulty Tier", autoGenDifficulty);
        
        EditorGUILayout.Space();
        
        // Show difficulty info
        string difficultyInfo = GetDifficultyInfo(autoGenDifficulty);
        EditorGUILayout.HelpBox(difficultyInfo, MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (levelCreationConfig == null)
        {
            EditorGUILayout.HelpBox("Please assign a LevelCreationConfig first!", MessageType.Warning);
        }
        else
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            
            if (GUILayout.Button("Generate Level Automatically", GUILayout.Height(40)))
            {
                GenerateLevelAutomatically();
            }
            
            GUI.backgroundColor = originalColor;
        }
        
        EditorGUILayout.HelpBox("Auto-generation will:\n• Create a new level\n• Arrange nodes in selected pattern\n• Generate core solvable paths with cycles\n• Add noise (dead-end paths) for complexity\n• Ensure level is solvable", MessageType.Info);
        
        EditorGUILayout.EndVertical();
    }
    
    private string GetDifficultyInfo(DifficultyTier difficulty)
    {
        return difficulty switch
        {
            DifficultyTier.Easy => "Easy: More connections, simpler paths, less noise",
            DifficultyTier.Medium => "Medium: Balanced connections and complexity",
            DifficultyTier.Hard => "Hard: Fewer connections, complex paths, more noise",
            DifficultyTier.Expert => "Expert: Minimal connections, very complex, heavy noise",
            _ => ""
        };
    }
    
    private string GetPatternDescription(GraphPattern pattern)
    {
        return pattern switch
        {
            GraphPattern.Triangular => "Triangular: Pyramid/triangle layers - strategic vertical flow",
            GraphPattern.Grid => "Grid: Rectangular layout - multiple parallel paths",
            GraphPattern.Circular => "Circular: Concentric rings - radial paths with cycles",
            GraphPattern.Diamond => "Diamond: Rhombus shape - focused center with spreading edges",
            GraphPattern.Tree => "Tree: Hierarchical branching - clear top-down structure",
            GraphPattern.Mixed => "Mixed: Combination of patterns - varied and complex",
            _ => ""
        };
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
        
        // Show weight input only for neutral nodes
        if (nodeTypeToAdd == NodeType.Neutral)
        {
            nodeWeight = EditorGUILayout.IntSlider("Node Weight", nodeWeight, -3, 3);
            EditorGUILayout.HelpBox("Weight: Positive = gives energy, Negative = costs energy", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Producers and Consumers have no energy cost/gain (weight = 0)", MessageType.Info);
        }
        
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
            string weightStr = nodes[i].Weight != 0 ? $", Weight: {nodes[i].Weight:+#;-#;0}" : "";
            string label = $"{nodes[i].NodeID} ({nodeTypeName}) - Max Out: {nodes[i].MaxOutgoingConnections}{weightStr}";
            
            if (GUILayout.Button(label, GUILayout.Width(350)))
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
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Validate Structure", GUILayout.Height(30)))
        {
            ValidateLevel();
        }
        
        if (GUILayout.Button("Check Solvability", GUILayout.Height(30)))
        {
            ValidateSolvability();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox(
            "Structure: Checks connections and reachability\n" +
            "Solvability: Verifies level can actually be solved",
            MessageType.Info);
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
        GameObject levelObj;
        
        // Use template from config if available
        if (levelCreationConfig != null && levelCreationConfig.LevelTemplate != null)
        {
            levelObj = Instantiate(levelCreationConfig.LevelTemplate);
            levelObj.name = "New_Level";
            Debug.Log("Created new level from template.");
        }
        else
        {
            levelObj = new GameObject("New_Level");
            Debug.Log("Created new empty level (no template in config).");
        }
        
        currentLevelObject = levelObj;
        
        // Ensure LevelController component exists
        currentLevel = levelObj.GetComponent<LevelController>();
        if (currentLevel == null)
        {
            currentLevel = levelObj.AddComponent<LevelController>();
        }
        
        Selection.activeGameObject = levelObj;
        
        Debug.Log("Add nodes and configure connections.");
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
        
        GameObject nodeObj = null;
        BaseNode node = null;
        string nodeID = $"Node_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        
        // Try to use prefab from config first
        if (levelCreationConfig != null)
        {
            GameObject prefab = levelCreationConfig.GetNodePrefabByType(nodeTypeToAdd);
            
            if (prefab != null)
            {
                // Instantiate from prefab
                nodeObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                
                if (nodeObj != null)
                {
                    nodeObj.transform.SetParent(currentLevel.transform);
                    nodeObj.transform.localPosition = Vector3.zero;
                    
                    node = nodeObj.GetComponent<BaseNode>();
                    
                    switch (nodeTypeToAdd)
                    {
                        case NodeType.Producer:
                            nodeObj.name = $"Producer_{nodeID}";
                            break;
                        case NodeType.Consumer:
                            nodeObj.name = $"Consumer_{nodeID}";
                            break;
                        case NodeType.Neutral:
                            nodeObj.name = $"Neutral_{nodeID}";
                            break;
                    }
                    
                    Debug.Log($"Created node from prefab: {prefab.name}");
                }
            }
            else
            {
                Debug.LogWarning($"No prefab assigned for {nodeTypeToAdd} in LevelCreationConfig. Creating from primitive.");
            }
        }
        
        // Fallback: Create from primitive if config not available or prefab missing
        if (nodeObj == null)
        {
            nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObj.transform.SetParent(currentLevel.transform);
            nodeObj.transform.localPosition = Vector3.zero;
            
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
            
            Debug.Log("Created node from primitive sphere (no prefab available).");
        }
        
        if (node != null)
        {
            node.NodeID = nodeID;
            node.MaxOutgoingConnections = maxOutgoingConnections;
            
            // Set weight for neutral nodes
            if (nodeTypeToAdd == NodeType.Neutral)
            {
                node.Weight = nodeWeight;
            }
            else
            {
                node.Weight = 0; // Producers and consumers have no weight
            }
            
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
            message = "✓ Level structure validation passed!";
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
    
    private void ValidateSolvability()
    {
        if (currentLevel == null) return;
        
        List<BaseNode> allNodes = currentLevel.GetAllNodes();
        
        if (allNodes.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No nodes in level to validate!", "OK");
            return;
        }
        
        // Check solvability
        bool isSolvable = SolutionValidator.IsLevelSolvable(allNodes, currentLevel);
        
        // Calculate metrics
        GraphMetrics metrics = GraphMetricsCalculator.Calculate(allNodes, currentLevel);
        
        string message;
        if (isSolvable)
        {
            message = "✓ LEVEL IS SOLVABLE!\n\n" +
                     "At least one valid solution exists where all producers can reach consumers " +
                     "simultaneously without conflicts.\n\n" +
                     $"Graph Metrics:\n" +
                     $"• Edges: {metrics.EdgeCount}\n" +
                     $"• Density: {metrics.GraphDensity:F2}\n" +
                     $"• Avg Path Length: {metrics.AveragePathLength:F1}\n" +
                     $"• Max Path Length: {metrics.MaxPathLength}\n" +
                     $"• Complexity: {metrics.ComplexityScore:F1}";
            
            Debug.Log($"✓ Level is SOLVABLE\n{metrics}");
            EditorUtility.DisplayDialog("Solvability Check", message, "OK");
        }
        else
        {
            message = "⚠ LEVEL MAY BE UNSOLVABLE!\n\n" +
                     "Could not find a valid solution where all producers can reach consumers " +
                     "simultaneously without conflicts.\n\n" +
                     "Possible issues:\n" +
                     "• Not enough connection capacity\n" +
                     "• Bottleneck nodes blocking multiple paths\n" +
                     "• Insufficient neutral nodes\n" +
                     "• Conflicting path requirements\n\n" +
                     $"Graph Metrics:\n" +
                     $"• Edges: {metrics.EdgeCount}\n" +
                     $"• Density: {metrics.GraphDensity:F2}\n" +
                     $"• Avg Path Length: {metrics.AveragePathLength:F1}";
            
            Debug.LogWarning($"⚠ Level may be UNSOLVABLE\n{metrics}");
            EditorUtility.DisplayDialog("Solvability Check", message, "OK");
        }
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
        var energyField = typeof(LevelConfig).GetField("startingEnergy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (prefabField != null) prefabField.SetValue(levelConfig, prefab);
        if (coinField != null) coinField.SetValue(levelConfig, 10); // Default coin reward
        if (nameField != null) nameField.SetValue(levelConfig, levelName);
        if (energyField != null) energyField.SetValue(levelConfig, 5); // Default starting energy
        
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
    
    private void FindOrCreateLevelCreationConfig()
    {
        // Try to find existing config first
        string[] guids = AssetDatabase.FindAssets("t:LevelCreationConfig");
        
        if (guids.Length > 0)
        {
            string configPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            levelCreationConfig = AssetDatabase.LoadAssetAtPath<LevelCreationConfig>(configPath);
            Debug.Log($"Found existing LevelCreationConfig at: {configPath}");
            
            if (!levelCreationConfig.IsValid())
            {
                EditorUtility.DisplayDialog(
                    "Config Found",
                    $"Found config at:\n{configPath}\n\nBut it's missing prefab assignments. Please configure it in the Inspector.",
                    "OK"
                );
            }
            
            // Select it in project view
            Selection.activeObject = levelCreationConfig;
            EditorGUIUtility.PingObject(levelCreationConfig);
            return;
        }
        
        // No config found, create new one
        bool create = EditorUtility.DisplayDialog(
            "Create LevelCreationConfig?",
            "No LevelCreationConfig found in project.\n\nCreate a new one?",
            "Create",
            "Cancel"
        );
        
        if (!create) return;
        
        // Ensure directory exists
        string directory = "Assets/ScriptableObjects";
        if (!AssetDatabase.IsValidFolder(directory))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        
        string path = $"{directory}/LevelCreationConfig.asset";
        
        // Create the config
        LevelCreationConfig newConfig = ScriptableObject.CreateInstance<LevelCreationConfig>();
        AssetDatabase.CreateAsset(newConfig, path);
        AssetDatabase.SaveAssets();
        
        levelCreationConfig = newConfig;
        
        Debug.Log($"Created new LevelCreationConfig at: {path}");
        EditorUtility.DisplayDialog(
            "Config Created",
            $"Created LevelCreationConfig at:\n{path}\n\nPlease assign the node prefabs in the Inspector.",
            "OK"
        );
        
        // Select it in project view
        Selection.activeObject = levelCreationConfig;
        EditorGUIUtility.PingObject(levelCreationConfig);
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
    
    private void GenerateLevelAutomatically()
    {
        if (levelCreationConfig == null)
        {
            EditorUtility.DisplayDialog("Error", "No LevelCreationConfig assigned!", "OK");
            return;
        }
        
        // Create new level first
        CreateNewLevel();
        
        if (currentLevel == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to create level!", "OK");
            return;
        }
        
        // Now find gameZone as a child of the created level
        Transform gameZoneTransform = currentLevel.transform.Find("gameZone");
        if (gameZoneTransform == null)
        {
            EditorUtility.DisplayDialog("Error", "Cannot find 'gameZone' as a child of the level!\n\nThe level template should contain a gameZone child object with a MeshRenderer to define the playable area.", "OK");
            return;
        }
        
        GameObject gameZone = gameZoneTransform.gameObject;
        MeshRenderer gameZoneRenderer = gameZone.GetComponent<MeshRenderer>();
        if (gameZoneRenderer == null)
        {
            EditorUtility.DisplayDialog("Error", "gameZone object doesn't have a MeshRenderer component!", "OK");
            return;
        }
        
        // Get bounds
        Bounds bounds = gameZoneRenderer.bounds;
        Debug.Log($"Game zone bounds: {bounds}");
        
        // Apply padding
        bounds.Expand(-levelCreationConfig.BoundsPadding * 2);
        
        // Step 1: Generate neutral nodes in chosen pattern
        List<BaseNode> neutrals = GraphPatternGenerator.GeneratePattern(
            autoGenPattern, 
            autoGenNeutralCount, 
            bounds, 
            levelCreationConfig, 
            currentLevel);
        
        // Assign weights to neutral nodes
        foreach (var neutral in neutrals)
        {
            neutral.Weight = LevelGenerationHelper.AssignWeightForDifficulty(autoGenDifficulty);
            neutral.MaxOutgoingConnections = LevelGenerationHelper.GetMaxConnectionsForDifficulty(autoGenDifficulty, false);
        }
        
        // Step 2: Place producers at the bottom
        List<BaseNode> producers = new List<BaseNode>();
        float producerZ = bounds.min.z + (bounds.size.z * 0.2f);
        for (int i = 0; i < autoGenProducerCount; i++)
        {
            float xSpread = bounds.size.x * 0.6f;
            float x = bounds.center.x - xSpread / 2 + (xSpread * i / Mathf.Max(1, autoGenProducerCount - 1));
            
            Vector3 pos = new Vector3(x, 0, producerZ);
            BaseNode producer = CreateNodeAtPosition(pos, NodeType.Producer);
            if (producer != null)
            {
                producer.MaxOutgoingConnections = LevelGenerationHelper.GetMaxConnectionsForDifficulty(autoGenDifficulty, true);
                producers.Add(producer);
            }
        }
        
        // Step 3: Place consumers at the top
        List<BaseNode> consumers = new List<BaseNode>();
        float consumerZ = bounds.max.z - (bounds.size.z * 0.2f);
        for (int i = 0; i < autoGenConsumerCount; i++)
        {
            float xSpread = bounds.size.x * 0.6f;
            float x = bounds.center.x - xSpread / 2 + (xSpread * i / Mathf.Max(1, autoGenConsumerCount - 1));
            
            Vector3 pos = new Vector3(x, 0, consumerZ);
            BaseNode consumer = CreateNodeAtPosition(pos, NodeType.Consumer);
            if (consumer != null)
            {
                consumers.Add(consumer);
            }
        }
        
        List<BaseNode> allGeneratedNodes = new List<BaseNode>();
        allGeneratedNodes.AddRange(producers);
        allGeneratedNodes.AddRange(consumers);
        allGeneratedNodes.AddRange(neutrals);
        
        // Step 4: Generate connections using core+noise strategy
        CoreNoiseGenerator.GenerateLevel(producers, consumers, neutrals, currentLevel, autoGenDifficulty);
        
        // Validate the level is actually solvable
        bool isSolvable = SolutionValidator.IsLevelSolvable(allGeneratedNodes, currentLevel);
        
        if (!isSolvable)
        {
            Debug.LogWarning("Generated level is not solvable! Retrying with different connections...");
            
            // Retry up to 3 times
            int retryCount = 0;
            while (!isSolvable && retryCount < 3)
            {
                SolutionValidator.GenerateSolutionFirstLevel(producers, consumers, neutrals, currentLevel, autoGenDifficulty);
                isSolvable = SolutionValidator.IsLevelSolvable(allGeneratedNodes, currentLevel);
                retryCount++;
            }
            
            if (!isSolvable)
            {
                EditorUtility.DisplayDialog("Warning", 
                    "Could not generate a guaranteed solvable level after 3 attempts.\n\n" +
                    "The level may still be playable but might be very difficult or impossible.\n\n" +
                    "Try adjusting node counts or difficulty tier.",
                    "OK");
            }
        }
        
        // Calculate graph metrics for difficulty validation
        GraphMetrics metrics = GraphMetricsCalculator.Calculate(allGeneratedNodes, currentLevel);
        
        EditorUtility.SetDirty(currentLevel);
        
        string solvabilityStatus = isSolvable ? "✓ SOLVABLE" : "⚠ MAY BE UNSOLVABLE";
        
        Debug.Log($"Generated level with {allGeneratedNodes.Count} nodes at difficulty {autoGenDifficulty}");
        Debug.Log($"Solvability: {solvabilityStatus}");
        Debug.Log($"Graph Metrics:\n{metrics}");
        
        EditorUtility.DisplayDialog("Success", 
            $"Level generated successfully!\n\n" +
            $"Pattern: {autoGenPattern}\n" +
            $"Producers: {autoGenProducerCount}\n" +
            $"Consumers: {autoGenConsumerCount}\n" +
            $"Neutral: {autoGenNeutralCount}\n" +
            $"Difficulty: {autoGenDifficulty}\n\n" +
            $"Solvability: {solvabilityStatus}\n\n" +
            $"--- Graph Metrics ---\n" +
            $"Edges: {metrics.EdgeCount}\n" +
            $"Density: {metrics.GraphDensity:F2}\n" +
            $"Avg Path Length: {metrics.AveragePathLength:F1}\n" +
            $"Max Path Length: {metrics.MaxPathLength}\n" +
            $"Complexity Score: {metrics.ComplexityScore:F1}", 
            "OK");
    }
    
    private BaseNode CreateNodeAtPosition(Vector3 position, NodeType nodeType)
    {
        if (currentLevel == null || levelCreationConfig == null) return null;
        
        GameObject prefab = levelCreationConfig.GetNodePrefabByType(nodeType);
        if (prefab == null) return null;
        
        GameObject nodeObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (nodeObj == null) return null;
        
        nodeObj.transform.SetParent(currentLevel.transform);
        nodeObj.transform.position = position;
        
        BaseNode node = nodeObj.GetComponent<BaseNode>();
        if (node != null)
        {
            string nodeID = $"Node_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            node.NodeID = nodeID;
            
            switch (nodeType)
            {
                case NodeType.Producer:
                    nodeObj.name = $"Producer_{nodeID}";
                    break;
                case NodeType.Consumer:
                    nodeObj.name = $"Consumer_{nodeID}";
                    break;
                case NodeType.Neutral:
                    nodeObj.name = $"Neutral_{nodeID}";
                    break;
            }
            
            currentLevel.AddNode(node);
        }
        
        return node;
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
            
            // Draw weight label for nodes with non-zero weight
            if (node.Weight != 0)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = node.Weight > 0 ? Color.green : Color.red;
                style.fontSize = 12;
                style.fontStyle = FontStyle.Bold;
                
                Vector3 labelPos = node.transform.position + Vector3.up * 0.7f;
                Handles.Label(labelPos, $"{node.Weight:+#;-#;0}", style);
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
