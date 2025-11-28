using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility to check if connections (line segments) intersect in 2D space
/// Ensures planar graph constraint - no edges can cross
/// </summary>
public static class ConnectionIntersectionChecker
{
    /// <summary>
    /// Check if a new connection would intersect with any existing connections
    /// </summary>
    public static bool WouldIntersectExisting(
        Vector3 newFrom, 
        Vector3 newTo, 
        List<BaseNode> allNodes, 
        LevelController level)
    {
        // Get all existing connections
        foreach (BaseNode node in allNodes)
        {
            if (node == null) continue;
            
            List<string> targets = level.GetConnectionMapping(node.NodeID);
            
            foreach (string targetID in targets)
            {
                BaseNode targetNode = allNodes.Find(n => n != null && n.NodeID == targetID);
                if (targetNode == null) continue;
                
                Vector3 existingFrom = node.transform.position;
                Vector3 existingTo = targetNode.transform.position;
                
                // Check if the new line intersects this existing line
                if (DoLinesIntersect(newFrom, newTo, existingFrom, existingTo))
                {
                    return true; // Intersection found
                }
            }
        }
        
        return false; // No intersections
    }
    
    /// <summary>
    /// Check if a connection between two node IDs would intersect existing connections
    /// </summary>
    public static bool WouldConnectionIntersect(
        string fromID,
        string toID,
        List<BaseNode> allNodes,
        LevelController level)
    {
        BaseNode fromNode = allNodes.Find(n => n != null && n.NodeID == fromID);
        BaseNode toNode = allNodes.Find(n => n != null && n.NodeID == toID);
        
        if (fromNode == null || toNode == null) return true;
        
        return WouldIntersectExisting(
            fromNode.transform.position, 
            toNode.transform.position, 
            allNodes, 
            level);
    }
    
    /// <summary>
    /// Check if two line segments intersect in 2D (XZ plane)
    /// Returns false if they only touch at endpoints
    /// </summary>
    public static bool DoLinesIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        // Convert to 2D (use X and Z, ignore Y)
        Vector2 a = new Vector2(p1.x, p1.z);
        Vector2 b = new Vector2(p2.x, p2.z);
        Vector2 c = new Vector2(p3.x, p3.z);
        Vector2 d = new Vector2(p4.x, p4.z);
        
        // Check if lines share an endpoint - this is allowed
        float epsilon = 0.01f;
        if (Vector2.Distance(a, c) < epsilon || Vector2.Distance(a, d) < epsilon ||
            Vector2.Distance(b, c) < epsilon || Vector2.Distance(b, d) < epsilon)
        {
            return false; // Sharing endpoint is OK
        }
        
        // Use cross product to determine intersection
        float d1 = CrossProduct2D(c - a, b - a);
        float d2 = CrossProduct2D(d - a, b - a);
        float d3 = CrossProduct2D(a - c, d - c);
        float d4 = CrossProduct2D(b - c, d - c);
        
        // Lines intersect if signs are different (points on opposite sides)
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate 2D cross product (z component of 3D cross product)
    /// </summary>
    private static float CrossProduct2D(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
    
    /// <summary>
    /// Get all existing connection line segments
    /// </summary>
    public static List<(Vector3 from, Vector3 to)> GetAllConnectionLines(List<BaseNode> allNodes, LevelController level)
    {
        List<(Vector3, Vector3)> lines = new List<(Vector3, Vector3)>();
        
        foreach (BaseNode node in allNodes)
        {
            if (node == null) continue;
            
            List<string> targets = level.GetConnectionMapping(node.NodeID);
            
            foreach (string targetID in targets)
            {
                BaseNode targetNode = allNodes.Find(n => n != null && n.NodeID == targetID);
                if (targetNode != null)
                {
                    lines.Add((node.transform.position, targetNode.transform.position));
                }
            }
        }
        
        return lines;
    }
}

