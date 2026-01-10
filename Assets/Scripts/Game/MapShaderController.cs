using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the map grayscale shader, updating colored zones around connected nodes and consumers
/// </summary>
public class MapShaderController : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private Material mapMaterial;
    [SerializeField] private float colorRadius = 3f;
    [SerializeField] private float smoothFalloff = 0.3f;
    
    [Header("Spawn Validation")]
    [SerializeField] private float minSpawnAreaRadius = 1.5f; // Minimum grayscale area required for spawning
    [SerializeField] private int spawnAreaSamplePoints = 8; // Number of points to sample around spawn position
    
    [Header("References")]
    [SerializeField] private LevelController levelController;
    
    // Shader property IDs (cached for performance)
    private static readonly int ColorRadiusID = Shader.PropertyToID("_ColorRadius");
    private static readonly int SmoothFalloffID = Shader.PropertyToID("_SmoothFalloff");
    private static readonly int ConnectedNodePositionsID = Shader.PropertyToID("_ConnectedNodePositions");
    private static readonly int ConnectedNodeCountID = Shader.PropertyToID("_ConnectedNodeCount");
    private static readonly int ConsumerPositionsID = Shader.PropertyToID("_ConsumerPositions");
    private static readonly int ConsumerCountID = Shader.PropertyToID("_ConsumerCount");
    private static readonly int PartialNodePositionsID = Shader.PropertyToID("_PartialNodePositions");
    private static readonly int PartialNodeProgressID = Shader.PropertyToID("_PartialNodeProgress");
    private static readonly int PartialNodeCountID = Shader.PropertyToID("_PartialNodeCount");
    
    // Maximum number of nodes shader can handle
    private const int MAX_NODES = 32;
    
    private void Awake()
    {
       
    }
    
    private void Start()
    {
        // Subscribe to connection change events
         // Get level controller if not assigned
        if (levelController == null)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                levelController = gameController.CurrentLevel;
            }
        }
        ConnectionManager.OnConnectionsChanged += OnConnectionsChanged;
        
        // Initial update
        UpdateShaderProperties();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        ConnectionManager.OnConnectionsChanged -= OnConnectionsChanged;
    }
    
    /// <summary>
    /// Called when connections change - update shader properties
    /// </summary>
    private void OnConnectionsChanged()
    {
        UpdateShaderProperties();
    }
    
    /// <summary>
    /// Update shader properties with current connected nodes and consumers
    /// Should be called when connections change
    /// </summary>
    public void UpdateShaderProperties()
    {
        if (mapMaterial == null)
        {
            Debug.LogWarning("MapShaderController: Map material not assigned!");
            return;
        }
        
        if (levelController == null)
        {
            // Try to get level controller
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                levelController = gameController.CurrentLevel;
            }
            
            if (levelController == null)
            {
                Debug.LogWarning("MapShaderController: Level controller not found!");
                return;
            }
        }
        
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager == null)
        {
            Debug.LogWarning("MapShaderController: ConnectionManager not found!");
            return;
        }
        
        // Get all nodes
        List<BaseNode> allNodes = levelController.GetAllNodes();
        
        // Get connected nodes (nodes connected to producer AND fully delivered)
        // Producers show colored zones only if not captured
        // Other nodes (non-producers, non-consumers) show colored zones if connected to producer AND fully delivered
        List<BaseNode> connectedNodes = new List<BaseNode>();
        List<BaseNode> partialNodes = new List<BaseNode>(); // Nodes with delivery progress but not fully delivered
        
        foreach (BaseNode node in allNodes)
        {
            if (node != null)
            {
                bool isConnectedToProducer = false;
                
                // Producers show colored zones only if not captured
                if (node is ProducerNode)
                {
                    isConnectedToProducer = !node.IsCaptured;
                }
                // Consumers only show colored zones if connected to producer
                else if (node is ConsumerNode)
                {
                    isConnectedToProducer = connectionManager.IsConnectedToProducer(node);
                }
                // Other nodes show colored zones if connected to producer
                else
                {
                    isConnectedToProducer = connectionManager.IsConnectedToProducer(node);
                }
                
                if (isConnectedToProducer)
                {
                    // Producers are always active (don't need deliveries)
                    if (node is ProducerNode)
                    {
                        connectedNodes.Add(node);
                    }
                    // Check if node is fully delivered
                    else if (node.IsFullyDelivered)
                    {
                        connectedNodes.Add(node);
                    }
                    else if (node.CurrentDeliveries > 0)
                    {
                        // Node has some deliveries but not fully delivered - show partial progress
                        partialNodes.Add(node);
                    }
                }
            }
        }
        
        // Get consumers that are connected to producer (for shader)
        List<ConsumerNode> consumers = levelController.GetConsumerNodes();
        List<ConsumerNode> connectedConsumers = new List<ConsumerNode>();
        foreach (ConsumerNode consumer in consumers)
        {
            if (consumer != null && connectionManager.IsConnectedToProducer(consumer))
            {
                connectedConsumers.Add(consumer);
            }
        }
        
        // Set shader properties
        mapMaterial.SetFloat(ColorRadiusID, colorRadius);
        mapMaterial.SetFloat(SmoothFalloffID, smoothFalloff);
        
        // Pass connected node positions to shader
        Vector4[] connectedNodePositions = new Vector4[MAX_NODES];
        int connectedCount = Mathf.Min(connectedNodes.Count, MAX_NODES);
        
        for (int i = 0; i < connectedCount; i++)
        {
            if (connectedNodes[i] != null)
            {
                Vector3 pos = connectedNodes[i].transform.position;
                connectedNodePositions[i] = new Vector4(pos.x, pos.y, pos.z, 0);
            }
        }
        
        // Fill remaining slots with zero
        for (int i = connectedCount; i < MAX_NODES; i++)
        {
            connectedNodePositions[i] = Vector4.zero;
        }
        
        mapMaterial.SetVectorArray(ConnectedNodePositionsID, connectedNodePositions);
        mapMaterial.SetInt(ConnectedNodeCountID, connectedCount);
        
        // Pass connected consumer positions to shader (only consumers connected to producer)
        Vector4[] consumerPositions = new Vector4[MAX_NODES];
        int consumerCount = Mathf.Min(connectedConsumers.Count, MAX_NODES);
        
        for (int i = 0; i < consumerCount; i++)
        {
            if (connectedConsumers[i] != null)
            {
                Vector3 pos = connectedConsumers[i].transform.position;
                consumerPositions[i] = new Vector4(pos.x, pos.y, pos.z, 0);
            }
        }
        
        // Fill remaining slots with zero
        for (int i = consumerCount; i < MAX_NODES; i++)
        {
            consumerPositions[i] = Vector4.zero;
        }
        
        mapMaterial.SetVectorArray(ConsumerPositionsID, consumerPositions);
        mapMaterial.SetInt(ConsumerCountID, consumerCount);
        
        // Pass partial node positions and progress to shader
        Vector4[] partialNodePositions = new Vector4[MAX_NODES];
        float[] partialNodeProgress = new float[MAX_NODES];
        int partialCount = Mathf.Min(partialNodes.Count, MAX_NODES);
        
        for (int i = 0; i < partialCount; i++)
        {
            if (partialNodes[i] != null)
            {
                Vector3 pos = partialNodes[i].transform.position;
                partialNodePositions[i] = new Vector4(pos.x, pos.y, pos.z, 0);
                partialNodeProgress[i] = partialNodes[i].DeliveryProgress;
            }
        }
        
        // Fill remaining slots with zero
        for (int i = partialCount; i < MAX_NODES; i++)
        {
            partialNodePositions[i] = Vector4.zero;
            partialNodeProgress[i] = 0f;
        }
        
        mapMaterial.SetVectorArray(PartialNodePositionsID, partialNodePositions);
        mapMaterial.SetFloatArray(PartialNodeProgressID, partialNodeProgress);
        mapMaterial.SetInt(PartialNodeCountID, partialCount);
    }
    
    /// <summary>
    /// Check if a world position is in a grayscale zone (not within radius of connected nodes/consumers)
    /// </summary>
    public bool IsPositionInGrayscaleZone(Vector3 position)
    {
        if (levelController == null)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                levelController = gameController.CurrentLevel;
            }
            
            if (levelController == null)
            {
                return true; // Default to allowing spawn if level not found
            }
        }
        
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager == null)
        {
            return true; // Default to allowing spawn if connection manager not found
        }
        
        // Get all nodes
        List<BaseNode> allNodes = levelController.GetAllNodes();
        Vector2 pos2D = new Vector2(position.x, position.z);
        
        // Check connected nodes
        foreach (BaseNode node in allNodes)
        {
            if (node != null)
            {
                bool shouldCheckNode = false;
                
                // Producers show colored zones only if not captured
                if (node is ProducerNode)
                {
                    shouldCheckNode = !node.IsCaptured;
                }
                // Consumers only show colored zones if connected to producer
                else if (node is ConsumerNode)
                {
                    shouldCheckNode = connectionManager.IsConnectedToProducer(node);
                }
                // Other nodes show colored zones if connected to producer
                else
                {
                    shouldCheckNode = connectionManager.IsConnectedToProducer(node);
                }
                
                if (shouldCheckNode)
                {
                    Vector2 nodePos2D = new Vector2(node.transform.position.x, node.transform.position.z);
                    float dist = Vector2.Distance(pos2D, nodePos2D);
                    
                    if (dist < colorRadius)
                    {
                        return false; // Within colored zone
                    }
                }
            }
        }
        
        return true; // In grayscale zone
    }
    
    /// <summary>
    /// Check if a position has sufficient grayscale area around it
    /// Samples multiple points in a circle around the position to ensure entire area is grayscale
    /// </summary>
    public bool HasMinimumGrayscaleArea(Vector3 position, float areaRadius)
    {
        // Sample multiple points around the position
        for (int i = 0; i < spawnAreaSamplePoints; i++)
        {
            float angle = (360f / spawnAreaSamplePoints) * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * areaRadius;
            Vector3 samplePos = position + new Vector3(offset.x, 0, offset.y);
            
            // Check if this sample point is in grayscale zone
            if (!IsPositionInGrayscaleZone(samplePos))
            {
                return false; // At least one sample point is in colored zone
            }
        }
        
        // Also check the center position
        if (!IsPositionInGrayscaleZone(position))
        {
            return false;
        }
        
        return true; // All sample points are in grayscale zone
    }
    
    /// <summary>
    /// Get the minimum spawn area radius
    /// </summary>
    public float GetMinSpawnAreaRadius()
    {
        return minSpawnAreaRadius;
    }
    
    /// <summary>
    /// Set the level controller reference
    /// </summary>
    public void SetLevelController(LevelController level)
    {
        levelController = level;
        UpdateShaderProperties();
    }
}
