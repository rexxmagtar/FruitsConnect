using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validates and fixes connectivity issues in generated levels
/// Ensures all consumers can reach at least one producer
/// </summary>
public static class ConnectivityValidator
{
    /// <summary>
    /// Ensure all consumers can reach at least one producer, fixing connectivity if needed
    /// </summary>
    public static void EnsureAllConsumersReachable(List<BaseNode> allNodes, LevelController level)
    {
        List<BaseNode> producers = allNodes.Where(n => n is ProducerNode).ToList();
        List<BaseNode> consumers = allNodes.Where(n => n is ConsumerNode).ToList();
        List<BaseNode> neutrals = allNodes.Where(n => n is NeutralNode).ToList();
        
        if (producers.Count == 0 || consumers.Count == 0)
            return;
        
        int fixedCount = 0;
        
        // Check each consumer
        foreach (ConsumerNode consumer in consumers)
        {
            if (!CanConsumerReachAnyProducer(consumer, allNodes, level))
            {
                Debug.LogWarning($"Consumer {consumer.NodeID} cannot reach any producer! Fixing...");
                
                // Find the shortest path to connect this consumer to a producer
                bool wasFixed = CreatePathToProducer(consumer, producers, neutrals, allNodes, level);
                
                if (wasFixed)
                {
                    fixedCount++;
                    Debug.Log($"Fixed connectivity for consumer {consumer.NodeID}");
                }
                else
                {
                    Debug.LogError($"Failed to fix connectivity for consumer {consumer.NodeID}");
                }
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"Fixed connectivity for {fixedCount} unreachable consumer(s)");
        }
        else
        {
            Debug.Log("All consumers are reachable!");
        }
    }
    
    /// <summary>
    /// Check if a consumer can reach any producer using BFS
    /// </summary>
    public static bool CanConsumerReachAnyProducer(ConsumerNode consumer, List<BaseNode> allNodes, LevelController level)
    {
        // Use BFS to check if consumer can reach any producer
        HashSet<string> visited = new HashSet<string>();
        Queue<string> queue = new Queue<string>();
        
        queue.Enqueue(consumer.NodeID);
        visited.Add(consumer.NodeID);
        
        var nodeDict = allNodes.ToDictionary(n => n.NodeID, n => n);
        
        while (queue.Count > 0)
        {
            string currentID = queue.Dequeue();
            
            // Check all nodes that have outgoing connections TO the current node
            foreach (var node in allNodes)
            {
                if (node == null) continue;
                
                List<string> targets = level.GetConnectionMapping(node.NodeID);
                
                if (targets.Contains(currentID) && !visited.Contains(node.NodeID))
                {
                    // This node can connect to current node
                    if (node is ProducerNode)
                    {
                        return true; // Found a path to producer!
                    }
                    
                    visited.Add(node.NodeID);
                    queue.Enqueue(node.NodeID);
                }
            }
        }
        
        return false; // No path found
    }
    
