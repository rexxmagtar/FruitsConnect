using System.Collections.Generic;
using UnityEngine;

public abstract class BaseNode : MonoBehaviour
{
    [Header("Node Configuration")]
    [SerializeField] protected string nodeID;
    [SerializeField] protected int maxOutgoingConnections = 1;
    
    [Header("Energy System")]
    [SerializeField] [Range(-3, 3)] protected int weight = 0;
    [SerializeField] protected bool isEnergyApplied = false;
    
    [Header("Visual Components")]
    [SerializeField] protected MeshRenderer meshRenderer;
    [SerializeField] protected Material defaultMaterial;
    [SerializeField] protected Material selectedMaterial;
    [SerializeField] protected Material hoverMaterial;
    
    [Header("Display")]
    [SerializeField] private NodeDisplay nodeDisplay;
    
    [Header("Connection Status Animation")]
    [SerializeField] private Color connectedColor = Color.white;
    [SerializeField] private Color disconnectedColor = Color.black;
    
    // Connection tracking
    protected List<Connection> outgoingConnections = new List<Connection>();
    protected List<Connection> incomingConnections = new List<Connection>();
    
    // Visual state
    private bool isSelected = false;
    private bool isHovered = false;
    
    // Store original material colors for restoration
    private Color[] originalMainMaterialColors;
    private System.Collections.Generic.Dictionary<Renderer, Color[]> originalChildMaterialColors = new System.Collections.Generic.Dictionary<Renderer, Color[]>();
    private bool colorsStored = false;
    
    // Properties
    public string NodeID 
    { 
        get => nodeID;
        set => nodeID = value;
    }
    
    public int MaxOutgoingConnections 
    { 
        get => maxOutgoingConnections;
        set => maxOutgoingConnections = value;
    }
    
    public int Weight 
    { 
        get => weight;
        set
        {
            weight = Mathf.Clamp(value, -3, 3);
            UpdateEnergyDisplay();
        }
    }
    
    public bool IsEnergyApplied 
    { 
        get => isEnergyApplied;
        set => isEnergyApplied = value;
    }
    
    public List<Connection> OutgoingConnections => outgoingConnections;
    public List<Connection> IncomingConnections => incomingConnections;
    
    protected virtual void Awake()
    {
        // Clean up old display components early to prevent them from running
        CleanupOldDisplays();
        
        // Get mesh renderer if not assigned
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        
        // Ensure we have a collider for click detection
        if (GetComponent<Collider>() == null)
        {
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
        }
    }
    
    protected virtual void Start()
    {
        // Clean up old display components
        CleanupOldDisplays();
        
        // Find node display if not assigned
        if (nodeDisplay == null)
        {
            nodeDisplay = GetComponentInChildren<NodeDisplay>();
        }
        
        // Update display if it exists
        if (nodeDisplay != null)
        {
            nodeDisplay.UpdateDisplay();
        }
        
        // Store original material colors (materials should be assigned by now)
        StoreOriginalMaterialColors();
        
        // Update connection status visual on start
        UpdateConnectionStatusVisual();
    }
    
