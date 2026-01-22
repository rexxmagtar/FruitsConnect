using UnityEngine;

/// <summary>
/// Configuration for the Level Creation/Generation tool
/// Stores prefabs for all node types and template settings
/// </summary>
[CreateAssetMenu(fileName = "LevelCreationConfig", menuName = "Fruit Connect/Level Creation Config")]
public class LevelCreationConfig : ScriptableObject
{
    [Header("Node Prefabs")]
    [Tooltip("Prefab for Producer nodes (red filled spheres - starting points)")]
    [SerializeField] private GameObject producerNodePrefab;
    
    [Tooltip("Prefab for Consumer nodes (blue outline spheres - endpoints)")]
    [SerializeField] private GameObject consumerNodePrefab;
    
    [Tooltip("Prefab for Neutral nodes (red outline spheres - pass-through)")]
    [SerializeField] private GameObject neutralNodePrefab;
    
    [Tooltip("Prefab for Double Neutral nodes (same as neutral but with different mesh)")]
    [SerializeField] private GameObject doubleNeutralNodePrefab;
    
    [Header("Level Template")]
    [Tooltip("Base level template to use as starting point for level creation")]
    [SerializeField] private GameObject levelTemplate;
    
    [Header("Connection Settings")]
    [Tooltip("Prefab for connection visual representation")]
    [SerializeField] private GameObject connectionPrefab;
    
    [Tooltip("Prefab for objects that animate along connection lines (spawns at node A, moves to node B)")]
    [SerializeField] private GameObject animationPrefab;
    
    [Header("Wall Settings")]
    [Tooltip("Prefab for walls placed between nodes that can't be connected")]
    [SerializeField] private GameObject wallPrefab;
    
    [Header("Default Grid Settings")]
    [Tooltip("Default spacing between nodes when placing on grid")]
    [SerializeField] private float defaultNodeSpacing = 2.0f;
    
    [Tooltip("Default number of columns in grid layout")]
    [SerializeField] private int defaultGridColumns = 5;
    
    [Tooltip("Default number of rows in grid layout")]
    [SerializeField] private int defaultGridRows = 5;
    
    [Header("Auto-Generation Settings")]
    [Tooltip("Minimum distance between nodes during auto-generation")]
    [SerializeField] private float minNodeDistance = 1.5f;
    
    [Tooltip("Padding from game zone bounds")]
    [SerializeField] private float boundsPadding = 0.5f;
    
    [Header("Spawn Zone Settings")]
    [Tooltip("Producer spawn zone size (0-1, percentage of Z-axis). Producers spawn at bottom")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float producerZoneSize = 0.3f;
    
    [Tooltip("Consumer spawn zone size (0-1, percentage of Z-axis). Consumers spawn at top")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float consumerZoneSize = 0.3f;
    
    [Tooltip("Neutral spawn zone margin from edges (0-1, percentage of Z-axis)")]
    [Range(0.0f, 0.3f)]
    [SerializeField] private float neutralZoneMargin = 0.2f;
    
    // Public accessors
    public GameObject ProducerNodePrefab => producerNodePrefab;
    public GameObject ConsumerNodePrefab => consumerNodePrefab;
    public GameObject NeutralNodePrefab => neutralNodePrefab;
    public GameObject DoubleNeutralNodePrefab => doubleNeutralNodePrefab;
    public GameObject LevelTemplate => levelTemplate;
    public GameObject ConnectionPrefab => connectionPrefab;
    public GameObject AnimationPrefab => animationPrefab;
    public GameObject WallPrefab => wallPrefab;
    public float DefaultNodeSpacing => defaultNodeSpacing;
    public int DefaultGridColumns => defaultGridColumns;
    public int DefaultGridRows => defaultGridRows;
    public float MinNodeDistance => minNodeDistance;
    public float BoundsPadding => boundsPadding;
    public float ProducerZoneSize => producerZoneSize;
    public float ConsumerZoneSize => consumerZoneSize;
    public float NeutralZoneMargin => neutralZoneMargin;
    
    /// <summary>
    /// Get node prefab by type
    /// </summary>
    /// <param name="nodeType">Type of node</param>
    /// <returns>Corresponding prefab GameObject</returns>
    public GameObject GetNodePrefabByType(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Producer => producerNodePrefab,
            NodeType.Consumer => consumerNodePrefab,
            NodeType.Neutral => GetNeutralNodePrefab(),
            NodeType.NeutralDouble => doubleNeutralNodePrefab != null ? doubleNeutralNodePrefab : neutralNodePrefab,
            _ => null
        };
    }
    
    /// <summary>
    /// Get neutral node prefab, randomly choosing between regular and double neutral prefabs
    /// Falls back to regular neutral if double neutral is not assigned
    /// </summary>
    /// <returns>Neutral node prefab GameObject</returns>
    public GameObject GetNeutralNodePrefab()
    {
        // If double neutral prefab is assigned, randomly choose between regular and double
        // 20% chance to use double neutral, 80% chance to use regular neutral
        if (doubleNeutralNodePrefab != null)
        {
            return Random.value < 0.2f ? doubleNeutralNodePrefab : neutralNodePrefab;
        }
        
        // Fallback to regular neutral if double neutral is not assigned
        return neutralNodePrefab;
    }
    
    /// <summary>
    /// Validate that all required prefabs are assigned
    /// </summary>
    /// <returns>True if config is valid</returns>
    public bool IsValid()
    {
        bool isValid = true;
        
        if (producerNodePrefab == null)
        {
            Debug.LogError("LevelCreationConfig: Producer Node Prefab is not assigned!");
            isValid = false;
        }
        
        if (consumerNodePrefab == null)
        {
            Debug.LogError("LevelCreationConfig: Consumer Node Prefab is not assigned!");
            isValid = false;
        }
        
        if (neutralNodePrefab == null)
        {
            Debug.LogError("LevelCreationConfig: Neutral Node Prefab is not assigned!");
            isValid = false;
        }
        
        if (levelTemplate == null)
        {
            Debug.LogWarning("LevelCreationConfig: Level Template is not assigned. Using empty template.");
        }
        
        return isValid;
    }
}

/// <summary>
/// Enum for node types
/// </summary>
public enum NodeType
{
    Producer,
    Consumer,
    Neutral,
    NeutralDouble
}

