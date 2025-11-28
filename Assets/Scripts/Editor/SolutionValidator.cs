using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validates that a level has at least one valid solution
/// Ensures all producers can connect to consumers simultaneously without conflicts
/// </summary>
public static class SolutionValidator
{
    /// <summary>
    /// Check if the level is actually solvable (all producers can reach consumers without conflicts)
    /// Returns true if at least one valid solution exists
    /// </summary>
    public static bool IsLevelSolvable(List<BaseNode> allNodes, LevelController level, int startingEnergy = 5)
    {
        List<ProducerNode> producers = allNodes.OfType<ProducerNode>().ToList();
        List<ConsumerNode> consumers = allNodes.OfType<ConsumerNode>().ToList();
        
        if (producers.Count == 0 || consumers.Count == 0)
            return false;
        
        // Try to find a valid solution using backtracking with energy constraints
        Dictionary<string, List<string>> usedConnections = new Dictionary<string, List<string>>();
        HashSet<string> nodesWithAppliedEnergy = new HashSet<string>();
        return TryFindSolution(producers, consumers, allNodes, level, 0, usedConnections, startingEnergy, nodesWithAppliedEnergy);
    }
    
    /// <summary>
    /// Backtracking algorithm to find if any valid solution exists with energy constraints
    /// </summary>
    private static bool TryFindSolution(
        List<ProducerNode> producers,
        List<ConsumerNode> consumers,
        List<BaseNode> allNodes,
        LevelController level,
        int producerIndex,
        Dictionary<string, List<string>> usedConnections,
        int currentEnergy,
        HashSet<string> nodesWithAppliedEnergy)
    {
        // Base case: all producers connected
        if (producerIndex >= producers.Count)
            return true;
        
        ProducerNode currentProducer = producers[producerIndex];
        
        // Try to connect this producer to each available consumer
        foreach (ConsumerNode consumer in consumers)
        {
            // Try to find a path from producer to consumer with energy constraints
            var pathResult = FindPathToConsumer(currentProducer, consumer, allNodes, level, usedConnections, currentEnergy, nodesWithAppliedEnergy);
            
            if (pathResult != null && pathResult.Path != null && pathResult.Path.Count > 0)
            {
                // Mark this path as used
                var backupConnections = new Dictionary<string, List<string>>(usedConnections);
                var backupEnergy = currentEnergy;
                var backupAppliedNodes = new HashSet<string>(nodesWithAppliedEnergy);
                
                MarkPathAsUsed(pathResult.Path, usedConnections);
                
                // Update energy and applied nodes from the path
                int newEnergy = pathResult.CurrentEnergy;
                foreach (string nodeID in pathResult.NodesWithAppliedEnergy)
                {
                    nodesWithAppliedEnergy.Add(nodeID);
                }
                
                // Recursively try next producer
                if (TryFindSolution(producers, consumers, allNodes, level, producerIndex + 1, usedConnections, newEnergy, nodesWithAppliedEnergy))
                {
                    return true; // Found a solution!
                }
                
                // Backtrack - restore connections and energy
                usedConnections = backupConnections;
                currentEnergy = backupEnergy;
                nodesWithAppliedEnergy = backupAppliedNodes;
            }
        }
        
        return false; // No solution found for this producer
    }
    
