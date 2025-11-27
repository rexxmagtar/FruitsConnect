using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Nodes")]
    [SerializeField] private List<BaseNode> allNodes = new List<BaseNode>();
    
    [Header("Connection Mappings")]
    [SerializeField] private List<ConnectionMapping> connectionMappings = new List<ConnectionMapping>();
    
    // Dictionary for fast lookup (built at runtime from serialized list)
    private Dictionary<string, List<string>> connectionDict;
    
    private void Awake()
    {
        BuildConnectionDictionary();
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
    /// Get all nodes in the level
    /// </summary>
    public List<BaseNode> GetAllNodes()
    {
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
    /// </summary>
    public void AddNode(BaseNode node)
    {
        if (!allNodes.Contains(node))
        {
            allNodes.Add(node);
        }
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

