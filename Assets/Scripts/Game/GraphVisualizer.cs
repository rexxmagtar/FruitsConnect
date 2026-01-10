using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Visualizes all possible connections from connection mappings as placeholder lines
/// These are visual only - no game logic
/// </summary>
public class GraphVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Material placeholderLineMaterial;
    [SerializeField] private Color placeholderLineColor = new Color(0.7f, 0.7f, 0.7f, 0.8f); // Lighter gray, more visible
    [SerializeField] private float placeholderLineWidth = 0.1f; // Increased width for visibility
    [SerializeField] private bool showPlaceholders = true;
    [SerializeField] private float groundLevelY = -0.48f; // Ground level Y coordinate for placeholder lines
    
    [Header("References")]
    private LevelController levelController;
    
    // Store created line renderers
    private List<GameObject> placeholderLineObjects = new List<GameObject>();
    private GameObject linesParent;
    
    // Shared material for all placeholder lines (to avoid material leaks)
    private Material sharedPlaceholderMaterial;
    
    private void Awake()
    {
        EnsureLinesParentInitialized();
    }
    
    /// <summary>
    /// Ensure linesParent is initialized (can be called from editor or runtime)
    /// </summary>
    private void EnsureLinesParentInitialized()
    {
        if (linesParent == null)
        {
            linesParent = new GameObject("PlaceholderLines");
            linesParent.transform.SetParent(transform);
        }
    }
    
    /// <summary>
    /// Get or create the shared material for placeholder lines
    /// </summary>
    private Material GetSharedPlaceholderMaterial()
    {
        if (sharedPlaceholderMaterial == null)
        {
            if (placeholderLineMaterial != null)
            {
                // Create a shared material instance from the assigned material
                sharedPlaceholderMaterial = new Material(placeholderLineMaterial);
            }
            else
            {
                // Create default material if none assigned - use Unlit shader for better visibility
                sharedPlaceholderMaterial = new Material(Shader.Find("Unlit/Color"));
                if (sharedPlaceholderMaterial.shader.name == "Hidden/InternalErrorShader")
                {
                    // Fallback to Sprites/Default if Unlit/Color not found
                    sharedPlaceholderMaterial = new Material(Shader.Find("Sprites/Default"));
                }
            }
        }
        
        // Update color in case it changed
        sharedPlaceholderMaterial.color = placeholderLineColor;
        
        return sharedPlaceholderMaterial;
    }
    
    private void Start()
    {
        // Find LevelController in Start (after all objects are initialized)
        if (levelController == null)
        {
            levelController = GetComponent<LevelController>();
            if (levelController == null)
            {
                levelController = FindFirstObjectByType<LevelController>();
            }
        }
        
        // Delay update slightly to ensure all nodes are initialized
        if (levelController != null)
        {
            StartCoroutine(DelayedUpdateVisualLines());
        }
        else
        {
            Debug.LogWarning("GraphVisualizer: LevelController not found in Start!");
        }
    }
    
    private System.Collections.IEnumerator DelayedUpdateVisualLines()
    {
        // Wait one frame to ensure all nodes are fully initialized
        yield return null;
        UpdateVisualLines();
    }
    
    /// <summary>
    /// Update all visual placeholder lines based on current connection mappings
    /// </summary>
    public void UpdateVisualLines()
    {
        // Don't create placeholders in edit mode - only at runtime
        if (!Application.isPlaying)
        {
            return;
        }
        
        // Ensure linesParent is initialized (important for editor calls)
        EnsureLinesParentInitialized();
        
        if (levelController == null)
        {
            // Try to find LevelController again
            levelController = GetComponent<LevelController>();
            if (levelController == null)
            {
                levelController = FindFirstObjectByType<LevelController>();
            }
            
            if (levelController == null)
            {
                Debug.LogWarning("GraphVisualizer: LevelController not found!");
                return;
            }
        }
        
        // Clear existing lines
        ClearPlaceholderLines();
        
        if (!showPlaceholders)
        {
            Debug.Log("GraphVisualizer: Placeholders disabled");
            return;
        }
        
        // Get all nodes
        List<BaseNode> allNodes = levelController.GetAllNodes();
        if (allNodes == null || allNodes.Count == 0)
        {
            Debug.LogWarning("GraphVisualizer: No nodes found in level");
            return;
        }
        
        // Create dictionary for fast node lookup
        Dictionary<string, BaseNode> nodeDict = new Dictionary<string, BaseNode>();
        foreach (var node in allNodes)
        {
            if (node != null && !string.IsNullOrEmpty(node.NodeID))
            {
                nodeDict[node.NodeID] = node;
            }
        }
        
        int linesCreated = 0;
        
        // Create placeholder lines for all possible connections
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            
            List<string> possibleConnections = levelController.GetConnectionMapping(node.NodeID);
            
            if (possibleConnections == null || possibleConnections.Count == 0)
            {
                continue;
            }
            
            foreach (string targetNodeID in possibleConnections)
            {
                if (nodeDict.ContainsKey(targetNodeID))
                {
                    BaseNode targetNode = nodeDict[targetNodeID];
                    if (targetNode != null && targetNode != node)
                    {
                        // Only create placeholder if connection doesn't already exist
                        if (!ConnectionExists(node, targetNode))
                        {
                            CreatePlaceholderLine(node, targetNode);
                            linesCreated++;
                        }
                    }
                }
            }
        }
        
        Debug.Log($"GraphVisualizer: Created {linesCreated} placeholder lines for {allNodes.Count} nodes");
    }
    
    /// <summary>
    /// Create a single placeholder line between two nodes
    /// </summary>
    private void CreatePlaceholderLine(BaseNode fromNode, BaseNode toNode)
    {
        if (fromNode == null || toNode == null) return;
        
        // Ensure linesParent is initialized (important for editor calls)
        EnsureLinesParentInitialized();
        if (linesParent == null)
        {
            Debug.LogError("GraphVisualizer: Failed to initialize linesParent!");
            return;
        }
        
        // Create GameObject for this line
        GameObject lineObj = new GameObject($"PlaceholderLine_{fromNode.NodeID}_to_{toNode.NodeID}");
        lineObj.transform.SetParent(linesParent.transform);
        
        // Add LineRenderer component
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        
        // Configure LineRenderer
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = placeholderLineWidth;
        lineRenderer.endWidth = placeholderLineWidth;
        
        // Set shared material (use sharedMaterial to avoid material leaks in edit mode)
        Material sharedMat = GetSharedPlaceholderMaterial();
        if (sharedMat != null)
        {
            lineRenderer.sharedMaterial = sharedMat;
        }
        
        // Disable shadows
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        
        // Set texture mode to tile
        lineRenderer.textureMode = LineTextureMode.Tile;
        
        // Set positions - ensure both start and end Y coordinates are always 0
        Vector3 startPos = fromNode.transform.position;
        Vector3 endPos = toNode.transform.position;
        startPos.y = 0f;
        endPos.y = 0f;
        
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
        
        // Store reference
        placeholderLineObjects.Add(lineObj);
    }
    
    /// <summary>
    /// Clear all placeholder lines
    /// </summary>
    private void ClearPlaceholderLines()
    {
        foreach (GameObject lineObj in placeholderLineObjects)
        {
            if (lineObj != null)
            {
                DestroyImmediate(lineObj);
            }
        }
        placeholderLineObjects.Clear();
    }
    
    /// <summary>
    /// Toggle visibility of placeholder lines
    /// </summary>
    public void SetShowPlaceholders(bool show)
    {
        showPlaceholders = show;
        UpdateVisualLines();
    }
    
    /// <summary>
    /// Hide placeholder line for a specific connection (from→to)
    /// Also hides the reverse direction placeholder if it exists (since connections are bidirectional)
    /// </summary>
    public void HidePlaceholderLine(BaseNode fromNode, BaseNode toNode)
    {
        if (fromNode == null || toNode == null) return;
        
        // Find and hide placeholder for from→to
        string placeholderName = $"PlaceholderLine_{fromNode.NodeID}_to_{toNode.NodeID}";
        HidePlaceholderByName(placeholderName);
        
        // Also hide reverse direction placeholder (to→from) since connections are bidirectional
        string reversePlaceholderName = $"PlaceholderLine_{toNode.NodeID}_to_{fromNode.NodeID}";
        HidePlaceholderByName(reversePlaceholderName);
    }
    
    /// <summary>
    /// Hide a placeholder line by name
    /// </summary>
    private void HidePlaceholderByName(string placeholderName)
    {
        for (int i = placeholderLineObjects.Count - 1; i >= 0; i--)
        {
            GameObject lineObj = placeholderLineObjects[i];
            if (lineObj != null && lineObj.name == placeholderName)
            {
                // Hide the placeholder by deactivating it
                lineObj.SetActive(false);
                // Optionally remove it from the list to keep it clean
                // placeholderLineObjects.RemoveAt(i);
                return;
            }
        }
    }
    
    /// <summary>
    /// Show placeholder line for a specific connection (from→to)
    /// Also shows the reverse direction placeholder if it exists (since connections are bidirectional)
    /// </summary>
    public void ShowPlaceholderLine(BaseNode fromNode, BaseNode toNode)
    {
        if (fromNode == null || toNode == null) return;
        
        // Find and show placeholder for from→to
        string placeholderName = $"PlaceholderLine_{fromNode.NodeID}_to_{toNode.NodeID}";
        ShowPlaceholderByName(placeholderName);
        
        // Also show reverse direction placeholder (to→from) since connections are bidirectional
        string reversePlaceholderName = $"PlaceholderLine_{toNode.NodeID}_to_{fromNode.NodeID}";
        ShowPlaceholderByName(reversePlaceholderName);
    }
    
    /// <summary>
    /// Show a placeholder line by name
    /// </summary>
    private void ShowPlaceholderByName(string placeholderName)
    {
        for (int i = placeholderLineObjects.Count - 1; i >= 0; i--)
        {
            GameObject lineObj = placeholderLineObjects[i];
            if (lineObj != null && lineObj.name == placeholderName)
            {
                // Show the placeholder by activating it
                lineObj.SetActive(true);
                return;
            }
        }
        
        // If placeholder doesn't exist, we need to recreate it
        // This can happen if UpdateVisualLines was called and cleared all placeholders
        // In this case, we should call UpdateVisualLines to recreate all placeholders
        // But to avoid infinite loops, we'll just update the visual lines
        UpdateVisualLines();
    }
    
    /// <summary>
    /// Check if a connection already exists between two nodes
    /// </summary>
    private bool ConnectionExists(BaseNode fromNode, BaseNode toNode)
    {
        if (fromNode == null || toNode == null) return false;
        
        // Check if from→to connection exists
        foreach (Connection conn in fromNode.OutgoingConnections)
        {
            if (conn != null && conn.ToNode == toNode)
            {
                return true;
            }
        }
        
        // Check if to→from connection exists (bidirectional)
        foreach (Connection conn in toNode.OutgoingConnections)
        {
            if (conn != null && conn.ToNode == fromNode)
            {
                return true;
            }
        }
        
        // Also check incoming connections
        foreach (Connection conn in fromNode.IncomingConnections)
        {
            if (conn != null && conn.FromNode == toNode)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Update line positions (call when nodes move)
    /// </summary>
    public void RefreshLinePositions()
    {
        if (!showPlaceholders) return;
        
        int lineIndex = 0;
        List<BaseNode> allNodes = levelController != null ? levelController.GetAllNodes() : new List<BaseNode>();
        Dictionary<string, BaseNode> nodeDict = new Dictionary<string, BaseNode>();
        
        foreach (var node in allNodes)
        {
            if (node != null && !string.IsNullOrEmpty(node.NodeID))
            {
                nodeDict[node.NodeID] = node;
            }
        }
        
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            
            List<string> possibleConnections = levelController.GetConnectionMapping(node.NodeID);
            
            foreach (string targetNodeID in possibleConnections)
            {
                if (lineIndex < placeholderLineObjects.Count)
                {
                    GameObject lineObj = placeholderLineObjects[lineIndex];
                    if (lineObj != null)
                    {
                        LineRenderer lineRenderer = lineObj.GetComponent<LineRenderer>();
                        if (lineRenderer != null && nodeDict.ContainsKey(targetNodeID))
                        {
                            BaseNode targetNode = nodeDict[targetNodeID];
                            if (targetNode != null)
                            {
                                // Ensure both start and end Y coordinates are always at ground level
                                Vector3 startPos = node.transform.position;
                                Vector3 endPos = targetNode.transform.position;
                                startPos.y = groundLevelY;
                                endPos.y = groundLevelY;
                                
                                lineRenderer.SetPosition(0, startPos);
                                lineRenderer.SetPosition(1, endPos);
                            }
                        }
                    }
                    lineIndex++;
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        ClearPlaceholderLines();
        
        // Clean up shared material
        if (sharedPlaceholderMaterial != null)
        {
            DestroyImmediate(sharedPlaceholderMaterial);
            sharedPlaceholderMaterial = null;
        }
    }
}