    /// <summary>
    /// Find a path from producer to consumer that doesn't conflict with already used connections
    /// and respects energy constraints. Returns null if no valid path exists
    /// </summary>
    private static PathState FindPathToConsumer(
        ProducerNode producer,
        ConsumerNode consumer,
        List<BaseNode> allNodes,
        LevelController level,
        Dictionary<string, List<string>> usedConnections,
        int startingEnergy,
        HashSet<string> globalAppliedNodes)
    {
        Queue<PathState> queue = new Queue<PathState>();
        HashSet<string> visited = new HashSet<string>();
        
        queue.Enqueue(new PathState(producer.NodeID, new List<string> { producer.NodeID }, startingEnergy, new HashSet<string>(globalAppliedNodes)));
        visited.Add(producer.NodeID);
        
        while (queue.Count > 0)
        {
            PathState current = queue.Dequeue();
            
            // Check all possible next nodes
            List<string> possibleTargets = level.GetConnectionMapping(current.NodeID);
            
            foreach (string targetID in possibleTargets)
            {
                // Check if this connection is already used
                if (usedConnections.ContainsKey(current.NodeID) && 
                    usedConnections[current.NodeID].Contains(targetID))
                {
                    continue; // This connection is already used by another path
                }
                
                // Check reverse direction too (connections are bidirectional)
                if (usedConnections.ContainsKey(targetID) && 
                    usedConnections[targetID].Contains(current.NodeID))
                {
                    continue;
                }
                
                BaseNode targetNode = allNodes.FirstOrDefault(n => n.NodeID == targetID);
                if (targetNode == null || visited.Contains(targetID))
                    continue;
                
                // Calculate new energy if this is the first time connecting to this node
                int newEnergy = current.CurrentEnergy;
                HashSet<string> newAppliedNodes = new HashSet<string>(current.NodesWithAppliedEnergy);
                
                if (!current.NodesWithAppliedEnergy.Contains(targetID))
                {
                    // Check if we can afford this node (negative weight costs energy)
                    if (targetNode.Weight < 0 && newEnergy < Mathf.Abs(targetNode.Weight))
                    {
                        continue; // Not enough energy to connect to this node
                    }
                    
                    // Apply energy change (positive = gain, negative = lose)
                    newEnergy += targetNode.Weight;
                    newAppliedNodes.Add(targetID);
                }
                
                // Build new path
                List<string> newPath = new List<string>(current.Path);
                newPath.Add(targetID);
                
                // Found the consumer!
                if (targetNode is ConsumerNode && targetID == consumer.NodeID)
                {
                    return new PathState(targetID, newPath, newEnergy, newAppliedNodes);
                }
                
                // Check if node has capacity for more connections
                if (!CanNodeBeUsedInPath(targetNode, targetID, usedConnections))
                    continue;
                
                visited.Add(targetID);
                queue.Enqueue(new PathState(targetID, newPath, newEnergy, newAppliedNodes));
            }
        }
        
        return null; // No path found
    }
    
    /// <summary>
    /// Check if a node can still be used in a path based on its capacity and current usage
    /// </summary>
    private static bool CanNodeBeUsedInPath(BaseNode node, string nodeID, Dictionary<string, List<string>> usedConnections)
    {
        if (!usedConnections.ContainsKey(nodeID))
            return true;
        
        int usedCount = usedConnections[nodeID].Count;
        return usedCount < node.MaxOutgoingConnections;
    }
    
    /// <summary>
    /// Mark all connections in a path as used
    /// </summary>
    private static void MarkPathAsUsed(List<string> path, Dictionary<string, List<string>> usedConnections)
    {
        for (int i = 0; i < path.Count - 1; i++)
        {
            string from = path[i];
            string to = path[i + 1];
            
            if (!usedConnections.ContainsKey(from))
                usedConnections[from] = new List<string>();
            
            usedConnections[from].Add(to);
        }
    }
    
    /// <summary>
    /// Helper class to track path state during BFS
    /// </summary>
    private class PathState
    {
        public string NodeID { get; set; }
        public List<string> Path { get; set; }
        public int CurrentEnergy { get; set; }
        public HashSet<string> NodesWithAppliedEnergy { get; set; }
        
        public PathState(string nodeID, List<string> path, int energy = 0, HashSet<string> appliedNodes = null)
        {
            NodeID = nodeID;
            Path = path;
            CurrentEnergy = energy;
            NodesWithAppliedEnergy = appliedNodes ?? new HashSet<string>();
        }
    }
    
