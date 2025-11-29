using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Displays connection slots above a neutral node using world space canvas
/// Shows circles - filled for used connections, empty for available slots
/// </summary>
[RequireComponent(typeof(Canvas))]
public class NodeConnectionDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseNode node;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform circlesContainer;
    
    [Header("Display Settings")]
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.3f); // Semi-transparent white
    [SerializeField] private Vector3 offsetFromNode = new Vector3(0, 1.0f, 0);
    [SerializeField] private float canvasScale = 0.01f;
    [SerializeField] private float circleSize = 20f;
    [SerializeField] private float circleSpacing = 25f;
    
    private List<Image> circleImages = new List<Image>();
    
    private void Awake()
    {
        // Get canvas if not assigned
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }
        
        // Setup canvas as world space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            transform.localScale = Vector3.one * canvasScale;
        }
        
        // Try to find node in parent if not assigned
        if (node == null)
        {
            node = GetComponentInParent<BaseNode>();
        }
        
        // Create container if not assigned
        if (circlesContainer == null)
        {
            GameObject containerObj = new GameObject("CirclesContainer");
            containerObj.transform.SetParent(transform);
            circlesContainer = containerObj.transform;
            
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(200, 50);
            containerRect.anchoredPosition = Vector2.zero;
        }
    }
    
    private void Start()
    {
        CreateCircles();
        UpdateDisplay();
    }
    
    private void LateUpdate()
    {
        // Update position to stay above node
        if (node != null)
        {
            transform.position = node.transform.position + offsetFromNode;
            
            // Face camera
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }
        }
    }
    
    /// <summary>
    /// Create circle UI elements for each connection slot
    /// </summary>
    private void CreateCircles()
    {
        if (node == null || circlesContainer == null) return;
        
        // Clear existing circles
        foreach (Image img in circleImages)
        {
            if (img != null) Destroy(img.gameObject);
        }
        circleImages.Clear();
        
        int maxConnections = node.MaxOutgoingConnections;
        
        // Create circles for each slot
        for (int i = 0; i < maxConnections; i++)
        {
            GameObject circleObj = new GameObject($"Circle_{i}");
            circleObj.transform.SetParent(circlesContainer);
            
            // Add Image component for circle
            Image circleImage = circleObj.AddComponent<Image>();
            
            // Create a simple circle sprite
            // Try Unity's built-in circle sprite first
            circleImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            if (circleImage.sprite == null)
            {
                // Fallback: create a simple circle sprite programmatically
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                Color[] colors = new Color[size * size];
                float center = size / 2f;
                float radius = center - 2f;
                
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        if (dist <= radius)
                        {
                            colors[y * size + x] = Color.white;
                        }
                        else
                        {
                            colors[y * size + x] = Color.clear;
                        }
                    }
                }
                
                tex.SetPixels(colors);
                tex.Apply();
                circleImage.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            }
            
            circleImage.type = Image.Type.Simple;
            circleImage.preserveAspect = true;
            
            // Setup rect transform
            RectTransform rectTransform = circleObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(circleSize, circleSize);
            
            // Position circles horizontally centered
            float totalWidth = (maxConnections - 1) * circleSpacing;
            float startX = -totalWidth / 2f;
            rectTransform.anchoredPosition = new Vector2(startX + i * circleSpacing, 0);
            
            circleImages.Add(circleImage);
        }
    }
    
    /// <summary>
    /// Update the display based on node's current connections
    /// </summary>
    public void UpdateDisplay()
    {
        if (node == null || circleImages.Count == 0) return;
        
        int usedConnections = node.OutgoingConnections.Count;
        int maxConnections = node.MaxOutgoingConnections;
        
        // Update each circle
        for (int i = 0; i < circleImages.Count && i < maxConnections; i++)
        {
            if (circleImages[i] != null)
            {
                // Fill circle if connection slot is used, otherwise leave empty
                if (i < usedConnections)
                {
                    circleImages[i].color = filledColor;
                }
                else
                {
                    circleImages[i].color = emptyColor;
                }
            }
        }
    }
    
    /// <summary>
    /// Set the node this display is tracking
    /// </summary>
    public void SetNode(BaseNode targetNode)
    {
        node = targetNode;
        CreateCircles();
        UpdateDisplay();
    }
    
    /// <summary>
    /// Create a node connection display for a given neutral node
    /// </summary>
    public static NodeConnectionDisplay CreateForNode(BaseNode node)
    {
        // Only create for neutral nodes
        if (!(node is NeutralNode))
        {
            return null;
        }
        
        // Create canvas game object
        GameObject displayObj = new GameObject($"{node.NodeID}_ConnectionDisplay");
        displayObj.transform.SetParent(node.transform);
        displayObj.transform.localPosition = Vector3.zero;
        
        // Add canvas
        Canvas canvas = displayObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Add canvas scaler
        UnityEngine.UI.CanvasScaler scaler = displayObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // Add graphic raycaster
        displayObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Add and setup display component
        NodeConnectionDisplay display = displayObj.AddComponent<NodeConnectionDisplay>();
        display.node = node;
        display.canvas = canvas;
        
        return display;
    }
}

