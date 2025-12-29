using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utility class for snapping positions to a grid and managing grid occupancy
/// </summary>
public static class GridSnapper
{
    /// <summary>
    /// Snap a position to the nearest grid point
    /// </summary>
    public static Vector3 SnapToGrid(Vector3 position, float gridSize = 1f)
    {
        float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
        float snappedZ = Mathf.Round(position.z / gridSize) * gridSize;
        
        return new Vector3(snappedX, position.y, snappedZ);
    }
    
    /// <summary>
    /// Get a grid position from grid coordinates
    /// </summary>
    public static Vector3 GetGridPosition(int gridX, int gridZ, float gridSize, Vector3 origin)
    {
        float worldX = origin.x + (gridX * gridSize);
        float worldZ = origin.z + (gridZ * gridSize);
        
        return new Vector3(worldX, origin.y, worldZ);
    }
    
    /// <summary>
    /// Convert world position to grid coordinates
    /// </summary>
    public static (int x, int z) WorldToGrid(Vector3 position, float gridSize, Vector3 origin)
    {
        int gridX = Mathf.RoundToInt((position.x - origin.x) / gridSize);
        int gridZ = Mathf.RoundToInt((position.z - origin.z) / gridSize);
        
        return (gridX, gridZ);
    }
    
    /// <summary>
    /// Check if a grid position is occupied by any existing nodes
    /// </summary>
    public static bool IsGridPositionOccupied(Vector3 position, List<BaseNode> existingNodes, float gridSize, float tolerance = 0.01f)
    {
        Vector3 snappedPos = SnapToGrid(position, gridSize);
        
        foreach (BaseNode node in existingNodes)
        {
            if (node == null) continue;
            
            Vector3 nodePos = SnapToGrid(node.transform.position, gridSize);
            
            // If positions snap to the same grid cell, they're on the same position
            // Use very small tolerance to account for floating point precision
            float distance = Vector3.Distance(snappedPos, nodePos);
            
            if (distance < tolerance)
            {
                return true; // Same grid cell - occupied
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Find the nearest unoccupied grid position
    /// </summary>
    public static Vector3 FindNearestUnoccupiedGridPosition(Vector3 desiredPosition, List<BaseNode> existingNodes, float gridSize, int searchRadius = 5)
    {
        Vector3 snappedDesired = SnapToGrid(desiredPosition, gridSize);
        
        // Check if desired position is free
        if (!IsGridPositionOccupied(snappedDesired, existingNodes, gridSize))
        {
            return snappedDesired;
        }
        
        // Search in expanding spiral pattern
        for (int radius = 1; radius <= searchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    // Only check positions on the edge of current radius
                    if (Mathf.Abs(x) == radius || Mathf.Abs(z) == radius)
                    {
                        Vector3 candidatePos = snappedDesired + new Vector3(x * gridSize, 0, z * gridSize);
                        
                        if (!IsGridPositionOccupied(candidatePos, existingNodes, gridSize))
                        {
                            return candidatePos;
                        }
                    }
                }
            }
        }
        
        // If no position found, return original snapped position
        return snappedDesired;
    }
}