    /// <summary>
    /// Remove old display components that are no longer used
    /// </summary>
    private void CleanupOldDisplays()
    {
        // Find and destroy old NodeEnergyDisplay components
        NodeEnergyDisplay[] oldEnergyDisplays = GetComponentsInChildren<NodeEnergyDisplay>(true);
        foreach (NodeEnergyDisplay oldDisplay in oldEnergyDisplays)
        {
            if (oldDisplay != null)
            {
                // Disable immediately to stop LateUpdate from running
                oldDisplay.enabled = false;
                if (oldDisplay.gameObject != null)
                {
                    Destroy(oldDisplay.gameObject);
                }
            }
        }
        
        // Find and destroy old NodeConnectionDisplay components
        NodeConnectionDisplay[] oldConnectionDisplays = GetComponentsInChildren<NodeConnectionDisplay>(true);
        foreach (NodeConnectionDisplay oldDisplay in oldConnectionDisplays)
        {
            if (oldDisplay != null)
            {
                // Disable immediately to stop LateUpdate from running
                oldDisplay.enabled = false;
                if (oldDisplay.gameObject != null)
                {
                    Destroy(oldDisplay.gameObject);
                }
            }
        }
        
        // Also look for GameObjects with "_EnergyDisplay" in the name (old runtime-created displays)
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in allChildren)
        {
            if (child != null && child.name.Contains("_EnergyDisplay") && child.GetComponent<NodeDisplay>() == null)
            {
                toDestroy.Add(child.gameObject);
            }
        }
        foreach (GameObject obj in toDestroy)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }
    
    /// <summary>
    /// Update the node display when weight or connections change
    /// </summary>
    public void UpdateEnergyDisplay()
    {
        if (nodeDisplay == null)
        {
            nodeDisplay = GetComponentInChildren<NodeDisplay>();
        }
        
        if (nodeDisplay != null)
        {
            nodeDisplay.UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Check if this node has available outgoing connection slots
    /// </summary>
    public bool HasAvailableOutgoingSlot()
    {
        return outgoingConnections.Count < maxOutgoingConnections;
    }
    
    /// <summary>
    /// Add an outgoing connection from this node
    /// </summary>
    public void AddOutgoingConnection(Connection connection)
    {
        if (!outgoingConnections.Contains(connection))
        {
            outgoingConnections.Add(connection);
            OnConnectionsChanged();
        }
    }
    
    /// <summary>
    /// Remove an outgoing connection from this node
    /// </summary>
    public void RemoveOutgoingConnection(Connection connection)
    {
        outgoingConnections.Remove(connection);
        OnConnectionsChanged();
    }
    
    /// <summary>
    /// Called when connections change - update node display
    /// </summary>
    protected virtual void OnConnectionsChanged()
    {
        if (nodeDisplay != null)
        {
            nodeDisplay.UpdateDisplay();
        }
        
        // Update connection status visual
        UpdateConnectionStatusVisual();
    }
    
    /// <summary>
    /// Add an incoming connection to this node
    /// </summary>
    public void AddIncomingConnection(Connection connection)
    {
        if (!incomingConnections.Contains(connection))
        {
            incomingConnections.Add(connection);
        }
    }
    
    /// <summary>
    /// Remove an incoming connection from this node
    /// </summary>
    public void RemoveIncomingConnection(Connection connection)
    {
        incomingConnections.Remove(connection);
    }
    
    /// <summary>
    /// Visual feedback when node is selected
    /// </summary>
    public virtual void OnSelect()
    {
        isSelected = true;
        UpdateVisual();
    }
    
    /// <summary>
    /// Visual feedback when node is deselected
    /// </summary>
    public virtual void OnDeselect()
    {
        isSelected = false;
        UpdateVisual();
    }
    
    /// <summary>
    /// Visual feedback when mouse hovers over node
    /// </summary>
    public virtual void OnHover()
    {
        isHovered = true;
        UpdateVisual();
    }
    
    /// <summary>
    /// Visual feedback when mouse exits node
    /// </summary>
    public virtual void OnHoverExit()
    {
        isHovered = false;
        UpdateVisual();
    }
    
    /// <summary>
    /// Update visual appearance based on state
    /// </summary>
    protected virtual void UpdateVisual()
    {
        if (meshRenderer == null) return;
        
        if (isSelected && selectedMaterial != null)
        {
            meshRenderer.material = selectedMaterial;
        }
        else if (isHovered && hoverMaterial != null)
        {
            meshRenderer.material = hoverMaterial;
        }
        else if (defaultMaterial != null)
        {
            meshRenderer.material = defaultMaterial;
        }
        
        // Update connection status visual after material is set
        UpdateConnectionStatusVisual();
    }
    
    /// <summary>
    /// Store original material colors for restoration
    /// Called lazily on first use if not already stored
    /// </summary>
    private void StoreOriginalMaterialColors()
    {
        if (colorsStored) return;
        
        // Store main renderer colors
        if (meshRenderer != null && meshRenderer.materials != null && meshRenderer.materials.Length > 0)
        {
            originalMainMaterialColors = new Color[meshRenderer.materials.Length];
            for (int i = 0; i < meshRenderer.materials.Length; i++)
            {
                if (meshRenderer.materials[i] != null)
                {
                    originalMainMaterialColors[i] = meshRenderer.materials[i].color;
                }
            }
        }
        
        // Store child renderer colors
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            // Skip the main renderer
            if (renderer == meshRenderer) continue;
            
            if (renderer.materials != null && renderer.materials.Length > 0)
            {
                Color[] colors = new Color[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i] != null)
                    {
                        colors[i] = renderer.materials[i].color;
                    }
                }
                originalChildMaterialColors[renderer] = colors;
            }
        }
        
        colorsStored = true;
    }
    
    /// <summary>
    /// Update material colors based on connection status to producer
    /// </summary>
    private void UpdateConnectionStatusVisual()
    {
        // Ensure original colors are stored
        if (!colorsStored)
        {
            StoreOriginalMaterialColors();
        }
        
        // Check if connected to producer
        bool isConnectedToProducer = IsConnectedToProducer();
        
        // Determine target color
        Color targetColor = isConnectedToProducer ? connectedColor : disconnectedColor;
        
        // Update main renderer
        if (meshRenderer != null && meshRenderer.materials != null)
        {
            for (int i = 0; i < meshRenderer.materials.Length; i++)
            {
                if (meshRenderer.materials[i] != null)
                {
                    // Get original color if available, otherwise use current
                    Color baseColor = (originalMainMaterialColors != null && i < originalMainMaterialColors.Length) 
                        ? originalMainMaterialColors[i] 
                        : meshRenderer.materials[i].color;
                    
                    // Apply color modulation (multiply original color by target color)
                    meshRenderer.materials[i].color = baseColor * targetColor;
                }
            }
        }
        
        // Update child renderers
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            // Skip the main renderer
            if (renderer == meshRenderer) continue;
            
            if (renderer.materials != null && renderer.materials.Length > 0)
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i] != null)
                    {
                        // Get original color if available
                        Color baseColor = Color.white; // Default to white
                        if (originalChildMaterialColors.ContainsKey(renderer) && 
                            i < originalChildMaterialColors[renderer].Length)
                        {
                            baseColor = originalChildMaterialColors[renderer][i];
                        }
                        else
                        {
                            // Store current color if not stored yet
                            baseColor = renderer.materials[i].color;
                            if (!originalChildMaterialColors.ContainsKey(renderer))
                            {
                                Color[] colors = new Color[renderer.materials.Length];
                                for (int j = 0; j < renderer.materials.Length; j++)
                                {
                                    if (renderer.materials[j] != null)
                                    {
                                        colors[j] = renderer.materials[j].color;
                                    }
                                }
                                originalChildMaterialColors[renderer] = colors;
                            }
                        }
                        
                        // Apply color modulation
                        renderer.materials[i].color = baseColor * targetColor;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Check if this node is connected to a producer
    /// </summary>
    private bool IsConnectedToProducer()
    {
        ConnectionManager manager = ConnectionManager.Instance;
        if (manager != null)
        {
            return manager.IsConnectedToProducer(this);
        }
        return false;
    }
    
    /// <summary>
    /// Public method to refresh connection status visual
    /// Can be called externally to update visuals after connection changes
    /// </summary>
    public void RefreshConnectionStatusVisual()
    {
        UpdateConnectionStatusVisual();
    }
    
    /// <summary>
    /// Handle mouse down on node - start drag
    /// </summary>
    private void OnMouseDown()
    {
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.OnNodeDragStart(this);
        }
    }
    
    /// <summary>
    /// Handle mouse drag - update connection preview
    /// </summary>
    private void OnMouseDrag()
    {
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.OnNodeDrag(this);
        }
    }
    
    /// <summary>
    /// Handle mouse up on node - complete drag
    /// </summary>
    private void OnMouseUp()
    {
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.OnNodeDragEnd(this);
        }
    }
    
    /// <summary>
    /// Handle mouse enter for hover effect
    /// </summary>
    private void OnMouseEnter()
    {
        OnHover();
        
        // Notify controller for drag over detection
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.OnNodeHoverEnter(this);
        }
    }
    
    /// <summary>
    /// Handle mouse exit for hover effect
    /// </summary>
    private void OnMouseExit()
    {
        OnHoverExit();
        
        // Notify controller for drag over detection
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.OnNodeHoverExit(this);
        }
    }
    
    /// <summary>
    /// Clear all connections (for level reset)
    /// </summary>
    public void ClearAllConnections()
    {
        outgoingConnections.Clear();
        incomingConnections.Clear();
        isEnergyApplied = false;
        OnConnectionsChanged();
    }
}

