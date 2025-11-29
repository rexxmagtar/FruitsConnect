using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Unified display for node information using a single world space canvas
/// Shows energy info (for nodes with weight) and connection slots (for neutral nodes)
/// </summary>
[RequireComponent(typeof(Canvas))]
public class NodeDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseNode node;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private Transform circlesContainer;
    [SerializeField] private GameObject circlePrefab; // Optional prefab for circles (if manually creating)
    
    [Header("Energy Display Settings")]
    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;
    
    [Header("Connection Display Settings")]
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.3f);
    
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
        }
        
        // Try to find node in parent if not assigned
        if (node == null)
        {
            node = GetComponentInParent<BaseNode>();
        }
    }
    
    private void Start()
    {
        InitializeDisplay();
        UpdateDisplay();
    }
    
    /// <summary>
    /// Initialize display elements - find references if not assigned
    /// </summary>
    private void InitializeDisplay()
    {
        if (node == null) return;
        
        // Find energy text if not assigned
        if (energyText == null)
        {
            energyText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Find or create circles (for neutral nodes)
        if (node is NeutralNode)
        {
            FindCircles();
        }
    }
    
    /// <summary>
    /// Find existing circle UI elements or create from prefab if needed
    /// </summary>
    private void FindCircles()
    {
        if (node == null) return;
        
        // Need container for circles
        if (circlesContainer == null)
        {
            // Try to find existing container
            Transform container = transform.Find("CirclesContainer");
            if (container != null)
            {
                circlesContainer = container;
            }
            else if (node is NeutralNode)
            {
                // Create container if we have a prefab to instantiate
                if (circlePrefab != null)
                {
                    GameObject containerObj = new GameObject("CirclesContainer");
                    containerObj.transform.SetParent(transform);
                    circlesContainer = containerObj.transform;
                    
                    RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                    containerRect.sizeDelta = new Vector2(200, 50);
                    containerRect.anchoredPosition = Vector2.zero;
                }
                else
                {
                    return; // No container and no prefab, can't create circles
                }
            }
            else
            {
                return; // Not a neutral node
            }
        }
        
        // Clear and refind circles
        circleImages.Clear();
        
        // Find all Image components in the circles container
        Image[] images = circlesContainer.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if (img.name.StartsWith("Circle_") || img.transform.parent == circlesContainer)
            {
                circleImages.Add(img);
            }
        }
        
        // If no circles found and we have a prefab, create them
        if (circleImages.Count == 0 && circlePrefab != null && node is NeutralNode)
        {
            CreateCirclesFromPrefab();
        }
        
        // Sort by name to ensure correct order
        circleImages.Sort((a, b) => string.Compare(a.name, b.name));
    }
    
    /// <summary>
    /// Create circles from prefab
    /// </summary>
    private void CreateCirclesFromPrefab()
    {
        if (circlePrefab == null || circlesContainer == null || node == null) return;
        
        int maxConnections = node.MaxOutgoingConnections;
        
        for (int i = 0; i < maxConnections; i++)
        {
            GameObject circleObj = Instantiate(circlePrefab, circlesContainer);
            circleObj.name = $"Circle_{i}";
            
            // Get Image component from the instantiated prefab
            Image circleImage = circleObj.GetComponent<Image>();
            if (circleImage == null)
            {
                // If prefab doesn't have Image, try to get it from children
                circleImage = circleObj.GetComponentInChildren<Image>();
            }
            
            if (circleImage != null)
            {
                circleImages.Add(circleImage);
            }
        }
    }
    
    /// <summary>
    /// Update all displays based on node's current state
    /// </summary>
    public void UpdateDisplay()
    {
        if (node == null) return;
        
        UpdateEnergyDisplay();
        UpdateConnectionDisplay();
    }
    
    /// <summary>
    /// Update energy display
    /// </summary>
    private void UpdateEnergyDisplay()
    {
        if (energyText == null) return;
        
        int weight = node.Weight;
        
        // Show/hide energy text based on weight
        if (weight == 0)
        {
            energyText.gameObject.SetActive(false);
        }
        else
        {
            energyText.gameObject.SetActive(true);
            energyText.text = weight > 0 ? $"+{weight}" : $"{weight}";
            energyText.color = weight > 0 ? positiveColor : negativeColor;
        }
    }
    
    /// <summary>
    /// Update connection display
    /// </summary>
    private void UpdateConnectionDisplay()
    {
        // Only update if this is a neutral node
        if (!(node is NeutralNode)) return;
        
        // Find circles if we have a container but no circles found yet
        if (circleImages.Count == 0 && circlesContainer != null)
        {
            FindCircles();
        }
        
        if (circleImages.Count == 0) return;
        
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
        InitializeDisplay();
        UpdateDisplay();
    }
    
    /// <summary>
    /// Refresh circle references (call this after manually adding circles)
    /// </summary>
    public void RefreshCircles()
    {
        FindCircles();
        UpdateDisplay();
    }
    
}

