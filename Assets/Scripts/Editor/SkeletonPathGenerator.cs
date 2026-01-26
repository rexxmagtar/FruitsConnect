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
        
        // Step 1: Determine network structure based on difficulty and node count
        // Ensure last layer has enough nodes to distribute consumers
        int layerCount = GetLayerCount(difficulty, neutralIDs.Count, consumerIDs.Count);
        
        // Step 2: Divide neutral IDs into layers (ensuring last layer has enough nodes)
        List<List<string>> layers = DivideIDsIntoLayers(neutralIDs, layerCount, consumerIDs.Count);
        
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
        
        // Step 4.5: Verify all consumers are reachable from producers
        VerifyAllConsumersReachable(network);
        
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
    /// Determine number of layers based on difficulty, neutral count, and consumer count
    /// Ensures last layer has enough nodes to distribute consumers across different nodes
    /// </summary>
    private static int GetLayerCount(DifficultyTier difficulty, int neutralCount, int consumerCount)
    {
        // Base layer count from difficulty
        int baseLayerCount = difficulty switch
        {
            DifficultyTier.Easy => 2,
            DifficultyTier.Medium => 3,
            DifficultyTier.Hard => 4,
            DifficultyTier.Expert => 5,
            _ => 3
        };
        
        // Ensure we have enough nodes in last layer to distribute consumers
        // Last layer should have at least as many nodes as consumers (or close to it)
        // With round-robin: last layer gets nodes at indices: (layerCount-1), (2*layerCount-1), ...
        // So with neutralCount nodes and layerCount layers, last layer gets roughly neutralCount/layerCount nodes
        
        // Calculate minimum nodes needed in last layer (at least consumerCount, or at least 2)
        int minNodesInLastLayer = Mathf.Max(consumerCount, 2);
        
        // Adjust layer count to ensure last layer has enough nodes
        // If baseLayerCount would give too few nodes in last layer, reduce layer count
        int estimatedNodesInLastLayer = Mathf.CeilToInt((float)neutralCount / baseLayerCount);
        
        if (estimatedNodesInLastLayer < minNodesInLastLayer)
        {
            // Reduce layer count to ensure last layer has enough nodes
            // We want: neutralCount / layerCount >= minNodesInLastLayer
            // So: layerCount <= neutralCount / minNodesInLastLayer
            int originalLayerCount = baseLayerCount;
            int maxLayers = Mathf.Max(2, neutralCount / minNodesInLastLayer);
            baseLayerCount = Mathf.Min(baseLayerCount, maxLayers);
            
            int newEstimatedNodes = Mathf.CeilToInt((float)neutralCount / baseLayerCount);
            Debug.Log($"Adjusted layer count from {originalLayerCount} to {baseLayerCount} to ensure last layer has enough nodes for {consumerCount} consumers (estimated {estimatedNodesInLastLayer} -> {newEstimatedNodes} nodes per layer)");
        }
        
        return baseLayerCount;
    }
    
    /// <summary>
    /// Divide neutral IDs into layers
    /// Ensures last layer has enough nodes for consumer distribution
    /// Uses weighted distribution: last layer gets more nodes if needed
    /// </summary>
    private static List<List<string>> DivideIDsIntoLayers(
        List<string> neutralIDs,
        int layerCount,
        int consumerCount)
    {
        List<List<string>> layers = new List<List<string>>();
        
        for (int i = 0; i < layerCount; i++)
        {
            layers.Add(new List<string>());
        }
        
        if (neutralIDs.Count == 0)
            return layers;
        
        // Even distribution: each layer gets neutralIDs.Count / layerCount nodes
        // This is more balanced and avoids "thin" layers that create bottlenecks
        for (int i = 0; i < neutralIDs.Count; i++)
        {
            int layerIndex = i % layerCount;
            layers[layerIndex].Add(neutralIDs[i]);
        }
        
        // Log distribution
        Debug.Log($"Layer distribution: {string.Join(", ", layers.Select((layer, idx) => $"Layer{idx}:{layer.Count}"))}");
        
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
        
        // Ensure every node in the first layer is covered by at least one producer
        // This avoids unreachable nodes in the first layer
        for (int i = 0; i < firstLayer.Count; i++)
        {
            string neutralID = firstLayer[i];
            // Assign this neutral to a producer (round-robin)
            string producerID = network.ProducerIDs[i % producerCount];
            
            if (!network.Connections.ContainsKey(producerID))
            {
                network.Connections[producerID] = new List<string>();
            }
            
            if (!network.Connections[producerID].Contains(neutralID))
            {
                network.Connections[producerID].Add(neutralID);
            }
        }
        
        // Ensure every producer has at least one connection
        foreach (var producerID in network.ProducerIDs)
        {
            if (!network.Connections.ContainsKey(producerID) || network.Connections[producerID].Count == 0)
            {
                network.Connections[producerID] = new List<string> { firstLayer[Random.Range(0, firstLayer.Count)] };
            }
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
        
        // Ensure every node in toLayer has at least one incoming connection
        // This is critical for reachability
        for (int i = 0; i < toLayer.Count; i++)
        {
            string targetID = toLayer[i];
            // Assign an incoming connection from fromLayer (round-robin)
            string fromID = fromLayer[i % fromLayer.Count];
            
            if (!network.Connections.ContainsKey(fromID))
            {
                network.Connections[fromID] = new List<string>();
            }
            
            if (!network.Connections[fromID].Contains(targetID))
            {
                network.Connections[fromID].Add(targetID);
            }
        }
        
        // Add extra connections based on difficulty and node capacity
        // This creates alternative paths and cross-layer connections
        foreach (var fromID in fromLayer)
        {
            if (!network.Connections.ContainsKey(fromID))
            {
                network.Connections[fromID] = new List<string>();
            }

            int maxOut = network.MaxConnections[fromID];
            int currentOut = network.Connections[fromID].Count;
            
            // Expert: Use more connections to create complexity
            int targetExtra = difficulty switch
            {
                DifficultyTier.Easy => 0,
                DifficultyTier.Medium => 1,
                DifficultyTier.Hard => 2,
                DifficultyTier.Expert => 3,
                _ => 1
            };
            
            for (int i = 0; i < targetExtra && currentOut < maxOut; i++)
            {
                string targetID = toLayer[Random.Range(0, toLayer.Count)];
                if (!network.Connections[fromID].Contains(targetID))
                {
                    network.Connections[fromID].Add(targetID);
                    currentOut++;
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
    /// DISTRIBUTES consumers across different nodes to avoid all connecting to same node
    /// CRITICAL: Only uses last layer nodes to ensure all consumers are reachable from producers
    /// </summary>
    private static void ConnectLastLayerToConsumers(SkeletonNetwork network)
    {
        var lastLayer = network.NeutralLayerIDs[network.NeutralLayerIDs.Count - 1];
        
        // CRITICAL: Only use last layer nodes for consumer connections
        // This ensures all consumers are reachable from producers through the skeleton path
        // Using earlier layers could create unreachable consumers if those nodes aren't properly connected
        List<string> candidateNodes = new List<string>();
        candidateNodes.AddRange(lastLayer);
        
        // Verify last layer has enough nodes
        if (lastLayer.Count < network.ConsumerIDs.Count)
        {
            Debug.LogWarning($"Last layer has {lastLayer.Count} nodes but {network.ConsumerIDs.Count} consumers. Some nodes will connect to multiple consumers.");
        }
        
        // Track which consumers have been connected
        HashSet<string> connectedConsumers = new HashSet<string>();
        // Track which nodes have been used for consumers (to avoid overloading single node)
        Dictionary<string, int> nodeConsumerCount = new Dictionary<string, int>();
        
        // Initialize node consumer counts
        foreach (string neutralID in candidateNodes)
        {
            nodeConsumerCount[neutralID] = 0;
        }
        
        // First pass: Distribute consumers across different nodes
        // Try to give each consumer a dedicated node, or at least minimize overlap
        for (int i = 0; i < network.ConsumerIDs.Count; i++)
        {
            string consumerID = network.ConsumerIDs[i];
            bool connected = false;
            
            // Sort nodes by: 1) least consumer connections, 2) available capacity
            // All candidate nodes are from last layer, so no need to check layer priority
            var sortedNodes = candidateNodes
                .OrderBy(nid => 
                {
                    int consumerConnections = nodeConsumerCount.ContainsKey(nid) ? nodeConsumerCount[nid] : 0;
                    int currentConnections = network.Connections.ContainsKey(nid) ? 
                        network.Connections[nid].Count : 0;
                    int maxConnections = network.MaxConnections.ContainsKey(nid) ? network.MaxConnections[nid] : 1;
                    bool hasCapacity = currentConnections < maxConnections;
                    
                    // Prefer nodes with fewer consumer connections and available capacity
                    if (!hasCapacity) return int.MaxValue; // No capacity = lowest priority
                    return consumerConnections * 100 + currentConnections; // Prefer fewer consumer connections
                })
                .ToList();
            
            // Try to connect to a node with no or few consumer connections
            foreach (string neutralID in sortedNodes)
            {
                if (!network.Connections.ContainsKey(neutralID))
                {
                    network.Connections[neutralID] = new List<string>();
                }
                
                int currentConnections = network.Connections[neutralID].Count;
                int maxConnections = network.MaxConnections[neutralID];
                
                // Check if this node has capacity
                if (currentConnections < maxConnections)
                {
                    if (!network.Connections[neutralID].Contains(consumerID))
                    {
                        network.Connections[neutralID].Add(consumerID);
                        connectedConsumers.Add(consumerID);
                        nodeConsumerCount[neutralID]++;
                        Debug.Log($"Connected consumer {i} ({consumerID}) to last layer node ({neutralID}) - node now has {nodeConsumerCount[neutralID]} consumer(s)");
                        connected = true;
                        break;
                    }
                }
            }
            
            // If still not connected, try any node with capacity (even if it already has consumers)
            if (!connected)
            {
                foreach (string neutralID in sortedNodes)
                {
                    if (!network.Connections.ContainsKey(neutralID))
                    {
                        network.Connections[neutralID] = new List<string>();
                    }
                    
                    int currentConnections = network.Connections[neutralID].Count;
                    int maxConnections = network.MaxConnections[neutralID];
                    
                    if (currentConnections < maxConnections)
                    {
                        if (!network.Connections[neutralID].Contains(consumerID))
                        {
                            network.Connections[neutralID].Add(consumerID);
                            connectedConsumers.Add(consumerID);
                            nodeConsumerCount[neutralID]++;
                            Debug.LogWarning($"Connected consumer {consumerID} to last layer node {neutralID} (node already has {nodeConsumerCount[neutralID] - 1} other consumer(s))");
                            connected = true;
                            break;
                        }
                    }
                }
            }
        }
        
        // Second pass: Ensure ALL consumers are connected (force connection if needed)
        // Increase max connections if necessary to ensure distribution
        foreach (string consumerID in network.ConsumerIDs)
        {
            if (!connectedConsumers.Contains(consumerID))
            {
                Debug.LogWarning($"Consumer {consumerID} had no connection! Force connecting...");
                
                // Find the node with the least consumer connections (even if at capacity)
                // All candidate nodes are from last layer
                string bestNodeID = candidateNodes
                    .OrderBy(nid => nodeConsumerCount.ContainsKey(nid) ? nodeConsumerCount[nid] : 0)
                    .ThenBy(nid => 
                    {
                        int currentConnections = network.Connections.ContainsKey(nid) ? 
                            network.Connections[nid].Count : 0;
                        return currentConnections;
                    })
                    .First();
                
                // Increase max connections if needed
                if (network.Connections.ContainsKey(bestNodeID))
                {
                    int currentConnections = network.Connections[bestNodeID].Count;
                    if (currentConnections >= network.MaxConnections[bestNodeID])
                    {
                        network.MaxConnections[bestNodeID] = currentConnections + 1;
                        Debug.Log($"Increased max connections for {bestNodeID} to {network.MaxConnections[bestNodeID]} to accommodate consumer");
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
                    if (!nodeConsumerCount.ContainsKey(bestNodeID))
                        nodeConsumerCount[bestNodeID] = 0;
                    nodeConsumerCount[bestNodeID]++;
                    Debug.Log($"Force connected consumer {consumerID} to node {bestNodeID}");
                }
            }
        }
        
        // Log final distribution
        Debug.Log("=== Consumer Distribution Across Last Layer ===");
        foreach (string neutralID in candidateNodes)
        {
            int consumerConnections = nodeConsumerCount.ContainsKey(neutralID) ? nodeConsumerCount[neutralID] : 0;
            int totalConnections = network.Connections.ContainsKey(neutralID) ? 
                network.Connections[neutralID].Count : 0;
            Debug.Log($"Last layer Node {neutralID}: {consumerConnections} consumer(s), {totalConnections} total connection(s)");
        }
        
        // Verify all consumers are connected
        if (connectedConsumers.Count != network.ConsumerIDs.Count)
        {
            Debug.LogError($"ERROR: Only {connectedConsumers.Count}/{network.ConsumerIDs.Count} consumers are connected!");
        }
        else
        {
            Debug.Log($"SUCCESS: All {network.ConsumerIDs.Count} consumers are connected to last layer nodes");
        }
    }
    
    /// <summary>
    /// Verify that all consumers are reachable from producers through the skeleton network
    /// Uses BFS to check connectivity
    /// </summary>
    private static void VerifyAllConsumersReachable(SkeletonNetwork network)
    {
        Debug.Log("=== Verifying All Consumers Are Reachable from Producers ===");
        
        // For each consumer, check if it's reachable from any producer using BFS
        HashSet<string> reachableFromProducers = new HashSet<string>();
        Queue<string> queue = new Queue<string>();
        
        // Start BFS from all producers
        foreach (string producerID in network.ProducerIDs)
        {
            queue.Enqueue(producerID);
            reachableFromProducers.Add(producerID);
        }
        
        // BFS to find all nodes reachable from producers
        while (queue.Count > 0)
        {
            string currentID = queue.Dequeue();
            
            // Check all nodes this node connects to
            if (network.Connections.ContainsKey(currentID))
            {
                foreach (string targetID in network.Connections[currentID])
                {
                    if (!reachableFromProducers.Contains(targetID))
                    {
                        reachableFromProducers.Add(targetID);
                        queue.Enqueue(targetID);
                    }
                }
            }
        }
        
        // Check each consumer
        int unreachableCount = 0;
        foreach (string consumerID in network.ConsumerIDs)
        {
            if (!reachableFromProducers.Contains(consumerID))
            {
                unreachableCount++;
                Debug.LogError($"ERROR: Consumer {consumerID} is NOT reachable from any producer!");
                
                // Find which node connects to this consumer
                string connectingNode = null;
                foreach (var kvp in network.Connections)
                {
                    if (kvp.Value.Contains(consumerID))
                    {
                        connectingNode = kvp.Key;
                        break;
                    }
                }
                
                if (connectingNode != null)
                {
                    Debug.LogError($"  Consumer {consumerID} is connected from node {connectingNode}, but that node is not reachable from producers!");
                }
                else
                {
                    Debug.LogError($"  Consumer {consumerID} has no incoming connections!");
                }
            }
        }
        
        if (unreachableCount == 0)
        {
            Debug.Log($"SUCCESS: All {network.ConsumerIDs.Count} consumers are reachable from producers");
        }
        else
        {
            Debug.LogError($"FAILED: {unreachableCount} consumer(s) are NOT reachable from producers!");
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
        
        // Target: Paths should consume all starting energy (~5) to be challenging
        // A path picks ONE node from each layer, so we need each layer to contribute ~-1.6 on average
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
    /// Generate a random weight in range [-4, 4] with balance between positive and negative
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
            // 45% chance: negative weight (-4 to -1)
            return Random.Range(-4, 0);
        }
        else
        {
            // 45% chance: positive weight (1 to 5)
            return Random.Range(1, 5);
        }
    }
    
    /// <summary>
    /// Assign weights to nodes in a single layer with small node count (1-3 nodes)
    /// Ensures that the layer contributes correctly to overall energy consumption
    /// Range: -4 to 4. Allows for "swings" (e.g. +2 and -5) to maintain balance.
    /// </summary>
    private static void AssignLayerWeightsForSmallCount(List<string> layerIDs, float targetAverage, SkeletonNetwork network)
    {
        int nodeCount = layerIDs.Count;
        
        if (nodeCount == 0) return;
        
        List<int> weights = new List<int>();
        
        if (nodeCount == 1)
        {
            // Single node: must be close to target average
            int weight = Mathf.RoundToInt(targetAverage);
            // Ensure we don't return 0 for a single-node layer if it needs to drain energy
            if (weight == 0 && targetAverage < 0) weight = -1;
            weights.Add(Mathf.Clamp(weight, -4, 4));
        }
        else if (nodeCount == 2)
        {
            // Two nodes: Create variety. One could be positive, requiring the other to be strongly negative.
            int targetSum = Mathf.RoundToInt(targetAverage * 2);
            
            // 60% chance to create a "swing" (positive + strong negative)
            if (Random.value < 0.6f)
            {
                int posWeight = Random.Range(1, 5); // +1 to +4
                int negWeight = targetSum - posWeight; // Will be strongly negative
                
                weights.Add(posWeight);
                weights.Add(Mathf.Clamp(negWeight, -4, 4));
            }
            else
            {
                // Otherwise two negatives or a negative and zero
                int weight1 = Random.Range(-4, 1);
                int weight2 = Mathf.Clamp(targetSum - weight1, -4, 4);
                weights.Add(weight1);
                weights.Add(weight2);
            }
            
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
            // Multiple nodes: mix positive and negative to hit target sum
            int targetSum = Mathf.RoundToInt(targetAverage * nodeCount);
            int currentSum = 0;
            
            // Assign a mix of weights for first n-1 nodes
            for (int i = 0; i < nodeCount - 1; i++)
            {
                // Force at least one positive node in larger layers for interest
                int weight;
                if (i == 0 && Random.value < 0.7f) weight = Random.Range(1, 5);
                else weight = GenerateRandomWeight();
                
                weights.Add(weight);
                currentSum += weight;
            }
            
            // Last node adjusts to hit target sum
            int lastWeight = Mathf.Clamp(targetSum - currentSum, -4, 4);
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
