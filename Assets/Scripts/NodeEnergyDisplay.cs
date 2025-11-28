using UnityEngine;
using TMPro;

/// <summary>
/// Displays energy information above a node using world space canvas
/// Shows weight value with color coding (green for positive, red for negative)
/// </summary>
[RequireComponent(typeof(Canvas))]
public class NodeEnergyDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseNode node;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private Canvas canvas;
    
    [Header("Display Settings")]
    [SerializeField] private Color positiveColor = Color.green;
    [SerializeField] private Color negativeColor = Color.red;
    [SerializeField] private Vector3 offsetFromNode = new Vector3(0, 1.2f, 0);
    [SerializeField] private float canvasScale = 0.01f;
    
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
    }
    
    private void Start()
    {
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
    /// Update the display based on node's current weight
    /// </summary>
    public void UpdateDisplay()
    {
        if (node == null || energyText == null)
            return;
        
        int weight = node.Weight;
        
        // Hide display if weight is 0 (producers/consumers)
        if (weight == 0)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // Format text with + or - sign
        energyText.text = weight > 0 ? $"+{weight}" : $"{weight}";
        
        // Set color based on sign
        energyText.color = weight > 0 ? positiveColor : negativeColor;
    }
    
    /// <summary>
    /// Set the node this display is tracking
    /// </summary>
    public void SetNode(BaseNode targetNode)
    {
        node = targetNode;
        UpdateDisplay();
    }
    
    /// <summary>
    /// Create a node energy display for a given node
    /// </summary>
    public static NodeEnergyDisplay CreateForNode(BaseNode node)
    {
        // Create canvas game object
        GameObject displayObj = new GameObject($"{node.NodeID}_EnergyDisplay");
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
        
        // Create text game object
        GameObject textObj = new GameObject("EnergyText");
        textObj.transform.SetParent(displayObj.transform);
        
        // Add TextMeshPro component
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 48;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        
        // Setup rect transform
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 100);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Add and setup display component
        NodeEnergyDisplay display = displayObj.AddComponent<NodeEnergyDisplay>();
        display.node = node;
        display.energyText = text;
        display.canvas = canvas;
        
        display.UpdateDisplay();
        
        return display;
    }
}


