using UnityEngine;

/// <summary>
/// Defines the goal types for monsters
/// </summary>
public enum MonsterGoalType
{
    DestroyConnection,      // Target a specific connection to destroy
    DestroyNodeConnections  // Target a node and destroy all its connections
}

/// <summary>
/// Represents a monster's goal - what it's trying to accomplish
/// </summary>
[System.Serializable]
public class MonsterGoal
{
    [Header("Goal Configuration")]
    public MonsterGoalType goalType;
    
    [Header("Target References")]
    public Connection targetConnection;  // Used when goalType is DestroyConnection
    public BaseNode targetNode;          // Used when goalType is DestroyNodeConnections
    
    /// <summary>
    /// Check if the goal is still valid (target exists and is not already captured)
    /// </summary>
    public bool IsValid()
    {
        switch (goalType)
        {
            case MonsterGoalType.DestroyConnection:
                return targetConnection != null && targetConnection.gameObject != null && !targetConnection.IsCaptured;
            
            case MonsterGoalType.DestroyNodeConnections:
                return targetNode != null && targetNode.gameObject != null && !targetNode.IsCaptured;
            
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Get the target position for movement
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        switch (goalType)
        {
            case MonsterGoalType.DestroyConnection:
                if (targetConnection != null && targetConnection.FromNode != null && targetConnection.ToNode != null)
                {
                    // Return midpoint of connection
                    Vector3 fromPos = targetConnection.FromNode.transform.position;
                    Vector3 toPos = targetConnection.ToNode.transform.position;
                    return (fromPos + toPos) / 2f;
                }
                break;
            
            case MonsterGoalType.DestroyNodeConnections:
                if (targetNode != null)
                {
                    return targetNode.transform.position;
                }
                break;
        }
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// Check if monster has reached the target (within bounds of target)
    /// </summary>
    public bool HasReachedTarget(Vector3 monsterPosition, float reachDistance = 0.5f)
    {
        switch (goalType)
        {
            case MonsterGoalType.DestroyConnection:
                if (targetConnection != null && targetConnection.gameObject != null)
                {
                    // Check if monster is within connection's collider bounds
                    BoxCollider connectionCollider = targetConnection.GetComponent<BoxCollider>();
                    if (connectionCollider != null && connectionCollider.enabled)
                    {
                        // Get world bounds of the collider
                        Bounds bounds = connectionCollider.bounds;
                        // Expand bounds slightly for easier reach detection
                        bounds.Expand(reachDistance);
                        return bounds.Contains(monsterPosition);
                    }
                    else
                    {
                        // Fallback: check distance to connection midpoint
                        Vector3 targetPos = GetTargetPosition();
                        float distance = Vector3.Distance(monsterPosition, targetPos);
                        return distance <= reachDistance;
                    }
                }
                break;
            
            case MonsterGoalType.DestroyNodeConnections:
                if (targetNode != null && targetNode.gameObject != null)
                {
                    // Check if monster is within node's collider bounds
                    Collider nodeCollider = targetNode.GetComponent<Collider>();
                    if (nodeCollider != null && nodeCollider.enabled)
                    {
                        // Get world bounds of the collider
                        Bounds bounds = nodeCollider.bounds;
                        // Expand bounds slightly for easier reach detection
                        bounds.Expand(reachDistance);
                        return bounds.Contains(monsterPosition);
                    }
                    else
                    {
                        // Fallback: check distance to node center
                        Vector3 targetPos = GetTargetPosition();
                        float distance = Vector3.Distance(monsterPosition, targetPos);
                        return distance <= reachDistance;
                    }
                }
                break;
        }
        
        return false;
    }
}
