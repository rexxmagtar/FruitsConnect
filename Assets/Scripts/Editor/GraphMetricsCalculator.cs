using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Graph metrics for analyzing level difficulty
/// Based on graph theory concepts - see GraphTheory.md for details
/// </summary>
public struct GraphMetrics
{
    public int NodeCount;
    public int EdgeCount;
    public float GraphDensity;
    public float AveragePathLength;
    public int MaxPathLength;
    public int AlternativePathsCount;
    public float ComplexityScore;
    
    public override string ToString()
    {
        return $"Nodes: {NodeCount}, Edges: {EdgeCount}, Density: {GraphDensity:F2}, " +
               $"Avg Path: {AveragePathLength:F1}, Max Path: {MaxPathLength}, " +
               $"Alt Paths: {AlternativePathsCount}, Complexity: {ComplexityScore:F1}";
    }
}

/// <summary>
/// Calculates graph theory metrics for generated levels
/// </summary>
public static class GraphMetricsCalculator
{
    /// <summary>
    /// Calculate comprehensive graph metrics for difficulty analysis
    /// </summary>
    public static GraphMetrics Calculate(List<BaseNode> allNodes, LevelController level)
    {
        GraphMetrics metrics = new GraphMetrics();
        
        List<BaseNode> producers = allNodes.Where(n => n is ProducerNode).ToList();
        List<BaseNode> consumers = allNodes.Where(n => n is ConsumerNode).ToList();
        
        // Basic counts
        metrics.NodeCount = allNodes.Count;
        metrics.EdgeCount = 0;
        
        // Count edges
        foreach (var node in allNodes)
        {
            List<string> mappings = level.GetConnectionMapping(node.NodeID);
            metrics.EdgeCount += mappings.Count;
        }
        
        // Graph density: Actual edges / Possible edges
        // For directed graph: possible edges = n * (n-1)
        int maxPossibleEdges = allNodes.Count * (allNodes.Count - 1);
        metrics.GraphDensity = maxPossibleEdges > 0 ? (float)metrics.EdgeCount / maxPossibleEdges : 0;
        
        // Calculate path lengths from each consumer to nearest producer
        List<int> pathLengths = new List<int>();
        int alternativePaths = 0;
        
        foreach (ConsumerNode consumer in consumers)
        {
            var pathInfo = FindShortestPathToProducer(consumer, allNodes, level);
            if (pathInfo.pathLength > 0)
            {
                pathLengths.Add(pathInfo.pathLength);
                alternativePaths += pathInfo.alternativePathCount;
            }
        }
        
        metrics.AveragePathLength = pathLengths.Count > 0 ? (float)pathLengths.Average() : 0;
        metrics.MaxPathLength = pathLengths.Count > 0 ? pathLengths.Max() : 0;
        metrics.AlternativePathsCount = alternativePaths;
        
        // Calculate complexity score (higher = harder)
        // Based on: long paths, few alternatives, low density
        float pathComplexity = metrics.AveragePathLength * 2f;
        float alternativesPenalty = 10f / (metrics.AlternativePathsCount + 1); // Fewer alternatives = harder
        float densityPenalty = (1f - metrics.GraphDensity) * 10f; // Sparser = harder
        
        metrics.ComplexityScore = pathComplexity + alternativesPenalty + densityPenalty;
        
        return metrics;
    }
    
    /// <summary>
    /// Find shortest path from consumer to any producer using BFS
    /// Returns (path length, number of alternative paths of same length)
    /// </summary>
    private static (int pathLength, int alternativePathCount) FindShortestPathToProducer(
        ConsumerNode consumer, 
        List<BaseNode> allNodes, 
        LevelController level)
    {
        // BFS to find shortest path from consumer to any producer
        Dictionary<string, int> distances = new Dictionary<string, int>();
        Queue<(string nodeID, int distance)> queue = new Queue<(string, int)>();
        HashSet<string> visited = new HashSet<string>();
        
        queue.Enqueue((consumer.NodeID, 0));
        distances[consumer.NodeID] = 0;
        visited.Add(consumer.NodeID);
        
        int shortestPath = int.MaxValue;
        int pathsFound = 0;
        
        while (queue.Count > 0)
        {
            var (currentID, distance) = queue.Dequeue();
            
            // Check all nodes that connect TO current node (reverse direction for tracing back to producer)
            foreach (var node in allNodes)
            {
                if (node == null) continue;
                
                List<string> targets = level.GetConnectionMapping(node.NodeID);
                
                if (targets.Contains(currentID))
                {
                    int newDistance = distance + 1;
                    
                    // Found a producer
                    if (node is ProducerNode)
                    {
                        if (newDistance < shortestPath)
                        {
                            shortestPath = newDistance;
                            pathsFound = 1;
                        }
                        else if (newDistance == shortestPath)
                        {
                            pathsFound++;
                        }
                        continue;
                    }
                    
                    // Visit this node if not visited or found shorter path
                    if (!visited.Contains(node.NodeID) || newDistance < distances.GetValueOrDefault(node.NodeID, int.MaxValue))
                    {
                        visited.Add(node.NodeID);
                        distances[node.NodeID] = newDistance;
                        queue.Enqueue((node.NodeID, newDistance));
                    }
                }
            }
        }
        
        return shortestPath == int.MaxValue ? (0, 0) : (shortestPath, pathsFound);
    }
}

