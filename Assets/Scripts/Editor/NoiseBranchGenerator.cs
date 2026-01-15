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
    /// All branches reconnect to skeleton or consumers (no dead ends)
    /// </summary>
    public static void AddNoiseBranches(
        SkeletonPathGenerator.SkeletonNetwork skeletonNetwork,
        List<string> unusedNeutralIDs,
        DifficultyTier difficulty,
        Dictionary<string, BaseNode> nodesByID)
    {
        if (unusedNeutralIDs.Count == 0)
        {
            Debug.Log("No unused neutral nodes for noise branches");
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
            
            // Create branch connections
            AddBranchConnections(
                branchStartID,
                branchNodeIDs,
                skeletonNetwork,
                skeletonNodeIDs,
                nodesByID);
            
            // Assign weights to branch nodes
            AssignBranchWeights(branchNodeIDs, skeletonNetwork);
            
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
        string lastBranchNode = branchNodeIDs[branchNodeIDs.Count - 1];
        
        if (!skeletonNetwork.Connections.ContainsKey(lastBranchNode))
        {
            skeletonNetwork.Connections[lastBranchNode] = new List<string>();
        }
        
        // Find reconnection target (prefer skeleton nodes, fallback to consumers)
        string reconnectionTarget = FindReconnectionTarget(
            lastBranchNode,
            branchStartID,
            skeletonNodeIDs,
            skeletonNetwork.ConsumerIDs,
            skeletonNetwork,
            nodesByID);
        
        if (reconnectionTarget != null && !skeletonNetwork.Connections[lastBranchNode].Contains(reconnectionTarget))
        {
            skeletonNetwork.Connections[lastBranchNode].Add(reconnectionTarget);
            Debug.Log($"Branch reconnected: {branchStartID} -> [{string.Join(", ", branchNodeIDs)}] -> {reconnectionTarget}");
        }
        else
        {
            Debug.LogWarning($"Could not find reconnection target for branch ending at {lastBranchNode}");
        }
    }
    
    /// <summary>
    /// Find a suitable reconnection target for a branch
    /// Prefers skeleton nodes, falls back to consumers
    /// </summary>
    private static string FindReconnectionTarget(
        string branchEndID,
        string branchStartID,
        List<string> skeletonNodeIDs,
        List<string> consumerIDs,
        SkeletonPathGenerator.SkeletonNetwork network,
        Dictionary<string, BaseNode> nodesByID)
    {
        // Get physical positions
        if (!nodesByID.ContainsKey(branchEndID))
        {
            Debug.LogWarning($"Branch end node {branchEndID} not found in nodesByID");
            return null;
        }
        
        Vector3 branchEndPos = nodesByID[branchEndID].transform.position;
        
        // Candidate list: skeleton nodes + consumers, excluding branch start
        List<string> candidates = new List<string>();
        candidates.AddRange(skeletonNodeIDs.Where(id => id != branchStartID));
        candidates.AddRange(consumerIDs);
        
        // Find nearest candidate with available capacity
        string bestCandidate = null;
        float bestDistance = float.MaxValue;
        
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
                float distance = Vector3.Distance(branchEndPos, nodesByID[candidateID].transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidateID;
                }
            }
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
    
    private static void AssignBranchWeights(
        List<string> branchNodeIDs,
        SkeletonPathGenerator.SkeletonNetwork network)
    {
        foreach (string nodeID in branchNodeIDs)
        {
            // Random weight with only 10% chance of being 0
            int weight = GenerateRandomWeight();
            network.Weights[nodeID] = weight;
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
}
