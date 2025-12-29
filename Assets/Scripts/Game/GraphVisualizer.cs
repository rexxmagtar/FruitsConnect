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
    
    [Header("References")]
    private LevelController levelController;
    
    // Store created line renderers
    private List<GameObject> placeholderLineObjects = new List<GameObject>();
    private GameObject linesParent;
    
    private void Awake()
    {
        // Create parent object for placeholder lines
        linesParent = new GameObject("PlaceholderLines");
        linesParent.transform.SetParent(transform);
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
                        CreatePlaceholderLine(node, targetNode);
                        linesCreated++;
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
        
        // Set material
        if (placeholderLineMaterial != null)
        {
            lineRenderer.material = new Material(placeholderLineMaterial); // Create instance
            lineRenderer.material.color = placeholderLineColor;
        }
        else
        {
            // Create default material if none assigned - use Unlit shader for better visibility
            Material defaultMat = new Material(Shader.Find("Unlit/Color"));
            if (defaultMat.shader.name == "Hidden/InternalErrorShader")
            {
                // Fallback to Sprites/Default if Unlit/Color not found
                defaultMat = new Material(Shader.Find("Sprites/Default"));
            }
            defaultMat.color = placeholderLineColor;
            lineRenderer.material = defaultMat;
        }
        
        // Ensure color is set
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = placeholderLineColor;
        }
        
        // Disable shadows
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        
        // Set positions
        lineRenderer.SetPosition(0, fromNode.transform.position);
        lineRenderer.SetPosition(1, toNode.transform.position);
        
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
                                lineRenderer.SetPosition(0, node.transform.position);
                                lineRenderer.SetPosition(1, targetNode.transform.position);
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
    }
}

