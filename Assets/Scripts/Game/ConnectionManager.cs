using System.Collections.Generic;
using UnityEngine;

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
    
    // Singleton
    private static ConnectionManager _instance;
    public static ConnectionManager Instance => _instance;
    
    // Active connections
    private List<Connection> activeConnections = new List<Connection>();
    
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
        
        // Initialize connection with manager's visual settings
        connection.Initialize(from, to, connectionWidth, connectionColor);
        
        // Add to nodes
        from.AddOutgoingConnection(connection);
        to.AddIncomingConnection(connection);
        
        // Add to active list
        activeConnections.Add(connection);
        
        Debug.Log($"Created connection from {from.NodeID} to {to.NodeID}");
        
        return true;
    }
    
    /// <summary>
    /// Remove a connection
    /// </summary>
    public void RemoveConnection(Connection connection)
    {
        if (connection == null) return;
        
        // Remove from list
        activeConnections.Remove(connection);
        
        // Destroy connection
        connection.DestroyConnection();
        
        Debug.Log($"Removed connection");
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
        
        // Rule 2: Connection mapping allows from→to
        if (currentLevel != null && !currentLevel.CanConnect(from.NodeID, to.NodeID))
        {
            Debug.LogWarning($"Connection from {from.NodeID} to {to.NodeID} not allowed by level mapping");
            return false;
        }
        
        // Rule 3: Connection doesn't already exist (in either direction)
        if (ConnectionExists(from, to))
        {
            Debug.LogWarning($"Connection between {from.NodeID} and {to.NodeID} already exists (connections are bidirectional)");
            return false;
        }
        
        return true;
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
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

