using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Ghost line state for visual feedback
/// </summary>
public enum GhostLineState
{
    Neutral,  // Not hovering over any node
    Valid,    // Hovering over a valid connection target
    Invalid   // Hovering over an invalid connection target
}

/// <summary>
/// Manages all connections in the level
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    [Header("References")]
    private LevelController currentLevel;
    
    [Header("Connection Prefab")]
    [SerializeField] private GameObject connectionPrefab;
    
    [Header("Settings")]
    [SerializeField] private Color connectionColor = Color.yellow;
    [SerializeField] private float connectionWidth = 0.1f;
    
    [Header("Ghost Line Settings")]
    [SerializeField] private Color ghostLineValidColor = Color.green;
    [SerializeField] private Color ghostLineInvalidColor = Color.red;
    [SerializeField] private Color ghostLineNeutralColor = Color.yellow;
    [SerializeField] private float ghostLineWidth = 0.1f; // Match connectionWidth
    
    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallLayer = -1; // Default to all layers
    [SerializeField] private string wallTag = "Wall"; // Tag for wall objects
    
    // Singleton
    private static ConnectionManager _instance;
    public static ConnectionManager Instance => _instance;
    
    // Active connections
    private List<Connection> activeConnections = new List<Connection>();
    
    // Ghost line (temporary visual during drag)
    private GameObject ghostLineObject;
    private LineRenderer ghostLineRenderer;
    
    public LevelController CurrentLevel 
    { 
        get => currentLevel;
        set => currentLevel = value;
    }
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Create default connection prefab if not assigned
        if (connectionPrefab == null)
        {
            CreateDefaultConnectionPrefab();
        }
    }
    
    /// <summary>
    /// Create a default connection prefab at runtime
    /// </summary>
    private void CreateDefaultConnectionPrefab()
    {
        connectionPrefab = new GameObject("ConnectionPrefab");
        connectionPrefab.AddComponent<LineRenderer>();
        connectionPrefab.AddComponent<Connection>();
        connectionPrefab.SetActive(false);
    }
    
    /// <summary>
    /// Create a connection between two nodes
    /// </summary>
    public bool CreateConnection(BaseNode from, BaseNode to)
    {
        // Validate connection
        if (!ValidateConnection(from, to))
        {
            Debug.Log($"Cannot create connection from {from.NodeID} to {to.NodeID}");
            return false;
        }
        
        // Create connection GameObject
        GameObject connectionObj = Instantiate(connectionPrefab, transform);
        connectionObj.SetActive(true);
        connectionObj.name = $"Connection_{from.NodeID}_to_{to.NodeID}";
        
        // Get Connection component
        Connection connection = connectionObj.GetComponent<Connection>();
        if (connection == null)
        {
            connection = connectionObj.AddComponent<Connection>();
        }
        
        // Get animation prefab from level config
        GameObject animationPrefab = null;
        GameController gameController = GameController.Instance;
        if (gameController != null && gameController.CurrentLevelConfig != null)
        {
            animationPrefab = gameController.CurrentLevelConfig.ConnectionAnimationPrefab;
        }
        
        // Initialize connection with manager's visual settings and animation prefab
        connection.Initialize(from, to, connectionWidth, connectionColor, animationPrefab);
        
        // Add to nodes
        from.AddOutgoingConnection(connection);
        to.AddIncomingConnection(connection);
        
        // Add to active list
        activeConnections.Add(connection);
        
        // Apply energy only if this is the first incoming connection to the target node
        if (to.IncomingConnections.Count == 1 && !to.IsEnergyApplied)
        {
            if (gameController != null)
            {
                // Apply energy (positive weight = gain, negative weight = lose)
                gameController.ModifyEnergy(to.Weight);
                to.IsEnergyApplied = true;
                
                Debug.Log($"Applied energy {to.Weight} from node {to.NodeID}. First connection established.");
            }
        }
        
        Debug.Log($"Created connection from {from.NodeID} to {to.NodeID}");
        
        // Update visuals for all nodes after connection change
        RefreshAllNodeVisuals();
        
        return true;
    }
    
    /// <summary>
    /// Remove a connection
    /// </summary>
    public void RemoveConnection(Connection connection)
    {
        if (connection == null) return;
        
        BaseNode toNode = connection.ToNode;
        
        // Check if this will be the last incoming connection to the target node
        bool willBeDisconnected = toNode != null && toNode.IncomingConnections.Count == 1;
        
        // Revert energy if this was the only connection and energy was applied
        if (willBeDisconnected && toNode.IsEnergyApplied)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                // Revert energy (opposite of what was applied)
                gameController.ModifyEnergy(-toNode.Weight);
                toNode.IsEnergyApplied = false;
                
                Debug.Log($"Reverted energy {-toNode.Weight} from node {toNode.NodeID}. Last connection removed.");
            }
        }
        
        // Remove from list
        activeConnections.Remove(connection);
        
        // Destroy connection
        connection.DestroyConnection();
        
        Debug.Log($"Removed connection");
        
        // After removal, break any chains that are no longer connected to a producer
        BreakDisconnectedChains();
        
        // Update visuals for all nodes after connection change
        RefreshAllNodeVisuals();
    }
    
    /// <summary>
    /// Break all chains that are no longer connected to a producer
    /// Called after removing a connection
    /// </summary>
    private void BreakDisconnectedChains()
    {
        if (currentLevel == null) return;
        
        List<BaseNode> allNodes = currentLevel.GetAllNodes();
        List<BaseNode> disconnectedNodes = new List<BaseNode>();
        
        // Find all nodes that are not connected to any producer
        foreach (BaseNode node in allNodes)
        {
            if (node == null) continue;
            
            // Skip producers - they're always connected
            if (node is ProducerNode) continue;
            
            // Check if this node is still connected to a producer
            if (!IsConnectedToProducer(node))
            {
                disconnectedNodes.Add(node);
            }
        }
        
        // If no disconnected nodes, we're done
        if (disconnectedNodes.Count == 0) return;
        
        Debug.Log($"Found {disconnectedNodes.Count} disconnected nodes. Breaking their connections...");
        
        // Collect all connections involving disconnected nodes
        List<Connection> connectionsToBreak = new List<Connection>();
        
        foreach (Connection conn in activeConnections)
        {
            if (conn == null) continue;
            
            // If either end is disconnected, mark for removal
            if (disconnectedNodes.Contains(conn.FromNode) || disconnectedNodes.Contains(conn.ToNode))
            {
                connectionsToBreak.Add(conn);
            }
        }
        
        // Break all collected connections (without calling BreakDisconnectedChains recursively)
        foreach (Connection conn in connectionsToBreak)
        {
            BaseNode toNode = conn.ToNode;
            
            // Revert energy if this node will lose all incoming connections and energy was applied
            if (toNode != null && toNode.IsEnergyApplied)
            {
                // Count how many incoming connections this node will still have after breaking
                int remainingIncoming = toNode.IncomingConnections.Count(c => !connectionsToBreak.Contains(c));
                
                // If no incoming connections will remain, revert energy
                if (remainingIncoming == 0)
                {
                    GameController gameController = GameController.Instance;
                    if (gameController != null)
                    {
                        gameController.ModifyEnergy(-toNode.Weight);
                        toNode.IsEnergyApplied = false;
                        
                        Debug.Log($"Reverted energy {-toNode.Weight} from disconnected node {toNode.NodeID}");
                    }
                }
            }
            
            // Remove from active list
            activeConnections.Remove(conn);
            
            // Destroy the connection
            conn.DestroyConnection();
        }
        
        Debug.Log($"Broke {connectionsToBreak.Count} connections from disconnected nodes");
        
        // Update visuals for all nodes after breaking connections
        RefreshAllNodeVisuals();
    }
    
    /// <summary>
    /// Clear all connections (for level reset)
    /// </summary>
    public void ClearAllConnections()
    {
        // Copy list to avoid modification during iteration
        List<Connection> connectionsToRemove = new List<Connection>(activeConnections);
        
        foreach (Connection connection in connectionsToRemove)
        {
            if (connection != null)
            {
                connection.DestroyConnection();
            }
        }
        
        activeConnections.Clear();
        
        // Also clear node connection lists
        if (currentLevel != null)
        {
            foreach (BaseNode node in currentLevel.GetAllNodes())
            {
                node.ClearAllConnections();
            }
        }
        
        Debug.Log("Cleared all connections");
        
        // Update visuals for all nodes after clearing connections
        RefreshAllNodeVisuals();
    }
    
    /// <summary>
    /// Validate if a connection can be created
    /// </summary>
    public bool ValidateConnection(BaseNode from, BaseNode to)
    {
        // Check nodes exist
        if (from == null || to == null)
        {
            Debug.LogWarning("Cannot connect null nodes");
            return false;
        }
        
        // Check not connecting to self
        if (from == to)
        {
            Debug.LogWarning("Cannot connect node to itself");
            return false;
        }
        
        // Consumer nodes cannot have outgoing connections
        if (from is ConsumerNode)
        {
            Debug.LogWarning($"Cannot connect from Consumer node {from.NodeID} - consumers are endpoints!");
            return false;
        }
        
        // Rule 1: SOURCE node has available outgoing slots
        if (!from.HasAvailableOutgoingSlot())
        {
            Debug.LogWarning($"Node {from.NodeID} has no available outgoing slots");
            return false;
        }
        
        // Rule 2: TARGET node can only have 1 incoming connection
        // This applies to ALL nodes (consumers and neutrals)
        // Multiple paths are created in mappings, but player can only build ONE at a time
        if (to.IncomingConnections.Count >= 1)
        {
            Debug.LogWarning($"Node {to.NodeID} already has an incoming connection - nodes can only have 1 input");
            return false;
        }
        
        // Rule 3: Connection mapping allows from→to (this already accounts for walls)
        if (currentLevel != null && !currentLevel.CanConnect(from.NodeID, to.NodeID))
        {
            Debug.LogWarning($"Connection from {from.NodeID} to {to.NodeID} not allowed by level mapping");
            return false;
        }
        
        // Rule 4: Connection doesn't already exist (in either direction)
        if (ConnectionExists(from, to))
        {
            Debug.LogWarning($"Connection between {from.NodeID} and {to.NodeID} already exists (connections are bidirectional)");
            return false;
        }
        
        // Rule 5: Energy check - if target node has no incoming connections and negative weight, check energy
        if (to.IncomingConnections.Count == 0 && to.Weight < 0)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null && !gameController.CanAffordNode(to))
            {
                Debug.LogWarning($"Not enough energy to connect to {to.NodeID}. Need {Mathf.Abs(to.Weight)}, have {gameController.GetCurrentEnergy()}");
                return false;
            }
        }
        
        // Rule 6: Producer path check - at least one node must be connected to a producer
        if (!IsConnectedToProducer(from) && !IsConnectedToProducer(to))
        {
            Debug.LogWarning($"Cannot create connection: neither {from.NodeID} nor {to.NodeID} is connected to a producer");
            return false;
        }
        
        // Rule 7: No intersection with existing connections
        if (WouldConnectionIntersect(from.transform.position, to.transform.position))
        {
            Debug.LogWarning($"Cannot create connection: would intersect with existing connection");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Check if a new connection would intersect with any existing connections
    /// </summary>
    private bool WouldConnectionIntersect(Vector3 fromPos, Vector3 toPos)
    {
        // Check against all active connections
        foreach (Connection conn in activeConnections)
        {
            if (conn == null || conn.FromNode == null || conn.ToNode == null) continue;
            
            Vector3 existingFrom = conn.FromNode.transform.position;
            Vector3 existingTo = conn.ToNode.transform.position;
            
            if (DoLinesIntersect(fromPos, toPos, existingFrom, existingTo))
            {
                return true; // Would intersect
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if two line segments intersect in 2D (XZ plane)
    /// Returns false if they only touch at endpoints
    /// </summary>
    private bool DoLinesIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
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
    private float CrossProduct2D(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
    
    /// <summary>
    /// Check if a node is connected to any producer through existing connections
    /// Uses BFS to traverse the connection graph backwards
    /// </summary>
    public bool IsConnectedToProducer(BaseNode node)
    {
        if (node == null) return false;
        
        // Producers are always connected to themselves
        if (node is ProducerNode) return true;
        
        // If node has incoming connections, it's already part of a network
        if (node.IncomingConnections.Count > 0) return true;
        
        // Otherwise, check if we can reach a producer through existing connections
        HashSet<BaseNode> visited = new HashSet<BaseNode>();
        Queue<BaseNode> queue = new Queue<BaseNode>();
        
        queue.Enqueue(node);
        visited.Add(node);
        
        while (queue.Count > 0)
        {
            BaseNode current = queue.Dequeue();
            
            // Check if we reached a producer
            if (current is ProducerNode)
            {
                return true;
            }
            
            // Explore incoming connections (walk backwards to find producer)
            foreach (Connection conn in current.IncomingConnections)
            {
                BaseNode fromNode = conn.FromNode;
                
                if (fromNode != null && !visited.Contains(fromNode))
                {
                    if (fromNode is ProducerNode)
                    {
                        return true;
                    }
                    
                    visited.Add(fromNode);
                    queue.Enqueue(fromNode);
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Refresh connection status visuals for all nodes in the level
    /// Called after connections are added or removed
    /// </summary>
    private void RefreshAllNodeVisuals()
    {
        if (currentLevel == null) return;
        
        List<BaseNode> allNodes = currentLevel.GetAllNodes();
        foreach (BaseNode node in allNodes)
        {
            if (node != null)
            {
                node.RefreshConnectionStatusVisual();
            }
        }
    }
    
    /// <summary>
    /// Check if a connection already exists between two nodes (in either direction)
    /// Connections are bidirectional - A→B is the same as B→A
    /// </summary>
    private bool ConnectionExists(BaseNode from, BaseNode to)
    {
        // Check if from→to exists
        foreach (Connection conn in from.OutgoingConnections)
        {
            if (conn.ToNode == to)
            {
                return true;
            }
        }
        
        // Check if to→from exists (reverse direction)
        foreach (Connection conn in to.OutgoingConnections)
        {
            if (conn.ToNode == from)
            {
                return true;
            }
        }
        
        // Also check incoming connections to handle all cases
        foreach (Connection conn in from.IncomingConnections)
        {
            if (conn.FromNode == to)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all active connections
    /// </summary>
    public List<Connection> GetActiveConnections()
    {
        return new List<Connection>(activeConnections);
    }
    
    /// <summary>
    /// Show ghost line from a start position
    /// </summary>
    public void ShowGhostLine(Vector3 startPosition, Vector3 endPosition, GhostLineState state)
    {
        // Create ghost line if it doesn't exist - use the same prefab as real connections
        if (ghostLineObject == null)
        {
            // Instantiate the connection prefab to get the same visual setup
            ghostLineObject = Instantiate(connectionPrefab, transform);
            ghostLineObject.name = "GhostLine";
            
            // Get the LineRenderer from the prefab instance
            ghostLineRenderer = ghostLineObject.GetComponent<LineRenderer>();
            if (ghostLineRenderer == null)
            {
                ghostLineRenderer = ghostLineObject.AddComponent<LineRenderer>();
            }
            
            // Remove Connection component - ghost shouldn't behave like a real connection
            Connection connectionComponent = ghostLineObject.GetComponent<Connection>();
            if (connectionComponent != null)
            {
                Destroy(connectionComponent);
            }
            
            // Remove or disable BoxCollider - ghost shouldn't be clickable
            BoxCollider boxCollider = ghostLineObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }
            
            // Setup LineRenderer to match connection settings
            ghostLineRenderer.positionCount = 2;
            ghostLineRenderer.startWidth = ghostLineWidth;
            ghostLineRenderer.endWidth = ghostLineWidth;
            
            // The prefab's material is already set, we just need to update its color
            // Create a material instance so we can change color without affecting the prefab
            if (ghostLineRenderer.material != null)
            {
                ghostLineRenderer.material = new Material(ghostLineRenderer.material);
            }
        }
        
        // Clamp end position to same Y plane as start position
        endPosition.y = startPosition.y;
        
        // Update positions
        ghostLineRenderer.SetPosition(0, startPosition);
        ghostLineRenderer.SetPosition(1, endPosition);
        
        // Update color based on state
        ghostLineRenderer.material.color = GetGhostLineColor(state);
        
        // Make sure it's visible
        ghostLineObject.SetActive(true);
    }
    
    /// <summary>
    /// Update ghost line end position and state color
    /// </summary>
    public void UpdateGhostLine(Vector3 endPosition, GhostLineState state)
    {
        if (ghostLineRenderer != null && ghostLineObject != null && ghostLineObject.activeSelf)
        {
            // Clamp to same Y plane as start position
            Vector3 startPos = ghostLineRenderer.GetPosition(0);
            endPosition.y = startPos.y;
            
            ghostLineRenderer.SetPosition(1, endPosition);
            ghostLineRenderer.material.color = GetGhostLineColor(state);
        }
    }
    
    /// <summary>
    /// Get the appropriate color for the ghost line state
    /// </summary>
    private Color GetGhostLineColor(GhostLineState state)
    {
        switch (state)
        {
            case GhostLineState.Valid:
                return ghostLineValidColor;
            case GhostLineState.Invalid:
                return ghostLineInvalidColor;
            case GhostLineState.Neutral:
            default:
                return ghostLineNeutralColor;
        }
    }
    
    /// <summary>
    /// Check if a line from start to end intersects with any wall
    /// </summary>
    public bool CheckWallIntersection(Vector3 startPosition, Vector3 endPosition)
    {
        // Clamp both positions to same Y plane
        endPosition.y = startPosition.y;
        
        // Perform a linecast to check for wall collisions
        RaycastHit[] hits = Physics.RaycastAll(startPosition, (endPosition - startPosition).normalized, 
                                                Vector3.Distance(startPosition, endPosition), wallLayer);
        
        // Check each hit to see if it's a wall
        foreach (RaycastHit hit in hits)
        {
            // Check by tag if wall tag is set
            if (!string.IsNullOrEmpty(wallTag) && hit.collider.CompareTag(wallTag))
            {
                return true;
            }
            
            // Also check if the hit object has a MeshRenderer (as mentioned by user)
            // and is not a node (nodes also have MeshRenderers)
            if (hit.collider.GetComponent<MeshRenderer>() != null && 
                hit.collider.GetComponent<BaseNode>() == null)
            {
                // It's likely a wall
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Hide ghost line
    /// </summary>
    public void HideGhostLine()
    {
        if (ghostLineObject != null)
        {
            ghostLineObject.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        
        // Clean up ghost line
        if (ghostLineObject != null)
        {
            Destroy(ghostLineObject);
        }
    }
}

