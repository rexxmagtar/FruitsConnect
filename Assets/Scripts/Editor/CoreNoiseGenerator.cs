using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates levels using a core template + noise strategy
/// 1. Creates core solvable paths with cycles
/// 2. Adds noise (dead-end paths) to increase complexity
/// </summary>
public static class CoreNoiseGenerator
{
    /// <summary>
    /// Generate a complete level with core paths and noise
    /// </summary>
    public static void GenerateLevel(
        List<BaseNode> producers,
        List<BaseNode> consumers,
        List<BaseNode> neutrals,
        LevelController level,
        DifficultyTier difficulty)
    {
        // Store all nodes for intersection checking
        List<BaseNode> allNodes = new List<BaseNode>();
        allNodes.AddRange(producers);
        allNodes.AddRange(consumers);
        allNodes.AddRange(neutrals);
        
        // Clear all existing connections
        ClearAllConnections(producers, consumers, neutrals, level);
        
        // Step 1: Create core solvable paths
        List<List<string>> corePaths = CreateCorePaths(producers, consumers, neutrals, level, difficulty, allNodes);
        
        // Step 2: Add multiple paths to consumers (based on difficulty)
        AddMultiplePathsToConsumers(producers, consumers, neutrals, level, difficulty, allNodes);
        
        // Step 3: Add cycles to make it interesting
        AddCyclesToCore(corePaths, neutrals, level, difficulty, allNodes);
        
        // Step 4: Add noise (dead-end paths)
        AddNoisePaths(neutrals, producers, consumers, level, difficulty, allNodes);
        
        Debug.Log($"Generated level with {corePaths.Count} core paths and noise complexity");
    }
    
    /// <summary>
    /// Clear all connections
    /// </summary>
    private static void ClearAllConnections(List<BaseNode> producers, List<BaseNode> consumers, List<BaseNode> neutrals, LevelController level)
    {
        foreach (var node in producers.Concat(consumers).Concat(neutrals))
        {
            level.UpdateConnectionMapping(node.NodeID, new List<string>());
        }
    }
    
    /// <summary>
    /// Create core solvable paths from producers to consumers
    /// </summary>
    private static List<List<string>> CreateCorePaths(
        List<BaseNode> producers,
        List<BaseNode> consumers,
        List<BaseNode> neutrals,
        LevelController level,
        DifficultyTier difficulty,
        List<BaseNode> allNodes)
    {
        List<List<string>> corePaths = new List<List<string>>();
        List<BaseNode> availableNeutrals = new List<BaseNode>(neutrals);
        
        // Determine path length based on difficulty
        int minPathLength = difficulty switch
        {
            DifficultyTier.Easy => 2,
            DifficultyTier.Medium => 3,
            DifficultyTier.Hard => 4,
            DifficultyTier.Expert => 5,
            _ => 3
        };
        
        int maxPathLength = minPathLength + 2;
        
        // Distribute producers across consumers more evenly
        // Track how many paths go to each consumer
        Dictionary<string, int> consumerPathCounts = new Dictionary<string, int>();
        foreach (var consumer in consumers)
        {
            consumerPathCounts[consumer.NodeID] = 0;
        }
        
        // Create a path for each producer
        foreach (ProducerNode producer in producers)
        {
            // Pick consumer with fewest paths (or random if tied)
            var targetConsumer = consumers
                .OrderBy(c => consumerPathCounts[c.NodeID])
                .ThenBy(c => Random.value)
                .First() as ConsumerNode;
            
            consumerPathCounts[targetConsumer.NodeID]++;
            
            // Build path
            List<string> path = new List<string> { producer.NodeID };
            
            int pathLength = Random.Range(minPathLength, maxPathLength + 1);
            
            // Add neutral nodes to path, preferring nearby ones
            BaseNode lastNode = producer;
            for (int i = 0; i < pathLength && availableNeutrals.Count > 0; i++)
            {
                // Find nearest available neutral
                var nearestNeutral = availableNeutrals
                    .OrderBy(n => Vector3.Distance(n.transform.position, lastNode.transform.position))
                    .First();
                
                path.Add(nearestNeutral.NodeID);
                lastNode = nearestNeutral;
                availableNeutrals.Remove(nearestNeutral);
            }
            
            // Add consumer to path
            path.Add(targetConsumer.NodeID);
            corePaths.Add(path);
            
            // Create connections for this path (checking for intersections)
            for (int i = 0; i < path.Count - 1; i++)
            {
                string from = path[i];
                string to = path[i + 1];
                
                AddConnectionIfNoIntersection(from, to, level, allNodes);
            }
        }
        
        return corePaths;
    }
    
