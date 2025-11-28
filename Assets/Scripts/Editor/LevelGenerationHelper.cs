using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Helper class for automatic level generation
/// Handles node positioning, connection generation, and connectivity validation
/// </summary>
public static class LevelGenerationHelper
{
    /// <summary>
    /// Create a node at a random valid position within bounds
    /// Producers spawn near bottom (Z min), Consumers near top (Z max)
    /// </summary>
    public static BaseNode CreateNodeAtRandomPosition(
        NodeType nodeType, 
        Bounds bounds, 
        List<BaseNode> existingNodes,
        LevelCreationConfig config,
        LevelController currentLevel,
        DifficultyTier difficulty)
    {
        int maxAttempts = 100;
        Vector3 position = Vector3.zero;
        bool validPositionFound = false;
        
        // Special minimum distance rules
        float minDistanceRequired = config.MinNodeDistance;
        
        // Producers and Consumers should be far apart from each other to prevent trivial solutions
        float minProducerConsumerDistance = config.MinNodeDistance * 3f;
        
        // Define spawn zones based on node type
        float zMin = bounds.min.z;
        float zMax = bounds.max.z;
        float zRange = zMax - zMin;
        
        float spawnZMin, spawnZMax;
        
        switch (nodeType)
        {
            case NodeType.Producer:
                // Producers spawn in bottom zone
                spawnZMin = zMin;
                spawnZMax = zMin + (zRange * config.ProducerZoneSize);
                break;
                
            case NodeType.Consumer:
                // Consumers spawn in top zone
                spawnZMin = zMax - (zRange * config.ConsumerZoneSize);
                spawnZMax = zMax;
                break;
                
            case NodeType.Neutral:
            default:
                // Neutral nodes can spawn in middle area with margins from edges
                spawnZMin = zMin + (zRange * config.NeutralZoneMargin);
                spawnZMax = zMax - (zRange * config.NeutralZoneMargin);
                break;
        }
        
        // Try to find a valid position
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            position = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                0f,  // Keep Y at 0 for flat gameplay
                Random.Range(spawnZMin, spawnZMax)
            );
            
            // Check distance from existing nodes
            validPositionFound = true;
            foreach (BaseNode existingNode in existingNodes)
            {
                if (existingNode == null) continue;
                
                float distance = Vector3.Distance(position, existingNode.transform.position);
                
                // If we're placing a Producer or Consumer, keep them far from opposite type
                if ((nodeType == NodeType.Producer && existingNode is ConsumerNode) ||
                    (nodeType == NodeType.Consumer && existingNode is ProducerNode))
                {
                    if (distance < minProducerConsumerDistance)
                    {
                        validPositionFound = false;
                        break;
                    }
                }
                // Regular minimum distance for all other cases
                else if (distance < minDistanceRequired)
                {
                    validPositionFound = false;
                    break;
                }
            }
            
