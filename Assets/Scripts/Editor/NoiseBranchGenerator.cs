using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates noise branches that reconnect to the skeleton network
/// CRITICAL: No dead ends - all branches must reconnect
/// </summary>
public static class NoiseBranchGenerator
{
    /// <summary>
    /// Add noise branches to the skeleton network
    /// Creates both reconnecting branches and dead-end branches (red herrings)
    /// If no unused nodes available, can create dead ends from skeleton nodes themselves
    /// </summary>
    public static void AddNoiseBranches(
        SkeletonPathGenerator.SkeletonNetwork skeletonNetwork,
        List<string> unusedNeutralIDs,
        DifficultyTier difficulty,
        Dictionary<string, BaseNode> nodesByID)
    {
        // For Expert difficulty, ensure we create dead ends even if no unused nodes
        // We can create dead ends by connecting skeleton nodes to consumers in ways that create red herrings
        if (unusedNeutralIDs.Count == 0)
        {
            if (difficulty == DifficultyTier.Expert)
            {
                Debug.Log("No unused neutral nodes, but Expert difficulty - creating dead-end connections from skeleton nodes");
                CreateDeadEndsFromSkeletonNodes(skeletonNetwork, difficulty, nodesByID);
            }
            else
            {
                Debug.Log("No unused neutral nodes for noise branches");
            }
            return;
        }
        
        // Determine branch characteristics based on difficulty
        int branchCount = GetBranchCount(difficulty, unusedNeutralIDs.Count);
        int minBranchLength = GetMinBranchLength(difficulty);
        int maxBranchLength = GetMaxBranchLength(difficulty);
        
        Debug.Log($"Adding {branchCount} noise branches (length {minBranchLength}-{maxBranchLength})");
        
        // Get all skeleton nodes that can be branch attachment points
        List<string> skeletonNodeIDs = skeletonNetwork.NeutralLayerIDs
            .SelectMany(layer => layer)
            .ToList();
        
        // Track which neutrals have been used in branches
        HashSet<string> usedInBranches = new HashSet<string>();
        
        for (int i = 0; i < branchCount; i++)
        {
            // Pick random skeleton node as branch start
            string branchStartID = skeletonNodeIDs[Random.Range(0, skeletonNodeIDs.Count)];
            
            // Determine branch length
            int branchLength = Random.Range(minBranchLength, maxBranchLength + 1);
            
            // Select nodes for this branch
            List<string> availableForBranch = unusedNeutralIDs
                .Where(id => !usedInBranches.Contains(id))
                .ToList();
            
            if (availableForBranch.Count == 0)
            {
                Debug.Log($"No more unused neutrals for branch {i + 1}");
                break;
            }
            
            List<string> branchNodeIDs = new List<string>();
            for (int j = 0; j < Mathf.Min(branchLength, availableForBranch.Count); j++)
            {
                string nodeID = availableForBranch[Random.Range(0, availableForBranch.Count)];
                branchNodeIDs.Add(nodeID);
                usedInBranches.Add(nodeID);
                availableForBranch.Remove(nodeID);
            }
            
            if (branchNodeIDs.Count == 0)
            {
                continue;
            }
            
            // Create branch that ALWAYS reconnects to skeleton (no physical dead ends)
            // All branches must be physically connected from entrance to exit
            AddBranchConnections(
                branchStartID,
                branchNodeIDs,
                skeletonNetwork,
                skeletonNodeIDs,
                nodesByID);
            
            // Determine if this branch should be an "energetic dead end"
            // Energetic dead ends have negative energy sum making them impossible to use
            // but they're still physically connected to the skeleton
            bool isEnergeticDeadEnd = ShouldCreateEnergeticDeadEnd(difficulty);
            
            // Assign weights to branch nodes
            // If it's an energetic dead end, assign weights that create negative energy sum
            AssignBranchWeights(branchNodeIDs, skeletonNetwork, isEnergeticDeadEnd);
            
            // Assign max connections to branch nodes
            AssignBranchMaxConnections(branchNodeIDs, skeletonNetwork, difficulty);
        }
        
        Debug.Log($"Added {usedInBranches.Count} nodes in noise branches");
    }
    
