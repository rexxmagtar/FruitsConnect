using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Metrics for tracking level generation complexity
/// Tracks solution paths, negative paths, and their connections
/// </summary>
public class LevelGenerationMetrics
{
    public int TotalSolutionPathsCount { get; set; }
    public int TotalPathsCount { get; set; }
    public int NegativePathsCount { get; set; }
    public int LinksBetweenNegativeAndSolutionPaths { get; set; }
    
    /// <summary>
    /// Calculate solution paths (paths from producers to consumers)
    /// </summary>
    public static int CalculateSolutionPaths(List<BaseNode> producers, List<BaseNode> consumers, LevelController level)
    {
        int solutionPaths = 0;
        
        foreach (var producer in producers)
        {
            foreach (var consumer in consumers)
            {
                if (HasPath(producer, consumer, level))
                {
                    solutionPaths++;
                }
            }
        }
        
        return solutionPaths;
    }
    
    /// <summary>
    /// Calculate total paths in the graph (all possible paths)
    /// </summary>
    public static int CalculateTotalPaths(List<BaseNode> allNodes, LevelController level)
    {
        int totalPaths = 0;
        
        // Count all possible paths between any two nodes
        for (int i = 0; i < allNodes.Count; i++)
        {
            for (int j = 0; j < allNodes.Count; j++)
            {
                if (i != j && HasPath(allNodes[i], allNodes[j], level))
                {
                    totalPaths++;
                }
            }
        }
        
        return totalPaths;
    }
    
    /// <summary>
    /// Calculate negative paths (dead-end paths that don't reach consumers)
    /// </summary>
    public static int CalculateNegativePaths(List<BaseNode> allNodes, List<BaseNode> consumers, LevelController level)
    {
        int negativePaths = 0;
        
        // Find nodes that cannot reach any consumer
        foreach (var node in allNodes)
        {
            if (node is ProducerNode || node is ConsumerNode) continue;
            
            bool canReachConsumer = false;
            foreach (var consumer in consumers)
            {
                if (HasPath(node, consumer, level))
                {
                    canReachConsumer = true;
                    break;
                }
            }
            
            if (!canReachConsumer)
            {
                // Count paths leading to this dead-end node
                foreach (var otherNode in allNodes)
                {
                    if (otherNode != node && HasPath(otherNode, node, level))
                    {
                        negativePaths++;
                    }
                }
            }
        }
        
        return negativePaths;
    }
    
    /// <summary>
    /// Calculate links between negative paths and solution paths
    /// </summary>
    public static int CalculateLinksBetweenNegativeAndSolutionPaths(
        List<BaseNode> allNodes, 
        List<BaseNode> producers, 
        List<BaseNode> consumers, 
        LevelController level)
    {
        int links = 0;
        
        // Find nodes in solution paths
        HashSet<string> solutionPathNodes = new HashSet<string>();
        foreach (var producer in producers)
        {
            foreach (var consumer in consumers)
            {
                if (HasPath(producer, consumer, level))
                {
                    // Get all nodes in this path
                    var pathNodes = GetPathNodes(producer, consumer, level);
                    foreach (var nodeID in pathNodes)
                    {
                        solutionPathNodes.Add(nodeID);
                    }
                }
            }
        }
        
        // Find negative path nodes (dead-ends)
        HashSet<string> negativePathNodes = new HashSet<string>();
        foreach (var node in allNodes)
        {
            if (node is ProducerNode || node is ConsumerNode) continue;
            if (solutionPathNodes.Contains(node.NodeID)) continue;
            
            bool canReachConsumer = false;
            foreach (var consumer in consumers)
            {
                if (HasPath(node, consumer, level))
                {
                    canReachConsumer = true;
                    break;
                }
            }
            
            if (!canReachConsumer)
            {
                negativePathNodes.Add(node.NodeID);
            }
        }
        
        // Count connections between solution and negative paths
        foreach (var solutionNodeID in solutionPathNodes)
        {
            var node = allNodes.FirstOrDefault(n => n.NodeID == solutionNodeID);
            if (node == null) continue;
            
            List<string> mappings = level.GetConnectionMapping(node.NodeID);
            foreach (var targetID in mappings)
            {
                if (negativePathNodes.Contains(targetID))
                {
                    links++;
                }
            }
        }
        
        return links;
    }
    
