using UnityEngine;
using System.Collections;

/// <summary>
/// Vulnerable zone that appears around boss - deals 2x damage when tapped
/// </summary>
[RequireComponent(typeof(Collider))]
public class VulnerableZone : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScaleMin = 0.9f;
    [SerializeField] private float pulseScaleMax = 1.1f;
    [SerializeField] private Color glowColor = Color.yellow;
    
    [Header("References")]
    [SerializeField] private Renderer zoneRenderer;
    [SerializeField] private Light glowLight;
    
    private Boss boss;
    private float duration;
    private float elapsedTime;
    private Vector3 originalScale;
    private bool isDestroyed = false;
    
    private void Awake()
    {
        // Ensure collider exists
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<SphereCollider>();
        }
        
        // Get renderer if not assigned
        if (zoneRenderer == null)
        {
            zoneRenderer = GetComponent<Renderer>();
        }
        
        // Store original scale
        originalScale = transform.localScale;
    }
    
    /// <summary>
    /// Initialize vulnerable zone
    /// </summary>
    public void Initialize(Boss bossRef, Vector3 position, float zoneDuration)
    {
        boss = bossRef;
        duration = zoneDuration;
        elapsedTime = 0f;
        transform.position = position;
        
        // Setup visual feedback
        SetupVisuals();
        
        // Start auto-destroy coroutine
        StartCoroutine(AutoDestroyCoroutine());
    }
    
    /// <summary>
    /// Setup visual feedback for vulnerable zone
    /// </summary>
    private void SetupVisuals()
    {
        // Add glow effect if renderer exists
        if (zoneRenderer != null)
        {
            // Create material with glow effect
            Material mat = zoneRenderer.material;
            if (mat != null)
            {
                mat.color = glowColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glowColor);
            }
        }
        
        // Add point light for glow if not assigned
        if (glowLight == null)
        {
            GameObject lightObj = new GameObject("VulnerableZoneLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            glowLight = lightObj.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = glowColor;
            glowLight.range = 3f;
            glowLight.intensity = 2f;
        }
    }
    
    private void Update()
    {
        if (isDestroyed) return;
        
        // Pulse animation
        float pulse = Mathf.Lerp(pulseScaleMin, pulseScaleMax, 
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        transform.localScale = originalScale * pulse;
        
        // Rotate slowly for visual effect
        transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        
        // Face camera
        if (Camera.main != null)
        {
            Vector3 directionToCamera = Camera.main.transform.position - transform.position;
            directionToCamera.y = 0; // Keep upright
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
    
    /// <summary>
    /// Handle tap/click on vulnerable zone
    /// </summary>
    private void OnMouseDown()
    {
        if (isDestroyed || boss == null) return;
        
        // Check if boss fight is active
        BossFightManager manager = BossFightManager.Instance;
        if (manager == null || !manager.IsBossFightActive)
        {
            return;
        }
        
        // Deal 2x damage to boss
        boss.TakeDamage(1, true);
        
        // Show "Perfect" feedback at this position
        boss.ShowPerfectHitFeedback(transform.position);
        
        // Destroy zone
        DestroyZone();
    }
    
    /// <summary>
    /// Auto-destroy zone after duration
    /// </summary>
    private IEnumerator AutoDestroyCoroutine()
    {
        yield return new WaitForSeconds(duration);
        DestroyZone();
    }
    
    /// <summary>
    /// Destroy the vulnerable zone
    /// </summary>
    private void DestroyZone()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        
        // Notify boss
        if (boss != null)
        {
            boss.RemoveVulnerableZone(this);
        }
        
        // Destroy GameObject
        Destroy(gameObject);
    }
}
