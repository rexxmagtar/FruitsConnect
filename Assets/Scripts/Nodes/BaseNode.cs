using System.Collections.Generic;
using UnityEngine;

public abstract class BaseNode : MonoBehaviour
{
    [Header("Node Configuration")]
    [SerializeField] protected string nodeID;
    [SerializeField] protected int maxOutgoingConnections = 1;
    
    [Header("Energy System")]
    [SerializeField] protected int weight = 0;
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
    [Header("Audio")]
    [SerializeField] private AudioClip activatedSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem connectionParticles;
    
    [Header("Delivery System")]
    [SerializeField] private int requiredDeliveries = 3; // Number of animated objects needed to activate this node
    [SerializeField] private float arrivalDetectionDistance = 1f; // Distance threshold for detecting when animation object reaches this node
    private int currentDeliveries = 0; // Current number of deliveries received
    
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
    
    // Mesh bounds for shader (cached)
    private float meshMinY = -0.5f;
    private float meshMaxY = 0.5f;
    
    // Smooth animation for delivery progress visualization
    private float currentVisualEffectPower = 1.0f; // Current visual effect power (for smooth interpolation)
    [SerializeField] private float progressAnimationSpeed = 2.0f; // Speed of progress animation
    
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
            weight = value;
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
    
    public float ArrivalDetectionDistance => arrivalDetectionDistance;
    
    // Capture state (blocks player interaction when captured by monster)
    private bool isCaptured = false;
    public bool IsCaptured 
    { 
        get => isCaptured;
        set => isCaptured = value;
    }
    
    // Delivery properties
    public int RequiredDeliveries => requiredDeliveries;
    public int CurrentDeliveries => currentDeliveries;
    public float DeliveryProgress => requiredDeliveries > 0 ? (float)currentDeliveries / requiredDeliveries : 0f;
    public bool IsFullyDelivered => currentDeliveries >= requiredDeliveries;
    
    /// <summary>
    /// Check if this node is activated and can be clicked/interacted with
    /// ProducerNodes are always activated (unless captured)
    /// Other nodes must be fully delivered to be activated
    /// </summary>
    public bool IsActivated()
    {
        // Don't allow interaction if node is captured
        if (isCaptured) return false;
        
        // ProducerNodes are always activated (they don't need deliveries)
        if (this is ProducerNode) return true;
        
        // Other nodes must be fully delivered to be activated
        return IsFullyDelivered;
    }
    
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
        