    /// <summary>
    /// Check if there is a path from fromNode to toNode
    /// </summary>
    private static bool HasPath(BaseNode fromNode, BaseNode toNode, LevelController level)
    {
        if (fromNode == null || toNode == null) return false;
        if (fromNode == toNode) return true;
        
        HashSet<string> visited = new HashSet<string>();
        Queue<BaseNode> queue = new Queue<BaseNode>();
        
        queue.Enqueue(fromNode);
        visited.Add(fromNode.NodeID);
        
        while (queue.Count > 0)
        {
            BaseNode current = queue.Dequeue();
            
            if (current == toNode)
            {
                return true;
            }
            
            List<string> targets = level.GetConnectionMapping(current.NodeID);
            foreach (string targetID in targets)
            {
                if (!visited.Contains(targetID))
                {
                    // Find node by ID
                    BaseNode targetNode = FindNodeByID(targetID, level);
                    if (targetNode != null)
                    {
                        visited.Add(targetID);
                        queue.Enqueue(targetNode);
                    }
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all node IDs in a path from fromNode to toNode
    /// </summary>
    private static List<string> GetPathNodes(BaseNode fromNode, BaseNode toNode, LevelController level)
    {
        List<string> pathNodes = new List<string>();
        
        if (fromNode == null || toNode == null) return pathNodes;
        
        Dictionary<string, string> parentMap = new Dictionary<string, string>();
        Queue<BaseNode> queue = new Queue<BaseNode>();
        HashSet<string> visited = new HashSet<string>();
        
        queue.Enqueue(fromNode);
        visited.Add(fromNode.NodeID);
        parentMap[fromNode.NodeID] = null;
        
        while (queue.Count > 0)
        {
            BaseNode current = queue.Dequeue();
            
            if (current == toNode)
            {
                // Reconstruct path
                string nodeID = toNode.NodeID;
                while (nodeID != null)
                {
                    pathNodes.Add(nodeID);
                    parentMap.TryGetValue(nodeID, out nodeID);
                }
                pathNodes.Reverse();
                return pathNodes;
            }
            
            List<string> targets = level.GetConnectionMapping(current.NodeID);
            foreach (string targetID in targets)
            {
                if (!visited.Contains(targetID))
                {
                    BaseNode targetNode = FindNodeByID(targetID, level);
                    if (targetNode != null)
                    {
                        visited.Add(targetID);
                        parentMap[targetID] = current.NodeID;
                        queue.Enqueue(targetNode);
                    }
                }
            }
        }
        
        return pathNodes;
    }
    
    /// <summary>
    /// Find a node by its ID
    /// </summary>
    private static BaseNode FindNodeByID(string nodeID, LevelController level)
    {
        if (level == null) return null;
        
        List<BaseNode> allNodes = level.GetAllNodes();
        return allNodes.FirstOrDefault(n => n != null && n.NodeID == nodeID);
    }
    
    public override string ToString()
    {
        float solutionRatio = TotalPathsCount > 0 ? (float)TotalSolutionPathsCount / TotalPathsCount : 0f;
        
        return $"Level Generation Metrics:\n" +
               $"  Solution Paths: {TotalSolutionPathsCount}\n" +
               $"  Total Paths: {TotalPathsCount}\n" +
               $"  Negative Paths: {NegativePathsCount}\n" +
               $"  Links (Negative↔Solution): {LinksBetweenNegativeAndSolutionPaths}\n" +
               $"  Solution/Total Ratio: {solutionRatio:F2}";
    }
}

