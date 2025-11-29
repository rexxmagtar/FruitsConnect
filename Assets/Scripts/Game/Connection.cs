using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Animation Settings")]
    [SerializeField] private GameObject animationPrefab;
    [SerializeField] private float animationSpeed = 2f; // Units per second
    [SerializeField] private float spawnInterval = 0.5f; // Time between spawning new objects
    [SerializeField] private bool isAnimating = false;
    
    // Active animation objects
    private List<GameObject> activeAnimationObjects = new List<GameObject>();
    private Coroutine animationCoroutine;
    
    public BaseNode FromNode => fromNode;
    public BaseNode ToNode => toNode;
    
    private void Awake()
    {
        // Get LineRenderer if not assigned
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }
        
        // CRITICAL: Ensure LineRenderer uses world space BEFORE any positions are set
        // This must be set early to ensure all positions are interpreted correctly
        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
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
    public void Initialize(BaseNode from, BaseNode to, float width, Color color, GameObject animPrefab = null)
    {
        fromNode = from;
        toNode = to;
        lineWidth = width;
        lineColor = color;
        animationPrefab = animPrefab;
        
        // Setup LineRenderer
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            
            // Ensure LineRenderer uses world space (default, but explicit for clarity)
            lineRenderer.useWorldSpace = true;
            
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
        
        // Start animation if prefab is provided
        if (animationPrefab != null)
        {
            StartAnimation();
        }
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


            transform.rotation = Quaternion.LookRotation(direction);
            
            // Set collider local center to zero (since transform is now at center)
            boxCollider.center = Vector3.zero;
            
            // Set collider size (elongated along Z axis which is the direction)
            boxCollider.size = new Vector3(lineWidth * 1f, lineWidth * 1f, distance);
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
    /// Start the connection animation
    /// </summary>
    private void StartAnimation()
    {
        if (animationPrefab == null || fromNode == null || toNode == null)
            return;
        
        if (isAnimating)
            return;
        
        isAnimating = true;
        animationCoroutine = StartCoroutine(AnimationLoop());
    }
    
    /// <summary>
    /// Stop the connection animation
    /// </summary>
    private void StopAnimation()
    {
        isAnimating = false;
        
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        // Clean up all active animation objects
        CleanupAnimationObjects();
    }
    
    /// <summary>
    /// Animation loop that spawns objects periodically
    /// </summary>
    private IEnumerator AnimationLoop()
    {
        while (isAnimating && fromNode != null && toNode != null)
        {
            SpawnAndAnimateObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }
    
    /// <summary>
    /// Spawn an animation object and start it moving along the connection
    /// </summary>
    private void SpawnAndAnimateObject()
    {
        if (animationPrefab == null || fromNode == null || toNode == null)
            return;
        
        // Spawn object at the from node position
        // Don't parent to Connection transform to avoid rotation/position issues
        // Parent to ConnectionManager or scene root instead
        GameObject animObject = Instantiate(animationPrefab, fromNode.transform.position, Quaternion.identity);
        
        // Parent to ConnectionManager's transform (or null for scene root) to keep world space positioning
        Transform parentTransform = ConnectionManager.Instance != null ? ConnectionManager.Instance.transform : null;
        if (parentTransform != null)
        {
            animObject.transform.SetParent(parentTransform, true); // true = keep world position
        }
        
        activeAnimationObjects.Add(animObject);
        
        // Start coroutine to move the object along the connection
        StartCoroutine(MoveObjectAlongConnection(animObject));
    }
    
    /// <summary>
    /// Move an object along the connection line from node A to node B
    /// Uses the SAME positions as LineRenderer (fromNode.position to toNode.position)
    /// </summary>
    private IEnumerator MoveObjectAlongConnection(GameObject obj)
    {
        if (obj == null || fromNode == null || toNode == null)
            yield break;
        
        float progress = 0f; // Progress from 0 (start) to 1 (end)
        
        while (obj != null && fromNode != null && toNode != null && progress < 1f)
        {
            // Use SAME positions as LineRenderer: fromNode.position and toNode.position
            Vector3 startPos = fromNode.transform.position;
            Vector3 endPos = toNode.transform.position;
            float currentDistance = Vector3.Distance(startPos, endPos);
            
            if (currentDistance > 0.01f)
            {
                // Calculate progress increment
                float progressIncrement = (animationSpeed * Time.deltaTime) / currentDistance;
                progress += progressIncrement;
                progress = Mathf.Clamp01(progress);
                
                // Move along the line using same positions as LineRenderer
                obj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            }
            else
            {
                obj.transform.position = endPos;
                progress = 1f;
            }
            
            yield return null;
        }
        
        // Ensure object reaches destination
        if (obj != null && toNode != null)
        {
            obj.transform.position = toNode.transform.position;
        }
        
        // Clean up
        if (obj != null)
        {
            activeAnimationObjects.Remove(obj);
            Destroy(obj);
        }
    }
    
    /// <summary>
    /// Clean up all active animation objects
    /// </summary>
    private void CleanupAnimationObjects()
    {
        foreach (GameObject obj in activeAnimationObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        activeAnimationObjects.Clear();
    }
    
    /// <summary>
    /// Clean up and destroy connection
    /// </summary>
    public void DestroyConnection()
    {
        // Stop animation
        StopAnimation();
        
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
    
    private void OnDestroy()
    {
        // Clean up animation objects when connection is destroyed
        StopAnimation();
    }
}

