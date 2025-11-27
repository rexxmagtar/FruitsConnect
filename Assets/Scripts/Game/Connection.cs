using UnityEngine;

/// <summary>
/// Represents a connection between two nodes using LineRenderer
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Connection : MonoBehaviour
{
    [Header("Connection References")]
    private BaseNode fromNode;
    private BaseNode toNode;
    
    [Header("Visual Components")]
    private LineRenderer lineRenderer;
    private BoxCollider boxCollider;
    
    // Visual settings (set by ConnectionManager)
    private float lineWidth = 0.1f;
    private Color lineColor = Color.yellow;
    
    public BaseNode FromNode => fromNode;
    public BaseNode ToNode => toNode;
    
    private void Awake()
    {
        // Get LineRenderer if not assigned
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        
        // Get or add BoxCollider for click detection
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }
        }
    }
    
    /// <summary>
    /// Initialize the connection between two nodes with custom visual settings
    /// </summary>
    public void Initialize(BaseNode from, BaseNode to, float width, Color color)
    {
        fromNode = from;
        toNode = to;
        lineWidth = width;
        lineColor = color;
        
        // Setup LineRenderer
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            
            // Create material if needed
            if (lineRenderer.material == null)
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            lineRenderer.material.color = lineColor;
            
            // Disable shadows and lighting
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
        
        // Update visual immediately
        UpdateVisual();
        UpdateCollider();
    }
    
    /// <summary>
    /// Update line positions between nodes
    /// </summary>
    public void UpdateVisual()
    {
        if (fromNode == null || toNode == null || lineRenderer == null)
            return;
        
        lineRenderer.SetPosition(0, fromNode.transform.position);
        lineRenderer.SetPosition(1, toNode.transform.position);
    }
    
    /// <summary>
    /// Update box collider to match line for click detection
    /// </summary>
    private void UpdateCollider()
    {
        if (fromNode == null || toNode == null || boxCollider == null)
            return;
        
        Vector3 fromPos = fromNode.transform.position;
        Vector3 toPos = toNode.transform.position;
        
        // Calculate center and size
        Vector3 centerWorld = (fromPos + toPos) / 2f;
        float distance = Vector3.Distance(fromPos, toPos);
        
        // Calculate direction for rotation
        Vector3 direction = toPos - fromPos;
        
        if (direction != Vector3.zero)
        {
            // Position transform at center point
            transform.position = centerWorld;
            
            // Rotate to align with line direction
            transform.rotation = Quaternion.LookRotation(direction);
            
            // Set collider local center to zero (since transform is now at center)
            boxCollider.center = Vector3.zero;
            
            // Set collider size (elongated along Z axis which is the direction)
            boxCollider.size = new Vector3(lineWidth * 3f, lineWidth * 3f, distance);
        }
    }
    
    /// <summary>
    /// Handle mouse click on connection to remove it
    /// </summary>
    private void OnMouseDown()
    {
        ConnectionManager manager = ConnectionManager.Instance;
        if (manager != null)
        {
            manager.RemoveConnection(this);
        }
    }
    
    /// <summary>
    /// Clean up and destroy connection
    /// </summary>
    public void DestroyConnection()
    {
        // Remove from nodes
        if (fromNode != null)
        {
            fromNode.RemoveOutgoingConnection(this);
        }
        
        if (toNode != null)
        {
            toNode.RemoveIncomingConnection(this);
        }
        
        // Destroy GameObject
        Destroy(gameObject);
    }
    
    private void LateUpdate()
    {
        // Update visual and collider each frame to handle node movement (if any)
        UpdateVisual();
        UpdateCollider();
    }
}