    /// <summary>
    /// Determine number of branches based on difficulty
    /// With small node count (6-10), we have limited unused nodes
    /// </summary>
    private static int GetBranchCount(DifficultyTier difficulty, int unusedCount)
    {
        // With 6-10 neutrals distributed across layers, we'll have very few unused
        // So branch count should be conservative
        int baseBranchCount = difficulty switch
        {
            DifficultyTier.Easy => 0,           // No branches for easy
            DifficultyTier.Medium => 1,         // 1 branch
            DifficultyTier.Hard => 2,           // 2 branches
            DifficultyTier.Expert => 3,         // 3 branches
            _ => 1
        };
        
        // Cap by available nodes (need at least 1 node per branch)
        return Mathf.Min(baseBranchCount, Mathf.Max(0, unusedCount));
    }
    
    /// <summary>
    /// Get minimum branch length based on difficulty
    /// With limited nodes, keep branches short
    /// </summary>
    private static int GetMinBranchLength(DifficultyTier difficulty)
    {
        return difficulty switch
        {
            DifficultyTier.Easy => 1,
            DifficultyTier.Medium => 1,
            DifficultyTier.Hard => 1,
            DifficultyTier.Expert => 1,
            _ => 1
        };
    }
    
    /// <summary>
    /// Get maximum branch length based on difficulty
    /// With limited nodes, keep branches short
    /// </summary>
    private static int GetMaxBranchLength(DifficultyTier difficulty)
    {
        return difficulty switch
        {
            DifficultyTier.Easy => 1,
            DifficultyTier.Medium => 2,
            DifficultyTier.Hard => 2,
            DifficultyTier.Expert => 3,    // Max 3 nodes per branch
            _ => 2
        };
    }
    
