using UnityEngine;
using System.Collections;

/// <summary>
/// Main monster behavior - handles health, movement, goal execution, and capture system
/// </summary>
[RequireComponent(typeof(Collider))]
public class Monster : MonoBehaviour
{
    [Header("Monster Configuration")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float movementSpeed = 1.5f;
    [SerializeField] private float reachDistance = 0.5f;
    [SerializeField] private float positionOffsetY = 1.5f; // Height above captured target
    [SerializeField] private float spawnAnimationDuration = 3f; // Time to wait for spawn animation
    
    [Header("Hit Animation Settings")]
    [SerializeField] private float hitScaleAmount = 1.2f; // Scale multiplier when hit
    [SerializeField] private float hitScaleDuration = 0.2f; // Duration of scale animation
    
    [Header("Attack Animation Settings")]
    [SerializeField] private float attackAnimationDuration = 1.5f; // Duration of attack animation
    
    [Header("Component References")]
    [SerializeField] private MonsterAiController aiController;
    [SerializeField] private MonsterHealthBar healthBar;
    [SerializeField] private GameObject healthBarPrefab; // Optional prefab for healthbar
    
    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip dieSound;
    [SerializeField] private AudioSource audioSource; // Optional audio source component
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem hitParticleEffect;
    
    [Header("Goal")]
    [SerializeField] private MonsterGoal currentGoal;
    
    // State
    private int currentHealth;
    private bool isGoalCompleted = false;
    private bool isDead = false;
    private bool canMove = false; // Can only move after spawn animation completes
    private bool isStunned = false; // Paused when hit
    private BaseNode capturedNode;
    private Connection capturedConnection;
    private Vector3 originalScale;
    private Coroutine hitScaleCoroutine;
    private Coroutine stunCoroutine;
    
    // Properties
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool IsGoalCompleted => isGoalCompleted;
    public MonsterGoal CurrentGoal => currentGoal;
    
    private void Awake()
    {
        // Get or add required components
        if (aiController == null)
        {
            aiController = GetComponent<MonsterAiController>();
            if (aiController == null)
            {
                aiController = gameObject.AddComponent<MonsterAiController>();
            }
        }
        
        // Ensure collider exists
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
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
        
        if (healthBar != null)
        {
            healthBar.Initialize(this);
        }
    }
    
    /// <summary>
    /// Initialize monster with a goal
    /// </summary>
    public void Initialize(MonsterGoal goal)
    {
        currentGoal = goal;
        isGoalCompleted = false;
        isDead = false;
        canMove = false;
        currentHealth = maxHealth;
        
        if (healthBar != null)
        {
            healthBar.UpdateDisplay();
        }
        
        // Play spawn sound
        PlaySpawnSound();
        
        // Start spawn animation delay - wait for spawn animation to complete before allowing movement
        StartCoroutine(SpawnAnimationDelay());
    }
    
    /// <summary>
    /// Wait for spawn animation to complete before allowing movement
    /// </summary>
    private IEnumerator SpawnAnimationDelay()
    {
        // Wait for spawn animation duration (3 seconds)
        while(aiController.IsInState("Spawn"))
        {
            Debug.Log("Monster: Spawning");
            yield return null;
        }
        
        // Now allow movement and start running animation
        canMove = true;
        if (aiController != null)
        {
            aiController.SetRunning();
        }
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
        }
        else
        {
            // Create healthbar GameObject
            healthBarObj = new GameObject("MonsterHealthBar");
            healthBarObj.transform.SetParent(transform);
            healthBarObj.transform.localPosition = Vector3.zero;
            
            // Add Canvas
            Canvas canvas = healthBarObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Add CanvasScaler
            UnityEngine.UI.CanvasScaler scaler = healthBarObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            // Add GraphicRaycaster
            healthBarObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add MonsterHealthBar component
            healthBar = healthBarObj.AddComponent<MonsterHealthBar>();
        }
        
        healthBar = healthBarObj.GetComponent<MonsterHealthBar>();
    }
    
