using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Boss behavior - handles health, tap damage, vulnerable zones, and animations
/// </summary>
[RequireComponent(typeof(Collider))]
public class Boss : MonoBehaviour
{
    [Header("Boss Configuration")]
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private float hitScaleAmount = 1.2f;
    [SerializeField] private float hitScaleDuration = 0.2f;
    
    [Header("Vulnerable Zone Settings")]
    [SerializeField] private GameObject vulnerableZonePrefab;
    [SerializeField] private float vulnerableZoneSpawnInterval = 5f;
    [SerializeField] private float vulnerableZoneDuration = 3f;
    [SerializeField] private float vulnerableZoneSpawnRadius = 2f;
    [SerializeField] private int maxVulnerableZones = 2;
    
    [Header("Component References")]
    [SerializeField] private BossHealthBar healthBar;
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Animator animator;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip dieSound;
    [SerializeField] private AudioClip escapeSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem hitParticleEffect;
    [SerializeField] private ParticleSystem dieParticleEffect;
    
    [Header("Perfect Hit Feedback")]
    [SerializeField] private GameObject perfectHitSpritePrefab;
    [SerializeField] private float perfectHitSpriteDuration = 0.3f;
    [SerializeField] private float perfectHitSpriteMoveDistance = 2f;
    
    // State
    private int currentHealth;
    private bool isDead = false;
    private bool hasEscaped = false;
    private bool isFighting = false;
    private Vector3 originalScale;
    private Coroutine hitScaleCoroutine;
    private Coroutine vulnerableZoneSpawnCoroutine;
    private List<VulnerableZone> activeVulnerableZones = new List<VulnerableZone>();
    
    // Animation triggers
    private const string TRIGGER_GET_HIT = "GetHit";
    private const string TRIGGER_DIE = "Die";
    private const string TRIGGER_ESCAPE = "Escape";
    
    // Properties
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool HasEscaped => hasEscaped;
    public bool IsFighting => isFighting;
    
    // Events
    public static event System.Action<Boss> OnBossDied;
    public static event System.Action<Boss> OnBossEscaped;
    
    private void Awake()
    {
        // Ensure collider exists
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        
        // Get Animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Get or add AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        
        // Initialize health
        currentHealth = maxHealth;
        
        // Store original scale for hit animation
        originalScale = transform.localScale;
    }
    
    private void Start()
    {
        // Create healthbar if it doesn't exist
        if (healthBar == null)
        {
            CreateHealthBar();
        }
        
        // Initialize health bar if it exists
        if (healthBar != null)
        {
            healthBar.Initialize(this);
        }
    }
    
    /// <summary>
    /// Initialize boss for fight
    /// </summary>
    public void StartBossFight()
    {
        if (isFighting) return;
        
        isFighting = true;
        currentHealth = maxHealth;
        isDead = false;
        hasEscaped = false;
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateDisplay();
        }
        
        // Start spawning vulnerable zones
        if (vulnerableZoneSpawnCoroutine != null)
        {
            StopCoroutine(vulnerableZoneSpawnCoroutine);
        }
        vulnerableZoneSpawnCoroutine = StartCoroutine(VulnerableZoneSpawnLoop());
        