    /// <summary>
    /// Create a path from consumer to a producer by adding necessary connections
    /// </summary>
    private static bool CreatePathToProducer(
        ConsumerNode consumer, 
        List<BaseNode> producers, 
        List<BaseNode> neutrals, 
        List<BaseNode> allNodes,
        LevelController level)
    {
        // Strategy: Find the nearest neutral or producer and create connection chain
        
        if (neutrals.Count > 0)
        {
            // Find nearest neutral to consumer
            var nearestNeutral = neutrals
                .OrderBy(n => Vector3.Distance(n.transform.position, consumer.transform.position))
                .First();
            
            // Connect neutral to consumer
            List<string> neutralMappings = level.GetConnectionMapping(nearestNeutral.NodeID);
            if (!neutralMappings.Contains(consumer.NodeID) && neutralMappings.Count < nearestNeutral.MaxOutgoingConnections)
            {
                neutralMappings.Add(consumer.NodeID);
                level.UpdateConnectionMapping(nearestNeutral.NodeID, neutralMappings);
            }
            
            // Now ensure this neutral can reach a producer
            if (!CanNodeReachProducer(nearestNeutral, allNodes, level))
            {
                // Find nearest producer to this neutral
                var nearestProducer = producers
                    .OrderBy(p => Vector3.Distance(p.transform.position, nearestNeutral.transform.position))
                    .First();
                
                // Connect producer to neutral
                List<string> producerMappings = level.GetConnectionMapping(nearestProducer.NodeID);
                if (!producerMappings.Contains(nearestNeutral.NodeID) && producerMappings.Count < nearestProducer.MaxOutgoingConnections)
                {
                    producerMappings.Add(nearestNeutral.NodeID);
                    level.UpdateConnectionMapping(nearestProducer.NodeID, producerMappings);
                }
                else if (neutrals.Count > 1)
                {
                    // Try to create a chain through another neutral
                    var bridgeNeutral = neutrals
                        .Where(n => n != nearestNeutral)
                        .OrderBy(n => Vector3.Distance(n.transform.position, nearestNeutral.transform.position))
                        .FirstOrDefault();
                    
                    if (bridgeNeutral != null)
                    {
                        // Connect first neutral to bridge neutral
                        List<string> bridgeMappings = level.GetConnectionMapping(nearestNeutral.NodeID);
                        if (!bridgeMappings.Contains(bridgeNeutral.NodeID) && bridgeMappings.Count < nearestNeutral.MaxOutgoingConnections)
                        {
                            bridgeMappings.Add(bridgeNeutral.NodeID);
                            level.UpdateConnectionMapping(nearestNeutral.NodeID, bridgeMappings);
                        }
                        
                        // Connect producer to bridge neutral
                        List<string> producerMappings2 = level.GetConnectionMapping(nearestProducer.NodeID);
                        if (!producerMappings2.Contains(bridgeNeutral.NodeID) && producerMappings2.Count < nearestProducer.MaxOutgoingConnections)
                        {
                            producerMappings2.Add(bridgeNeutral.NodeID);
                            level.UpdateConnectionMapping(nearestProducer.NodeID, producerMappings2);
                        }
                    }
                }
            }
            
            return true;
        }
        else
        {
            // No neutrals - connect directly to nearest producer (fallback)
            var nearestProducer = producers
                .OrderBy(p => Vector3.Distance(p.transform.position, consumer.transform.position))
                .First();
            
            List<string> producerMappings = level.GetConnectionMapping(nearestProducer.NodeID);
            if (!producerMappings.Contains(consumer.NodeID) && producerMappings.Count < nearestProducer.MaxOutgoingConnections)
            {
                producerMappings.Add(consumer.NodeID);
                level.UpdateConnectionMapping(nearestProducer.NodeID, producerMappings);
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a node can reach any producer (BFS in both directions)
    /// </summary>
    private static bool CanNodeReachProducer(BaseNode node, List<BaseNode> allNodes, LevelController level)
    {
        if (node is ProducerNode)
            return true;
        
        HashSet<string> visited = new HashSet<string>();
        Queue<string> queue = new Queue<string>();
        
        queue.Enqueue(node.NodeID);
        visited.Add(node.NodeID);
        
        while (queue.Count > 0)
        {
            string currentID = queue.Dequeue();
            
            // Check all nodes that have outgoing connections FROM the current node
            List<string> targets = level.GetConnectionMapping(currentID);
            foreach (string targetID in targets)
            {
                if (visited.Contains(targetID))
                    continue;
                
                BaseNode targetNode = allNodes.FirstOrDefault(n => n.NodeID == targetID);
                if (targetNode == null)
                    continue;
                
                if (targetNode is ProducerNode)
                    return true;
                
                visited.Add(targetID);
                queue.Enqueue(targetID);
            }
            
            // Also check reverse direction (nodes that connect TO current)
            foreach (var otherNode in allNodes)
            {
                if (otherNode == null || visited.Contains(otherNode.NodeID))
                    continue;
                
                List<string> otherTargets = level.GetConnectionMapping(otherNode.NodeID);
                if (otherTargets.Contains(currentID))
                {
                    if (otherNode is ProducerNode)
                        return true;
                    
                    visited.Add(otherNode.NodeID);
                    queue.Enqueue(otherNode.NodeID);
                }
            }
        }
        
        return false;
    }
}

