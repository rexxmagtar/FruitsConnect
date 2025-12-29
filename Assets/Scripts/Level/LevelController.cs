using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Nodes")]
    [SerializeField] private List<BaseNode> allNodes = new List<BaseNode>();
    
    [Header("Connection Mappings")]
    [SerializeField] private List<ConnectionMapping> connectionMappings = new List<ConnectionMapping>();
    
    [Header("Visualization")]
    [SerializeField] private GraphVisualizer graphVisualizer;
    
    // Dictionary for fast lookup (built at runtime from serialized list)
    private Dictionary<string, List<string>> connectionDict;
    
    private void Awake()
    {
        BuildConnectionDictionary();
        
        // Get or create GraphVisualizer
        if (graphVisualizer == null)
        {
            graphVisualizer = GetComponent<GraphVisualizer>();
            if (graphVisualizer == null)
            {
                graphVisualizer = gameObject.AddComponent<GraphVisualizer>();
            }
        }
    }
    
    private void Start()
    {
        // Update visual lines after level is fully initialized
        if (graphVisualizer != null)
        {
            graphVisualizer.UpdateVisualLines();
        }
    }
    
    /// <summary>
    /// Build dictionary from serialized connection mappings
    /// </summary>
    private void BuildConnectionDictionary()
    {
        connectionDict = new Dictionary<string, List<string>>();
        
        foreach (var mapping in connectionMappings)
        {
            if (!string.IsNullOrEmpty(mapping.nodeID))
            {
                connectionDict[mapping.nodeID] = new List<string>(mapping.validTargetIDs);
            }
        }
    }
    
    /// <summary>
    /// Get all nodes in the level (removes nulls and duplicates)
    /// </summary>
    public List<BaseNode> GetAllNodes()
    {
        // Remove null entries
        allNodes.RemoveAll(n => n == null);
        
        // Remove duplicates by NodeID (keep first occurrence)
        HashSet<string> seenNodeIDs = new HashSet<string>();
        List<BaseNode> uniqueNodes = new List<BaseNode>();
        
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            
            // If node has no ID or ID is unique, add it
            if (string.IsNullOrEmpty(node.NodeID) || !seenNodeIDs.Contains(node.NodeID))
            {
                if (!string.IsNullOrEmpty(node.NodeID))
                {
                    seenNodeIDs.Add(node.NodeID);
                }
                uniqueNodes.Add(node);
            }
        }
        
        // Update the list if duplicates were found
        if (uniqueNodes.Count != allNodes.Count)
        {
            allNodes = uniqueNodes;
        }
        
        return allNodes;
    }
    
    /// <summary>
    /// Get list of valid connection target IDs for a given node
    /// </summary>
    public List<string> GetValidConnectionsFor(string nodeID)
    {
        if (connectionDict != null && connectionDict.ContainsKey(nodeID))
        {
            return connectionDict[nodeID];
        }
        
        return new List<string>();
    }
    
    /// <summary>
    /// Get all producer nodes
    /// </summary>
    public List<ProducerNode> GetProducerNodes()
    {
        return allNodes.OfType<ProducerNode>().ToList();
    }
    
    /// <summary>
    /// Get all consumer nodes
    /// </summary>
    public List<ConsumerNode> GetConsumerNodes()
    {
        return allNodes.OfType<ConsumerNode>().ToList();
    }
    
    /// <summary>
    /// Check if a connection from nodeA to nodeB is valid according to mappings
    /// </summary>
    public bool CanConnect(string fromNodeID, string toNodeID)
    {
        if (connectionDict != null && connectionDict.ContainsKey(fromNodeID))
        {
            return connectionDict[fromNodeID].Contains(toNodeID);
        }
        
        return false;
    }
    
    /// <summary>
    /// Add a node to the level (used by editor)
    /// Prevents duplicates by checking both reference and NodeID
    /// </summary>
    public void AddNode(BaseNode node)
    {
        if (node == null) return;
        
        // Check if node already exists by reference
        if (allNodes.Contains(node))
        {
            return; // Already in list
        }
        
        // Check if node with same NodeID already exists (prevents duplicates)
        if (!string.IsNullOrEmpty(node.NodeID))
        {
            var existingNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == node.NodeID);
            if (existingNode != null)
            {
                Debug.LogWarning($"Node with ID {node.NodeID} already exists in level. Skipping duplicate.");
                return; // Node with this ID already exists
            }
        }
        
        allNodes.Add(node);
        
        // Update visual lines when nodes are added
        if (graphVisualizer != null)
        {
            graphVisualizer.UpdateVisualLines();
        }
    }
    
    /// <summary>
    /// Remove duplicate nodes from the list (cleanup method)
    /// </summary>
    public void RemoveDuplicateNodes()
    {
        // Remove null entries first
        allNodes.RemoveAll(n => n == null);
        
        // Remove duplicates by NodeID (keep first occurrence)
        HashSet<string> seenNodeIDs = new HashSet<string>();
        List<BaseNode> uniqueNodes = new List<BaseNode>();
        
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            
            // If node has no ID or ID is unique, add it
            if (string.IsNullOrEmpty(node.NodeID) || !seenNodeIDs.Contains(node.NodeID))
            {
                if (!string.IsNullOrEmpty(node.NodeID))
                {
                    seenNodeIDs.Add(node.NodeID);
                }
                uniqueNodes.Add(node);
            }
            else
            {
                Debug.LogWarning($"Removing duplicate node with ID: {node.NodeID}");
            }
        }
        
        allNodes = uniqueNodes;
        Debug.Log($"Removed duplicates. Node count: {allNodes.Count}");
    }
    
    /// <summary>
    /// Remove a node from the level (used by editor)
    /// </summary>
    public void RemoveNode(BaseNode node)
    {
        allNodes.Remove(node);
        
        // Also remove from connection mappings
        connectionMappings.RemoveAll(m => m.nodeID == node.NodeID);
        
        // Remove this node from other nodes' target lists
        foreach (var mapping in connectionMappings)
        {
            mapping.validTargetIDs.Remove(node.NodeID);
        }
        
        BuildConnectionDictionary();
        
        // Update visual lines when nodes are removed
        if (graphVisualizer != null)
        {
            graphVisualizer.UpdateVisualLines();
        }
    }
    
    /// <summary>
    /// Update connection mapping for a node (used by editor)
    /// </summary>
    public void UpdateConnectionMapping(string nodeID, List<string> validTargets)
    {
        var existing = connectionMappings.Find(m => m.nodeID == nodeID);
        
        if (existing != null)
        {
            existing.validTargetIDs = new List<string>(validTargets);
        }
        else
        {
            connectionMappings.Add(new ConnectionMapping
            {
                nodeID = nodeID,
                validTargetIDs = new List<string>(validTargets)
            });
        }
        
        BuildConnectionDictionary();
        
        // Update visual lines when mappings change
        if (graphVisualizer != null)
        {
            graphVisualizer.UpdateVisualLines();
        }
    }
    
    /// <summary>
    /// Get connection mapping for a node (used by editor)
    /// </summary>
    public List<string> GetConnectionMapping(string nodeID)
    {
        var mapping = connectionMappings.Find(m => m.nodeID == nodeID);
        return mapping != null ? new List<string>(mapping.validTargetIDs) : new List<string>();
    }
}

/// <summary>
/// Serializable class for storing connection mappings in inspector
/// </summary>
[System.Serializable]
public class ConnectionMapping
{
    public string nodeID;
    public List<string> validTargetIDs = new List<string>();
}