    /// <summary>
    /// Add multiple possible paths to consumers in connection mappings
    /// Creates alternative connection options - player must choose which one to build
    /// Only adds to mappings (doesn't build connections) - player builds them during gameplay
    /// </summary>
    private static void AddMultiplePathsToConsumers(
        List<BaseNode> producers,
        List<BaseNode> consumers,
        List<BaseNode> neutrals,
        LevelController level,
        DifficultyTier difficulty,
        List<BaseNode> allNodes)
    {
        // Determine how many alternative connection options per consumer based on difficulty
        int alternativeOptionsPerConsumer = difficulty switch
        {
            DifficultyTier.Easy => 0,      // No alternatives - keep it simple
            DifficultyTier.Medium => 1,    // 1 alternative option per consumer
            DifficultyTier.Hard => 2,      // 2 alternative options per consumer
            DifficultyTier.Expert => 3,    // 3 alternative options per consumer
            _ => 1
        };
        
        if (alternativeOptionsPerConsumer == 0) return;
        
        // Get nodes that can connect to consumers (producers and neutrals with connections)
        List<BaseNode> connectableNodes = new List<BaseNode>();
        connectableNodes.AddRange(producers);
        connectableNodes.AddRange(neutrals.Where(n =>
        {
            var mappings = level.GetConnectionMapping(n.NodeID);
            return mappings.Count > 0 && mappings.Count < n.MaxOutgoingConnections;
        }));
        
        // For each consumer, add multiple possible connection sources
        foreach (ConsumerNode consumer in consumers)
        {
            // Find nodes that could connect to this consumer
            // Prefer nodes that are:
            // 1. Not already mapped to this consumer
            // 2. Have capacity for more connections
            // 3. Are reasonably close (but not too close - want some variety)
            var candidateNodes = connectableNodes
                .Where(n =>
                {
                    var mappings = level.GetConnectionMapping(n.NodeID);
                    return !mappings.Contains(consumer.NodeID) && 
                           mappings.Count < n.MaxOutgoingConnections;
                })
                .OrderBy(n => Vector3.Distance(n.transform.position, consumer.transform.position))
                .ToList();
            
            // Add alternative connection options to mappings
            int added = 0;
            foreach (var candidate in candidateNodes)
            {
                if (added >= alternativeOptionsPerConsumer) break;
                
                // Check if this connection would intersect with existing connections
                if (ConnectionIntersectionChecker.WouldConnectionIntersect(
                    candidate.NodeID, consumer.NodeID, allNodes, level))
                {
                    continue; // Skip if would intersect
                }
                
                // Add to connection mappings (this makes it a POSSIBLE connection)
                // Player will choose which one to actually build
                List<string> mappings = level.GetConnectionMapping(candidate.NodeID);
                mappings.Add(consumer.NodeID);
                level.UpdateConnectionMapping(candidate.NodeID, mappings);
                
                added++;
                Debug.Log($"Added alternative connection option: {candidate.NodeID} -> {consumer.NodeID}");
            }
        }
    }
    
    /// <summary>
    /// Add cycles to the core paths to create multiple solution options
    /// </summary>
    private static void AddCyclesToCore(
        List<List<string>> corePaths,
        List<BaseNode> neutrals,
        LevelController level,
        DifficultyTier difficulty,
        List<BaseNode> allNodes)
    {
        // Determine number of cycles based on difficulty
        int cycleCount = difficulty switch
        {
            DifficultyTier.Easy => corePaths.Count * 2,
            DifficultyTier.Medium => corePaths.Count,
            DifficultyTier.Hard => Mathf.Max(1, corePaths.Count / 2),
            DifficultyTier.Expert => Mathf.Max(1, corePaths.Count / 3),
            _ => corePaths.Count
        };
        
        // Get all nodes that are part of core paths
        HashSet<string> coreNodeIDs = new HashSet<string>();
        foreach (var path in corePaths)
        {
            foreach (var nodeID in path)
            {
                coreNodeIDs.Add(nodeID);
            }
        }
        
        List<BaseNode> coreNodes = neutrals.Where(n => coreNodeIDs.Contains(n.NodeID)).ToList();
        
        // Create cycles by connecting nodes that are close but not directly connected
        for (int i = 0; i < cycleCount && coreNodes.Count >= 2; i++)
        {
            // Pick two random core nodes
            BaseNode node1 = coreNodes[Random.Range(0, coreNodes.Count)];
            BaseNode node2 = coreNodes[Random.Range(0, coreNodes.Count)];
            
            if (node1 == node2) continue;
            
            // Check if they're not already connected
            List<string> mappings1 = level.GetConnectionMapping(node1.NodeID);
            List<string> mappings2 = level.GetConnectionMapping(node2.NodeID);
            
            if (!mappings1.Contains(node2.NodeID) && !mappings2.Contains(node1.NodeID))
            {
                // Check capacity
                if (mappings1.Count < node1.MaxOutgoingConnections)
                {
                    AddConnectionIfNoIntersection(node1.NodeID, node2.NodeID, level, allNodes);
                }
            }
        }
    }
    
