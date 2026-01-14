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
    [SerializeField] private float groundLevelY = -0.48f; // Ground level Y coordinate for connections
    
    public float GroundLevelY => groundLevelY;
    
    [Header("Ghost Line Settings")]
    [SerializeField] private Color ghostLineValidColor = Color.green;
    [SerializeField] private Color ghostLineInvalidColor = Color.red;
    [SerializeField] private Color ghostLineNeutralColor = Color.yellow;
    [SerializeField] private float ghostLineWidth = 0.1f; // Match connectionWidth
    
    [Header("Audio")]
    [SerializeField] private AudioClip connectionCreatedSound;
    [SerializeField] private AudioSource audioSource;
    
    // Singleton
    private static ConnectionManager _instance;
    public static ConnectionManager Instance => _instance;
    
    // Active connections
    private List<Connection> activeConnections = new List<Connection>();
    
    // Event for when connections change (notifies MapShaderController)
    public static event System.Action OnConnectionsChanged;
    
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
        
        // Get or add AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
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
        
        // Apply energy immediately when connection is created (if connected to producer)
        ApplyEnergyForConnection(to);
        
        Debug.Log($"Created connection from {from.NodeID} to {to.NodeID}. Node {to.NodeID} needs {to.RequiredDeliveries} deliveries to activate.");
        
        // Hide placeholder line for this connection
        if (currentLevel != null)
        {
            GraphVisualizer graphVisualizer = currentLevel.GetGraphVisualizer();
            if (graphVisualizer != null)
            {
                graphVisualizer.HidePlaceholderLine(from, to);
            }
        }
        
        // Note: Pulse animation and particle effect will be triggered when node is fully activated (after deliveries)
        // See BaseNode.ActivateNode()
        
        // Update visuals for all nodes after connection change
        RefreshAllNodeVisuals();
        
        // Notify MapShaderController that connections changed
        OnConnectionsChanged?.Invoke();
        
        // Play connection creation sound
        PlayConnectionCreatedSound();
        
        return true;
    }
    
    /// <summary>
    /// Remove a connection
    /// </summary>
    public void RemoveConnection(Connection connection)
    {
        if (connection == null) return;
        
        // Store node references before destroying connection
        BaseNode fromNode = connection.FromNode;
        BaseNode toNode = connection.ToNode;
        
        // Remove from list
        activeConnections.Remove(connection);
        
        // Destroy connection
        connection.DestroyConnection();
        
        Debug.Log($"Removed connection");
        
        // Reset deliveries for the target node if it's no longer connected to a producer
        // This ensures immediate reset when connection is broken
        if (toNode != null && !IsConnectedToProducer(toNode))
        {
            toNode.ResetDeliveries();
        }
        
        // After removal, break any chains that are no longer connected to a producer
        BreakDisconnectedChains();
        
        // Show placeholder line again for this connection
        if (currentLevel != null && fromNode != null && toNode != null)
        {
            GraphVisualizer graphVisualizer = currentLevel.GetGraphVisualizer();
            if (graphVisualizer != null)
            {
                graphVisualizer.ShowPlaceholderLine(fromNode, toNode);
            }
        }
        
        // Update visuals for all nodes after connection change
        RefreshAllNodeVisuals();
        
        // Notify MapShaderController that connections changed
        OnConnectionsChanged?.Invoke();
    }
    
    /// <summary>
    /// Remove all connections from/to a specific node
    /// Used by monsters to destroy all connections to a node
    /// </summary>
    public void RemoveAllConnectionsFromNode(BaseNode node)
    {
        if (node == null) return;
        
        // Collect all connections to remove
        List<Connection> connectionsToRemove = new List<Connection>();
        
        // Get outgoing connections
        foreach (Connection conn in node.OutgoingConnections)
        {
            if (conn != null && !connectionsToRemove.Contains(conn))
            {
                connectionsToRemove.Add(conn);
            }
        }
        
        // Get incoming connections
        foreach (Connection conn in node.IncomingConnections)
        {
            if (conn != null && !connectionsToRemove.Contains(conn))
            {
                connectionsToRemove.Add(conn);
            }
        }
        
        // Remove all collected connections
        // RemoveConnection already handles showing placeholder lines
        foreach (Connection conn in connectionsToRemove)
        {
            RemoveConnection(conn);
        }
        
        Debug.Log($"Removed all connections from/to node {node.NodeID}");
    }
    
    /// <summary>
    /// Check if a connection is captured by a monster
    /// </summary>
    public bool IsConnectionCaptured(Connection connection)
    {
        if (connection == null) return false;
        return connection.IsCaptured;
    }
    
    /// <summary>
    /// Check if a node is captured by a monster
    /// </summary>
    public bool IsNodeCaptured(BaseNode node)
    {
        if (node == null) return false;
        return node.IsCaptured;
    }
    
    /// <summary>
    /// Break all chains that are no longer connected to a producer
    /// Called after removing a connection
    /// Iteratively checks ALL connections until no more need to be broken
    /// Guarantees no infinite loops by tracking checked connections per iteration
    /// </summary>
    private void BreakDisconnectedChains()
    {
        if (currentLevel == null) return;
        
        // Safety limit: can't iterate more than the number of connections
        // (worst case: break one connection per iteration)
        int initialConnectionCount = activeConnections.Count;
        int maxIterations = initialConnectionCount;
        int iterationCount = 0;
        
        // Keep iterating until no more disconnected connections are found
        while (iterationCount < maxIterations)
        {
            iterationCount++;
            
            // Track connections checked in this iteration to prevent checking same connection twice
            // (reset each iteration so connections can be re-checked if they become disconnected)
            HashSet<Connection> checkedThisIteration = new HashSet<Connection>();
            
            // Check ALL active connections to see if they still lead to a producer
            List<Connection> connectionsToBreak = new List<Connection>();
            
            foreach (Connection conn in activeConnections)
            {
                if (conn == null || conn.ToNode == null) continue;
                
                // Skip if already checked in this iteration (prevents duplicate checks within same iteration)
                if (checkedThisIteration.Contains(conn)) continue;
                
                // Mark as checked for this iteration only
                checkedThisIteration.Add(conn);
                
                // A connection should exist only if its ToNode is connected to a producer
                // Check if the ToNode is still connected to a producer
                if (!IsConnectedToProducer(conn.ToNode))
                {
                    connectionsToBreak.Add(conn);
                }
            }
            
            // If no connections to break, we're done
            if (connectionsToBreak.Count == 0)
            {
                break;
            }
            
            Debug.Log($"Iteration {iterationCount}: Found {connectionsToBreak.Count} connections to break");
            
            // Break all collected connections
            foreach (Connection conn in connectionsToBreak)
            {
                // Remove from active list
                activeConnections.Remove(conn);
                
                // Destroy the connection
                conn.DestroyConnection();
            }
            
            // Guarantee: we break at least one connection per iteration (if any exist)
            // Since we remove connections from activeConnections, the next iteration
            // will have fewer connections to check, guaranteeing termination
        }
        
        // Safety check: if we hit max iterations, something went wrong
        if (iterationCount >= maxIterations && activeConnections.Count > 0)
        {
            Debug.LogError($"BreakDisconnectedChains reached max iterations ({maxIterations}) with {activeConnections.Count} connections remaining. This should never happen!");
        }
        else if (iterationCount > 1)
        {
            Debug.Log($"BreakDisconnectedChains completed in {iterationCount} iterations (started with {initialConnectionCount} connections)");
        }
        
        // Recalculate total energy from scratch after all destructions are done
        RecalculateTotalEnergy();
        
        // Update visuals for all nodes after breaking connections
        RefreshAllNodeVisuals();
    }
    
    /// <summary>
    /// Apply energy immediately when a connection is created
    /// Energy is applied if the target node is connected to a producer
    /// </summary>
    private void ApplyEnergyForConnection(BaseNode targetNode)
    {
        if (targetNode == null) return;
        
        GameController gameController = GameController.Instance;
        if (gameController == null) return;
        
        // Check if node is connected to producer
        bool isConnectedToProducer = IsConnectedToProducer(targetNode);
        
        // Apply energy immediately if:
        // 1. Target node is a ProducerNode (and not captured)
        // 2. Target node is connected to a producer (even if not fully delivered yet)
        if (isConnectedToProducer && !targetNode.IsEnergyApplied)
        {
            // For ProducerNode, always apply immediately
            if (targetNode is ProducerNode && !targetNode.IsCaptured)
            {
                gameController.ModifyEnergy(targetNode.Weight);
                targetNode.IsEnergyApplied = true;
                Debug.Log($"Applied energy {targetNode.Weight} immediately for ProducerNode {targetNode.NodeID}");
            }
            // For other nodes, apply immediately if connected to producer
            else if (targetNode.IncomingConnections.Count > 0)
            {
                gameController.ModifyEnergy(targetNode.Weight);
                targetNode.IsEnergyApplied = true;
                Debug.Log($"Applied energy {targetNode.Weight} immediately for node {targetNode.NodeID} (connected to producer)");
            }
        }
    }
    
    /// <summary>
    /// Recalculate total energy from scratch based on currently connected nodes
    /// Called after connection destruction sequence is complete
    /// </summary>
    private void RecalculateTotalEnergy()
    {
        if (currentLevel == null) return;
        
        GameController gameController = GameController.Instance;
        if (gameController == null) return;
        
        // Get starting energy
        int startingEnergy = gameController.GetMaxEnergy();
        
        // Reset all energy applied flags
        List<BaseNode> allNodes = currentLevel.GetAllNodes();
        foreach (BaseNode node in allNodes)
        {
            if (node != null)
            {
                node.IsEnergyApplied = false;
            }
        }
        
        // Calculate total energy: starting energy + sum of weights of all connected nodes
        int totalEnergy = startingEnergy;
        
        foreach (BaseNode node in allNodes)
        {
            if (node == null) continue;
            
            // Producers are always active (don't need deliveries or incoming connections)
            if (node is ProducerNode && !node.IsCaptured)
            {
                totalEnergy += node.Weight;
                node.IsEnergyApplied = true;
            }
            // Count nodes that are connected to a producer (energy is applied immediately when connection is made)
            // Note: Energy is applied when connection is created, not when fully delivered
            else if (node.IncomingConnections.Count > 0 && IsConnectedToProducer(node))
            {
                totalEnergy += node.Weight;
                node.IsEnergyApplied = true;
            }
            else
            {
                // Reset energy applied flag if node is not connected to producer (or is a producer that's captured)
                node.IsEnergyApplied = false;
                
                // Reset deliveries if node is no longer connected to a producer
                // This ensures that when a connection breaks, the delivered value resets
                node.ResetDeliveries();
            }
        }
        
        // Set current energy directly (clamp to prevent negative)
        int calculatedEnergy = Mathf.Max(0, totalEnergy);
        gameController.SetEnergy(calculatedEnergy);
        
        Debug.Log($"Recalculated energy: {calculatedEnergy} (starting: {startingEnergy}, node weights sum: {totalEnergy - startingEnergy})");
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
        
        // Notify MapShaderController that connections changed
        OnConnectionsChanged?.Invoke();
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
        
        // Check nodes are not captured
        if (from.IsCaptured)
        {
            Debug.LogWarning($"Cannot connect from captured node {from.NodeID}");
            return false;
        }
        
        if (to.IsCaptured)
        {
            Debug.LogWarning($"Cannot connect to captured node {to.NodeID}");
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
        
        // Rule 0: SOURCE node must be fully activated (ProducerNodes are always active)
        // Nodes that are connected but not yet fully delivered cannot build new connections
        if (!(from is ProducerNode) && !from.IsFullyDelivered)
        {
            Debug.LogWarning($"Cannot connect from node {from.NodeID} - node is connected but not yet fully activated (deliveries: {from.CurrentDeliveries}/{from.RequiredDeliveries})");
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
        
        return true;
    }
    
    /// <summary>
    /// Check if a node is connected to any producer through existing connections
    /// Uses BFS to traverse the connection graph backwards
    /// </summary>
    public bool IsConnectedToProducer(BaseNode node)
    {
        if (node == null) return false;
        
        // Producers are connected to themselves only if not captured
        if (node is ProducerNode) return !node.IsCaptured;
        
        // If node has no incoming connections, it cannot be connected to a producer
        if (node.IncomingConnections.Count == 0) return false;
        
        // Check if we can reach a producer through existing connections using BFS
        HashSet<BaseNode> visited = new HashSet<BaseNode>();
        Queue<BaseNode> queue = new Queue<BaseNode>();
        
        queue.Enqueue(node);
        visited.Add(node);
        
        while (queue.Count > 0)
        {
            BaseNode current = queue.Dequeue();
            
            // Check if we reached a producer (only if not captured)
            if (current is ProducerNode && !current.IsCaptured)
            {
                return true;
            }
            
            // Explore incoming connections (walk backwards to find producer)
            foreach (Connection conn in current.IncomingConnections)
            {
                BaseNode fromNode = conn.FromNode;
                
                if (fromNode != null && !visited.Contains(fromNode))
                {
                    if (fromNode is ProducerNode && !fromNode.IsCaptured)
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
            
            // Set higher sorting order to ensure ghost line renders above placeholder lines
            ghostLineRenderer.sortingOrder = 100;
            
            // The prefab's material is already set, we just need to update its color
            // Create a material instance so we can change color without affecting the prefab
            if (ghostLineRenderer.material != null)
            {
                ghostLineRenderer.material = new Material(ghostLineRenderer.material);
            }
        }
        
        // Ensure both start and end Y coordinates are always 0
        startPosition.y = 0.1f;
        endPosition.y = 0.1f;
        
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
            // Ensure both start and end Y coordinates are always at ground level
            Vector3 startPos = ghostLineRenderer.GetPosition(0);
            startPos.y = groundLevelY;
            endPosition.y = groundLevelY;
            
            ghostLineRenderer.SetPosition(0, startPos);
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
    /// Hide ghost line
    /// </summary>
    public void HideGhostLine()
    {
        if (ghostLineObject != null)
        {
            ghostLineObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Play sound effect when connection is created
    /// </summary>
    private void PlayConnectionCreatedSound()
    {
        if (connectionCreatedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(connectionCreatedSound);
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

