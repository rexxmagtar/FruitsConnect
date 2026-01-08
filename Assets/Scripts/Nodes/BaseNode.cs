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
    [SerializeField] protected Material grayscaleMaterial;
    
    [Header("Display")]
    [SerializeField] private NodeDisplay nodeDisplay;
    
    [Header("Pulse Animation")]
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem connectionParticles;
    
    // Connection tracking
    protected List<Connection> outgoingConnections = new List<Connection>();
    protected List<Connection> incomingConnections = new List<Connection>();
    
    // Visual state
    private bool isSelected = false;
    private bool isHovered = false;
    
    // Animation tracking
    private Coroutine currentAnimationCoroutine;
    private Vector3 originalScale;
    
    // Store original materials for restoration
    private Material[] originalMainMaterials;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> originalChildMaterials = new System.Collections.Generic.Dictionary<Renderer, Material[]>();
    private bool materialsStored = false;
    
    // Cache grayscale material instances to avoid creating new ones each frame
    private Material[] cachedGrayscaleMainMaterials;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> cachedGrayscaleChildMaterials = new System.Collections.Generic.Dictionary<Renderer, Material[]>();
    
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
        
        // Store original materials (materials should be assigned by now)
        StoreOriginalMaterials();
        
        // Store original scale for animation restoration
        originalScale = transform.localScale;
        
        // Initialize pulse curve if not set (default ease in-out)
        if (pulseCurve == null || pulseCurve.keys.Length == 0)
        {
            pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
        
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
        
        // Play pulse animation when node is selected to start connection
        PlayPulseAnimation();
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
    /// Store original materials for restoration
    /// Called lazily on first use if not already stored
    /// </summary>
    private void StoreOriginalMaterials()
    {
        if (materialsStored) return;
        
        // Store main renderer materials
        if (meshRenderer != null && meshRenderer.materials != null && meshRenderer.materials.Length > 0)
        {
            originalMainMaterials = new Material[meshRenderer.materials.Length];
            for (int i = 0; i < meshRenderer.materials.Length; i++)
            {
                originalMainMaterials[i] = meshRenderer.materials[i];
            }
        }
        
        // Store child renderer materials
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            // Skip the main renderer
            if (renderer == meshRenderer || renderer is ParticleSystemRenderer) continue;
            
            if (renderer.materials != null && renderer.materials.Length > 0)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    materials[i] = renderer.materials[i];
                }
                originalChildMaterials[renderer] = materials;
            }
        }
        
        materialsStored = true;
    }
    
    /// <summary>
    /// Create or get cached grayscale material instance for a given original material
    /// </summary>
    private Material GetGrayscaleMaterialInstance(Material originalMaterial, int materialIndex, bool isMainRenderer)
    {
        if (grayscaleMaterial == null || originalMaterial == null)
        {
            return originalMaterial;
        }
        
        // For main renderer, cache materials
        if (isMainRenderer)
        {
            if (cachedGrayscaleMainMaterials == null || cachedGrayscaleMainMaterials.Length <= materialIndex)
            {
                if (originalMainMaterials != null)
                {
                    cachedGrayscaleMainMaterials = new Material[originalMainMaterials.Length];
                }
            }
            
            // Create cached instance if it doesn't exist
            if (cachedGrayscaleMainMaterials != null && 
                materialIndex < cachedGrayscaleMainMaterials.Length && 
                cachedGrayscaleMainMaterials[materialIndex] == null)
            {
                Material grayscaleInstance = new Material(grayscaleMaterial);
                
                // Copy main texture and color from original
                if (originalMaterial.HasProperty("_MainTex"))
                {
                    grayscaleInstance.SetTexture("_MainTex", originalMaterial.GetTexture("_MainTex"));
                }
                if (originalMaterial.HasProperty("_Color"))
                {
                    grayscaleInstance.SetColor("_Color", originalMaterial.GetColor("_Color"));
                }
                // Set grayscale intensity to full
                grayscaleInstance.SetFloat("_GrayscaleIntensity", 1.0f);
                
                cachedGrayscaleMainMaterials[materialIndex] = grayscaleInstance;
            }
            
            return cachedGrayscaleMainMaterials != null && 
                   materialIndex < cachedGrayscaleMainMaterials.Length ? 
                   cachedGrayscaleMainMaterials[materialIndex] : originalMaterial;
        }
        
        return originalMaterial;
    }
    
    /// <summary>
    /// Get cached grayscale material for child renderer
    /// </summary>
    private Material GetGrayscaleMaterialInstanceForChild(Renderer renderer, Material originalMaterial, int materialIndex)
    {
        if (grayscaleMaterial == null || originalMaterial == null)
        {
            return originalMaterial;
        }
        
        // Initialize cache for this renderer if needed
        if (!cachedGrayscaleChildMaterials.ContainsKey(renderer))
        {
            if (originalChildMaterials.ContainsKey(renderer))
            {
                Material[] originalMats = originalChildMaterials[renderer];
                cachedGrayscaleChildMaterials[renderer] = new Material[originalMats.Length];
            }
            else
            {
                return originalMaterial;
            }
        }
        
        Material[] cachedMats = cachedGrayscaleChildMaterials[renderer];
        
        // Create cached instance if it doesn't exist
        if (cachedMats != null && 
            materialIndex < cachedMats.Length && 
            cachedMats[materialIndex] == null)
        {
            Material grayscaleInstance = new Material(grayscaleMaterial);
            
            // Copy main texture and color from original
            if (originalMaterial.HasProperty("_MainTex"))
            {
                grayscaleInstance.SetTexture("_MainTex", originalMaterial.GetTexture("_MainTex"));
            }
            if (originalMaterial.HasProperty("_Color"))
            {
                grayscaleInstance.SetColor("_Color", originalMaterial.GetColor("_Color"));
            }
            grayscaleInstance.SetFloat("_GrayscaleIntensity", 1.0f);
            
            cachedMats[materialIndex] = grayscaleInstance;
        }
        
        return cachedMats != null && 
               materialIndex < cachedMats.Length ? 
               cachedMats[materialIndex] : originalMaterial;
    }
    
    /// <summary>
    /// Update materials based on connection status to producer
    /// Applies grayscale material when disconnected, restores original when connected
    /// </summary>
    private void UpdateConnectionStatusVisual()
    {
        // Ensure original materials are stored
        if (!materialsStored)
        {
            StoreOriginalMaterials();
        }
        
        // Check if connected to producer
        bool isConnectedToProducer = IsConnectedToProducer();
        
        // Update main renderer
        if (meshRenderer != null && originalMainMaterials != null && originalMainMaterials.Length > 0)
        {
            Material[] newMaterials;
            
            // Get the current base material (could be selected/hover/default from UpdateVisual)
            Material currentBaseMaterial = null;
            if (meshRenderer.materials != null && meshRenderer.materials.Length > 0)
            {
                currentBaseMaterial = meshRenderer.materials[0];
            }
            
            // If no current material or it's not one of our state materials, use original
            if (currentBaseMaterial == null || 
                (currentBaseMaterial != selectedMaterial && 
                 currentBaseMaterial != hoverMaterial && 
                 currentBaseMaterial != defaultMaterial))
            {
                currentBaseMaterial = originalMainMaterials[0];
            }
            
            if (isConnectedToProducer)
            {
                // Connected: use current base material (or original if no state material)
                newMaterials = new Material[originalMainMaterials.Length];
                newMaterials[0] = currentBaseMaterial;
                // Copy remaining original materials if any
                for (int i = 1; i < originalMainMaterials.Length; i++)
                {
                    newMaterials[i] = originalMainMaterials[i];
                }
            }
            else
            {
                // Disconnected: add grayscale material as second material
                if (grayscaleMaterial != null)
                {
                    // Create array with current base material + remaining originals + grayscale material
                    newMaterials = new Material[originalMainMaterials.Length + 1];
                    // Use current base material as first
                    newMaterials[0] = currentBaseMaterial;
                    // Copy remaining original materials if any
                    for (int i = 1; i < originalMainMaterials.Length; i++)
                    {
                        newMaterials[i] = originalMainMaterials[i];
                    }
                    // Add grayscale material as last material
                    newMaterials[originalMainMaterials.Length] = GetGrayscaleMaterialInstance(originalMainMaterials[0], 0, true);
                }
                else
                {
                    // No grayscale material: use current base material
                    newMaterials = new Material[originalMainMaterials.Length];
                    newMaterials[0] = currentBaseMaterial;
                    for (int i = 1; i < originalMainMaterials.Length; i++)
                    {
                        newMaterials[i] = originalMainMaterials[i];
                    }
                }
            }
            
            meshRenderer.materials = newMaterials;
        }
        
        // Update child renderers
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            // Skip the main renderer
            if (renderer == meshRenderer || renderer is ParticleSystemRenderer) continue;
            
            if (originalChildMaterials.ContainsKey(renderer))
            {
                Material[] originalMats = originalChildMaterials[renderer];
                Material[] newMaterials;
                
                if (isConnectedToProducer)
                {
                    // Connected: use original materials
                    newMaterials = new Material[originalMats.Length];
                    for (int i = 0; i < originalMats.Length; i++)
                    {
                        newMaterials[i] = originalMats[i];
                    }
                }
                else
                {
                    // Disconnected: add grayscale material as second material
                    if (grayscaleMaterial != null)
                    {
                        // Create array with original materials + grayscale material
                        newMaterials = new Material[originalMats.Length + 1];
                        // Keep all original materials
                        for (int i = 0; i < originalMats.Length; i++)
                        {
                            newMaterials[i] = originalMats[i];
                        }
                        // Add grayscale material as second material
                        newMaterials[originalMats.Length] = GetGrayscaleMaterialInstanceForChild(renderer, originalMats[0], 0);
                    }
                    else
                    {
                        // No grayscale material: use original materials
                        newMaterials = new Material[originalMats.Length];
                        for (int i = 0; i < originalMats.Length; i++)
                        {
                            newMaterials[i] = originalMats[i];
                        }
                    }
                }
                
                renderer.materials = newMaterials;
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
    
    /// <summary>
    /// Play a pulse animation (scale up then back down)
    /// </summary>
    public void PlayPulseAnimation()
    {
        // Stop any existing animation
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
        
        // Start new animation
        currentAnimationCoroutine = StartCoroutine(PulseAnimationCoroutine());
    }
    
    /// <summary>
    /// Coroutine that performs the pulse animation
    /// </summary>
    private System.Collections.IEnumerator PulseAnimationCoroutine()
    {
        float elapsedTime = 0f;
        float halfDuration = pulseDuration * 0.5f;
        
        // First half: scale up from original to pulseScale
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            float curveValue = pulseCurve.Evaluate(t);
            float currentScale = Mathf.Lerp(1f, pulseScale, curveValue);
            transform.localScale = originalScale * currentScale;
            yield return null;
        }
        
        // Second half: scale down from pulseScale back to original
        elapsedTime = 0f;
        while (elapsedTime < halfDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / halfDuration;
            float curveValue = pulseCurve.Evaluate(1f - t); // Reverse the curve
            float currentScale = Mathf.Lerp(1f, pulseScale, curveValue);
            transform.localScale = originalScale * currentScale;
            yield return null;
        }
        
        // Ensure we're back to original scale
        transform.localScale = originalScale;
        currentAnimationCoroutine = null;
    }
    
    /// <summary>
    /// Play particle system effect when connection is made to this node
    /// </summary>
    public void PlayConnectionParticles()
    {
        if (connectionParticles != null)
        {
            connectionParticles.Play();
        }
    }
    
    /// <summary>
    /// Clean up cached material instances to prevent memory leaks
    /// </summary>
    private void OnDestroy()
    {
        // Clean up cached grayscale materials
        if (cachedGrayscaleMainMaterials != null)
        {
            foreach (Material mat in cachedGrayscaleMainMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
        
        foreach (var kvp in cachedGrayscaleChildMaterials)
        {
            if (kvp.Value != null)
            {
                foreach (Material mat in kvp.Value)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }
        }
    }
}

