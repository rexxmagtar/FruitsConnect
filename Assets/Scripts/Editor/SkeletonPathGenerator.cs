using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates a unified skeleton network connecting all producers to all consumers
/// Uses pure logical graph generation (node IDs only, no positions)
/// </summary>
public static class SkeletonPathGenerator
{
    /// <summary>
    /// Skeleton network structure - PURE LOGIC, no positions
    /// </summary>
    public class SkeletonNetwork
    {
        public List<string> ProducerIDs;
        public List<List<string>> NeutralLayerIDs; // Node IDs organized in layers
        public List<string> ConsumerIDs;
        public Dictionary<string, List<string>> Connections; // Node ID → Connected Node IDs
        public Dictionary<string, int> Weights; // Node ID → Weight (-3 to +3)
        public Dictionary<string, int> MaxConnections; // Node ID → Max outgoing connections
    }
    
    /// <summary>
    /// Build unified skeleton network connecting all producers to all consumers
    /// </summary>
    public static SkeletonNetwork BuildUnifiedSkeletonNetwork(
        List<string> producerIDs,
        List<string> consumerIDs,
        List<string> neutralIDs,
        DifficultyTier difficulty)
    {
        if (producerIDs.Count == 0)
        {
            Debug.LogError("SkeletonPathGenerator: No producers provided");
            return null;
        }
        
        if (consumerIDs.Count == 0)
        {
            Debug.LogError("SkeletonPathGenerator: No consumers provided");
            return null;
        }
        
        if (neutralIDs.Count == 0)
        {
            Debug.LogError("SkeletonPathGenerator: No neutral nodes provided");
            return null;
        }
        
        // Step 1: Determine network structure based on difficulty
        int layerCount = GetLayerCount(difficulty);
        
        // Step 2: Divide neutral IDs into layers
        List<List<string>> layers = DivideIDsIntoLayers(neutralIDs, layerCount);
        
        SkeletonNetwork network = new SkeletonNetwork
        {
            ProducerIDs = producerIDs,
            NeutralLayerIDs = layers,
            ConsumerIDs = consumerIDs,
            Connections = new Dictionary<string, List<string>>(),
            Weights = new Dictionary<string, int>(),
            MaxConnections = new Dictionary<string, int>()
        };
        
        // Step 3: Assign max connections based on difficulty probabilities
        AssignMaxConnections(network, difficulty);
        
        // Step 4: Build connections logically
        // Producers → Layer 0 → Layer 1 → ... → Layer N → Consumers
        ConnectProducersToFirstLayer(network);
        
        for (int i = 0; i < layers.Count - 1; i++)
        {
            ConnectLayers(network, i, i + 1, difficulty);
        }
        
        ConnectLastLayerToConsumers(network);
        
        // Step 5: Adjust max connections to match actual connections made
        // This ensures nodes have enough capacity for the connections we created
        AdjustMaxConnectionsToActual(network);
        
        // Step 6: Add cycles within skeleton for complexity (before weight assignment)
        AddSkeletonCycles(network, difficulty);
        
        // Step 6: Assign weights to achieve balance
        AssignNetworkWeights(network, difficulty);
        
        Debug.Log($"Built skeleton network: {producerIDs.Count} producers, {consumerIDs.Count} consumers, {neutralIDs.Count} neutrals across {layerCount} layers");
        
        return network;
    }
    
    /// <summary>
    /// Determine number of layers based on difficulty
    /// With 6-10 neutrals, we need fewer layers to ensure each layer has nodes
    /// </summary>
    private static int GetLayerCount(DifficultyTier difficulty)
    {
        return difficulty switch
        {
            DifficultyTier.Easy => 2,           // 2 layers (simple path)
            DifficultyTier.Medium => 3,         // 3 layers
            DifficultyTier.Hard => 4,           // 4 layers
            DifficultyTier.Expert => 5,         // 5 layers (with 6-10 nodes, ~1-2 per layer)
            _ => 3
        };
    }
    