        // Initialize visual effect power based on current progress
        currentVisualEffectPower = 1.0f - DeliveryProgress;
    }
    
    private void Update()
    {
        // Calculate target effect power based on connection status
        // If disconnected or captured, always target full grayscale (1.0)
        // Otherwise, target based on delivery progress
        float targetEffectPower;
        
        // Check if connected to producer AND not captured
        // For grayscale progress, we want to show progress incrementally as items are delivered
        // So we check if connected to producer (path-wise), not if fully delivered
        bool isConnectedToProducer = !isCaptured && IsConnectedToProducer();
        
        if (isConnectedToProducer)
        {
            // Connected: target based on delivery progress (updates incrementally as items are delivered)
            targetEffectPower = 1.0f - DeliveryProgress;
        }
        else
        {
            // Disconnected or captured: always full grayscale
            targetEffectPower = 1.0f;
        }
        
        // Only update if there's a difference (avoid unnecessary updates)
        if (Mathf.Abs(currentVisualEffectPower - targetEffectPower) > 0.001f)
        {
            // Smoothly interpolate towards target
            currentVisualEffectPower = Mathf.Lerp(currentVisualEffectPower, targetEffectPower, 
                progressAnimationSpeed * Time.deltaTime);
            
            // Update shader with interpolated value
            UpdateGrayscaleMaterialProgress();
        }
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
    /// Calculate mesh bounds for shader vertical progress (in world space)
    /// </summary>
    private void CalculateMeshBounds()
    {
        // Try to get mesh bounds from MeshRenderer (world space)
        if (meshRenderer != null)
        {
            Bounds bounds = meshRenderer.bounds;
            meshMinY = bounds.min.y;
            meshMaxY = bounds.max.y;
        }
        else
        {
            // Try to get from MeshFilter and convert to world space
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds localBounds = meshFilter.sharedMesh.bounds;
                // Convert local bounds to world space
                Vector3 worldMin = transform.TransformPoint(localBounds.min);
                Vector3 worldMax = transform.TransformPoint(localBounds.max);
                meshMinY = worldMin.y;
                meshMaxY = worldMax.y;
            }
            else
            {
                // Default bounds if no mesh found (use world position as center)
                float centerY = transform.position.y;
                meshMinY = centerY - 0.5f;
                meshMaxY = centerY + 0.5f;
            }
        }
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
                // Set shader properties for new grayscale shader
                if (grayscaleInstance.HasProperty("_Brightness"))
                {
                    grayscaleInstance.SetFloat("_Brightness", 1.0f);
                }
                if (grayscaleInstance.HasProperty("_Contrast"))
                {
                    grayscaleInstance.SetFloat("_Contrast", 1.0f);
                }
                // Set effect power (inverted delivery progress: 1.0 = all grayscale, 0.0 = all color)
                if (grayscaleInstance.HasProperty("_EffectPower"))
                {
                    grayscaleInstance.SetFloat("_EffectPower", 1.0f - DeliveryProgress);
                }
                // Set mesh bounds for vertical progress (world space)
                if (grayscaleInstance.HasProperty("_MeshMinY"))
                {
                    grayscaleInstance.SetFloat("_MeshMinY", meshMinY);
                }
                if (grayscaleInstance.HasProperty("_MeshMaxY"))
                {
                    grayscaleInstance.SetFloat("_MeshMaxY", meshMaxY);
                }
                
                cachedGrayscaleMainMaterials[materialIndex] = grayscaleInstance;
            }
            
            // Get the material (either newly created or cached)
            Material resultMat = cachedGrayscaleMainMaterials != null && 
                                 materialIndex < cachedGrayscaleMainMaterials.Length ? 
                                 cachedGrayscaleMainMaterials[materialIndex] : originalMaterial;
            
            // Always update effect power and mesh bounds on the material
            if (resultMat != null)
            {
                if (resultMat.HasProperty("_EffectPower"))
                {
                    resultMat.SetFloat("_EffectPower", 1.0f - DeliveryProgress);
                }
                if (resultMat.HasProperty("_MeshMinY"))
                {
                    resultMat.SetFloat("_MeshMinY", meshMinY);
                }
                if (resultMat.HasProperty("_MeshMaxY"))
                {
                    resultMat.SetFloat("_MeshMaxY", meshMaxY);
                }
            }
            
            return resultMat;
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
            // Set shader properties for new grayscale shader
            if (grayscaleInstance.HasProperty("_Brightness"))
            {
                grayscaleInstance.SetFloat("_Brightness", 1.0f);
            }
            if (grayscaleInstance.HasProperty("_Contrast"))
            {
                grayscaleInstance.SetFloat("_Contrast", 1.0f);
            }
            // Set effect power (inverted delivery progress: 1.0 = all grayscale, 0.0 = all color)
            if (grayscaleInstance.HasProperty("_EffectPower"))
            {
                grayscaleInstance.SetFloat("_EffectPower", 1.0f - DeliveryProgress);
            }
            // Set mesh bounds for vertical progress (world space)
            if (grayscaleInstance.HasProperty("_MeshMinY"))
            {
                grayscaleInstance.SetFloat("_MeshMinY", meshMinY);
            }
            if (grayscaleInstance.HasProperty("_MeshMaxY"))
            {
                grayscaleInstance.SetFloat("_MeshMaxY", meshMaxY);
            }
            
            cachedMats[materialIndex] = grayscaleInstance;
        }
        
        // Get the material (either newly created or cached)
        Material resultMat = cachedMats != null && 
                             materialIndex < cachedMats.Length ? 
                             cachedMats[materialIndex] : originalMaterial;
        
        // Always update delivery progress and mesh bounds on the material
        if (resultMat != null)
        {
            if (resultMat.HasProperty("_DeliveryProgress"))
            {
                resultMat.SetFloat("_DeliveryProgress", DeliveryProgress);
            }
            if (resultMat.HasProperty("_MeshMinY"))
            {
                resultMat.SetFloat("_MeshMinY", meshMinY);
            }
            if (resultMat.HasProperty("_MeshMaxY"))
            {
                resultMat.SetFloat("_MeshMaxY", meshMaxY);
            }
        }
        
        return resultMat;
    }
    
    /// <summary>
    /// Update delivery progress on all grayscale material instances
    /// </summary>
    private void UpdateGrayscaleMaterialProgress()
    {
        // Use current visual effect power (smoothly interpolated value)
        float effectPower = currentVisualEffectPower;
        
        // Update main renderer grayscale materials
        if (cachedGrayscaleMainMaterials != null)
        {
            foreach (Material mat in cachedGrayscaleMainMaterials)
            {
                if (mat != null)
                {
                    if (mat.HasProperty("_EffectPower"))
                    {
                        mat.SetFloat("_EffectPower", effectPower);
                    }
                    if (mat.HasProperty("_MeshMinY"))
                    {
                        mat.SetFloat("_MeshMinY", meshMinY);
                    }
                    if (mat.HasProperty("_MeshMaxY"))
                    {
                        mat.SetFloat("_MeshMaxY", meshMaxY);
                    }
                }
            }
        }
        
        // Update child renderer grayscale materials
        foreach (var kvp in cachedGrayscaleChildMaterials)
        {
            if (kvp.Value != null)
            {
                foreach (Material mat in kvp.Value)
                {
                    if (mat != null)
                    {
                        if (mat.HasProperty("_EffectPower"))
                        {
                            mat.SetFloat("_EffectPower", effectPower);
                        }
                        if (mat.HasProperty("_MeshMinY"))
                        {
                            mat.SetFloat("_MeshMinY", meshMinY);
                        }
                        if (mat.HasProperty("_MeshMaxY"))
                        {
                            mat.SetFloat("_MeshMaxY", meshMaxY);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Update materials based on connection status to producer
    /// Applies grayscale material when disconnected or captured, restores original when connected and not captured
    /// </summary>
    private void UpdateConnectionStatusVisual()
    {
        // Ensure original materials are stored
        if (!materialsStored)
        {
            StoreOriginalMaterials();
        }
        
        // Check if connected to producer AND not captured
        // Producers are always considered connected (don't need deliveries)
        // Other nodes must be fully delivered to show as connected
        // If captured, always show grayscale (even producers)
        bool isConnectedToProducer = !isCaptured && IsConnectedToProducer() && (this is ProducerNode || IsFullyDelivered);
        
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
                    // GetGrayscaleMaterialInstance already updates delivery progress
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
                        // GetGrayscaleMaterialInstanceForChild already updates delivery progress
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
        
        // Recalculate mesh bounds in case transform changed (shader uses world space)
        CalculateMeshBounds();
        
        // Update delivery progress on all grayscale materials
        UpdateGrayscaleMaterialProgress();
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
    /// Called when an animated object is delivered to this node
    /// Returns true if the node becomes fully activated after this delivery
    /// </summary>
    public bool OnDeliveryReceived()
    {
        if (IsFullyDelivered)
        {
            return false; // Already fully delivered
        }
        
        currentDeliveries++;
        currentDeliveries = Mathf.Min(currentDeliveries, requiredDeliveries);
        
        // Update visuals to show progress
        RefreshConnectionStatusVisual();
        
        // Notify MapShaderController to update shader with new progress
        MapShaderController mapShaderController = FindFirstObjectByType<MapShaderController>();
        if (mapShaderController != null)
        {
            mapShaderController.UpdateShaderProperties();
        }
        
        // Check if node should be activated now
        if (IsFullyDelivered)
        {
            // Node is now fully delivered - activate it
            ActivateNode();
            
            // If this is a consumer node, check win condition after activation
            if (this is ConsumerNode)
            {
                GameController gameController = GameController.Instance;
                if (gameController != null)
                {
                    gameController.CheckWinCondition();
                }
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Activate the node (apply energy, etc.)
    /// Called when node receives enough deliveries
    /// Note: Energy may already be applied when connection is created (if connected to producer)
    /// This check ensures energy is only applied once, even with multiple connections
    /// </summary>
    protected virtual void ActivateNode()
    {
        // Apply energy if node has connections and energy not yet applied
        // Energy is typically applied when connection is created, but this ensures it's applied once
        if (incomingConnections.Count > 0 && !isEnergyApplied)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                // Apply energy (positive weight = gain, negative weight = lose)
                gameController.ModifyEnergy(weight);
                isEnergyApplied = true;
                
                Debug.Log($"Node {nodeID} activated! Applied energy {weight} after receiving {currentDeliveries} deliveries.");
            }
        }
        
        // Trigger particle effect when node is fully activated
        PlayConnectionParticles();
        
        // Trigger pulse animation when node is fully activated
        PlayPulseAnimation();
        
        // Update visuals
        RefreshConnectionStatusVisual();

        PlayActivatedSound();
        
        // Notify MapShaderController
        MapShaderController mapShaderController = FindFirstObjectByType<MapShaderController>();
        if (mapShaderController != null)
        {
            mapShaderController.UpdateShaderProperties();
        }
    }
    
    private void PlayActivatedSound()
    {
        if (activatedSound != null)
        {
            audioSource.PlayOneShot(activatedSound);
        }
    }
    /// <summary>
    /// Reset delivery count (for level reset)
    /// </summary>
    public virtual void ResetDeliveries()
    {
        currentDeliveries = 0;
        currentVisualEffectPower = 1.0f; // Reset visual to all grayscale
        RefreshConnectionStatusVisual();
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
        // Don't allow interaction if node is not activated
        if (!IsActivated()) return;
        
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
        // OnMouseDrag is called on the source node being dragged from
        // We should only allow dragging from activated nodes
        if (!IsActivated()) return;
        
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
        // Always notify controller for drag over detection (even for inactive nodes)
        // This allows dragging TO inactive nodes from active nodes
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.OnNodeHoverEnter(this);
        }
        
        // Only show visual hover effect if node is activated
        // Inactive nodes can still be connection targets, but won't show hover visual
        if (IsActivated())
        {
            OnHover();
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
        ResetDeliveries();
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