        Debug.Log($"Boss fight started! Health: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// Create healthbar at runtime
    /// </summary>
    private void CreateHealthBar()
    {
        GameObject healthBarObj;
        
        if (healthBarPrefab != null)
        {
            healthBarObj = Instantiate(healthBarPrefab, transform);
            healthBar = healthBarObj.GetComponent<BossHealthBar>();
        }
        else
        {
            // Create healthbar GameObject
            healthBarObj = new GameObject("BossHealthBar");
            healthBarObj.transform.SetParent(transform);
            healthBarObj.transform.localPosition = new Vector3(0, 2f, 0); // Position above boss
            
            // Add Canvas
            Canvas canvas = healthBarObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localScale = Vector3.one * 0.01f; // Scale for world space
            
            // Add CanvasScaler
            UnityEngine.UI.CanvasScaler scaler = healthBarObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            // Add GraphicRaycaster
            healthBarObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add BossHealthBar component
            healthBar = healthBarObj.AddComponent<BossHealthBar>();
        }
        
        if (healthBar == null)
        {
            healthBar = healthBarObj.GetComponent<BossHealthBar>();
        }
        
        // Initialize health bar
        if (healthBar != null)
        {
            healthBar.Initialize(this);
        }
    }
    
    /// <summary>
    /// Handle tap/click on boss - take damage
    /// </summary>
    private void OnMouseDown()
    {
        if (!isFighting || isDead || hasEscaped) return;
        
        // Check if gameplay is enabled (or boss fight is active)
        BossFightManager manager = BossFightManager.Instance;
        if (manager == null || !manager.IsBossFightActive)
        {
            return;
        }
        
        TakeDamage(1, false);
    }
    
    /// <summary>
    /// Take damage from player tap or vulnerable zone
    /// </summary>
    public void TakeDamage(int damage, bool isVulnerableZone = false)
    {
        if (isDead || hasEscaped || !isFighting) return;
        
        // Apply damage multiplier for vulnerable zones
        int finalDamage = isVulnerableZone ? damage * 2 : damage;
        
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Update healthbar
        if (healthBar != null)
        {
            healthBar.UpdateDisplay();
        }
        
        // Trigger get hit animation
        if (animator != null)
        {
            animator.SetTrigger(TRIGGER_GET_HIT);
        }
        
        // Play hit sound
        PlayHitSound();
        
        // Play hit particle effect
        PlayHitParticleEffect();
        
        // Play hit scale animation
        PlayHitScaleAnimation();
        
        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Spawn vulnerable zone around boss
    /// </summary>
    private void SpawnVulnerableZone()
    {
        if (activeVulnerableZones.Count >= maxVulnerableZones) return;
        if (vulnerableZonePrefab == null) return;
        
        // Calculate spawn position around boss
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnOffset = new Vector3(
            Mathf.Cos(angle) * vulnerableZoneSpawnRadius,
            0f,
            Mathf.Sin(angle) * vulnerableZoneSpawnRadius
        );
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        // Spawn vulnerable zone
        GameObject zoneObj = Instantiate(vulnerableZonePrefab, spawnPosition, Quaternion.identity);
        VulnerableZone zone = zoneObj.GetComponent<VulnerableZone>();
        
        if (zone == null)
        {
            zone = zoneObj.AddComponent<VulnerableZone>();
        }
        
        zone.Initialize(this, spawnPosition, vulnerableZoneDuration);
        activeVulnerableZones.Add(zone);
        
        Debug.Log($"Spawned vulnerable zone at {spawnPosition}");
    }
    
    /// <summary>
    /// Coroutine to spawn vulnerable zones at intervals
    /// </summary>
    private IEnumerator VulnerableZoneSpawnLoop()
    {
        while (isFighting && !isDead && !hasEscaped)
        {
            yield return new WaitForSeconds(vulnerableZoneSpawnInterval);
            
            if (isFighting && !isDead && !hasEscaped)
            {
                SpawnVulnerableZone();
            }
        }
    }
    
    /// <summary>
    /// Remove vulnerable zone from active list
    /// </summary>
    public void RemoveVulnerableZone(VulnerableZone zone)
    {
        if (zone != null && activeVulnerableZones.Contains(zone))
        {
            activeVulnerableZones.Remove(zone);
        }
    }
    
    /// <summary>
    /// Show "Perfect" sprite feedback at position
    /// </summary>
    public void ShowPerfectHitFeedback(Vector3 position)
    {
        if (perfectHitSpritePrefab == null) return;
        
        GameObject perfectObj = Instantiate(perfectHitSpritePrefab, position, Quaternion.identity);
        StartCoroutine(DestroyPerfectHitSprite(perfectObj));
    }
    
    /// <summary>
    /// Animate perfect hit sprite: move up and fade away
    /// </summary>
    private IEnumerator DestroyPerfectHitSprite(GameObject spriteObj)
    {
        if (spriteObj == null) yield break;
        
        // Get components
        SpriteRenderer spriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
        Transform spriteTransform = spriteObj.transform;
        
        if (spriteRenderer == null || spriteTransform == null)
        {
            // Fallback: just wait and destroy
            yield return new WaitForSeconds(perfectHitSpriteDuration);
            if (spriteObj != null)
            {
                Destroy(spriteObj);
            }
            yield break;
        }
        
        // Store initial values
        Vector3 startPosition = spriteTransform.position;
        Vector3 endPosition = startPosition + Vector3.up * perfectHitSpriteMoveDistance;
        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        // Animate over duration
        float elapsedTime = 0f;
        while (elapsedTime < perfectHitSpriteDuration)
        {
            if (spriteObj == null) yield break;
            
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / perfectHitSpriteDuration;
            
            // Move up
            spriteTransform.position = Vector3.Lerp(startPosition, endPosition, t);
            
            // Fade out
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }
        
        // Ensure final state
        if (spriteObj != null)
        {
            spriteTransform.position = endPosition;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = endColor;
            }
            Destroy(spriteObj);
        }
    }
    
    /// <summary>
    /// Play scale animation when boss gets hit
    /// </summary>
    private void PlayHitScaleAnimation()
    {
        // Stop any existing hit animation
        if (hitScaleCoroutine != null)
        {
            StopCoroutine(hitScaleCoroutine);
        }
        
        hitScaleCoroutine = StartCoroutine(HitScaleAnimationCoroutine());
    }
    
    /// <summary>
    /// Coroutine for hit scale animation
    /// </summary>
    private IEnumerator HitScaleAnimationCoroutine()
    {
        Vector3 targetScale = originalScale * hitScaleAmount;
        float elapsedTime = 0f;
        
        // Scale up
        while (elapsedTime < hitScaleDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (hitScaleDuration * 0.5f);
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        // Scale back down
        elapsedTime = 0f;
        while (elapsedTime < hitScaleDuration * 0.5f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / (hitScaleDuration * 0.5f);
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        // Ensure we're back to original scale
        transform.localScale = originalScale;
        hitScaleCoroutine = null;
    }
    
    /// <summary>
    /// Play hit sound effect
    /// </summary>
    private void PlayHitSound()
    {
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }
    
    /// <summary>
    /// Handle boss death
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        isFighting = false;
        
        // Stop vulnerable zone spawning
        if (vulnerableZoneSpawnCoroutine != null)
        {
            StopCoroutine(vulnerableZoneSpawnCoroutine);
            vulnerableZoneSpawnCoroutine = null;
        }
        
        // Clear all vulnerable zones
        foreach (var zone in activeVulnerableZones)
        {
            if (zone != null)
            {
                Destroy(zone.gameObject);
            }
        }
        activeVulnerableZones.Clear();
        
        // Trigger die animation
        if (animator != null)
        {
            animator.SetTrigger(TRIGGER_DIE);
        }
        
        // Play die sound
        PlayDieSound();
        
        // Start death sequence
        StartCoroutine(DeathSequence());
    }
    
    /// <summary>
    /// Death sequence: wait for animation, then notify manager
    /// </summary>
    private IEnumerator DeathSequence()
    {
        // Wait for dying animation
        yield return new WaitForSeconds(1f);
        
        if (dieParticleEffect != null)
        {
            dieParticleEffect.Play();
        }
        
        // Hide boss renderer
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Notify manager
        OnBossDied?.Invoke(this);
    }
    
    /// <summary>
    /// Handle boss escape (timer ran out)
    /// </summary>
    public void Escape()
    {
        if (hasEscaped || isDead) return;
        
        hasEscaped = true;
        isFighting = false;
        
        // Stop vulnerable zone spawning
        if (vulnerableZoneSpawnCoroutine != null)
        {
            StopCoroutine(vulnerableZoneSpawnCoroutine);
            vulnerableZoneSpawnCoroutine = null;
        }
        
        // Clear all vulnerable zones
        foreach (var zone in activeVulnerableZones)
        {
            if (zone != null)
            {
                Destroy(zone.gameObject);
            }
        }
        activeVulnerableZones.Clear();
        
        // Trigger escape animation
        if (animator != null)
        {
            animator.SetTrigger(TRIGGER_ESCAPE);
        }
        
        // Play escape sound
        PlayEscapeSound();
        
        // Start escape sequence
        StartCoroutine(EscapeSequence());
    }
    
    /// <summary>
    /// Escape sequence: wait for animation, then notify manager
    /// </summary>
    private IEnumerator EscapeSequence()
    {
        // Wait for escape animation
        yield return new WaitForSeconds(1f);
        
        // Notify manager
        OnBossEscaped?.Invoke(this);
    }
    
    /// <summary>
    /// Play die sound effect
    /// </summary>
    private void PlayDieSound()
    {
        if (dieSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dieSound);
        }
    }
    
    /// <summary>
    /// Play escape sound effect
    /// </summary>
    private void PlayEscapeSound()
    {
        if (escapeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(escapeSound);
        }
    }
    
    /// <summary>
    /// Play hit particle effect
    /// </summary>
    private void PlayHitParticleEffect()
    {
        if (hitParticleEffect != null)
        {
            hitParticleEffect.Play();
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup coroutines
        if (hitScaleCoroutine != null)
        {
            StopCoroutine(hitScaleCoroutine);
        }
        if (vulnerableZoneSpawnCoroutine != null)
        {
            StopCoroutine(vulnerableZoneSpawnCoroutine);
        }
        
        // Cleanup vulnerable zones
        foreach (var zone in activeVulnerableZones)
        {
            if (zone != null)
            {
                Destroy(zone.gameObject);
            }
        }
    }
}
