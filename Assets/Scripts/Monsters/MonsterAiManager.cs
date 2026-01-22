using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages monster spawning, goal assignment, and lifecycle
/// </summary>
public class MonsterAiManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private int maxSpawnAttempts = 20; // Maximum attempts to find valid spawn position
    
    [Header("Manual Spawn Zone")]
    [SerializeField] private float spawnZoneWidth = 20f;
    [SerializeField] private float spawnZoneHeight = 20f;
    
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
    
    // Track all active individual spawn coroutines to ensure they can be stopped
    private List<Coroutine> activeSpawnMonsterCoroutines = new List<Coroutine>();
    
    // Track all active portals to ensure they can be cleaned up
    private List<GameObject> activePortals = new List<GameObject>();
    
    // Flag to prevent spawning when level is complete
    private bool spawningEnabled = true;
    
    // Grid spawning
    [System.Serializable]
    private class SpawnCell
    {
        public Vector2Int gridPos;
        public Vector3 worldPos;
        public float nextSpawnTime;
        public bool isSpawning; // Currently performing a spawn animation
    }
    
    private List<SpawnCell> spawnGrid = new List<SpawnCell>();
    private const int GRID_SIZE = 10;
    private float levelStartTime;
    
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
        
        // Stop spawning (this also destroys portals)
        StopSpawning();
        
        // Ensure all portals are destroyed
        DestroyAllPortals();
    }
    
    /// <summary>
    /// Start spawning monsters (called when gameplay starts)
    /// </summary>
    public void StartSpawning()
    {
        spawningEnabled = true;
        levelStartTime = Time.time;
        
        GameController gameController = GameController.Instance;
        LevelConfig config = (gameController != null) ? gameController.CurrentLevelConfig : null;
        
        // Initialize grid
        InitializeSpawnGrid(config);
        
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        spawnCoroutine = StartCoroutine(SpawnMonstersCoroutine());
    }
    
    /// <summary>
    /// Initialize the 10x10 spawn grid based on manual dimensions centered on level
    /// </summary>
    private void InitializeSpawnGrid(LevelConfig config)
    {
        spawnGrid.Clear();
        
        Vector3 center = Vector3.zero;
        if (currentLevel != null)
        {
            if (currentLevel.TerrainMeshRenderer != null)
            {
                center = currentLevel.TerrainMeshRenderer.bounds.center;
            }
            else
            {
                // Fallback: calculate center from nodes
                var allNodes = currentLevel.GetAllNodes();
                if (allNodes.Count > 0)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (var node in allNodes) sum += node.transform.position;
                    center = sum / allNodes.Count;
                }
            }
        }
        
        float startX = center.x - spawnZoneWidth / 2f;
        float startZ = center.z - spawnZoneHeight / 2f;
        float cellWidth = spawnZoneWidth / GRID_SIZE;
        float cellHeight = spawnZoneHeight / GRID_SIZE;
        
        float minInterval = config != null ? config.MinSpawnInterval : 10f;
        float maxInterval = config != null ? config.MaxSpawnInterval : 30f;
        float safeOffset = config != null ? config.SafeTimeOffset : 20f;

        for (int x = 0; x < GRID_SIZE; x++)
        {
            for (int z = 0; z < GRID_SIZE; z++)
            {
                Vector3 cellPos = new Vector3(
                    startX + (x + 0.5f) * cellWidth,
                    center.y+0.02f,
                    startZ + (z + 0.5f) * cellHeight
                );
                
                spawnGrid.Add(new SpawnCell
                {
                    gridPos = new Vector2Int(x, z),
                    worldPos = cellPos,
                    nextSpawnTime = levelStartTime + safeOffset + Random.Range(minInterval, maxInterval)
                });
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Vector3.zero;
        if (currentLevel != null)
        {
            if (currentLevel.TerrainMeshRenderer != null)
            {
                center = currentLevel.TerrainMeshRenderer.bounds.center;
            }
            else
            {
                var allNodes = currentLevel.GetAllNodes();
                if (allNodes.Count > 0)
                {
                    Vector3 sum = Vector3.zero;
                    foreach (var node in allNodes) sum += node.transform.position;
                    center = sum / allNodes.Count;
                }
            }
        }

        // Draw main boundary
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, new Vector3(spawnZoneWidth, 0.1f, spawnZoneHeight));

        // Draw grid lines
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        float startX = center.x - spawnZoneWidth / 2f;
        float startZ = center.z - spawnZoneHeight / 2f;
        float cellWidth = spawnZoneWidth / GRID_SIZE;
        float cellHeight = spawnZoneHeight / GRID_SIZE;

        for (int i = 0; i <= GRID_SIZE; i++)
        {
            // Vertical lines
            float x = startX + i * cellWidth;
            Gizmos.DrawLine(new Vector3(x, center.y, startZ), new Vector3(x, center.y, startZ + spawnZoneHeight));

            // Horizontal lines
            float z = startZ + i * cellHeight;
            Gizmos.DrawLine(new Vector3(startX, center.y, z), new Vector3(startX + spawnZoneWidth, center.y, z));
        }
        
        // Draw individual cells if they exist (runtime)
        if (Application.isPlaying && spawnGrid != null)
        {
            foreach (var cell in spawnGrid)
            {
                if (cell.isSpawning)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(cell.worldPos, 0.3f);
                }
            }
        }
    }
    
    /// <summary>
    /// Stop spawning monsters and cancel all active spawn coroutines
    /// </summary>
    public void StopSpawning()
    {
        spawningEnabled = false;
        
        // Stop main spawn coroutine
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        // Stop all active individual spawn coroutines
        foreach (Coroutine coroutine in activeSpawnMonsterCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeSpawnMonsterCoroutines.Clear();
        
        // Destroy all active portals immediately
        DestroyAllPortals();
    }
    
    /// <summary>
    /// Coroutine that spawns monsters using independent grid cell timers
    /// </summary>
    private IEnumerator SpawnMonstersCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Check grid more frequently
            
            GameController gameController = GameController.Instance;
            if (gameController == null || gameController.CurrentLevelConfig == null) continue;
            
            LevelConfig config = gameController.CurrentLevelConfig;
            
            if (spawningEnabled && gameController.GameplayEnabled)
            {
                // Calculate current total (active + in-progress) once per check
                int spawningCount = 0;
                foreach (var c in spawnGrid) if (c.isSpawning) spawningCount++;
                int totalPotentialMonsters = activeMonsters.Count + spawningCount;

                // To avoid bias (always checking cell 0 first), we shuffle a temporary list
                List<SpawnCell> shuffledGrid = new List<SpawnCell>(spawnGrid);
                for (int i = 0; i < shuffledGrid.Count; i++)
                {
                    int rnd = Random.Range(i, shuffledGrid.Count);
                    SpawnCell temp = shuffledGrid[i];
                    shuffledGrid[i] = shuffledGrid[rnd];
                    shuffledGrid[rnd] = temp;
                }

                // Check each cell in the shuffled grid
                foreach (var cell in shuffledGrid)
                {
                    // If cell is ready to spawn and under max limit
                    if (Time.time >= cell.nextSpawnTime && totalPotentialMonsters < config.MaxActiveMonsters && !cell.isSpawning)
                    {
                        // Check if neighbors are currently spawning
                        if (!IsNeighborSpawning(cell))
                        {
                            // Start spawn for this cell
                            StartCoroutine(SpawnMonsterAtCell(cell));
                            
                            // Increment potential count immediately to prevent other cells from spawning in this same frame
                            totalPotentialMonsters++;
                        }
                    }
                    
                    // If nextSpawnTime is not set yet, initialize it
                    if (cell.nextSpawnTime <= 0)
                    {
                        cell.nextSpawnTime = Time.time + Random.Range(config.MinSpawnInterval, config.MaxSpawnInterval);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Check if any neighbor cell is currently performing a spawn animation
    /// </summary>
    private bool IsNeighborSpawning(SpawnCell cell)
    {
        foreach (var other in spawnGrid)
        {
            if (other == cell) continue;
            
            // If other is neighbor (including diagonal) and is spawning
            if (Mathf.Abs(other.gridPos.x - cell.gridPos.x) <= 1 && 
                Mathf.Abs(other.gridPos.y - cell.gridPos.y) <= 1)
            {
                if (other.isSpawning) return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Coroutine to spawn a monster at a specific grid cell
    /// </summary>
    private IEnumerator SpawnMonsterAtCell(SpawnCell cell)
    {
        cell.isSpawning = true;
        
        // Use the old SpawnMonsterCoroutine logic but for this specific cell
        Coroutine spawnCoroutine = StartCoroutine(SpawnMonsterCoroutine(cell));
        activeSpawnMonsterCoroutines.Add(spawnCoroutine);
        
        yield return spawnCoroutine;
        
        activeSpawnMonsterCoroutines.Remove(spawnCoroutine);
        cell.isSpawning = false;
        
        // Set next spawn time for this cell
        GameController gameController = GameController.Instance;
        if (gameController != null && gameController.CurrentLevelConfig != null)
        {
            cell.nextSpawnTime = Time.time + Random.Range(
                gameController.CurrentLevelConfig.MinSpawnInterval, 
                gameController.CurrentLevelConfig.MaxSpawnInterval
            );
        }
    }
    
    /// <summary>
    /// Coroutine to spawn a new monster with portal animation (updated for grid)
    /// </summary>
    private IEnumerator SpawnMonsterCoroutine(SpawnCell cell)
    {
        // Check if spawning is still enabled before starting
        if (!spawningEnabled)
        {
            yield break;
        }
        
        // Get level and config
        GameController gameController = GameController.Instance;
        if (currentLevel == null && gameController != null)
        {
            currentLevel = gameController.CurrentLevel;
        }
        
        if (currentLevel == null)
        {
            Debug.LogError("MonsterAiManager: No level reference available!");
            yield break;
        }

        LevelConfig config = (gameController != null) ? gameController.CurrentLevelConfig : null;
        if (config == null || config.MonsterPrefabs == null || config.MonsterPrefabs.Count == 0)
        {
            Debug.LogError("MonsterAiManager: No monster prefabs assigned in LevelConfig!");
            yield break;
        }
        
        // Double-check spawning is enabled and gameplay is enabled
        if (!spawningEnabled || (gameController != null && !gameController.GameplayEnabled))
        {
            yield break;
        }
        
        // Use cell position
        Vector3 spawnPos = cell.worldPos;
        
        // Check if spawn position is in grayscale zone
        if (mapShaderController == null) mapShaderController = FindFirstObjectByType<MapShaderController>();
        if (mapShaderController != null)
        {
            float minSpawnAreaRadius = mapShaderController.GetMinSpawnAreaRadius();
            if (!mapShaderController.HasMinimumGrayscaleArea(spawnPos, minSpawnAreaRadius))
            {
                // If cell center is not valid, try to find a valid spot near it
                bool foundValidSpot = false;
                for (int i = 0; i < 5; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                    if (mapShaderController.HasMinimumGrayscaleArea(spawnPos + randomOffset, minSpawnAreaRadius))
                    {
                        spawnPos += randomOffset;
                        foundValidSpot = true;
                        break;
                    }
                }
                
                if (!foundValidSpot)
                {
                    // If no valid spot in this cell, skip this spawn attempt
                    yield break;
                }
            }
        }
        
        // Show portal animation
        GameObject portalObj = null;
        bool spawnCancelled = false;
        
        if (portalPrefab != null)
        {
            portalObj = ShowPortalAnimation(spawnPos);
            if (portalObj != null)
            {
                activePortals.Add(portalObj);
                
                // Start portal animation coroutine
                StartCoroutine(PortalAnimationCoroutine(portalObj, spawnPos, () => {
                    spawnCancelled = true;
                }));
                
                // Wait for portal animation to complete
                yield return new WaitForSeconds(portalAppearDuration + portalStayDuration + portalDisappearDuration);
            }
        }
        
        // Final check before spawning monster
        GameController finalCheck = GameController.Instance;
        if (!spawningEnabled || (finalCheck != null && !finalCheck.GameplayEnabled) || spawnCancelled)
        {
            // Clean up portal if spawn was cancelled
            if (portalObj != null)
            {
                activePortals.Remove(portalObj);
                Destroy(portalObj);
            }
            yield break;
        }
        
        // Clean up portal (monster is about to spawn)
        if (portalObj != null)
        {
            activePortals.Remove(portalObj);
            Destroy(portalObj);
        }
        
        // Instantiate monster
        GameObject prefabToSpawn = config.MonsterPrefabs[Random.Range(0, config.MonsterPrefabs.Count)];
        
        if (prefabToSpawn == null)
        {
            Debug.LogError("MonsterAiManager: Selected monster prefab is null!");
            yield break;
        }
        
        GameObject monsterObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        Monster monster = monsterObj.GetComponent<Monster>();
        
        if (monster == null)
        {
            Debug.LogError("MonsterAiManager: Monster prefab doesn't have Monster component!");
            Destroy(monsterObj);
            yield break;
        }
        
        // Assign closest goal
        MonsterGoal goal = GetClosestGoal(spawnPos);
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
    /// Get the closest valid goal for a monster at a given position
    /// </summary>
    public MonsterGoal GetClosestGoal(Vector3 position)
    {
        if (currentLevel == null) return null;
        
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager == null) return null;
        
        MonsterGoal closestGoal = null;
        float minDistance = float.MaxValue;
        
        // Check all active connections
        List<Connection> activeConnections = connectionManager.GetActiveConnections();
        foreach (var conn in activeConnections)
        {
            if (conn != null && !conn.IsCaptured)
            {
                Vector3 targetPos = (conn.FromNode.transform.position + conn.ToNode.transform.position) / 2f;
                float dist = Vector3.Distance(position, targetPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestGoal = new MonsterGoal { goalType = MonsterGoalType.DestroyConnection, targetConnection = conn };
                }
            }
        }
        
        // Check all nodes with connections
        List<BaseNode> allNodes = currentLevel.GetAllNodes();
        foreach (var node in allNodes)
        {
            if (node != null && !node.IsCaptured && (node.OutgoingConnections.Count > 0 || node.IncomingConnections.Count > 0))
            {
                float dist = Vector3.Distance(position, node.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestGoal = new MonsterGoal { goalType = MonsterGoalType.DestroyNodeConnections, targetNode = node };
                }
            }
        }
        
        // Fallback to producer nodes if no connections found
        if (closestGoal == null)
        {
            foreach (var node in allNodes)
            {
                if (node != null && !node.IsCaptured && node is ProducerNode)
                {
                    float dist = Vector3.Distance(position, node.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestGoal = new MonsterGoal { goalType = MonsterGoalType.DestroyNodeConnections, targetNode = node };
                    }
                }
            }
        }
        
        return closestGoal;
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
        // Stop spawning first (this also destroys all portals)
        StopSpawning();
        
        foreach (Monster monster in activeMonsters)
        {
            if (monster != null)
            {
                Destroy(monster.gameObject);
            }
        }
        
        activeMonsters.Clear();
        
        // Ensure all portals are destroyed (safety check)
        DestroyAllPortals();
    }
    
    /// <summary>
    /// Destroy all active portals immediately
    /// </summary>
    private void DestroyAllPortals()
    {
        // Create a copy to avoid modification during iteration
        List<GameObject> portalsToDestroy = new List<GameObject>(activePortals);
        
        foreach (GameObject portal in portalsToDestroy)
        {
            if (portal != null)
            {
                Destroy(portal);
            }
        }
        
        activePortals.Clear();
        
        // Safety check: Find and destroy any orphaned portals in the scene
        // This handles cases where portals might not have been tracked properly
        // Find all GameObjects with "Portal" in the name (but exclude PortalBoss)
        GameObject[] allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allGameObjects)
        {
            if (obj != null && obj.name.Contains("Portal") && !obj.name.Contains("PortalBoss"))
            {
                // Additional check: verify it's likely a portal by checking if it has a Renderer
                // (portals should have renderers)
                if (obj.GetComponent<Renderer>() != null)
                {
                    Destroy(obj);
                }
            }
        }
    }
    
    /// <summary>
    /// Set the current level reference
    /// </summary>
    public void SetLevel(LevelController level)
    {
        currentLevel = level;
    }
}