    /// <summary>
    /// Divide neutral IDs into layers
    /// Simple round-robin distribution
    /// </summary>
    private static List<List<string>> DivideIDsIntoLayers(
        List<string> neutralIDs,
        int layerCount)
    {
        List<List<string>> layers = new List<List<string>>();
        
        for (int i = 0; i < layerCount; i++)
        {
            layers.Add(new List<string>());
        }
        
        // Round-robin distribution
        for (int i = 0; i < neutralIDs.Count; i++)
        {
            int layerIndex = i % layerCount;
            layers[layerIndex].Add(neutralIDs[i]);
        }
        
        return layers;
    }
    
    /// <summary>
    /// Assign max outgoing connections based on difficulty probabilities
    /// </summary>
    private static void AssignMaxConnections(SkeletonNetwork network, DifficultyTier difficulty)
    {
        // Track distribution for logging
        Dictionary<int, int> distribution = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 } };
        
        // Assign max connections to all neutral nodes based on difficulty
        foreach (var layerIDs in network.NeutralLayerIDs)
        {
            foreach (var neutralID in layerIDs)
            {
                int maxConnections = AssignMaxOutgoingConnections(difficulty);
                network.MaxConnections[neutralID] = maxConnections;
                distribution[maxConnections]++;
            }
        }
        
        // Producers get higher capacity (fixed at 3)
        foreach (var producerID in network.ProducerIDs)
        {
            network.MaxConnections[producerID] = 3;
        }
        
        Debug.Log($"Max connections distribution for {difficulty}: 1={distribution[1]}, 2={distribution[2]}, 3={distribution[3]}");
    }
    
    /// <summary>
    /// Assign max outgoing connections based on difficulty probability distribution
    /// </summary>
    private static int AssignMaxOutgoingConnections(DifficultyTier difficulty)
    {
        float roll = Random.value;
        
        return difficulty switch
        {
            DifficultyTier.Easy => roll < 0.8f ? 1 : 2,
            // Easy: 80% = 1 connection, 20% = 2 connections
            
            DifficultyTier.Medium => roll < 0.65f ? 1 : (roll < 0.95f ? 2 : 3),
            // Medium: 65% = 1 connection, 30% = 2 connections, 5% = 3 connections
            
            DifficultyTier.Hard => roll < 0.55f ? 1 : (roll < 0.90f ? 2 : 3),
            // Hard: 55% = 1 connection, 35% = 2 connections, 10% = 3 connections
            
            DifficultyTier.Expert => roll < 0.60f ? 1 : (roll < 0.85f ? 2 : 3),
            // Expert: 60% = 1 connection, 25% = 2 connections, 15% = 3 connections
            
            _ => 1
        };
    }
    
    /// <summary>
    /// Connect producers to first neutral layer
    /// Ensures better distribution to avoid multiple producers connecting to same node
    /// </summary>
    private static void ConnectProducersToFirstLayer(SkeletonNetwork network)
    {
        var firstLayer = network.NeutralLayerIDs[0];
        int producerCount = network.ProducerIDs.Count;
        
        // Calculate how many nodes each producer should connect to
        int connectionsPerProducer = Mathf.Min(2, Mathf.Max(1, firstLayer.Count / producerCount));
        
        // Track which neutrals are already assigned to avoid early overlap
        HashSet<string> assignedNeutrals = new HashSet<string>();
        
        for (int p = 0; p < producerCount; p++)
        {
            string producerID = network.ProducerIDs[p];
            network.Connections[producerID] = new List<string>();
            
            // Calculate this producer's range in the first layer
            // Distribute nodes evenly: each producer gets a segment of the layer
            int segmentSize = Mathf.Max(connectionsPerProducer, firstLayer.Count / producerCount);
            int segmentStart = (p * segmentSize) % firstLayer.Count;
            
            // Try to assign unique nodes first
            int assigned = 0;
            for (int offset = 0; offset < firstLayer.Count && assigned < connectionsPerProducer; offset++)
            {
                int idx = (segmentStart + offset) % firstLayer.Count;
                string neutralID = firstLayer[idx];
                
                if (!assignedNeutrals.Contains(neutralID))
                {
                    network.Connections[producerID].Add(neutralID);
                    assignedNeutrals.Add(neutralID);
                    assigned++;
                }
            }
            
            // If we couldn't find enough unique nodes, allow sharing
            // This can happen when there are many producers and few neutrals
            if (assigned < connectionsPerProducer)
            {
                for (int offset = 0; offset < firstLayer.Count && assigned < connectionsPerProducer; offset++)
                {
                    int idx = (segmentStart + offset) % firstLayer.Count;
                    string neutralID = firstLayer[idx];
                    
                    if (!network.Connections[producerID].Contains(neutralID))
                    {
                        network.Connections[producerID].Add(neutralID);
                        assigned++;
                    }
                }
            }
            
            Debug.Log($"Producer {p} ({producerID}): Connected to {network.Connections[producerID].Count} neutrals: {string.Join(", ", network.Connections[producerID])}");
        }
    }
    
    /// <summary>
    /// Connect two neutral layers
    /// </summary>
    private static void ConnectLayers(
        SkeletonNetwork network,
        int fromLayerIdx,
        int toLayerIdx,
        DifficultyTier difficulty)
    {
        var fromLayer = network.NeutralLayerIDs[fromLayerIdx];
        var toLayer = network.NeutralLayerIDs[toLayerIdx];
        
        // Each node in fromLayer connects to 1-2 nodes in toLayer
        foreach (var fromID in fromLayer)
        {
            if (!network.Connections.ContainsKey(fromID))
            {
                network.Connections[fromID] = new List<string>();
            }
            
            // Determine connections based on max capacity and difficulty
            int maxOut = network.MaxConnections[fromID];
            int connectionsToMake = difficulty switch
            {
                DifficultyTier.Easy => Mathf.Min(maxOut, 1),
                DifficultyTier.Medium => Mathf.Min(maxOut, 1),
                DifficultyTier.Hard => Mathf.Min(maxOut, 2),
                DifficultyTier.Expert => Mathf.Min(maxOut, 2), // Expert: Use full capacity
                _ => 1
            };
            
            // Connect to next nodes in sequence (with wraparound)
            int startIdx = fromLayer.IndexOf(fromID);
            for (int i = 0; i < connectionsToMake && i < toLayer.Count; i++)
            {
                int targetIdx = (startIdx + i) % toLayer.Count;
                string targetID = toLayer[targetIdx];
                
                if (!network.Connections[fromID].Contains(targetID))
                {
                    network.Connections[fromID].Add(targetID);
                }
            }
        }
    }
    
    /// <summary>
    /// Adjust max connections to match actual connections created
    /// Ensures nodes have capacity for the connections we built
    /// </summary>
    private static void AdjustMaxConnectionsToActual(SkeletonNetwork network)
    {
        int adjustedCount = 0;
        
        foreach (var kvp in network.Connections)
        {
            string nodeID = kvp.Key;
            int actualConnections = kvp.Value.Count;
            
            // If actual connections exceed max, increase max to match
            if (network.MaxConnections.ContainsKey(nodeID))
            {
                if (actualConnections > network.MaxConnections[nodeID])
                {
                    int oldMax = network.MaxConnections[nodeID];
                    network.MaxConnections[nodeID] = actualConnections;
                    Debug.Log($"Adjusted max connections for {nodeID}: {oldMax} -> {actualConnections}");
                    adjustedCount++;
                }
            }
            else
            {
                // Node doesn't have max set (shouldn't happen, but handle it)
                network.MaxConnections[nodeID] = actualConnections;
            }
        }
        
        // Log final distribution
        Dictionary<int, int> finalDistribution = new Dictionary<int, int>();
        foreach (var kvp in network.MaxConnections)
        {
            int maxConn = kvp.Value;
            if (!finalDistribution.ContainsKey(maxConn))
                finalDistribution[maxConn] = 0;
            finalDistribution[maxConn]++;
        }
        
        string distStr = string.Join(", ", finalDistribution.Select(kv => $"{kv.Key}={kv.Value}"));
        Debug.Log($"Adjusted {adjustedCount} nodes. Final max connections distribution: {distStr}");
    }
    
    /// <summary>
    /// Connect last neutral layer to consumers
    /// GUARANTEES that every consumer gets at least one connection
    /// </summary>
    private static void ConnectLastLayerToConsumers(SkeletonNetwork network)
    {
        var lastLayer = network.NeutralLayerIDs[network.NeutralLayerIDs.Count - 1];
        
        // Track which consumers have been connected
        HashSet<string> connectedConsumers = new HashSet<string>();
        
        // First pass: Try to connect each consumer to dedicated nodes
        for (int i = 0; i < network.ConsumerIDs.Count; i++)
        {
            string consumerID = network.ConsumerIDs[i];
            
            // Try to find a node in last layer with capacity
            for (int j = 0; j < lastLayer.Count; j++)
            {
                int nodeIdx = (i + j) % lastLayer.Count;
                string neutralID = lastLayer[nodeIdx];
                
                if (!network.Connections.ContainsKey(neutralID))
                {
                    network.Connections[neutralID] = new List<string>();
                }
                
                // Check if this node has capacity
                if (network.Connections[neutralID].Count < network.MaxConnections[neutralID])
                {
                    if (!network.Connections[neutralID].Contains(consumerID))
                    {
                        network.Connections[neutralID].Add(consumerID);
                        connectedConsumers.Add(consumerID);
                        Debug.Log($"Connected consumer {i} ({consumerID}) to last layer node {nodeIdx} ({neutralID})");
                        break; // This consumer is connected, move to next
                    }
                }
            }
        }
        
        // Second pass: Ensure ALL consumers are connected (force connection if needed)
        foreach (string consumerID in network.ConsumerIDs)
        {
            if (!connectedConsumers.Contains(consumerID))
            {
                // This consumer has no connection! Force add to any node in last layer
                Debug.LogWarning($"Consumer {consumerID} had no connection! Force connecting...");
                
                // Find the node with the least connections
                string bestNodeID = lastLayer[0];
                int minConnections = network.Connections.ContainsKey(bestNodeID) ? 
                    network.Connections[bestNodeID].Count : 0;
                
                foreach (string neutralID in lastLayer)
                {
                    int connCount = network.Connections.ContainsKey(neutralID) ? 
                        network.Connections[neutralID].Count : 0;
                    
                    if (connCount < minConnections)
                    {
                        minConnections = connCount;
                        bestNodeID = neutralID;
                    }
                }
                
                // Force connection
                if (!network.Connections.ContainsKey(bestNodeID))
                {
                    network.Connections[bestNodeID] = new List<string>();
                }
                
                if (!network.Connections[bestNodeID].Contains(consumerID))
                {
                    network.Connections[bestNodeID].Add(consumerID);
                    Debug.Log($"Force connected consumer {consumerID} to node {bestNodeID}");
                }
            }
        }
    }
    
    /// <summary>
    /// Add cycles within skeleton to create alternative paths and complexity
    /// </summary>
    private static void AddSkeletonCycles(SkeletonNetwork network, DifficultyTier difficulty)
    {
        int cycleCount = difficulty switch
        {
            DifficultyTier.Easy => 0,           // No cycles
            DifficultyTier.Medium => 1,         // 1 cycle
            DifficultyTier.Hard => 2,           // 2 cycles
            DifficultyTier.Expert => 3,         // 3 cycles
            _ => 0
        };
        
        if (cycleCount == 0) return;
        
        // Create cycles by connecting nodes across layers (not just sequential)
        List<string> allSkeletonNodes = network.NeutralLayerIDs
            .SelectMany(layer => layer)
            .ToList();
        
        int cyclesAdded = 0;
        int maxAttempts = cycleCount * 10;
        
        for (int attempt = 0; attempt < maxAttempts && cyclesAdded < cycleCount; attempt++)
        {
            // Pick two random nodes
            string node1ID = allSkeletonNodes[Random.Range(0, allSkeletonNodes.Count)];
            string node2ID = allSkeletonNodes[Random.Range(0, allSkeletonNodes.Count)];
            
            if (node1ID == node2ID) continue;
            
            // Check both have capacity
            if (!network.Connections.ContainsKey(node1ID))
            {
                network.Connections[node1ID] = new List<string>();
            }
            
            if (!network.Connections.ContainsKey(node2ID))
            {
                network.Connections[node2ID] = new List<string>();
            }
            
            bool node1HasCapacity = network.Connections[node1ID].Count < network.MaxConnections[node1ID];
            bool node2HasCapacity = network.Connections[node2ID].Count < network.MaxConnections[node2ID];
            
            // Check if not already connected (either direction)
            bool alreadyConnected = network.Connections[node1ID].Contains(node2ID) || 
                                   network.Connections[node2ID].Contains(node1ID);
            
            if (node1HasCapacity && !alreadyConnected)
            {
                network.Connections[node1ID].Add(node2ID);
                cyclesAdded++;
                Debug.Log($"Added cycle: {node1ID} -> {node2ID}");
            }
        }
        
        Debug.Log($"Added {cyclesAdded} cycles to skeleton network");
    }
    
    /// <summary>
    /// Assign weights to neutral nodes to ensure ANY path is solvable
    /// Key: Each layer's weights must be carefully balanced for small node counts
    /// </summary>
    private static void AssignNetworkWeights(SkeletonNetwork network, DifficultyTier difficulty)
    {
        int layerCount = network.NeutralLayerIDs.Count;
        
        // Target: Any path should consume ~5 energy (start with 5, end with ~0)
        // A path picks ONE node from each layer, so we need each layer to contribute ~-1 on average
        float targetWeightPerLayer = -5.0f / layerCount;
        
        Debug.Log($"Assigning weights: {layerCount} layers, target per layer: {targetWeightPerLayer:F2}");
        
        // Assign weights layer by layer with strict control
        for (int layerIdx = 0; layerIdx < layerCount; layerIdx++)
        {
            var layer = network.NeutralLayerIDs[layerIdx];
            
            // For small layers (1-2 nodes), we need precise control
            AssignLayerWeightsForSmallCount(layer, targetWeightPerLayer, network);
        }
        
        // Verify paths are solvable
        VerifyPathSolvability(network);
    }
    
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
    /// Assign weights to nodes in a single layer with small node count (1-3 nodes)
    /// Ensures that the layer contributes correctly to overall energy consumption
    /// Zero weights only appear 10% of the time
    /// </summary>
    private static void AssignLayerWeightsForSmallCount(List<string> layerIDs, float targetAverage, SkeletonNetwork network)
    {
        int nodeCount = layerIDs.Count;
        
        if (nodeCount == 0) return;
        
        List<int> weights = new List<int>();
        
        if (nodeCount == 1)
        {
            // Single node: must be close to target average
            // Target is around -1, so assign -1 or -2
            // Avoid 0 for single nodes as it would make layer useless
            int weight = targetAverage <= -1.5f ? Random.Range(-3, -1) : Random.Range(-2, 1);
            if (weight == 0) weight = -1; // Ensure single node is never 0
            weights.Add(weight);
        }
        else if (nodeCount == 2)
        {
            // Two nodes: create complementary weights that average to target
            // Target ~-1, so we want sum of ~-2
            int targetSum = Mathf.RoundToInt(targetAverage * 2);
            
            // Pick first weight randomly (with 10% zero chance)
            int weight1 = GenerateRandomWeight();
            
            // Calculate complementary weight to hit target sum
            int weight2 = Mathf.Clamp(targetSum - weight1, -3, 3);
            
            weights.Add(weight1);
            weights.Add(weight2);
            
            // Shuffle
            if (Random.value > 0.5f)
            {
                int temp = weights[0];
                weights[0] = weights[1];
                weights[1] = temp;
            }
        }
        else // 3+ nodes
        {
            // Multiple nodes: create a mix that averages to target
            int targetSum = Mathf.RoundToInt(targetAverage * nodeCount);
            
            // First assign random weights to n-1 nodes (with 10% zero chance)
            int currentSum = 0;
            for (int i = 0; i < nodeCount - 1; i++)
            {
                int weight = GenerateRandomWeight();
                weights.Add(weight);
                currentSum += weight;
            }
            
            // Last node adjusts to hit target sum
            int lastWeight = Mathf.Clamp(targetSum - currentSum, -3, 3);
            weights.Add(lastWeight);
            
            // Shuffle for randomness
            for (int i = weights.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = weights[i];
                weights[i] = weights[j];
                weights[j] = temp;
            }
        }
        
        // Assign to nodes
        for (int i = 0; i < layerIDs.Count; i++)
        {
            network.Weights[layerIDs[i]] = weights[i];
        }
        
        float actualAvg = weights.Count > 0 ? (float)weights.Average() : 0f;
        int zeroCount = weights.Count(w => w == 0);
        Debug.Log($"Layer weights: {layerIDs.Count} nodes, target avg: {targetAverage:F2}, actual avg: {actualAvg:F2}, sum: {weights.Sum()}, zeros: {zeroCount}");
    }
    
    /// <summary>
    /// Verify that ALL possible paths through the network are solvable
    /// With small node counts, we can test all combinations
    /// </summary>
    private static void VerifyPathSolvability(SkeletonNetwork network)
    {
        // Calculate all possible path combinations
        int totalPaths = 1;
        foreach (var layer in network.NeutralLayerIDs)
        {
            totalPaths *= layer.Count;
        }
        
        Debug.Log($"Verifying all {totalPaths} possible skeleton paths...");
        
        int solvableCount = 0;
        int worstEnergy = 5;
        int bestEnergy = 5;
        List<int> allPathEnergies = new List<int>();
        
        // Generate all possible paths recursively
        VerifyPathsRecursive(network, 0, 5, ref solvableCount, ref worstEnergy, ref bestEnergy, allPathEnergies);
        
        float avgEnergy = allPathEnergies.Count > 0 ? (float)allPathEnergies.Average() : 0f;
        
        Debug.Log($"Path verification: {solvableCount}/{totalPaths} paths solvable");
        Debug.Log($"Energy range: Worst={worstEnergy}, Best={bestEnergy}, Average={avgEnergy:F2}");
        
        if (solvableCount < totalPaths)
        {
            Debug.LogWarning($"WARNING: {totalPaths - solvableCount} paths are UNSOLVABLE!");
        }
        else if (worstEnergy < 1)
        {
            Debug.LogWarning($"Some paths end with very low energy ({worstEnergy}). Consider adjusting.");
        }
    }
    
    /// <summary>
    /// Recursively test all path combinations
    /// </summary>
    private static void VerifyPathsRecursive(
        SkeletonNetwork network,
        int layerIdx,
        int currentEnergy,
        ref int solvableCount,
        ref int worstEnergy,
        ref int bestEnergy,
        List<int> allPathEnergies)
    {
        // Base case: reached end of layers
        if (layerIdx >= network.NeutralLayerIDs.Count)
        {
            // Path complete
            allPathEnergies.Add(currentEnergy);
            
            if (currentEnergy >= 0)
            {
                solvableCount++;
            }
            
            worstEnergy = Mathf.Min(worstEnergy, currentEnergy);
            bestEnergy = Mathf.Max(bestEnergy, currentEnergy);
            return;
        }
        
        // Try each node in current layer
        var layer = network.NeutralLayerIDs[layerIdx];
        foreach (string nodeID in layer)
        {
            int nodeWeight = network.Weights[nodeID];
            int newEnergy = currentEnergy + nodeWeight;
            
            // Recurse to next layer
            VerifyPathsRecursive(network, layerIdx + 1, newEnergy, ref solvableCount, ref worstEnergy, ref bestEnergy, allPathEnergies);
        }
    }
}