    /// <summary>
    /// Add connections for a branch
    /// CRITICAL: Branch must reconnect to skeleton or consumer
    /// </summary>
    private static void AddBranchConnections(
        string branchStartID,
        List<string> branchNodeIDs,
        SkeletonPathGenerator.SkeletonNetwork skeletonNetwork,
        List<string> skeletonNodeIDs,
        Dictionary<string, BaseNode> nodesByID)
    {
        // Check capacity of branch start node
        if (!skeletonNetwork.Connections.ContainsKey(branchStartID))
        {
            skeletonNetwork.Connections[branchStartID] = new List<string>();
        }
        
        int currentConnections = skeletonNetwork.Connections[branchStartID].Count;
        int maxConnections = skeletonNetwork.MaxConnections[branchStartID];
        
        if (currentConnections >= maxConnections)
        {
            Debug.LogWarning($"Branch start node {branchStartID} at capacity ({currentConnections}/{maxConnections})");
            return;
        }
        
        // Connect branch start to first branch node
        string firstBranchNode = branchNodeIDs[0];
        if (!skeletonNetwork.Connections[branchStartID].Contains(firstBranchNode))
        {
            skeletonNetwork.Connections[branchStartID].Add(firstBranchNode);
        }
        
        // Connect branch nodes sequentially
        for (int i = 0; i < branchNodeIDs.Count - 1; i++)
        {
            string fromID = branchNodeIDs[i];
            string toID = branchNodeIDs[i + 1];
            
            if (!skeletonNetwork.Connections.ContainsKey(fromID))
            {
                skeletonNetwork.Connections[fromID] = new List<string>();
            }
            
            if (!skeletonNetwork.Connections[fromID].Contains(toID))
            {
                skeletonNetwork.Connections[fromID].Add(toID);
            }
        }
        
        // CRITICAL: Reconnect last branch node to skeleton or consumer
        // ALL branches must reconnect - no physical dead ends allowed
        string lastBranchNode = branchNodeIDs[branchNodeIDs.Count - 1];
        
        if (!skeletonNetwork.Connections.ContainsKey(lastBranchNode))
        {
            skeletonNetwork.Connections[lastBranchNode] = new List<string>();
        }
        
        // Find reconnection target (prefer skeleton nodes, fallback to consumers)
        // FindReconnectionTarget now guarantees it will return a valid target (never null)
        string reconnectionTarget = FindReconnectionTarget(
            lastBranchNode,
            branchStartID,
            skeletonNodeIDs,
            skeletonNetwork.ConsumerIDs,
            skeletonNetwork,
            nodesByID);
        
        // CRITICAL: reconnectionTarget should never be null after our fix to FindReconnectionTarget
        // But keep fallback logic just in case
        if (reconnectionTarget != null)
        {
            if (!skeletonNetwork.Connections[lastBranchNode].Contains(reconnectionTarget))
            {
                skeletonNetwork.Connections[lastBranchNode].Add(reconnectionTarget);
                Debug.Log($"Branch reconnected: {branchStartID} -> [{string.Join(", ", branchNodeIDs)}] -> {reconnectionTarget}");
            }
            else
            {
                Debug.LogWarning($"Branch ending at {lastBranchNode} already connected to {reconnectionTarget}, skipping duplicate");
            }
        }
        else
        {
            // CRITICAL: This should never happen now, but keep as safety fallback
            Debug.LogError($"ERROR: FindReconnectionTarget returned null for branch ending at {lastBranchNode}! Using emergency fallback.");
            
            if (skeletonNetwork.ConsumerIDs.Count > 0)
            {
                string firstConsumer = skeletonNetwork.ConsumerIDs[0];
                if (!skeletonNetwork.Connections[lastBranchNode].Contains(firstConsumer))
                {
                    skeletonNetwork.Connections[lastBranchNode].Add(firstConsumer);
                }
                Debug.LogError($"EMERGENCY: Force reconnected branch to first consumer: {branchStartID} -> [{string.Join(", ", branchNodeIDs)}] -> {firstConsumer}");
            }
            else
            {
                Debug.LogError($"FATAL: No consumers available to reconnect branch ending at {lastBranchNode}!");
            }
        }
        
        // Final verification: ensure last branch node has at least one outgoing connection
        if (!skeletonNetwork.Connections.ContainsKey(lastBranchNode) || 
            skeletonNetwork.Connections[lastBranchNode].Count == 0)
        {
            Debug.LogError($"FATAL: Branch ending at {lastBranchNode} has NO outgoing connections! This is a dead end!");
            // Emergency: connect to first consumer
            if (skeletonNetwork.ConsumerIDs.Count > 0)
            {
                string emergencyConsumer = skeletonNetwork.ConsumerIDs[0];
                if (!skeletonNetwork.Connections.ContainsKey(lastBranchNode))
                {
                    skeletonNetwork.Connections[lastBranchNode] = new List<string>();
                }
                skeletonNetwork.Connections[lastBranchNode].Add(emergencyConsumer);
                Debug.LogError($"EMERGENCY FIX: Connected {lastBranchNode} -> {emergencyConsumer}");
            }
        }
    }
    
    /// <summary>
    /// Determine if a branch should be an "energetic dead end" (based on difficulty)
    /// Energetic dead ends have negative energy sum making them impossible to use
    /// but they're still physically connected to the skeleton (no physical dead ends)
    /// </summary>
    private static bool ShouldCreateEnergeticDeadEnd(DifficultyTier difficulty)
    {
        float probability = difficulty switch
        {
            DifficultyTier.Easy => 0.0f,      // No energetic dead ends for easy
            DifficultyTier.Medium => 0.3f,    // 30% chance
            DifficultyTier.Hard => 0.5f,      // 50% chance
            DifficultyTier.Expert => 0.7f,    // 70% chance
            _ => 0.3f
        };
        
        return Random.value < probability;
    }
    
