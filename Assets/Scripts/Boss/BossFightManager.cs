using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages boss fight flow - transitions, timer, win/loss conditions
/// </summary>
public class BossFightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossFightUI bossFightUI;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform bossCameraViewTransform;
    
    [Header("Boss Fight Settings")]
    [SerializeField] private float bossAlertDisplayDuration = 2.5f;
    [SerializeField] private float cameraTransitionDuration = 1.5f;
    
    // Singleton
    private static BossFightManager _instance;
    public static BossFightManager Instance => _instance;
    
    // State
    private Boss currentBoss;
    private LevelController currentLevel;
    private LevelConfig currentLevelConfig;
    private bool isBossFightActive = false;
    private float timeRemaining;
    private float timeLimit;
    private List<GameObject> hiddenObjects = new List<GameObject>();
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool terrainWasVisible = true;
    
    // Properties
    public bool IsBossFightActive => isBossFightActive;
    public float TimeRemaining => timeRemaining;
    public float TimeLimit => timeLimit;
    public Boss CurrentBoss => currentBoss;
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Get or find CameraController
        if (cameraController == null)
        {
            cameraController = CameraController.Instance;
            if (cameraController == null)
            {
                GameObject cameraObj = new GameObject("CameraController");
                cameraController = cameraObj.AddComponent<CameraController>();
            }
        }
        
        // Get or find BossFightUI
        if (bossFightUI == null)
        {
            bossFightUI = FindFirstObjectByType<BossFightUI>();
        }
    }
    
    private void Update()
    {
        if (isBossFightActive)
        {
            UpdateTimer();
        }
    }
    
    /// <summary>
    /// Start boss fight sequence
    /// </summary>
    public void StartBossFight(LevelController level, LevelConfig config)
    {
        if (isBossFightActive)
        {
            Debug.LogWarning("BossFightManager: Boss fight already active!");
            return;
        }
        
        currentLevel = level;
        currentLevelConfig = config;
        
        // Find boss in level
        currentBoss = FindBossInLevel(level);
        if (currentBoss == null)
        {
            Debug.LogError("BossFightManager: No boss found in level! Cannot start boss fight.");
            // Fall back to normal level complete
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                gameController.OnLevelCompleteFallback();
            }
            return;
        }
        
        // Get time limit from config
        timeLimit = config.BossFightTimeLimit;
        timeRemaining = timeLimit;
        
        // Start transition sequence
        StartCoroutine(BossFightTransitionSequence());
    }
    
    /// <summary>
    /// Find boss in level
    /// </summary>
    private Boss FindBossInLevel(LevelController level)
    {
        if (level == null) return null;
        
        // Search for Boss component in level
        Boss boss = level.GetComponentInChildren<Boss>();
        if (boss == null)
        {
            // Search in all nodes
            List<BaseNode> nodes = level.GetAllNodes();
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    boss = node.GetComponentInChildren<Boss>();
                    if (boss != null) break;
                }
            }
        }
        
        // If still not found, search entire scene
        if (boss == null)
        {
            boss = FindFirstObjectByType<Boss>();
        }
        
        return boss;
    }
    
    /// <summary>
    /// Transition sequence: hide map, move camera, show alert, start fight
    /// </summary>
    private IEnumerator BossFightTransitionSequence()
    {
        // Store original camera position
        if (cameraController != null)
        {
            cameraController.StoreCurrentPositionAsOriginal();
        }
        
        // Step 1: Hide map elements (nodes, connections, monsters, terrain)
        HideMapElements();
        
        // Step 2: Show boss alert UI immediately (before camera transition completes)
        if (bossFightUI != null)
        {
            bossFightUI.ShowBossAlert();
        }
        
        // Step 3: Move camera to boss view
        if (cameraController != null)
        {
            if (bossCameraViewTransform != null)
            {
                // Use predefined transform for camera position and rotation
                cameraController.MoveToPosition(
                    bossCameraViewTransform.position,
                    bossCameraViewTransform.rotation.eulerAngles,
                    cameraTransitionDuration
                );
            }
            else
            {
                // Fallback: calculate position relative to boss
                if (currentBoss != null)
                {
                    Vector3 bossPosition = currentBoss.transform.position;
                    Vector3 cameraPosition = bossPosition + Vector3.back * 8f + Vector3.up * 5f;
                    Vector3 cameraRotation = Quaternion.LookRotation(bossPosition - cameraPosition).eulerAngles;
                    cameraController.MoveToPosition(cameraPosition, cameraRotation, cameraTransitionDuration);
                }
                Debug.LogWarning("BossFightManager: BossCameraViewTransform not assigned. Using fallback camera position.");
            }
        }
        
        // Wait for camera transition
        yield return new WaitForSeconds(cameraTransitionDuration);
        
        // Wait for alert display
        yield return new WaitForSeconds(bossAlertDisplayDuration);
        
        // Step 4: Hide alert, show fight UI, start boss fight
        if (bossFightUI != null)
        {
            bossFightUI.HideBossAlert();
            bossFightUI.ShowFightUI();
        }
        
        // Initialize boss for fight
        if (currentBoss != null)
        {
            currentBoss.StartBossFight();
        }
        
        // Subscribe to boss events
        Boss.OnBossDied += OnBossDied;
        Boss.OnBossEscaped += OnBossEscaped;
        
        // Start fight
        isBossFightActive = true;
        
        Debug.Log("Boss fight started!");
    }
    
    /// <summary>
    /// Hide all map elements except boss
    /// </summary>
    private void HideMapElements()
    {
        hiddenObjects.Clear();
        
        if (currentLevel == null) return;
        
        // Hide all nodes
        List<BaseNode> nodes = currentLevel.GetAllNodes();
        foreach (var node in nodes)
        {
            if (node != null && node.gameObject != currentBoss.gameObject)
            {
                node.gameObject.SetActive(false);
                hiddenObjects.Add(node.gameObject);
            }
        }
        
        // Hide all connections
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager != null)
        {
            List<Connection> connections = connectionManager.GetActiveConnections();
            foreach (var connection in connections)
            {
                if (connection != null)
                {
                    connection.gameObject.SetActive(false);
                    hiddenObjects.Add(connection.gameObject);
                }
            }
        }
        
        // Hide all monsters
        MonsterAiManager monsterManager = MonsterAiManager.Instance;
        if (monsterManager != null)
        {
            // Get all monsters (we'll need to add a method to MonsterAiManager or find them)
            Monster[] monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                if (monster != null && monster.gameObject != currentBoss.gameObject)
                {
                    monster.gameObject.SetActive(false);
                    hiddenObjects.Add(monster.gameObject);
                }
            }
        }
        
        // Hide gameplay UI
        GameplayUI gameplayUI = FindFirstObjectByType<GameplayUI>();
        if (gameplayUI != null)
        {
            gameplayUI.Hide();
        }
        
        // Hide terrain renderer from LevelController if available
        if (currentLevel != null && currentLevel.TerrainMeshRenderer != null)
        {
            terrainWasVisible = currentLevel.TerrainMeshRenderer.enabled;
            currentLevel.TerrainMeshRenderer.enabled = false;
        }
    }
    
    /// <summary>
    /// Restore all hidden map elements
    /// </summary>
    private void RestoreMapElements()
    {
        foreach (var obj in hiddenObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        hiddenObjects.Clear();
        
        // Restore terrain renderer visibility from LevelController if available
        if (currentLevel != null && currentLevel.TerrainMeshRenderer != null)
        {
            currentLevel.TerrainMeshRenderer.enabled = terrainWasVisible;
        }
    }
    
    /// <summary>
    /// Update timer countdown
    /// </summary>
    private void UpdateTimer()
    {
        if (!isBossFightActive) return;
        
        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(0f, timeRemaining);
        
        // Update UI
        if (bossFightUI != null)
        {
            bossFightUI.UpdateTimer(timeRemaining, timeLimit);
        }
        
        // Check if time ran out
        if (timeRemaining <= 0f && currentBoss != null && !currentBoss.IsDead && !currentBoss.HasEscaped)
        {
            // Trigger boss escape
            currentBoss.Escape();
        }
    }
    
    /// <summary>
    /// Handle boss death
    /// </summary>
    private void OnBossDied(Boss boss)
    {
        if (boss != currentBoss) return;
        
        isBossFightActive = false;
        
        // Award gold
        if (currentLevelConfig != null)
        {
            int goldReward = currentLevelConfig.BossGoldReward;
            GameManager.Instance.AddCoins(goldReward);
            Debug.Log($"Boss defeated! Awarded {goldReward} gold.");
        }
        
        // Wait for death animation, then show level complete
        StartCoroutine(EndBossFightSequence(true));
    }
    
    /// <summary>
    /// Handle boss escape
    /// </summary>
    private void OnBossEscaped(Boss boss)
    {
        if (boss != currentBoss) return;
        
        isBossFightActive = false;
        
        Debug.Log("Boss escaped! Time ran out.");
        
        // Wait for escape animation, then show level complete
        StartCoroutine(EndBossFightSequence(false));
    }
    
    /// <summary>
    /// End boss fight sequence: show level complete screen directly (no camera return animation)
    /// </summary>
    private IEnumerator EndBossFightSequence(bool bossDefeated)
    {
        // Wait a bit for animations
        yield return new WaitForSeconds(2f);
        
        // Hide fight UI
        if (bossFightUI != null)
        {
            bossFightUI.HideFightUI();
        }
        
        // Unsubscribe from boss events
        Boss.OnBossDied -= OnBossDied;
        Boss.OnBossEscaped -= OnBossEscaped;
        
        // Show level complete screen directly (no camera return, no map restoration)
        GameController gameController = GameController.Instance;
        if (gameController != null)
        {
            gameController.ShowLevelCompleteScreen();
        }
        
        // Reset state
        currentBoss = null;
        currentLevel = null;
        currentLevelConfig = null;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        Boss.OnBossDied -= OnBossDied;
        Boss.OnBossEscaped -= OnBossEscaped;
        
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