    private void Update()
    {
        if (isDead) return;
        
        // Only move if spawn animation is complete, not stunned, and goal is not completed
        if (canMove && !isStunned && !isGoalCompleted && currentGoal != null && currentGoal.IsValid())
        {
            MoveTowardTarget();
        }
    }
    
    /// <summary>
    /// Move monster toward its target
    /// </summary>
    private void MoveTowardTarget()
    {
        Vector3 targetPos = currentGoal.GetTargetPosition();
        Vector3 currentPos = transform.position;
        
        // Check if we've reached the target
        if (currentGoal.HasReachedTarget(currentPos, reachDistance))
        {
            CompleteGoal();
            return;
        }
        
        // Move toward target
        Vector3 direction = (targetPos - currentPos).normalized;
        Vector3 newPosition = Vector3.MoveTowards(currentPos, targetPos, movementSpeed * Time.deltaTime);
        transform.position = newPosition;
        
        // Rotate to face movement direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    /// <summary>
    /// Complete the current goal - destroy target and capture it
    /// </summary>
    private void CompleteGoal()
    {
        if (isGoalCompleted || currentGoal == null) return;
        
        isGoalCompleted = true;
        canMove = false; // Stop movement
        
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager == null)
        {
            Debug.LogError("Monster: ConnectionManager not found!");
            return;
        }
        
        // Start attack sequence: attack animation -> falling down -> sleep
        StartCoroutine(AttackSequence());
    }
    
    /// <summary>
    /// Attack sequence: perform attack, wait for attack animation to complete, then destroy/capture, then show falling down animation
    /// </summary>
    private IEnumerator AttackSequence()
    {
        // Trigger attack animation
        if (aiController != null)
        {
            aiController.TriggerAttack();
        }
        yield return new WaitForSeconds(0.5f);
        
        // Wait for attack animation to complete
        // We'll wait for the configured duration, or check if animator is still in attack state
        while (aiController.IsAttacking())
        {
            Debug.Log("Monster: Attacking");
            yield return null;
        }
        
        // Attack animation is complete, now destroy/capture the target
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager == null)
        {
            Debug.LogError("Monster: ConnectionManager not found!");
            yield break;
        }
        
        switch (currentGoal.goalType)
        {
            case MonsterGoalType.DestroyConnection:
                if (currentGoal.targetConnection != null)
                {
                    // Store connection reference before destruction
                    Connection targetConn = currentGoal.targetConnection;
                    BaseNode fromNode = targetConn.FromNode;
                    BaseNode toNode = targetConn.ToNode;
                    
                    // Capture the connection BEFORE destroying it (mark as captured)
                    targetConn.IsCaptured = true;
                    capturedConnection = targetConn;
                    
                    // Destroy the connection
                    connectionManager.RemoveConnection(targetConn);
                    
                    // Position monster on top of connection midpoint
                    Vector3 connectionMidpoint = (fromNode != null && toNode != null) 
                        ? ((fromNode.transform.position + toNode.transform.position) / 2f)
                        : currentGoal.GetTargetPosition();
                    transform.position = connectionMidpoint + Vector3.up * positionOffsetY;
                }
                break;
            
            case MonsterGoalType.DestroyNodeConnections:
                if (currentGoal.targetNode != null)
                {
                    // Capture the node BEFORE destroying connections
                    currentGoal.targetNode.IsCaptured = true;
                    capturedNode = currentGoal.targetNode;
                    
                    // Destroy all connections from/to this node
                    connectionManager.RemoveAllConnectionsFromNode(currentGoal.targetNode);
                    
                    // Position monster on top of node
                    Vector3 nodePos = currentGoal.targetNode.transform.position;
                    transform.position = nodePos + Vector3.up * positionOffsetY;
                }
                break;
        }
        