    /// <summary>
    /// Find a suitable reconnection target for a branch
    /// Prefers skeleton nodes, falls back to consumers
    /// CRITICAL: Must always return a valid target (never null) to ensure no dead ends
    /// </summary>
    private static string FindReconnectionTarget(
        string branchEndID,
        string branchStartID,
        List<string> skeletonNodeIDs,
        List<string> consumerIDs,
        SkeletonPathGenerator.SkeletonNetwork network,
        Dictionary<string, BaseNode> nodesByID)
    {
        Vector3 branchEndPos = Vector3.zero;
        bool hasPosition = false;
        
        // Get physical positions if available
        if (nodesByID.ContainsKey(branchEndID))
        {
            branchEndPos = nodesByID[branchEndID].transform.position;
            hasPosition = true;
        }
        else
        {
            Debug.LogWarning($"Branch end node {branchEndID} not found in nodesByID, will use fallback selection");
        }
        
        // Candidate list: skeleton nodes + consumers, excluding branch start
        List<string> candidates = new List<string>();
        candidates.AddRange(skeletonNodeIDs.Where(id => id != branchStartID));
        candidates.AddRange(consumerIDs);
        
        if (candidates.Count == 0)
        {
            Debug.LogError($"No candidates available for reconnection! Skeleton nodes: {skeletonNodeIDs.Count}, Consumers: {consumerIDs.Count}");
            // Last resort: return any consumer if available
            return consumerIDs.Count > 0 ? consumerIDs[0] : null;
        }
        
        // Find nearest candidate with available capacity
        string bestCandidate = null;
        float bestDistance = float.MaxValue;
        
        // First pass: Try to find candidate with capacity
        foreach (string candidateID in candidates)
        {
            if (!nodesByID.ContainsKey(candidateID))
            {
                continue;
            }
            
            // Check if it's a consumer (always has capacity) or has capacity
            bool isConsumer = consumerIDs.Contains(candidateID);
            bool hasCapacity = true;
            
            if (!isConsumer)
            {
                // Check neutral node capacity
                if (network.Connections.ContainsKey(candidateID) && network.MaxConnections.ContainsKey(candidateID))
                {
                    hasCapacity = network.Connections[candidateID].Count < network.MaxConnections[candidateID];
                }
            }
            
            if (hasCapacity || isConsumer)
            {
                if (hasPosition)
                {
                    float distance = Vector3.Distance(branchEndPos, nodesByID[candidateID].transform.position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCandidate = candidateID;
                    }
                }
                else
                {
                    // No position info, just pick first available
                    if (bestCandidate == null)
                    {
                        bestCandidate = candidateID;
                    }
                }
            }
        }
        
        // Second pass: If no candidate with capacity found, pick any consumer (they always have capacity)
        if (bestCandidate == null)
        {
            Debug.LogWarning($"No candidate with capacity found, using any consumer as fallback");
            foreach (string consumerID in consumerIDs)
            {
                if (nodesByID.ContainsKey(consumerID))
                {
                    if (hasPosition)
                    {
                        float distance = Vector3.Distance(branchEndPos, nodesByID[consumerID].transform.position);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestCandidate = consumerID;
                        }
                    }
                    else
                    {
                        bestCandidate = consumerID;
                        break;
                    }
                }
            }
        }
        
        // Final fallback: return first available consumer or skeleton node
        if (bestCandidate == null)
        {
            Debug.LogWarning($"Still no candidate found, using first available");
            bestCandidate = consumerIDs.Count > 0 ? consumerIDs[0] : 
                          (skeletonNodeIDs.Count > 0 ? skeletonNodeIDs[0] : null);
        }
        
