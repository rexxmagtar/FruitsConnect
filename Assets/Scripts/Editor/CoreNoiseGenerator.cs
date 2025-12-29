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
        
        // Step 1: Create core solvable paths (GUARANTEED to be solvable)
        List<List<string>> corePaths = CreateCorePaths(producers, consumers, neutrals, level, difficulty, allNodes);
        
        // CRITICAL: Verify all consumers have at least one connection
        // If any consumer is unreachable, fix it immediately
        foreach (var consumer in consumers)
        {
            bool hasConnection = false;
            foreach (var node in allNodes)
            {
                var mappings = level.GetConnectionMapping(node.NodeID);
                if (mappings.Contains(consumer.NodeID))
                {
                    hasConnection = true;
                    break;
                }
            }
            
            if (!hasConnection)
            {
                Debug.LogWarning($"CRITICAL: Consumer {consumer.NodeID} has no connections! Creating emergency path.");
                
                // Emergency path creation - MUST succeed
                bool emergencyPathCreated = false;
                
                // Strategy 1: Find ANY neutral with available capacity and connect it
                var nearestNeutral = neutrals
                    .Where(n => 
                    {
                        var mappings = level.GetConnectionMapping(n.NodeID);
                        return mappings.Count < n.MaxOutgoingConnections;
                    })
                    .OrderBy(n => Vector3.Distance(n.transform.position, consumer.transform.position))
                    .FirstOrDefault();
                
                if (nearestNeutral != null)
                {
                    var mappings = level.GetConnectionMapping(nearestNeutral.NodeID);
                    if (!mappings.Contains(consumer.NodeID))
                    {
                        mappings.Add(consumer.NodeID);
                        level.UpdateConnectionMapping(nearestNeutral.NodeID, mappings);
                        Debug.Log($"Emergency Strategy 1: Connected {nearestNeutral.NodeID} -> {consumer.NodeID}");
                        emergencyPathCreated = true;
                    }
                }
                
                // Strategy 2: If no neutral available, create Producer -> Neutral -> Consumer path
                if (!emergencyPathCreated)
                {
                    var anyProducer = producers
                        .Where(p => 
                        {
                            var mappings = level.GetConnectionMapping(p.NodeID);
                            return mappings.Count < p.MaxOutgoingConnections;
                        })
                        .FirstOrDefault();
                    
                    var anyNeutral = neutrals
                        .Where(n => 
                        {
                            var mappings = level.GetConnectionMapping(n.NodeID);
                            return mappings.Count < n.MaxOutgoingConnections;
                        })
                        .FirstOrDefault();
                    
                    if (anyProducer != null && anyNeutral != null)
                    {
                        // Create path: Producer -> Neutral -> Consumer
                        var producerMappings = level.GetConnectionMapping(anyProducer.NodeID);
                        if (!producerMappings.Contains(anyNeutral.NodeID))
                        {
                            producerMappings.Add(anyNeutral.NodeID);
                            level.UpdateConnectionMapping(anyProducer.NodeID, producerMappings);
                        }
                        
                        var neutralMappings = level.GetConnectionMapping(anyNeutral.NodeID);
                        if (!neutralMappings.Contains(consumer.NodeID))
                        {
                            neutralMappings.Add(consumer.NodeID);
                            level.UpdateConnectionMapping(anyNeutral.NodeID, neutralMappings);
                            Debug.Log($"Emergency Strategy 2: Created path {anyProducer.NodeID} -> {anyNeutral.NodeID} -> {consumer.NodeID}");
                            emergencyPathCreated = true;
                        }
                    }
                }
                
                // Strategy 3: Last resort - use ANY producer and neutral, even if at capacity
                if (!emergencyPathCreated)
                {
                    Debug.LogError($"CRITICAL: Standard emergency paths failed for {consumer.NodeID}! Attempting force connection.");
                    
                    var anyProducer = producers.FirstOrDefault();
                    var anyNeutral = neutrals.FirstOrDefault();
                    
                    if (anyProducer != null && anyNeutral != null)
                    {
                        var producerMappings = level.GetConnectionMapping(anyProducer.NodeID);
                        if (!producerMappings.Contains(anyNeutral.NodeID))
                        {
                            producerMappings.Add(anyNeutral.NodeID);
                            level.UpdateConnectionMapping(anyProducer.NodeID, producerMappings);
                        }
                        
                        var neutralMappings = level.GetConnectionMapping(anyNeutral.NodeID);
                        if (!neutralMappings.Contains(consumer.NodeID))
                        {
                            neutralMappings.Add(consumer.NodeID);
                            level.UpdateConnectionMapping(anyNeutral.NodeID, neutralMappings);
                            Debug.LogError($"Emergency Strategy 3 (FORCED): Created path {anyProducer.NodeID} -> {anyNeutral.NodeID} -> {consumer.NodeID}");
                            emergencyPathCreated = true;
                        }
                    }
                }
                
                if (!emergencyPathCreated)
                {
                    Debug.LogError($"FATAL: Could not create any path to consumer {consumer.NodeID}! Level may be unsolvable.");
                }
            }
        }
        
        // Final verification - log all consumer connections before adding complexity
        Debug.Log("=== Consumer Connection Verification (After Core Paths) ===");
        foreach (var consumer in consumers)
        {
            List<string> incomingConnections = new List<string>();
            foreach (var node in allNodes)
            {
                var mappings = level.GetConnectionMapping(node.NodeID);
                if (mappings.Contains(consumer.NodeID))
                {
                    incomingConnections.Add(node.NodeID);
                }
            }
            
            if (incomingConnections.Count == 0)
            {
                Debug.LogError($"ERROR: Consumer {consumer.NodeID} has NO connections after core path creation!");
            }
            else
            {
                Debug.Log($"Consumer {consumer.NodeID} has {incomingConnections.Count} incoming connection(s): {string.Join(", ", incomingConnections)}");
            }
        }
        
        // Step 2: Add multiple paths to consumers (based on difficulty)
        AddMultiplePathsToConsumers(producers, consumers, neutrals, level, difficulty, allNodes);
        
        // Step 3: Add cycles to make it interesting
        AddCyclesToCore(corePaths, neutrals, level, difficulty, allNodes);
        
        // Step 4: Add noise (dead-end paths) - enhanced version
        // Only add noise AFTER core paths are guaranteed
        AddNoisePaths(neutrals, producers, consumers, level, difficulty, allNodes);
        
        // Step 5: Connect negative paths to solution paths
        ConnectNegativePathsToSolutions(producers, consumers, neutrals, level, difficulty, allNodes);
        
        // Calculate and log metrics
        LevelGenerationMetrics metrics = new LevelGenerationMetrics
        {
            TotalSolutionPathsCount = LevelGenerationMetrics.CalculateSolutionPaths(producers, consumers, level),
            TotalPathsCount = LevelGenerationMetrics.CalculateTotalPaths(allNodes, level),
            NegativePathsCount = LevelGenerationMetrics.CalculateNegativePaths(allNodes, consumers, level),
            LinksBetweenNegativeAndSolutionPaths = LevelGenerationMetrics.CalculateLinksBetweenNegativeAndSolutionPaths(
                allNodes, producers, consumers, level)
        };
        
        Debug.Log($"Generated level with {corePaths.Count} core paths and noise complexity");
        Debug.Log(metrics.ToString());
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
    /// GUARANTEES every consumer gets at least one connection
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
        
        // PHASE 1: Create ONE guaranteed path for EACH consumer first
        // This ensures every consumer is connected before adding complexity
        foreach (var consumer in consumers)
        {
            // Find a producer that can connect
            var availableProducer = producers
                .Where(p => 
                {
                    var mappings = level.GetConnectionMapping(p.NodeID);
                    return mappings.Count < p.MaxOutgoingConnections;
                })
                .OrderBy(p => Random.value)
                .FirstOrDefault();
            
            if (availableProducer == null) continue;
            
            // Create a guaranteed path: Producer -> Neutral(s) -> Consumer
            List<string> guaranteedPath = new List<string> { availableProducer.NodeID };
            
            // Determine how many neutrals we need
            int requiredNeutrals = difficulty == DifficultyTier.Expert ? Mathf.Max(1, minPathLength - 1) : 1;
            
            BaseNode lastNode = availableProducer;
            List<BaseNode> usedNeutrals = new List<BaseNode>();
            
            // Add required neutrals to path
            for (int i = 0; i < requiredNeutrals; i++)
            {
                BaseNode nextNeutral = null;
                
                // Try to find an available neutral
                if (availableNeutrals.Count > 0)
                {
                    nextNeutral = availableNeutrals
                        .Where(n => !usedNeutrals.Contains(n))
                        .OrderBy(n => Vector3.Distance(n.transform.position, lastNode.transform.position))
                        .FirstOrDefault();
                }
                
                // If no available neutrals, use any neutral (for Expert, allow reuse)
                if (nextNeutral == null && neutrals.Count > 0)
                {
                    nextNeutral = neutrals
                        .Where(n => !usedNeutrals.Contains(n))
                        .OrderBy(n => Vector3.Distance(n.transform.position, lastNode.transform.position))
                        .FirstOrDefault();
                }
                
                if (nextNeutral == null) break; // No more neutrals available
                
                guaranteedPath.Add(nextNeutral.NodeID);
                usedNeutrals.Add(nextNeutral);
                lastNode = nextNeutral;
                
                // Remove from available list if not Expert
                if (difficulty != DifficultyTier.Expert && availableNeutrals.Contains(nextNeutral))
                {
                    availableNeutrals.Remove(nextNeutral);
                }
            }
            
            // Ensure we have at least one neutral (minimum path: Producer -> Neutral -> Consumer)
            if (guaranteedPath.Count == 1 && neutrals.Count > 0)
            {
                var anyNeutral = neutrals
                    .Where(n => !usedNeutrals.Contains(n))
                    .OrderBy(n => Vector3.Distance(n.transform.position, availableProducer.transform.position))
                    .FirstOrDefault();
                
                if (anyNeutral != null)
                {
                    guaranteedPath.Add(anyNeutral.NodeID);
                    usedNeutrals.Add(anyNeutral);
                }
            }
            
            // Add consumer to path
            guaranteedPath.Add(consumer.NodeID);
            
            // Create connections for this guaranteed path
            // Use a simpler connection method that bypasses intersection checks if needed
            bool pathSuccess = true;
            for (int i = 0; i < guaranteedPath.Count - 1; i++)
            {
                string from = guaranteedPath[i];
                string to = guaranteedPath[i + 1];
                
                // Expert: Block direct producer-to-consumer
                if (difficulty == DifficultyTier.Expert)
                {
                    BaseNode fromNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == from);
                    BaseNode toNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == to);
                    
                    if (fromNode is ProducerNode && toNode is ConsumerNode)
                    {
                        // This shouldn't happen if we have neutrals, but skip just in case
                        continue;
                    }
                }
                
                // Try to add connection, but if intersection check fails, try anyway for guaranteed paths
                if (!AddConnectionIfNoIntersection(from, to, level, allNodes, difficulty))
                {
                    // For guaranteed paths, try to add anyway (bypass intersection check)
                    var mappings = level.GetConnectionMapping(from);
                    if (!mappings.Contains(to))
                    {
                        BaseNode fromNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == from);
                        if (fromNode != null && mappings.Count < fromNode.MaxOutgoingConnections)
                        {
                            mappings.Add(to);
                            level.UpdateConnectionMapping(from, mappings);
                            Debug.Log($"Guaranteed path: Added connection {from} -> {to} (bypassed intersection check)");
                        }
                        else
                        {
                            pathSuccess = false;
                            break;
                        }
                    }
                }
            }
            
            if (pathSuccess)
            {
                corePaths.Add(guaranteedPath);
                consumerPathCounts[consumer.NodeID] = 1;
                Debug.Log($"Created guaranteed path for consumer {consumer.NodeID}: {string.Join(" -> ", guaranteedPath)}");
            }
            else
            {
                Debug.LogWarning($"Failed to create guaranteed path for consumer {consumer.NodeID}");
            }
        }
        
        // PHASE 2: Create additional paths for complexity (beyond the guaranteed ones)
        // Determine how many additional paths per producer based on difficulty
        int additionalPathsPerProducer = difficulty switch
        {
            DifficultyTier.Easy => 0,      // Already have 1 per consumer
            DifficultyTier.Medium => 0,   // Already have 1 per consumer
            DifficultyTier.Hard => 1,     // Add 1 more path per producer
            DifficultyTier.Expert => 2,    // Add 2 more paths per producer for complexity
            _ => 0
        };
        
        // Create additional paths for each producer
        foreach (ProducerNode producer in producers)
        {
            for (int pathIndex = 0; pathIndex < additionalPathsPerProducer; pathIndex++)
            {
                // For Expert, try to create paths to different consumers
                ConsumerNode targetConsumer;
                if (difficulty == DifficultyTier.Expert && consumers.Count > 1)
                {
                    // Distribute paths across different consumers
                    targetConsumer = consumers
                        .OrderBy(c => consumerPathCounts[c.NodeID])
                        .ThenBy(c => Random.value)
                        .First() as ConsumerNode;
                }
                else
                {
                    // Pick consumer with fewest paths (or random if tied)
                    targetConsumer = consumers
                        .OrderBy(c => consumerPathCounts[c.NodeID])
                        .ThenBy(c => Random.value)
                        .First() as ConsumerNode;
                }
                
                consumerPathCounts[targetConsumer.NodeID]++;
                
                // Build path
                List<string> path = new List<string> { producer.NodeID };
                
                // For Expert, ensure minimum path length is enforced (no shortcuts)
                int pathLength = Random.Range(minPathLength, maxPathLength + 1);
                
                // Expert difficulty: Ensure paths are never too short
                if (difficulty == DifficultyTier.Expert)
                {
                    // Expert must have at least 4 neutral nodes in path (5 total including producer and consumer)
                    pathLength = Mathf.Max(pathLength, 4);
                    pathLength = Mathf.Min(pathLength, maxPathLength + 2); // Allow slightly longer paths
                }
                
                // For Expert, allow neutrals to be reused across paths (creates cycles and alternatives)
                // For other difficulties, try to use different neutrals
                List<BaseNode> pathNeutrals;
                if (difficulty == DifficultyTier.Expert)
                {
                    // Expert: Allow reuse, but prefer different neutrals for variety
                    // Get neutrals that haven't been used in previous paths from this producer
                    var usedInPreviousPaths = corePaths
                        .Where(p => p[0] == producer.NodeID)
                        .SelectMany(p => p.Skip(1).Take(p.Count - 2)) // Skip producer and consumer
                        .ToHashSet();
                    
                    pathNeutrals = availableNeutrals
                        .Where(n => !usedInPreviousPaths.Contains(n.NodeID))
                        .ToList();
                    
                    // If we've used all neutrals, allow reuse
                    if (pathNeutrals.Count < pathLength)
                    {
                        pathNeutrals = new List<BaseNode>(availableNeutrals);
                    }
                }
                else
                {
                    // Other difficulties: Use different neutrals for each path
                    pathNeutrals = new List<BaseNode>(availableNeutrals);
                }
                
                // Add neutral nodes to path, preferring nearby ones
                BaseNode lastNode = producer;
                for (int i = 0; i < pathLength && pathNeutrals.Count > 0; i++)
                {
                    // Find nearest available neutral from our subset
                    var nearestNeutral = pathNeutrals
                        .OrderBy(n => Vector3.Distance(n.transform.position, lastNode.transform.position))
                        .FirstOrDefault();
                    
                    if (nearestNeutral == null) break;
                    
                    path.Add(nearestNeutral.NodeID);
                    lastNode = nearestNeutral;
                    pathNeutrals.Remove(nearestNeutral);
                    
                    // Remove from main list only for non-Expert (prevents reuse)
                    if (difficulty != DifficultyTier.Expert)
                    {
                        availableNeutrals.Remove(nearestNeutral);
                    }
                }
                
                // Ensure path has enough nodes - if not enough, try to add more neutrals
                // CRITICAL: We must ensure every consumer gets at least one path
                if (difficulty == DifficultyTier.Expert && path.Count < minPathLength + 2)
                {
                    // Path is too short - try to add more neutrals
                    // Find more available neutrals (can reuse for Expert)
                    var additionalNeutrals = availableNeutrals
                        .Where(n => !path.Contains(n.NodeID))
                        .OrderBy(n => Vector3.Distance(n.transform.position, lastNode.transform.position))
                        .Take(minPathLength + 2 - path.Count)
                        .ToList();
                    
                    foreach (var additionalNeutral in additionalNeutrals)
                    {
                        path.Add(additionalNeutral.NodeID);
                        lastNode = additionalNeutral;
                    }
                    
                    // If still too short, use any available neutrals (even if already used)
                    if (path.Count < minPathLength + 2)
                    {
                        var anyNeutrals = neutrals
                            .Where(n => !path.Contains(n.NodeID))
                            .OrderBy(n => Vector3.Distance(n.transform.position, lastNode.transform.position))
                            .Take(minPathLength + 2 - path.Count)
                            .ToList();
                        
                        foreach (var neutral in anyNeutrals)
                        {
                            path.Add(neutral.NodeID);
                            lastNode = neutral;
                        }
                    }
                }
                
                // CRITICAL: Ensure path has at least producer -> neutral -> consumer (minimum 3 nodes)
                if (path.Count < 3)
                {
                    Debug.LogWarning($"Path too short, adding minimum required nodes");
                    // Add at least one neutral if we don't have any
                    if (path.Count == 1)
                    {
                        var anyNeutral = neutrals.FirstOrDefault();
                        if (anyNeutral != null)
                        {
                            path.Add(anyNeutral.NodeID);
                        }
                    }
                }
                
                // Add consumer to path - ALWAYS add consumer to ensure solvability
                path.Add(targetConsumer.NodeID);
                corePaths.Add(path);
                
                // Create connections for this path (checking for intersections)
                // CRITICAL: Never create direct producer-to-consumer connections
                for (int i = 0; i < path.Count - 1; i++)
                {
                    string from = path[i];
                    string to = path[i + 1];
                    
                    // Expert difficulty: Block any direct producer-to-consumer connections
                    if (difficulty == DifficultyTier.Expert)
                    {
                        BaseNode fromNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == from);
                        BaseNode toNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == to);
                        
                        if (fromNode is ProducerNode && toNode is ConsumerNode)
                        {
                            Debug.LogWarning($"Expert difficulty: Blocked direct producer-to-consumer connection {from} -> {to}");
                            continue; // Skip this connection
                        }
                    }
                    
                    AddConnectionIfNoIntersection(from, to, level, allNodes, difficulty);
                }
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
        // Expert should have many alternatives for maximum complexity
        int alternativeOptionsPerConsumer = difficulty switch
        {
            DifficultyTier.Easy => 0,      // No alternatives - keep it simple
            DifficultyTier.Medium => 1,    // 1 alternative option per consumer
            DifficultyTier.Hard => 2,      // 2 alternative options per consumer
            DifficultyTier.Expert => 5,    // 5 alternative options per consumer for maximum paths
            _ => 1
        };
        
        if (alternativeOptionsPerConsumer == 0) return;
        
        // Get nodes that can connect to consumers
        // IMPORTANT: For Expert difficulty, NEVER allow direct producer-to-consumer connections
        // Only neutrals that are already part of paths can connect to consumers
        List<BaseNode> connectableNodes = new List<BaseNode>();
        
        if (difficulty == DifficultyTier.Expert)
        {
            // Expert: Only use neutrals that are already in paths (no direct producer connections)
            connectableNodes.AddRange(neutrals.Where(n =>
            {
                var mappings = level.GetConnectionMapping(n.NodeID);
                return mappings.Count > 0 && mappings.Count < n.MaxOutgoingConnections;
            }));
        }
        else
        {
            // Other difficulties: Allow producers and neutrals
            connectableNodes.AddRange(producers);
            connectableNodes.AddRange(neutrals.Where(n =>
            {
                var mappings = level.GetConnectionMapping(n.NodeID);
                return mappings.Count > 0 && mappings.Count < n.MaxOutgoingConnections;
            }));
        }
        
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
                
                // Expert difficulty: NEVER allow direct producer-to-consumer connections (extra safety check)
                if (difficulty == DifficultyTier.Expert && candidate is ProducerNode)
                {
                    continue; // Skip producers for Expert difficulty
                }
                
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
        // Expert should have MORE cycles for complexity, not fewer
        int cycleCount = difficulty switch
        {
            DifficultyTier.Easy => corePaths.Count * 2,
            DifficultyTier.Medium => corePaths.Count * 2,
            DifficultyTier.Hard => corePaths.Count * 3,
            DifficultyTier.Expert => corePaths.Count * 4, // More cycles for Expert
            _ => corePaths.Count * 2
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
                    AddConnectionIfNoIntersection(node1.NodeID, node2.NodeID, level, allNodes, difficulty);
                }
            }
        }
    }
    
    /// <summary>
    /// Add noise paths (dead ends) to increase apparent complexity
    /// Enhanced version that generates more negative paths and connects them to solutions
    /// </summary>
    private static void AddNoisePaths(
        List<BaseNode> neutrals,
        List<BaseNode> producers,
        List<BaseNode> consumers,
        LevelController level,
        DifficultyTier difficulty,
        List<BaseNode> allNodes)
    {
        // Determine noise intensity based on difficulty - increased for more complexity
        float noiseIntensity = difficulty switch
        {
            DifficultyTier.Easy => 0.3f,      // Increased from 0.2f
            DifficultyTier.Medium => 0.5f,     // Increased from 0.4f
            DifficultyTier.Hard => 0.7f,      // Increased from 0.6f
            DifficultyTier.Expert => 0.9f,     // Increased from 0.8f
            _ => 0.5f
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
        
        // Add noise connections - create more dead-end paths
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
                    AddConnectionIfNoIntersection(nearbyConnectable.NodeID, unusedNode.NodeID, level, allNodes, difficulty);
                    
                    // Chain more dead-end nodes - increased probability
                    float chainProbability = difficulty switch
                    {
                        DifficultyTier.Easy => 0.4f,
                        DifficultyTier.Medium => 0.5f,
                        DifficultyTier.Hard => 0.6f,
                        DifficultyTier.Expert => 0.7f,
                        _ => 0.5f
                    };
                    
                    if (Random.value < chainProbability && unusedNeutrals.Count > 0)
                    {
                        var nextDeadEnd = unusedNeutrals
                            .Where(n => n != unusedNode)
                            .OrderBy(n => Vector3.Distance(n.transform.position, unusedNode.transform.position))
                            .FirstOrDefault();
                        
                        if (nextDeadEnd != null && unusedNode.MaxOutgoingConnections > 0)
                        {
                            AddConnectionIfNoIntersection(unusedNode.NodeID, nextDeadEnd.NodeID, level, allNodes, difficulty);
                            
                            // Maybe add one more level of chaining
                            if (Random.value < 0.3f && unusedNeutrals.Count > 1)
                            {
                                var thirdDeadEnd = unusedNeutrals
                                    .Where(n => n != unusedNode && n != nextDeadEnd)
                                    .OrderBy(n => Vector3.Distance(n.transform.position, nextDeadEnd.transform.position))
                                    .FirstOrDefault();
                                
                                if (thirdDeadEnd != null && nextDeadEnd.MaxOutgoingConnections > 0)
                                {
                                    AddConnectionIfNoIntersection(nextDeadEnd.NodeID, thirdDeadEnd.NodeID, level, allNodes, difficulty);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Add more extra connections between existing nodes to create false paths
        // Increased multiplier for more complexity
        float extraConnectionsMultiplier = difficulty switch
        {
            DifficultyTier.Easy => 0.3f,
            DifficultyTier.Medium => 0.5f,
            DifficultyTier.Hard => 0.7f,
            DifficultyTier.Expert => 1.0f,
            _ => 0.5f
        };
        
        int extraConnections = (int)(allNodes.Count * noiseIntensity * extraConnectionsMultiplier);
        
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
                .Take(5) // Increased from 3 to 5 for more options
                .ToList();
            
            if (nearbyNodes.Count > 0)
            {
                var target = nearbyNodes[Random.Range(0, nearbyNodes.Count)];
                
                // Check reverse doesn't exist
                var reverseMappings = level.GetConnectionMapping(target.NodeID);
                if (!reverseMappings.Contains(node1.NodeID))
                {
                    AddConnectionIfNoIntersection(node1.NodeID, target.NodeID, level, allNodes, difficulty);
                }
            }
        }
    }
    
    /// <summary>
    /// Connect negative paths (dead-ends) to solution paths to increase complexity
    /// </summary>
    private static void ConnectNegativePathsToSolutions(
        List<BaseNode> producers,
        List<BaseNode> consumers,
        List<BaseNode> neutrals,
        LevelController level,
        DifficultyTier difficulty,
        List<BaseNode> allNodes)
    {
        // Find nodes that are part of solution paths
        HashSet<string> solutionPathNodes = new HashSet<string>();
        
        // Get all nodes in core paths
        foreach (var producer in producers)
        {
            foreach (var consumer in consumers)
            {
                // Use BFS to find all nodes in paths from producer to consumer
                Queue<BaseNode> queue = new Queue<BaseNode>();
                HashSet<string> visited = new HashSet<string>();
                
                queue.Enqueue(producer);
                visited.Add(producer.NodeID);
                solutionPathNodes.Add(producer.NodeID);
                
                while (queue.Count > 0)
                {
                    BaseNode current = queue.Dequeue();
                    
                    if (current == consumer)
                    {
                        solutionPathNodes.Add(consumer.NodeID);
                        break;
                    }
                    
                    List<string> targets = level.GetConnectionMapping(current.NodeID);
                    foreach (string targetID in targets)
                    {
                        if (!visited.Contains(targetID))
                        {
                            BaseNode targetNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == targetID);
                            if (targetNode != null)
                            {
                                visited.Add(targetID);
                                solutionPathNodes.Add(targetID);
                                queue.Enqueue(targetNode);
                            }
                        }
                    }
                }
            }
        }
        
        // Find negative path nodes (nodes that cannot reach consumers)
        List<BaseNode> negativePathNodes = new List<BaseNode>();
        foreach (var node in neutrals)
        {
            if (solutionPathNodes.Contains(node.NodeID)) continue;
            
            bool canReachConsumer = false;
            foreach (var consumer in consumers)
            {
                if (HasPathToNode(node, consumer, level, allNodes))
                {
                    canReachConsumer = true;
                    break;
                }
            }
            
            if (!canReachConsumer)
            {
                negativePathNodes.Add(node);
            }
        }
        
        // Connect negative paths to solution paths
        int connectionsToMake = difficulty switch
        {
            DifficultyTier.Easy => Mathf.Max(1, negativePathNodes.Count / 3),
            DifficultyTier.Medium => Mathf.Max(2, negativePathNodes.Count / 2),
            DifficultyTier.Hard => Mathf.Max(3, (int)(negativePathNodes.Count * 0.7f)),
            DifficultyTier.Expert => negativePathNodes.Count,
            _ => negativePathNodes.Count / 2
        };
        
        int connectionsMade = 0;
        foreach (var negativeNode in negativePathNodes)
        {
            if (connectionsMade >= connectionsToMake) break;
            
            // Find nearest solution path node
            BaseNode nearestSolutionNode = null;
            float minDistance = float.MaxValue;
            
            foreach (var solutionNodeID in solutionPathNodes)
            {
                BaseNode solutionNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == solutionNodeID);
                if (solutionNode != null && solutionNode != negativeNode)
                {
                    float distance = Vector3.Distance(negativeNode.transform.position, solutionNode.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestSolutionNode = solutionNode;
                    }
                }
            }
            
            if (nearestSolutionNode != null)
            {
                // Try to connect negative node to solution node
                List<string> solutionMappings = level.GetConnectionMapping(nearestSolutionNode.NodeID);
                if (solutionMappings.Count < nearestSolutionNode.MaxOutgoingConnections)
                {
                    if (AddConnectionIfNoIntersection(nearestSolutionNode.NodeID, negativeNode.NodeID, level, allNodes, difficulty))
                    {
                        connectionsMade++;
                    }
                }
                // Or connect from negative node to solution node (if negative node has capacity)
                else if (negativeNode.MaxOutgoingConnections > 0)
                {
                    List<string> negativeMappings = level.GetConnectionMapping(negativeNode.NodeID);
                    if (negativeMappings.Count < negativeNode.MaxOutgoingConnections)
                    {
                        if (AddConnectionIfNoIntersection(negativeNode.NodeID, nearestSolutionNode.NodeID, level, allNodes, difficulty))
                        {
                            connectionsMade++;
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Connected {connectionsMade} negative paths to solution paths");
    }
    
    /// <summary>
    /// Check if there is a path from fromNode to toNode
    /// </summary>
    private static bool HasPathToNode(BaseNode fromNode, BaseNode toNode, LevelController level, List<BaseNode> allNodes)
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
                    BaseNode targetNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == targetID);
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
    /// Add a connection between two nodes if it doesn't intersect existing connections
    /// </summary>
    private static bool AddConnectionIfNoIntersection(string fromID, string toID, LevelController level, List<BaseNode> allNodes, DifficultyTier difficulty = DifficultyTier.Medium)
    {
        // Check if connection already exists
        List<string> mappings = level.GetConnectionMapping(fromID);
        if (mappings.Contains(toID))
        {
            return false; // Already exists
        }
        
        // Find the actual nodes
        BaseNode fromNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == fromID);
        BaseNode toNode = allNodes.FirstOrDefault(n => n != null && n.NodeID == toID);
        
        // Expert difficulty: NEVER allow direct producer-to-consumer connections
        if (difficulty == DifficultyTier.Expert && fromNode is ProducerNode && toNode is ConsumerNode)
        {
            return false; // Block direct connections on Expert
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

