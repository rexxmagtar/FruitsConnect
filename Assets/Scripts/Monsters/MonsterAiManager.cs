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
    
    [Header("References")]
    [SerializeField] private LevelController currentLevel;
    
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
                    SpawnMonster();
                }
            }
        }
    }
    
    /// <summary>
    /// Spawn a new monster with a random goal
    /// </summary>
    private void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("MonsterAiManager: Monster prefab not assigned!");
            return;
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
                return;
            }
        }
        
        // Get spawn position
        Vector3 spawnPos = Monster.GetRandomSpawnPosition(currentLevel);
        
        // Instantiate monster
        GameObject monsterObj = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        Monster monster = monsterObj.GetComponent<Monster>();
        
        if (monster == null)
        {
            Debug.LogError("MonsterAiManager: Monster prefab doesn't have Monster component!");
            Destroy(monsterObj);
            return;
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
