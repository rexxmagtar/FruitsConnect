using System.Collections;
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
    private static readonly int GlobalColorBlendID = Shader.PropertyToID("_GlobalColorBlend");
    private static readonly int ConnectedNodePositionsID = Shader.PropertyToID("_ConnectedNodePositions");
    private static readonly int ConnectedNodeCountID = Shader.PropertyToID("_ConnectedNodeCount");
    private static readonly int ConsumerPositionsID = Shader.PropertyToID("_ConsumerPositions");
    private static readonly int ConsumerCountID = Shader.PropertyToID("_ConsumerCount");
    
    // Maximum number of nodes shader can handle
    private const int MAX_NODES = 32;
    
    // List of all terrain materials that use MapGrayscaleShader
    private List<Material> terrainMaterials = new List<Material>();
    private Dictionary<Material, float> originalColorRadii = new Dictionary<Material, float>();
    private List<Terrain> terrainComponents = new List<Terrain>();
    private MaterialPropertyBlock terrainPropertyBlock;
    
    private void Awake()
    {
        terrainPropertyBlock = new MaterialPropertyBlock();
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
        
        // Initialize terrain materials
        InitializeTerrainMaterials();
        
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
    /// Initialize terrain materials by finding all materials using MapGrayscaleShader under "Terrain" parent
    /// </summary>
    public void InitializeTerrainMaterials()
    {
        terrainMaterials.Clear();
        originalColorRadii.Clear();
        terrainComponents.Clear();
        
        if (levelController == null)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                levelController = gameController.CurrentLevel;
            }
        }
        
        if (levelController == null)
        {
            Debug.LogWarning("MapShaderController: Level controller not found! Cannot initialize terrain materials.");
            return;
        }
        
        // Find "Terrain" parent GameObject in the level
        Transform terrainParent = levelController.transform.Find("Terrain");
        if (terrainParent == null)
        {
            // Try to find by name in all children
            terrainParent = levelController.transform.GetComponentsInChildren<Transform>()
                .FirstOrDefault(t => t.name == "Terrain");
        }
        
        if (terrainParent == null)
        {
            Debug.LogWarning("MapShaderController: 'Terrain' parent GameObject not found in level!");
            return;
        }
        
        // Get all Renderer components under Terrain parent
        Renderer[] renderers = terrainParent.GetComponentsInChildren<Renderer>(true);
        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            // Get all materials from this renderer
            Material[] materials = renderer.sharedMaterials;
            foreach (Material mat in materials)
            {
                if (mat == null) continue;
                
                // Check if material uses MapGrayscaleShader
                if (mat.shader != null && (mat.shader.name == "Custom/MapGrayscaleShader" || mat.shader.name == "Custom/MapGrayscaleShaderStandard" || mat.shader.name == "Custom/TerrainGrayscaleUnlit"))
                {
                    if (uniqueMaterials.Add(mat))
                    {
                        terrainMaterials.Add(mat);
                        // Store original color radius if it exists
                        if (mat.HasProperty(ColorRadiusID))
                        {
                            originalColorRadii[mat] = mat.GetFloat(ColorRadiusID);
                        }
                        else
                        {
                            originalColorRadii[mat] = colorRadius;
                        }
                    }
                }
            }
        }

        // Also check for Unity Terrain components which don't use Renderer
        Terrain[] terrains = terrainParent.GetComponentsInChildren<Terrain>(true);
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null) continue;
            
            Material mat = terrain.materialTemplate;
            if (mat == null) continue;

            if (mat.shader != null && (mat.shader.name == "Custom/MapGrayscaleShader" || mat.shader.name == "Custom/MapGrayscaleShaderStandard" || mat.shader.name == "Custom/TerrainGrayscaleUnlit"))
            {
                if (uniqueMaterials.Add(mat))
                {
                    terrainMaterials.Add(mat);
                    if (mat.HasProperty(ColorRadiusID))
                    {
                        originalColorRadii[mat] = mat.GetFloat(ColorRadiusID);
                    }
                    else
                    {
                        originalColorRadii[mat] = colorRadius;
                    }
                }
                
                // Cache the terrain component for manual flushing
                if (!terrainComponents.Contains(terrain))
                {
                    terrainComponents.Add(terrain);
                }
            }
        }

        // Check levelController's direct reference if it exists
        if (levelController.TerrainMeshRenderer != null)
        {
            foreach (Material mat in levelController.TerrainMeshRenderer.sharedMaterials)
            {
                if (mat == null) continue;
                if (mat.shader != null && (mat.shader.name == "Custom/MapGrayscaleShader" || mat.shader.name == "Custom/MapGrayscaleShaderStandard" || mat.shader.name == "Custom/TerrainGrayscaleUnlit"))
                {
                    if (uniqueMaterials.Add(mat))
                    {
                        terrainMaterials.Add(mat);
                        if (mat.HasProperty(ColorRadiusID))
                        {
                            originalColorRadii[mat] = mat.GetFloat(ColorRadiusID);
                        }
                        else
                        {
                            originalColorRadii[mat] = colorRadius;
                        }
                    }
                }
            }
        }
        
        // Also add the original mapMaterial if it exists and uses the shader
        if (mapMaterial != null && mapMaterial.shader != null && (mapMaterial.shader.name == "Custom/MapGrayscaleShader" || mapMaterial.shader.name == "Custom/MapGrayscaleShaderStandard" || mapMaterial.shader.name == "Custom/TerrainGrayscaleUnlit"))
        {
            if (uniqueMaterials.Add(mapMaterial))
            {
                terrainMaterials.Add(mapMaterial);
                if (mapMaterial.HasProperty(ColorRadiusID))
                {
                    originalColorRadii[mapMaterial] = mapMaterial.GetFloat(ColorRadiusID);
                }
                else
                {
                    originalColorRadii[mapMaterial] = colorRadius;
                }
            }
        }
        
        Debug.Log($"MapShaderController: Initialized {terrainMaterials.Count} terrain materials.");
    }
    
    /// <summary>
    /// Update shader properties with current connected nodes and consumers
    /// Should be called when connections change
    /// </summary>
    public void UpdateShaderProperties()
    {
        // Get list of materials to update (all terrain materials + original mapMaterial if different)
        List<Material> materialsToUpdate = new List<Material>(terrainMaterials);
        if (mapMaterial != null && !terrainMaterials.Contains(mapMaterial))
        {
            materialsToUpdate.Add(mapMaterial);
        }
        
        if (materialsToUpdate.Count == 0)
        {
            Debug.LogWarning("MapShaderController: No materials to update!");
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
                // Consumers only show colored zones if connected to producer AND fully activated
                else if (node is ConsumerNode)
                {
                    isConnectedToProducer = connectionManager.IsConnectedToProducer(node) && node.IsFullyDelivered;
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
                }
            }
        }
        
        // Get consumers that are connected to producer AND fully activated (for shader)
        List<ConsumerNode> consumers = levelController.GetConsumerNodes();
        List<ConsumerNode> connectedConsumers = new List<ConsumerNode>();
        foreach (ConsumerNode consumer in consumers)
        {
            if (consumer != null && connectionManager.IsConnectedToProducer(consumer) && consumer.IsFullyDelivered)
            {
                connectedConsumers.Add(consumer);
            }
        }
        
        // Prepare shader data
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
        
        // Update all materials
        foreach (Material mat in materialsToUpdate)
        {
            if (mat == null) continue;
            
            mat.SetFloat(ColorRadiusID, colorRadius);
            mat.SetFloat(SmoothFalloffID, smoothFalloff);
            // Set global color blend to 1.0 (normal operation - distance-based coloring)
            if (mat.HasProperty(GlobalColorBlendID))
            {
                mat.SetFloat(GlobalColorBlendID, 1.0f);
            }
            mat.SetVectorArray(ConnectedNodePositionsID, connectedNodePositions);
            mat.SetInt(ConnectedNodeCountID, connectedCount);
            mat.SetVectorArray(ConsumerPositionsID, consumerPositions);
            mat.SetInt(ConsumerCountID, consumerCount);
        }

        // Terrains often don't pick up shared material changes at runtime.
        // We use MaterialPropertyBlock to force the terrain renderer to update.
        if (terrainPropertyBlock == null) terrainPropertyBlock = new MaterialPropertyBlock();
        
        terrainPropertyBlock.SetFloat(ColorRadiusID, colorRadius);
        terrainPropertyBlock.SetFloat(SmoothFalloffID, smoothFalloff);
        terrainPropertyBlock.SetFloat(GlobalColorBlendID, 1.0f);
        terrainPropertyBlock.SetVectorArray(ConnectedNodePositionsID, connectedNodePositions);
        terrainPropertyBlock.SetInt(ConnectedNodeCountID, connectedCount);
        terrainPropertyBlock.SetVectorArray(ConsumerPositionsID, consumerPositions);
        terrainPropertyBlock.SetInt(ConsumerCountID, consumerCount);

        // Apply to all cached terrains
        foreach (Terrain terrain in terrainComponents)
        {
            if (terrain != null)
            {
                terrain.SetSplatMaterialPropertyBlock(terrainPropertyBlock);
                terrain.Flush();
            }
        }
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
                // Consumers only show colored zones if connected to producer AND fully activated
                else if (node is ConsumerNode)
                {
                    shouldCheckNode = connectionManager.IsConnectedToProducer(node) && node.IsFullyDelivered;
                }
                // Other nodes show colored zones if connected to producer AND fully delivered
                else
                {
                    shouldCheckNode = connectionManager.IsConnectedToProducer(node) && node.IsFullyDelivered;
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
        InitializeTerrainMaterials();
        UpdateShaderProperties();
    }
    
    /// <summary>
    /// Smoothly remove grayscale from all terrain materials over duration
    /// Interpolates _GlobalColorBlend from 1.0 (distance-based) to 0.0 (full color everywhere)
    /// </summary>
    public IEnumerator RemoveGrayscaleFromAllMaterials(float duration)
    {
        if (terrainMaterials.Count == 0)
        {
            Debug.LogWarning("MapShaderController: No terrain materials to animate!");
            yield break;
        }
        
        // Check if materials support GlobalColorBlend property
        bool hasBlendProperty = false;
        foreach (Material mat in terrainMaterials)
        {
            if (mat != null && mat.HasProperty(GlobalColorBlendID))
            {
                hasBlendProperty = true;
                break;
            }
        }
        
        if (!hasBlendProperty)
        {
            Debug.LogWarning("MapShaderController: Materials don't support _GlobalColorBlend property! Shader may need update.");
            yield break;
        }
        
        float elapsedTime = 0f;
        const float startBlend = 1.0f; // Start with distance-based coloring (normal operation)
        const float endBlend = 0.0f;   // End with full color everywhere (blend = 0 means full color)
        
        // Wait one frame to ensure materials are initialized
        yield return null;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // Smooth interpolation using ease-out curve
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            // Interpolate blend factor: 1.0 (distance-based) -> 0.0 (full color everywhere)
            // Shader: finalColor = lerp(texColor.rgb, distanceBasedColor, _GlobalColorBlend)
            // When blend = 0: full color (texColor.rgb)
            // When blend = 1: distance-based color (distanceBasedColor)
            float currentBlend = Mathf.Lerp(startBlend, endBlend, t);
            
            // Update all materials
            foreach (Material mat in terrainMaterials)
            {
                if (mat == null) continue;
                
                if (mat.HasProperty(GlobalColorBlendID))
                {
                    mat.SetFloat(GlobalColorBlendID, currentBlend);
                }
            }

            // Update property block for terrains
            if (terrainPropertyBlock == null) terrainPropertyBlock = new MaterialPropertyBlock();
            terrainPropertyBlock.SetFloat(GlobalColorBlendID, currentBlend);

            // Flush terrains
            foreach (Terrain terrain in terrainComponents)
            {
                if (terrain != null)
                {
                    terrain.SetSplatMaterialPropertyBlock(terrainPropertyBlock);
                    terrain.Flush();
                }
            }
            
            yield return null;
        }
        
        // Ensure final state - set to 0 for full color everywhere
        if (terrainPropertyBlock == null) terrainPropertyBlock = new MaterialPropertyBlock();
        terrainPropertyBlock.SetFloat(GlobalColorBlendID, endBlend);

        foreach (Material mat in terrainMaterials)
        {
            if (mat == null) continue;
            if (mat.HasProperty(GlobalColorBlendID))
            {
                mat.SetFloat(GlobalColorBlendID, endBlend);
            }
        }

        // Final flush
        foreach (Terrain terrain in terrainComponents)
        {
            if (terrain != null)
            {
                terrain.SetSplatMaterialPropertyBlock(terrainPropertyBlock);
                terrain.Flush();
            }
        }
        
        Debug.Log("MapShaderController: Grayscale removal animation completed.");
    }
}
