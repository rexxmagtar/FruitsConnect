using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages monster spawning, goal assignment, and lifecycle
/// </summary>
public class MonsterAiManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private float minSpawnInterval = 10f;
    [SerializeField] private float maxSpawnInterval = 30f;
    [SerializeField] private int maxActiveMonsters = 3;
    [SerializeField] private int maxSpawnAttempts = 20; // Maximum attempts to find valid spawn position
    
    [Header("Portal Animation")]
    [SerializeField] private GameObject portalPrefab; // Portal sprite prefab
    [SerializeField] private float portalAppearDuration = 0.5f; // Time for portal to appear
    [SerializeField] private float portalDisappearDuration = 0.5f; // Time for portal to disappear
    [SerializeField] private float portalStayDuration = 0.3f; // Time portal stays at full scale
    
    [Header("References")]
    [SerializeField] private LevelController currentLevel;
    [SerializeField] private MapShaderController mapShaderController;
    
    // Singleton
    private static MonsterAiManager _instance;
    public static MonsterAiManager Instance => _instance;
    
    // Active monsters
    private List<Monster> activeMonsters = new List<Monster>();
    
    // Spawn coroutine
    private Coroutine spawnCoroutine;
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Get level reference from GameController
        GameController gameController = GameController.Instance;
        if (gameController != null)
        {
            currentLevel = gameController.CurrentLevel;
        }
        
        // Start spawning when gameplay begins
        GameController.OnLevelWon += StopSpawning;
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        
        GameController.OnLevelWon -= StopSpawning;
        
        // Stop spawning
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
    
    /// <summary>
    /// Start spawning monsters (called when gameplay starts)
    /// </summary>
    public void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        spawnCoroutine = StartCoroutine(SpawnMonstersCoroutine());
    }
    
    /// <summary>
    /// Stop spawning monsters
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine that spawns monsters at random intervals
    /// </summary>
    private IEnumerator SpawnMonstersCoroutine()
    {
        while (true)
        {
            // Wait for random interval
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Check if we can spawn (gameplay enabled and under max limit)
            GameController gameController = GameController.Instance;
            if (gameController != null && gameController.GameplayEnabled)
            {
                if (activeMonsters.Count < maxActiveMonsters)
                {
                    yield return StartCoroutine(SpawnMonsterCoroutine());
                }
            }
        }
    }
    
    /// <summary>
    /// Coroutine to spawn a new monster with portal animation
    /// </summary>
    private IEnumerator SpawnMonsterCoroutine()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("MonsterAiManager: Monster prefab not assigned!");
            yield break;
        }
        
        if (currentLevel == null)
        {
            // Try to get level from GameController
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                currentLevel = gameController.CurrentLevel;
            }
            
            if (currentLevel == null)
            {
                Debug.LogError("MonsterAiManager: No level reference available!");
                yield break;
            }
        }
        
        // Get valid spawn position in grayscale zone
        Vector3 spawnPos = GetValidGrayscaleSpawnPosition();
        
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("MonsterAiManager: Could not find valid grayscale spawn position!");
            yield break;
        }
        
        // Show portal animation
        GameObject portalObj = null;
        Coroutine portalAnimationCoroutine = null;
        bool spawnCancelled = false;
        
        if (portalPrefab != null)
        {
            portalObj = ShowPortalAnimation(spawnPos);
            portalAnimationCoroutine = StartCoroutine(PortalAnimationCoroutine(portalObj, spawnPos, () => { spawnCancelled = true; }));
            
            // Wait for portal animation, but check periodically if spawn was cancelled
            float totalDuration = portalAppearDuration + portalStayDuration + portalDisappearDuration;
            float elapsed = 0f;
            float checkInterval = 0.1f; // Check every 0.1 seconds
            
            while (elapsed < totalDuration && !spawnCancelled)
            {
                yield return new WaitForSeconds(checkInterval);
                elapsed += checkInterval;
                
                // Check if spawn position is still valid
                if (mapShaderController != null)
                {
                    float minSpawnAreaRadius = mapShaderController.GetMinSpawnAreaRadius();
                    if (!mapShaderController.HasMinimumGrayscaleArea(spawnPos, minSpawnAreaRadius))
                    {
                        // Position is no longer valid, cancel spawn
                        spawnCancelled = true;
                        if (portalObj != null)
                        {
                            // Stop portal animation and close it
                            if (portalAnimationCoroutine != null)
                            {
                                StopCoroutine(portalAnimationCoroutine);
                            }
                            // Wait for portal to close before destroying
                            yield return StartCoroutine(ClosePortalAnimation(portalObj));
                            Destroy(portalObj);
                        }
                        yield break;
                    }
                }
            }
            
            // Wait for any remaining time
            if (!spawnCancelled && elapsed < totalDuration)
            {
                yield return new WaitForSeconds(totalDuration - elapsed);
            }
        }
        
        // If spawn was cancelled, don't spawn monster
        if (spawnCancelled)
        {
            if (portalObj != null)
            {
                Destroy(portalObj);
            }
            yield break;
        }
        
        // Clean up portal
        if (portalObj != null)
        {
            Destroy(portalObj);
        }
        
        // Instantiate monster
        GameObject monsterObj = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        Monster monster = monsterObj.GetComponent<Monster>();
        
        if (monster == null)
        {
            Debug.LogError("MonsterAiManager: Monster prefab doesn't have Monster component!");
            Destroy(monsterObj);
            yield break;
        }
        
        // Assign random goal
        MonsterGoal goal = GetRandomGoal();
        if (goal != null && goal.IsValid())
        {
            monster.Initialize(goal);
            activeMonsters.Add(monster);
        }
        else
        {
            // No valid goal available, destroy monster
            Debug.LogWarning("MonsterAiManager: No valid goal available for monster, destroying it.");
            Destroy(monsterObj);
        }
    }
    
    /// <summary>
    /// Get a valid spawn position in a grayscale zone with minimum area
    /// </summary>
    private Vector3 GetValidGrayscaleSpawnPosition()
    {
        if (currentLevel == null)
        {
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                currentLevel = gameController.CurrentLevel;
            }
            
            if (currentLevel == null)
            {
                return Vector3.zero;
            }
        }
        
        // Get MapShaderController if not assigned
        if (mapShaderController == null)
        {
            mapShaderController = FindFirstObjectByType<MapShaderController>();
        }
        
        if (mapShaderController == null)
        {
            // Fallback to old method if MapShaderController not found
            Debug.LogWarning("MonsterAiManager: MapShaderController not found, using fallback spawn position!");
            return Monster.GetRandomSpawnPosition(currentLevel);
        }
        
        float minSpawnAreaRadius = mapShaderController.GetMinSpawnAreaRadius();
        
        // Try to find valid spawn position
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Get random spawn position
            Vector3 candidatePos = Monster.GetRandomSpawnPosition(currentLevel);
            
            // Check if position has minimum grayscale area
            if (mapShaderController.HasMinimumGrayscaleArea(candidatePos, minSpawnAreaRadius))
            {
                return candidatePos;
            }
        }
        
        // Could not find valid position
        return Vector3.zero;
    }
    
    /// <summary>
    /// Show portal animation at spawn position
    /// Returns the portal GameObject
    /// Note: Portal prefab should have all properties (rotation, scale, etc.) set in prefab
    /// </summary>
    private GameObject ShowPortalAnimation(Vector3 position)
    {
        if (portalPrefab == null)
        {
            return null;
        }
        
        // Instantiate portal - all properties are set in prefab
        GameObject portalObj = Instantiate(portalPrefab, position, portalPrefab.transform.rotation);
        
        return portalObj;
    }
    
    /// <summary>
    /// Coroutine for portal animation (just waits, no scaling/rotation)
    /// Portal animation is handled by shader/material
    /// </summary>
    private IEnumerator PortalAnimationCoroutine(GameObject portalObj, Vector3 spawnPos, System.Action onCancelled)
    {
        if (portalObj == null) yield break;
        
        // Wait for portal appear duration
        yield return new WaitForSeconds(portalAppearDuration);
        
        // Stay at full visibility
        yield return new WaitForSeconds(portalStayDuration);
        
        // Wait for portal disappear duration
        yield return new WaitForSeconds(portalDisappearDuration);
    }
    
    /// <summary>
    /// Coroutine to close portal animation (when spawn is cancelled)
    /// Animates portal closing quickly
    /// </summary>
    private IEnumerator ClosePortalAnimation(GameObject portalObj)
    {
        if (portalObj == null) yield break;
        
        // Close portal quickly with fade out effect
        // Since portal properties are in prefab, we can use a simple fade or just wait a short time
        // The portal will be destroyed after this coroutine completes
        float closeDuration = 0.3f; // Quick close animation
        float elapsed = 0f;
        
        // If portal has a renderer, we can fade it out
        Renderer portalRenderer = portalObj.GetComponent<Renderer>();
        if (portalRenderer != null && portalRenderer.material != null)
        {
            Material mat = portalRenderer.material;
            Color originalColor = mat.color;
            
            while (elapsed < closeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / closeDuration;
                Color fadeColor = originalColor;
                fadeColor.a = Mathf.Lerp(originalColor.a, 0f, t);
                mat.color = fadeColor;
                yield return null;
            }
            
            // Ensure fully transparent
            Color finalColor = originalColor;
            finalColor.a = 0f;
            mat.color = finalColor;
        }
        else
        {
            // If no renderer, just wait for close duration
            yield return new WaitForSeconds(closeDuration);
        }
    }
    
    /// <summary>
    /// Get a random goal for a monster
    /// </summary>
    private MonsterGoal GetRandomGoal()
    {
        if (currentLevel == null) return null;
        
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager == null) return null;
        
        // Randomly choose goal type
        MonsterGoalType goalType = (Random.Range(0, 2) == 0) 
            ? MonsterGoalType.DestroyConnection 
            : MonsterGoalType.DestroyNodeConnections;
        
        MonsterGoal goal = new MonsterGoal();
        goal.goalType = goalType;
        
        switch (goalType)
        {
            case MonsterGoalType.DestroyConnection:
                // Get a random active connection
                List<Connection> activeConnections = connectionManager.GetActiveConnections();
                
                // Filter out captured connections
                activeConnections = activeConnections.Where(c => c != null && !c.IsCaptured).ToList();
                
                if (activeConnections.Count > 0)
                {
                    Connection randomConnection = activeConnections[Random.Range(0, activeConnections.Count)];
                    goal.targetConnection = randomConnection;
                    return goal;
                }
                break;
            
            case MonsterGoalType.DestroyNodeConnections:
                // Get a random node with connections
                List<BaseNode> allNodes = currentLevel.GetAllNodes();
                
                // Filter nodes that have connections and are not captured
                List<BaseNode> nodesWithConnections = allNodes.Where(n => 
                    n != null && 
                    !n.IsCaptured && 
                    (n.OutgoingConnections.Count > 0 || n.IncomingConnections.Count > 0)
                ).ToList();
                
                if (nodesWithConnections.Count > 0)
                {
                    BaseNode randomNode = nodesWithConnections[Random.Range(0, nodesWithConnections.Count)];
                    goal.targetNode = randomNode;
                    return goal;
                }
                break;
        }
        
        // If no valid target found for chosen type, try the other type
        if (goalType == MonsterGoalType.DestroyConnection)
        {
            // Try node goal instead
            List<BaseNode> allNodesFallback = currentLevel.GetAllNodes();
            List<BaseNode> nodesWithConnections = allNodesFallback.Where(n => 
                n != null && 
                !n.IsCaptured && 
                (n.OutgoingConnections.Count > 0 || n.IncomingConnections.Count > 0)
            ).ToList();
            
            if (nodesWithConnections.Count > 0)
            {
                goal.goalType = MonsterGoalType.DestroyNodeConnections;
                goal.targetNode = nodesWithConnections[Random.Range(0, nodesWithConnections.Count)];
                return goal;
            }
        }
        else
        {
            // Try connection goal instead
            List<Connection> activeConnectionsFallback = connectionManager.GetActiveConnections();
            activeConnectionsFallback = activeConnectionsFallback.Where(c => c != null && !c.IsCaptured).ToList();
            
            if (activeConnectionsFallback.Count > 0)
            {
                goal.goalType = MonsterGoalType.DestroyConnection;
                goal.targetConnection = activeConnectionsFallback[Random.Range(0, activeConnectionsFallback.Count)];
                return goal;
            }
        }
        
        // If still no valid goals, fall back to producer nodes (even if they don't have connections)
        List<BaseNode> allNodesForProducerFallback = currentLevel.GetAllNodes();
        List<BaseNode> producerNodes = allNodesForProducerFallback.Where(n => 
            n != null && 
            !n.IsCaptured && 
            n is ProducerNode
        ).ToList();
        
        if (producerNodes.Count > 0)
        {
            goal.goalType = MonsterGoalType.DestroyNodeConnections;
            goal.targetNode = producerNodes[Random.Range(0, producerNodes.Count)];
            return goal;
        }
        
        // No valid goals available
        return null;
    }
    
    /// <summary>
    /// Called when a monster dies
    /// </summary>
    public void OnMonsterDied(Monster monster)
    {
        if (monster != null)
        {
            activeMonsters.Remove(monster);
        }
    }
    
    /// <summary>
    /// Kill all active monsters by forcing them to take lethal damage
    /// Used when all consumers are activated to prevent monsters from capturing nodes
    /// </summary>
    public void KillAllMonsters()
    {
        // Create a copy of the list to avoid modification during iteration
        List<Monster> monstersToKill = new List<Monster>(activeMonsters);
        
        foreach (Monster monster in monstersToKill)
        {
            if (monster != null && !monster.IsDead)
            {
                // Deal enough damage to kill the monster (use MaxHealth to ensure death)
                monster.TakeDamage(monster.MaxHealth);
            }
        }
        
        Debug.Log($"Killed all {monstersToKill.Count} active monsters");
    }
    
    /// <summary>
    /// Clear all active monsters (for level reset)
    /// </summary>
    public void ClearAllMonsters()
    {
        foreach (Monster monster in activeMonsters)
        {
            if (monster != null)
            {
                Destroy(monster.gameObject);
            }
        }
        
        activeMonsters.Clear();
    }
    
    /// <summary>
    /// Set the current level reference
    /// </summary>
    public void SetLevel(LevelController level)
    {
        currentLevel = level;
    }
}
