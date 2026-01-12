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
    [SerializeField] private List<Transform> vulnerableZoneSpawnPoints = new List<Transform>();
    [SerializeField] private int maxVulnerableZones = 2;
    
    [Header("Component References")]
    [SerializeField] private BossHealthBar healthBar;
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Animator animator;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip vulnerableZoneHitSound;
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
    
    [Header("Hit Effect Pool")]
    [SerializeField] private BossHitEffectPool hitEffectPool;
    
    // State
    private int currentHealth;
    private bool isDead = false;
    private bool hasEscaped = false;
    private bool isFighting = false;
    private Vector3 originalScale;
    private Coroutine hitScaleCoroutine;
    private Coroutine vulnerableZoneSpawnCoroutine;
    private List<VulnerableZone> activeVulnerableZones = new List<VulnerableZone>();
    private Dictionary<Transform, VulnerableZone> occupiedSpawnPoints = new Dictionary<Transform, VulnerableZone>();
    
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
        
        // Initialize hit effect pool if available
        if (hitEffectPool == null)
        {
            hitEffectPool = BossHitEffectPool.Instance;
        }
        
        if (hitEffectPool != null)
        {
            hitEffectPool.Initialize();
        }
        
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
        
        // Calculate touch position
        Vector3 hitPosition = GetTouchPosition();
        
        TakeDamage(1, false, hitPosition);
    }
    
    /// <summary>
    /// Get the world position where the boss was touched
    /// </summary>
    private Vector3 GetTouchPosition()
    {
        // Get mouse/touch position
        Vector3 inputPosition = Input.mousePosition;
        
        // Use raycast to find hit point on boss collider
        Ray ray = Camera.main.ScreenPointToRay(inputPosition);
        RaycastHit hit;
        
        Collider bossCollider = GetComponent<Collider>();
        if (bossCollider != null && bossCollider.Raycast(ray, out hit, 1000f))
        {
            return hit.point;
        }
        
        // Fallback: use boss position
        return transform.position;
    }
    
    /// <summary>
    /// Take damage from player tap or vulnerable zone
    /// </summary>
    public void TakeDamage(int damage, bool isVulnerableZone = false, Vector3? hitPosition = null)
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
        
        // Play hit sound (different sound for vulnerable zones)
        if (isVulnerableZone)
        {
            PlayVulnerableZoneHitSound();
        }
        else
        {
            PlayHitSound();
        }
        
        // Play hit particle effect
        PlayHitParticleEffect();
        
        // Spawn hit effect prefab at touch position (only for regular hits, not vulnerable zones)
        // Vulnerable zones already have their own specific prefab (perfect hit sprite)
        if (!isVulnerableZone && hitPosition.HasValue)
        {
            SpawnHitEffect(hitPosition.Value);
        }
        
        // Play hit scale animation
        PlayHitScaleAnimation();
        
        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Spawn vulnerable zone at a random spawn point
    /// </summary>
    private void SpawnVulnerableZone()
    {
        if (activeVulnerableZones.Count >= maxVulnerableZones) return;
        if (vulnerableZonePrefab == null) return;
        
        // Validate spawn points
        if (vulnerableZoneSpawnPoints == null || vulnerableZoneSpawnPoints.Count == 0)
        {
            Debug.LogWarning("Boss: No vulnerable zone spawn points defined!");
            return;
        }
        
        // Filter out null transforms and get available spawn points
        List<Transform> availableSpawnPoints = new List<Transform>();
        foreach (Transform spawnPoint in vulnerableZoneSpawnPoints)
        {
            if (spawnPoint != null && !occupiedSpawnPoints.ContainsKey(spawnPoint))
            {
                availableSpawnPoints.Add(spawnPoint);
            }
        }
        
        // Check if we have any available spawn points
        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning("Boss: No available spawn points for vulnerable zone!");
            return;
        }
        
        // Pick a random available spawn point
        Transform selectedSpawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        
        // Spawn vulnerable zone as a child of the spawn point transform
        GameObject zoneObj = Instantiate(vulnerableZonePrefab, selectedSpawnPoint);
        zoneObj.transform.localPosition = Vector3.zero;
        zoneObj.transform.localRotation = Quaternion.identity;
        
        VulnerableZone zone = zoneObj.GetComponent<VulnerableZone>();
        
        if (zone == null)
        {
            zone = zoneObj.AddComponent<VulnerableZone>();
        }
        
        // Initialize zone (position is handled by parent transform)
        zone.Initialize(this, vulnerableZoneDuration);
        activeVulnerableZones.Add(zone);
        
        // Mark this spawn point as occupied
        occupiedSpawnPoints[selectedSpawnPoint] = zone;
        
        Debug.Log($"Spawned vulnerable zone as child of spawn point: {selectedSpawnPoint.name} (world position: {selectedSpawnPoint.position})");
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
    /// Remove vulnerable zone from active list and free its spawn point
    /// </summary>
    public void RemoveVulnerableZone(VulnerableZone zone)
    {
        if (zone != null && activeVulnerableZones.Contains(zone))
        {
            activeVulnerableZones.Remove(zone);
            
            // Find and remove the spawn point entry for this zone
            Transform spawnPointToRemove = null;
            foreach (var kvp in occupiedSpawnPoints)
            {
                if (kvp.Value == zone)
                {
                    spawnPointToRemove = kvp.Key;
                    break;
                }
            }
            
            if (spawnPointToRemove != null)
            {
                occupiedSpawnPoints.Remove(spawnPointToRemove);
            }
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
    /// Play vulnerable zone hit sound effect
    /// </summary>
    private void PlayVulnerableZoneHitSound()
    {
        if (vulnerableZoneHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(vulnerableZoneHitSound);
        }
        else if (hitSound != null && audioSource != null)
        {
            // Fallback to regular hit sound if vulnerable zone sound not assigned
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
        occupiedSpawnPoints.Clear();
        
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
        occupiedSpawnPoints.Clear();
        
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
    
    /// <summary>
    /// Spawn hit effect prefab at position using pool
    /// </summary>
    private void SpawnHitEffect(Vector3 position)
    {
        // Get pool reference if not assigned
        if (hitEffectPool == null)
        {
            hitEffectPool = BossHitEffectPool.Instance;
        }
        
        if (hitEffectPool == null)
        {
            Debug.LogWarning("Boss: HitEffectPool not found! Hit effects will not spawn.");
            return;
        }
        
        // Get random effect from pool
        GameObject effect = hitEffectPool.GetRandomEffect();
        if (effect == null)
        {
            Debug.LogWarning("Boss: Could not get hit effect from pool!");
            return;
        }
        
        // Set position
        effect.transform.position = position;
        
        // Activate and play
        HitEffectPrefab hitEffect = effect.GetComponent<HitEffectPrefab>();
        if (hitEffect != null)
        {
            hitEffect.Play();
        }
        else
        {
            // Fallback: just activate if no HitEffectPrefab component
            effect.SetActive(true);
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
        activeVulnerableZones.Clear();
        occupiedSpawnPoints.Clear();
    }
}
