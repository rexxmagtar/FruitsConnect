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
    
    // Connection tracking
    protected List<Connection> outgoingConnections = new List<Connection>();
    protected List<Connection> incomingConnections = new List<Connection>();
    
    // Visual state
    private bool isSelected = false;
    private bool isHovered = false;
    
    // Energy display
    private NodeEnergyDisplay energyDisplay;
    
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
        // Create energy display for nodes with non-zero weight
        if (weight != 0)
        {
            CreateEnergyDisplay();
        }
    }
    
    /// <summary>
    /// Create the energy display UI above this node
    /// </summary>
    private void CreateEnergyDisplay()
    {
        if (energyDisplay == null)
        {
            energyDisplay = NodeEnergyDisplay.CreateForNode(this);
        }
    }
    
    /// <summary>
    /// Update the energy display when weight changes
    /// </summary>
    public void UpdateEnergyDisplay()
    {
        if (weight != 0 && energyDisplay == null)
        {
            CreateEnergyDisplay();
        }
        else if (weight == 0 && energyDisplay != null)
        {
            Destroy(energyDisplay.gameObject);
            energyDisplay = null;
        }
        else if (energyDisplay != null)
        {
            energyDisplay.UpdateDisplay();
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
        }
    }
    
    /// <summary>
    /// Remove an outgoing connection from this node
    /// </summary>
    public void RemoveOutgoingConnection(Connection connection)
    {
        outgoingConnections.Remove(connection);
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
    }
}