        // Trigger falling down animation (shows that target is occupied)
        if (aiController != null)
        {
            aiController.TriggerFallingDown();
        }
        
        // Wait for falling down animation to complete, then trigger sleep
        // The falling down animation transitions to sleep automatically via animator
        // But we can also explicitly trigger fall sleep after a delay
        yield return new WaitForSeconds(1f);
        
    }
    
    /// <summary>
    /// Handle tap/click on monster - take damage
    /// </summary>
    private void OnMouseDown()
    {
        // Only process taps when gameplay is enabled
        GameController gameController = GameController.Instance;
        if (gameController == null || !gameController.GameplayEnabled)
        {
            return;
        }
        
        if (isDead) return;
        
        TakeDamage(1);
    }
    
    /// <summary>
    /// Take damage from player tap
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Update healthbar
        if (healthBar != null)
        {
            healthBar.UpdateDisplay();
        }
        
        // Trigger get hit animation
        if (aiController != null)
        {
            aiController.TriggerGetHit();
        }
        
        // Play hit sound
        PlayHitSound();
        
        // Play hit particle effect
        PlayHitParticleEffect();
        
        // Play hit scale animation for better UX
        PlayHitScaleAnimation();
        
        // Stun monster for 1 second (stop movement)
        StunMonster(1f);
        
        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Stun monster for a duration (stops movement)
    /// </summary>
    private void StunMonster(float duration)
    {
        // Stop any existing stun coroutine
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }
        
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }
    
    /// <summary>
    /// Coroutine that stuns the monster for a duration
    /// </summary>
    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
        stunCoroutine = null;
    }
    
    /// <summary>
    /// Play scale animation when monster gets hit
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
    /// Play spawn sound effect
    /// </summary>
    private void PlaySpawnSound()
    {
        if (spawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
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
    /// Handle monster death
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Stop movement
        canMove = false;
        
        // Trigger die animation
        if (aiController != null)
        {
            aiController.TriggerDie();
        }
        
        // Play die sound
        PlayDieSound();
        
        
        // Start death sequence coroutine - wait for dying animation to finish
        StartCoroutine(DeathSequence());
    }
    
    /// <summary>
    /// Death sequence: wait for dying animation to complete, then clean up and destroy
    /// </summary>
    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(0.5f);
        // Wait for dying animation to complete
        if (aiController != null)
        {
            while (aiController.IsDying())
            {
                Debug.Log("Monster: Dying");
                yield return null;
            }
        }
        else
        {
            // Fallback: wait a short time if animator check fails
            yield return new WaitForSeconds(1f);
        }
        
        // Free captured target
        if (capturedNode != null)
        {
            capturedNode.IsCaptured = false;
            capturedNode = null;
        }
        
        if (capturedConnection != null)
        {
            capturedConnection.IsCaptured = false;
            capturedConnection = null;
        }
        
        // Notify manager
        MonsterAiManager manager = MonsterAiManager.Instance;
        if (manager != null)
        {
            manager.OnMonsterDied(this);
        }
        
        // Destroy monster
        Destroy(gameObject);
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
    /// Get spawn position for monster (random position on map)
    /// </summary>
    public static Vector3 GetRandomSpawnPosition(LevelController level)
    {
        if (level == null) return Vector3.zero;
        
        // Get all nodes to find bounds
        var allNodes = level.GetAllNodes();
        if (allNodes.Count == 0) return Vector3.zero;
        
        // Calculate bounds
        Vector3 min = allNodes[0].transform.position;
        Vector3 max = allNodes[0].transform.position;
        
        foreach (var node in allNodes)
        {
            Vector3 pos = node.transform.position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }
        
        // Spawn at random position near edges
        float x = Random.Range(min.x - 2f, max.x + 2f);
        float z = Random.Range(min.z - 2f, max.z + 2f);
        float y = allNodes[0].transform.position.y; // Use same Y as nodes
        
        return new Vector3(x, y, z);
    }
}