    /// <summary>
    /// Add noise paths (dead ends) to increase apparent complexity
    /// </summary>
    private static void AddNoisePaths(
        List<BaseNode> neutrals,
        List<BaseNode> producers,
        List<BaseNode> consumers,
        LevelController level,
        DifficultyTier difficulty,
        List<BaseNode> allNodes)
    {
        // Determine noise intensity based on difficulty
        float noiseIntensity = difficulty switch
        {
            DifficultyTier.Easy => 0.2f,
            DifficultyTier.Medium => 0.4f,
            DifficultyTier.Hard => 0.6f,
            DifficultyTier.Expert => 0.8f,
            _ => 0.4f
        };
        
        // Find unused neutral nodes
        List<BaseNode> unusedNeutrals = neutrals.Where(n =>
        {
            var mappings = level.GetConnectionMapping(n.NodeID);
            return mappings.Count == 0;
        }).ToList();
        
        // Get all nodes that can have outgoing connections
        var connectableNodes = producers.Cast<BaseNode>()
            .Concat(neutrals)
            .Where(n => level.GetConnectionMapping(n.NodeID).Count < n.MaxOutgoingConnections)
            .ToList();
        
        // Add noise connections
        foreach (var unusedNode in unusedNeutrals)
        {
            if (Random.value > noiseIntensity) continue;
            
            // Connect this unused node to a nearby node that has capacity
            var nearbyConnectable = connectableNodes
                .Where(n => n != unusedNode)
                .OrderBy(n => Vector3.Distance(n.transform.position, unusedNode.transform.position))
                .FirstOrDefault();
            
            if (nearbyConnectable != null)
            {
                List<string> mappings = level.GetConnectionMapping(nearbyConnectable.NodeID);
                if (mappings.Count < nearbyConnectable.MaxOutgoingConnections)
                {
                    AddConnectionIfNoIntersection(nearbyConnectable.NodeID, unusedNode.NodeID, level, allNodes);
                    
                    // Maybe chain a few more dead-end nodes
                    if (Random.value < 0.3f && unusedNeutrals.Count > 0)
                    {
                        var nextDeadEnd = unusedNeutrals
                            .Where(n => n != unusedNode)
                            .OrderBy(n => Vector3.Distance(n.transform.position, unusedNode.transform.position))
                            .FirstOrDefault();
                        
                        if (nextDeadEnd != null && unusedNode.MaxOutgoingConnections > 0)
                        {
                            AddConnectionIfNoIntersection(unusedNode.NodeID, nextDeadEnd.NodeID, level, allNodes);
                        }
                    }
                }
            }
        }
        
        // Add some extra connections between existing nodes to create false paths
        int extraConnections = (int)(allNodes.Count * noiseIntensity);
        
        for (int i = 0; i < extraConnections; i++)
        {
            var node1 = allNodes[Random.Range(0, allNodes.Count)];
            
            // Skip if no capacity
            var mappings = level.GetConnectionMapping(node1.NodeID);
            if (mappings.Count >= node1.MaxOutgoingConnections) continue;
            
            // Find nearby nodes
            var nearbyNodes = allNodes
                .Where(n => n != node1 && !mappings.Contains(n.NodeID))
                .OrderBy(n => Vector3.Distance(n.transform.position, node1.transform.position))
                .Take(3)
                .ToList();
            
            if (nearbyNodes.Count > 0)
            {
                var target = nearbyNodes[Random.Range(0, nearbyNodes.Count)];
                
                // Check reverse doesn't exist
                var reverseMappings = level.GetConnectionMapping(target.NodeID);
                if (!reverseMappings.Contains(node1.NodeID))
                {
                    AddConnectionIfNoIntersection(node1.NodeID, target.NodeID, level, allNodes);
                }
            }
        }
    }
    
    /// <summary>
    /// Add a connection between two nodes if it doesn't intersect existing connections
    /// </summary>
    private static bool AddConnectionIfNoIntersection(string fromID, string toID, LevelController level, List<BaseNode> allNodes)
    {
        // Check if connection already exists
        List<string> mappings = level.GetConnectionMapping(fromID);
        if (mappings.Contains(toID))
        {
            return false; // Already exists
        }
        
        // Check if this connection would intersect with any existing connections
        if (ConnectionIntersectionChecker.WouldConnectionIntersect(fromID, toID, allNodes, level))
        {
            return false; // Would intersect
        }
        
        // Safe to add
        mappings.Add(toID);
        level.UpdateConnectionMapping(fromID, mappings);
        return true;
    }
}