    /// <summary>
    /// Generate a solvable level by working backwards from a solution
    /// This ensures the level is always solvable
    /// </summary>
    public static void GenerateSolutionFirstLevel(
        List<BaseNode> producers,
        List<BaseNode> consumers,
        List<BaseNode> neutrals,
        LevelController level,
        DifficultyTier difficulty)
    {
        // Clear all existing connections
        foreach (var node in producers.Concat(neutrals).Concat(consumers))
        {
            level.UpdateConnectionMapping(node.NodeID, new List<string>());
        }
        
        // Create a solution path for each producer
        Dictionary<string, List<string>> solution = new Dictionary<string, List<string>>();
        List<BaseNode> availableNeutrals = new List<BaseNode>(neutrals);
        
        foreach (ProducerNode producer in producers)
        {
            // Pick a random consumer
            ConsumerNode targetConsumer = consumers[Random.Range(0, consumers.Count)] as ConsumerNode;
            
            // Build a path: Producer -> Neutrals -> Consumer
            List<string> path = new List<string> { producer.NodeID };
            
            // Determine path length based on difficulty
            int pathLength = difficulty switch
            {
                DifficultyTier.Easy => Random.Range(1, 3),
                DifficultyTier.Medium => Random.Range(2, 4),
                DifficultyTier.Hard => Random.Range(3, 5),
                DifficultyTier.Expert => Random.Range(4, 6),
                _ => 2
            };
            
            // Add neutral nodes to the path
            for (int i = 0; i < pathLength && availableNeutrals.Count > 0; i++)
            {
                // Pick nearest available neutral
                string lastNode = path[path.Count - 1];
                BaseNode lastNodeObj = producers.Concat(neutrals).Concat(consumers)
                    .FirstOrDefault(n => n.NodeID == lastNode);
                
                if (lastNodeObj != null && availableNeutrals.Count > 0)
                {
                    var nearestNeutral = availableNeutrals
                        .OrderBy(n => Vector3.Distance(n.transform.position, lastNodeObj.transform.position))
                        .First();
                    
                    path.Add(nearestNeutral.NodeID);
                    availableNeutrals.Remove(nearestNeutral);
                }
            }
            
            // Add consumer to path
            path.Add(targetConsumer.NodeID);
            
            // Convert path to connections (checking for intersections)
            List<BaseNode> allNodes = producers.Cast<BaseNode>().Concat(consumers).Concat(neutrals).ToList();
            
            for (int i = 0; i < path.Count - 1; i++)
            {
                string from = path[i];
                string to = path[i + 1];
                
                List<string> mappings = level.GetConnectionMapping(from);
                if (!mappings.Contains(to))
                {
                    // Check for intersection before adding
                    if (!ConnectionIntersectionChecker.WouldConnectionIntersect(from, to, allNodes, level))
                    {
                        mappings.Add(to);
                        level.UpdateConnectionMapping(from, mappings);
                    }
                }
            }
        }
        
        // Add some decoy connections based on difficulty
        AddDecoyConnections(producers.Concat(neutrals).ToList(), consumers, level, difficulty);
    }
    
    /// <summary>
    /// Add extra connections to increase difficulty (red herrings)
    /// </summary>
    private static void AddDecoyConnections(
        List<BaseNode> connectableNodes,
        List<BaseNode> consumers,
        LevelController level,
        DifficultyTier difficulty)
    {
        float decoyProbability = difficulty switch
        {
            DifficultyTier.Easy => 0.1f,
            DifficultyTier.Medium => 0.2f,
            DifficultyTier.Hard => 0.35f,
            DifficultyTier.Expert => 0.5f,
            _ => 0.2f
        };
        
        List<BaseNode> allNodes = connectableNodes.Concat(consumers).ToList();
        
        foreach (var node in connectableNodes)
        {
            List<string> currentMappings = level.GetConnectionMapping(node.NodeID);
            
            // Try to add decoy connections
            if (currentMappings.Count < node.MaxOutgoingConnections)
            {
                var nearbyNodes = allNodes
                    .Where(n => n != node && !currentMappings.Contains(n.NodeID))
                    .OrderBy(n => Vector3.Distance(n.transform.position, node.transform.position))
                    .Take(3);
                
                foreach (var target in nearbyNodes)
                {
                    if (Random.value < decoyProbability && currentMappings.Count < node.MaxOutgoingConnections)
                    {
                        // Check reverse connection doesn't exist
                        List<string> reverseMappings = level.GetConnectionMapping(target.NodeID);
                        if (!reverseMappings.Contains(node.NodeID))
                        {
                            // Check for intersection before adding
                            if (!ConnectionIntersectionChecker.WouldConnectionIntersect(node.NodeID, target.NodeID, allNodes, level))
                            {
                                currentMappings.Add(target.NodeID);
                            }
                        }
                    }
                }
                
                level.UpdateConnectionMapping(node.NodeID, currentMappings);
            }
        }
    }
}