            if (validPositionFound) break;
        }
        
        if (!validPositionFound)
        {
            Debug.LogWarning($"Could not find valid position for {nodeType} node after {maxAttempts} attempts");
            return null;
        }
        
        // Create the node
        GameObject nodeObj = null;
        BaseNode node = null;
        string nodeID = $"Node_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        
        GameObject prefab = config.GetNodePrefabByType(nodeType);
        
        if (prefab != null)
        {
            nodeObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            
            if (nodeObj != null)
            {
                nodeObj.transform.SetParent(currentLevel.transform);
                nodeObj.transform.position = position;
                
                node = nodeObj.GetComponent<BaseNode>();
                
                switch (nodeType)
                {
                    case NodeType.Producer:
                        nodeObj.name = $"Producer_{nodeID}";
                        node.MaxOutgoingConnections = GetMaxConnectionsForDifficulty(difficulty, true);
                        node.Weight = 0; // Producers have no weight
                        break;
                    case NodeType.Consumer:
                        nodeObj.name = $"Consumer_{nodeID}";
                        node.MaxOutgoingConnections = 0; // Consumers have no outgoing
                        node.Weight = 0; // Consumers have no weight
                        break;
                    case NodeType.Neutral:
                        nodeObj.name = $"Neutral_{nodeID}";
                        node.MaxOutgoingConnections = GetMaxConnectionsForDifficulty(difficulty, false);
                        node.Weight = AssignWeightForDifficulty(difficulty); // Assign random weight
                        break;
                }
                
                node.NodeID = nodeID;
                currentLevel.AddNode(node);
            }
        }
        
        return node;
    }
    
    /// <summary>
    /// Get maximum outgoing connections based on difficulty
    /// </summary>
    public static int GetMaxConnectionsForDifficulty(DifficultyTier difficulty, bool isProducer)
    {
        // Producers typically have more connections
        if (isProducer)
        {
            return difficulty switch
            {
                DifficultyTier.Easy => 4,
                DifficultyTier.Medium => 3,
                DifficultyTier.Hard => 2,
                DifficultyTier.Expert => 2,
                _ => 3
            };
        }
        else
        {
            return difficulty switch
            {
                DifficultyTier.Easy => 3,
                DifficultyTier.Medium => 2,
                DifficultyTier.Hard => 2,
                DifficultyTier.Expert => 1,
                _ => 2
            };
        }
    }
    
    /// <summary>
    /// Assign a random weight to a neutral node based on difficulty
    /// Positive weight = gives energy to player
    /// Negative weight = costs energy
    /// </summary>
    public static int AssignWeightForDifficulty(DifficultyTier difficulty)
    {
        // Determine probability of positive vs negative weight
        float positiveWeightChance = difficulty switch
        {
            DifficultyTier.Easy => 0.6f,      // 60% positive (more energy givers)
            DifficultyTier.Medium => 0.5f,    // 50/50 split
            DifficultyTier.Hard => 0.4f,      // 40% positive (more energy cost)
            DifficultyTier.Expert => 0.3f,    // 30% positive (much harder)
            _ => 0.5f
        };
        
        bool isPositive = Random.value < positiveWeightChance;
        
        // Generate random weight in range 1-3
        int magnitude = Random.Range(1, 4); // Range is 1 to 3 inclusive
        
        return isPositive ? magnitude : -magnitude;
    }
    
    /// <summary>
    /// Generate connections between nodes based on difficulty
    /// </summary>
    public static void GenerateConnections(List<BaseNode> allNodes, DifficultyTier difficulty, LevelController currentLevel)
    {
        if (allNodes.Count < 2) return;
        
        // Separate nodes by type
        List<BaseNode> producers = allNodes.Where(n => n is ProducerNode).ToList();
        List<BaseNode> consumers = allNodes.Where(n => n is ConsumerNode).ToList();
        List<BaseNode> neutrals = allNodes.Where(n => n is NeutralNode).ToList();
        
        // Connection probability based on difficulty
        float connectionProbability = difficulty switch
        {
            DifficultyTier.Easy => 0.7f,
            DifficultyTier.Medium => 0.5f,
            DifficultyTier.Hard => 0.35f,
            DifficultyTier.Expert => 0.25f,
            _ => 0.5f
        };
        
        // First, connect producers to neutral nodes
        if (neutrals.Count > 0)
        {
            foreach (ProducerNode producer in producers)
            {
                List<string> producerMappings = currentLevel.GetConnectionMapping(producer.NodeID);
                
                // Find nearby neutral nodes
                var nearbyNeutrals = neutrals
                    .OrderBy(n => Vector3.Distance(n.transform.position, producer.transform.position))
                    .Take(2) // Connect to 2 nearest neutral nodes
                    .ToList();
                
                foreach (BaseNode neutral in nearbyNeutrals)
                {
                    if (producerMappings.Count >= producer.MaxOutgoingConnections)
                        break;
                    
                    if (!producerMappings.Contains(neutral.NodeID))
                    {
                        producerMappings.Add(neutral.NodeID);
                    }
                }
                
                currentLevel.UpdateConnectionMapping(producer.NodeID, producerMappings);
            }
        }
        
        // Then, connect neutral nodes to consumers
        if (neutrals.Count > 0)
        {
            foreach (ConsumerNode consumer in consumers)
            {
                // Find nearest neutral node to connect to this consumer
                BaseNode nearestNeutral = neutrals.OrderBy(n => Vector3.Distance(n.transform.position, consumer.transform.position)).First();
                
                List<string> mappings = currentLevel.GetConnectionMapping(nearestNeutral.NodeID);
                if (mappings.Count < nearestNeutral.MaxOutgoingConnections && !mappings.Contains(consumer.NodeID))
                {
                    mappings.Add(consumer.NodeID);
                    currentLevel.UpdateConnectionMapping(nearestNeutral.NodeID, mappings);
                }
            }
        }
        else if (producers.Count > 0 && consumers.Count > 0)
        {
            // Only if there are no neutral nodes at all, allow direct connection
            foreach (ConsumerNode consumer in consumers)
            {
                BaseNode nearestProducer = producers.OrderBy(n => Vector3.Distance(n.transform.position, consumer.transform.position)).First();
                
                List<string> mappings = currentLevel.GetConnectionMapping(nearestProducer.NodeID);
                if (mappings.Count < nearestProducer.MaxOutgoingConnections && !mappings.Contains(consumer.NodeID))
                {
                    mappings.Add(consumer.NodeID);
                    currentLevel.UpdateConnectionMapping(nearestProducer.NodeID, mappings);
                }
            }
        }
        
        // Now connect all non-consumer nodes based on proximity and difficulty
        List<BaseNode> connectableNodes = new List<BaseNode>();
        connectableNodes.AddRange(producers);
        connectableNodes.AddRange(neutrals);
        
        foreach (BaseNode node in connectableNodes)
        {
            List<string> currentMappings = currentLevel.GetConnectionMapping(node.NodeID);
            
            // Find nearby nodes
            List<BaseNode> nearbyNodes = allNodes
                .Where(n => n != node && n.NodeID != node.NodeID)
                .OrderBy(n => Vector3.Distance(n.transform.position, node.transform.position))
                .ToList();
            
            // Try to connect to nearby nodes
            foreach (BaseNode target in nearbyNodes)
            {
                // Check if we've reached max connections
                if (currentMappings.Count >= node.MaxOutgoingConnections)
                    break;
                
                // Don't connect if already connected
                if (currentMappings.Contains(target.NodeID))
                    continue;
                
                // IMPORTANT: Prevent direct Producer -> Consumer connections to avoid trivial solutions
                if (node is ProducerNode && target is ConsumerNode)
                {
                    continue;
                }
                
                // Check if reverse connection exists (avoid bidirectional)
                List<string> reverseMappings = currentLevel.GetConnectionMapping(target.NodeID);
                if (reverseMappings.Contains(node.NodeID))
                    continue;
                
                // Distance-based connection with probability
                float distance = Vector3.Distance(node.transform.position, target.transform.position);
                float maxConnectionDistance = difficulty switch
                {
                    DifficultyTier.Easy => 10f,
                    DifficultyTier.Medium => 7f,
                    DifficultyTier.Hard => 5f,
                    DifficultyTier.Expert => 4f,
                    _ => 7f
                };
                
                if (distance <= maxConnectionDistance && Random.value < connectionProbability)
                {
                    currentMappings.Add(target.NodeID);
                }
            }
            
            currentLevel.UpdateConnectionMapping(node.NodeID, currentMappings);
        }
    }
    
    /// <summary>
    /// Place walls between nodes that are NOT connected by mapping
    /// AND have no other nodes between them in 3D space
    /// </summary>
    public static void PlaceWalls(List<BaseNode> allNodes, LevelCreationConfig config, LevelController currentLevel)
    {
        if (config.WallPrefab == null)
        {
            Debug.LogWarning("No wall prefab assigned in LevelCreationConfig. Skipping wall placement.");
            return;
        }
        
        // Create a parent object for walls
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.SetParent(currentLevel.transform);
        
        int wallsPlaced = 0;
        
        // For each pair of nodes
        for (int i = 0; i < allNodes.Count; i++)
        {
            for (int j = i + 1; j < allNodes.Count; j++)
            {
                BaseNode nodeA = allNodes[i];
                BaseNode nodeB = allNodes[j];
                
                if (nodeA == null || nodeB == null) continue;
                
                // Check if they're connected by mapping
                List<string> mappingsA = currentLevel.GetConnectionMapping(nodeA.NodeID);
                List<string> mappingsB = currentLevel.GetConnectionMapping(nodeB.NodeID);
                bool isConnected = mappingsA.Contains(nodeB.NodeID) || mappingsB.Contains(nodeA.NodeID);
                
                // If NOT connected, check if there are any nodes between them
                if (!isConnected)
                {
                    bool hasNodesBetween = AreThereNodesBetween(nodeA, nodeB, allNodes);
                    
                    // Place wall only if NO nodes between them
                    if (!hasNodesBetween)
                    {
                        // Place a wall between them
                        Vector3 midPoint = (nodeA.transform.position + nodeB.transform.position) / 2f;
                        midPoint.y = 0f;  // Ensure wall is at Y=0
                        Vector3 direction = (nodeB.transform.position - nodeA.transform.position).normalized;
                        
                        GameObject wall = PrefabUtility.InstantiatePrefab(config.WallPrefab) as GameObject;
                        if (wall != null)
                        {
                            wall.transform.SetParent(wallsParent.transform);
                            wall.transform.position = midPoint;
                            
                            // Rotate wall to face the connection direction
                            wall.transform.rotation = Quaternion.LookRotation(direction);
                            
                            wall.name = $"Wall_{nodeA.NodeID}_{nodeB.NodeID}";
                            wallsPlaced++;
                        }
                    }
                }
            }
        }
        
        int totalPairs = (allNodes.Count * (allNodes.Count - 1)) / 2;
        int nonConnectedPairs = 0;
        int clearedPairs = 0;
        
        // Count statistics for debugging
        for (int i = 0; i < allNodes.Count; i++)
        {
            for (int j = i + 1; j < allNodes.Count; j++)
            {
                List<string> mappingsA = currentLevel.GetConnectionMapping(allNodes[i].NodeID);
                List<string> mappingsB = currentLevel.GetConnectionMapping(allNodes[j].NodeID);
                bool isConnected = mappingsA.Contains(allNodes[j].NodeID) || mappingsB.Contains(allNodes[i].NodeID);
                
                if (!isConnected)
                {
                    nonConnectedPairs++;
                    if (!AreThereNodesBetween(allNodes[i], allNodes[j], allNodes))
                    {
                        clearedPairs++;
                    }
                }
            }
        }
        
        Debug.Log($"Wall Placement Statistics:");
        Debug.Log($"  Total node pairs: {totalPairs}");
        Debug.Log($"  Non-connected pairs: {nonConnectedPairs}");
        Debug.Log($"  Pairs with clear line of sight: {clearedPairs}");
        Debug.Log($"  Walls placed: {wallsPlaced}");
        
        if (wallsPlaced == 0 && clearedPairs > 0)
        {
            Debug.LogWarning("No walls placed! Check if WallPrefab is properly assigned in LevelCreationConfig.");
        }
    }
    
    /// <summary>
    /// Check if there are any nodes between two nodes in 3D space
    /// Uses line segment intersection with threshold
    /// </summary>
    private static bool AreThereNodesBetween(BaseNode nodeA, BaseNode nodeB, List<BaseNode> allNodes)
    {
        Vector3 posA = nodeA.transform.position;
        Vector3 posB = nodeB.transform.position;
        Vector3 lineDirection = (posB - posA).normalized;
        float lineLength = Vector3.Distance(posA, posB);
        
        // Threshold for considering a node "between" two others (node radius)
        float threshold = 0.8f; // Approximately node radius + small margin
        
        int nodesBetweenCount = 0;
        
        foreach (BaseNode otherNode in allNodes)
        {
            if (otherNode == null || otherNode == nodeA || otherNode == nodeB)
                continue;
            
            Vector3 posOther = otherNode.transform.position;
            
            // Calculate distance from point to line segment
            Vector3 toOther = posOther - posA;
            float projectionLength = Vector3.Dot(toOther, lineDirection);
            
            // Check if projection is within the line segment (not behind or beyond)
            if (projectionLength > 0.1f && projectionLength < lineLength - 0.1f) // Small margin
            {
                // Calculate perpendicular distance from line
                Vector3 projectionPoint = posA + lineDirection * projectionLength;
                float perpendicularDistance = Vector3.Distance(posOther, projectionPoint);
                
                // If node is close to the line between A and B, it's "between" them
                if (perpendicularDistance < threshold)
                {
                    nodesBetweenCount++;
                }
            }
        }
        
        // Return true if any nodes are between them
        return nodesBetweenCount > 0;
    }
}

/// <summary>
/// Difficulty tiers for auto-generated levels
/// </summary>
public enum DifficultyTier
{
    Easy,
    Medium,
    Hard,
    Expert
}