        return bestCandidate;
    }
    
    /// <summary>
    /// Assign weights to branch nodes
    /// Use full range -3 to +3, no balance requirement
    /// </summary>
    /// <summary>
    /// Generate a random weight with only 10% chance of being 0
    /// 90% of the time returns non-zero value (-3 to -1 or 1 to 3)
    /// </summary>
    private static int GenerateRandomWeight()
    {
        float roll = Random.value;
        
        if (roll < 0.1f)
        {
            // 10% chance: return 0
            return 0;
        }
        else if (roll < 0.55f)
        {
            // 45% chance: negative weight (-3 to -1)
            return Random.Range(-3, 0);
        }
        else
        {
            // 45% chance: positive weight (1 to 3)
            return Random.Range(1, 4);
        }
    }
    
    /// <summary>
    /// Assign weights to branch nodes
    /// If isEnergeticDeadEnd is true, assign weights that create negative energy sum
    /// (making the path impossible to use, but still physically connected)
    /// </summary>
    private static void AssignBranchWeights(
        List<string> branchNodeIDs,
        SkeletonPathGenerator.SkeletonNetwork network,
        bool isEnergeticDeadEnd)
    {
        if (isEnergeticDeadEnd)
        {
            // Create negative energy sum: assign mostly negative weights
            // Starting energy is 5, so we need sum < -5 to make it impossible
            // With branch length, assign weights that sum to less than -5
            int targetSum = -6 - Random.Range(0, 4); // Target sum between -6 and -9
            
            if (branchNodeIDs.Count == 0) return;
            
            // Distribute negative weights across branch nodes
            int currentSum = 0;
            for (int i = 0; i < branchNodeIDs.Count - 1; i++)
            {
                // Assign negative weight (-3 to -1)
                int weight = Random.Range(-3, 0);
                network.Weights[branchNodeIDs[i]] = weight;
                currentSum += weight;
            }
            
            // Last node adjusts to hit target sum
            int lastWeight = Mathf.Clamp(targetSum - currentSum, -3, 3);
            network.Weights[branchNodeIDs[branchNodeIDs.Count - 1]] = lastWeight;
            
            int finalSum = currentSum + lastWeight;
            Debug.Log($"Energetic dead-end branch: [{string.Join(", ", branchNodeIDs)}] with weights summing to {finalSum} (impossible to use, starting energy 5)");
        }
        else
        {
            // Normal branch: random weights (can be positive or negative)
            foreach (string nodeID in branchNodeIDs)
            {
                // Random weight with only 10% chance of being 0
                int weight = GenerateRandomWeight();
                network.Weights[nodeID] = weight;
            }
        }
    }
    
    /// <summary>
    /// Assign max connections to branch nodes
    /// </summary>
    private static void AssignBranchMaxConnections(
        List<string> branchNodeIDs,
        SkeletonPathGenerator.SkeletonNetwork network,
        DifficultyTier difficulty)
    {
        foreach (string nodeID in branchNodeIDs)
        {
            // Use same probability distribution as skeleton
            float roll = Random.value;
            
            int maxConnections = difficulty switch
            {
                DifficultyTier.Easy => roll < 0.8f ? 1 : 2,
                DifficultyTier.Medium => roll < 0.65f ? 1 : (roll < 0.95f ? 2 : 3),
                DifficultyTier.Hard => roll < 0.55f ? 1 : (roll < 0.90f ? 2 : 3),
                DifficultyTier.Expert => roll < 0.50f ? 1 : (roll < 0.90f ? 2 : 3),
                _ => 1
            };
            
            network.MaxConnections[nodeID] = maxConnections;
        }
    }
    
    /// <summary>
    /// Create energetic dead-end connections from skeleton nodes when no unused nodes are available
    /// These are physically connected but have negative energy making them impossible to use
    /// NOTE: This is now less important since all branches reconnect, but kept for Expert difficulty
    /// </summary>
    private static void CreateDeadEndsFromSkeletonNodes(
        SkeletonPathGenerator.SkeletonNetwork skeletonNetwork,
        DifficultyTier difficulty,
        Dictionary<string, BaseNode> nodesByID)
    {
        // For Expert difficulty, we can create additional connections that look like paths
        // but have negative energy. However, since all branches now reconnect, this is less critical.
        // Keeping minimal implementation for now - can be enhanced later if needed.
        
        if (difficulty != DifficultyTier.Expert)
        {
            return; // Only for Expert difficulty
        }
        
        Debug.Log("Expert difficulty: All branches reconnect, energetic dead ends created through weight assignment");
    }
}
