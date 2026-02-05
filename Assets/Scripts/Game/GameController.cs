using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JigsawSystem;
using Core;

/// <summary>
/// Main game controller - handles level loading, gameplay, and win conditions
/// </summary>
public class GameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private GameplayUI gameplayUI;
    [SerializeField] private LevelCompleteUI levelCompleteUI;
    [SerializeField] private LevelFailedUI levelFailedUI;
    
    [Header("Gameplay")]
    [SerializeField] private bool gameplayEnabled = false;
    
    [Header("Level Complete Effects")]
    [SerializeField] private AudioClip levelCompleteColorReturnSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Energy System")]
    [SerializeField] private int currentEnergy = 0;
    [SerializeField] private int startingEnergy = 5;
    
    // Rewards tracking during level
    private int monsterCoinsEarned = 0;
    
    // Level restart attempt tracking
    private int currentLevelRestartAttempts = 0;
    
    // Singleton
    private static GameController _instance;
    public static GameController Instance => _instance;
    
    // Current level
    private LevelController currentLevel;
    private GameObject currentLevelInstance;
    private LevelConfig currentLevelConfig;
    
    // Drag connection state
    private BaseNode dragStartNode;
    private BaseNode currentHoveredNode;
    private bool isDragging = false;
    
    // Events
    public static event System.Action OnLevelWon;
    public static event System.Action OnLevelReset;
    
    public LevelController CurrentLevel => currentLevel;
    public bool GameplayEnabled => gameplayEnabled;
    public LevelConfig CurrentLevelConfig => currentLevelConfig;
    public bool IsDragging => isDragging; // Expose drag state for cut manager
    
    /// <summary>
    /// Set visibility of the current level instance
    /// </summary>
    public void SetLevelVisibility(bool visible)
    {
        if (currentLevelInstance != null)
        {
            currentLevelInstance.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Get current player energy
    /// </summary>
    public int GetCurrentEnergy() => currentEnergy;
    
    /// <summary>
    /// Get maximum player energy (starting energy for the level)
    /// </summary>
    public int GetMaxEnergy() => startingEnergy;

    /// <summary>
    /// Increment max energy (starting energy for current level) and add 1 energy
    /// </summary>
    public void IncrementMaxEnergy()
    {
        startingEnergy++;
        currentEnergy++;
        Debug.Log($"Max energy increased to {startingEnergy}. Current energy: {currentEnergy}");
    }

    /// <summary>
    /// Check if player can afford connecting to a node (if it has negative weight)
    /// </summary>
    public bool CanAffordNode(BaseNode node)
    {
        if (node == null) return false;
        
        // If node has negative weight (costs energy), check if we have enough
        if (node.Weight < 0)
        {
            return currentEnergy >= Mathf.Abs(node.Weight);
        }
        
        // Positive or zero weight nodes are always affordable
        return true;
    }
    
    /// <summary>
    /// Modify player energy and update UI
    /// </summary>
    public void ModifyEnergy(int amount)
    {
        currentEnergy += amount;
        
        // Clamp to prevent negative energy
        if (currentEnergy < 0)
        {
            currentEnergy = 0;
        }
        
        Debug.Log($"Energy modified by {amount}. Current energy: {currentEnergy}");
        
        // TODO: Update UI when energy UI is implemented
    }
    
    /// <summary>
    /// Add bounty from killing a monster
    /// </summary>
    public void AddMonsterBounty(int amount)
    {
        if (amount <= 0) return;
        monsterCoinsEarned += amount;
        Debug.Log($"Monster bounty added: {amount}. Total for level: {monsterCoinsEarned}");
    }
    
    /// <summary>
    /// Set energy directly (used for recalculation after connection changes)
    /// </summary>
    public void SetEnergy(int energy)
    {
        currentEnergy = Mathf.Max(0, energy);
        Debug.Log($"Energy set to {currentEnergy}");
        
        // TODO: Update UI when energy UI is implemented
    }
    
    private void Awake()
    {
        Debug.Log("GameController: Awake");
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
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.Log("GameController: Destroying duplicate instance");
            Destroy(gameObject);
            return;
        }
        
        // Get ConnectionManager if not assigned
        if (connectionManager == null)
        {
            connectionManager = ConnectionManager.Instance;
            if (connectionManager == null)
            {
                // Create ConnectionManager if it doesn't exist
                GameObject cmObj = new GameObject("ConnectionManager");
                connectionManager = cmObj.AddComponent<ConnectionManager>();
                cmObj.transform.SetParent(transform);
            }
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to level complete UI events
        LevelCompleteUI.OnContinueButtonPressed += OnContinueToNextLevel;
        LevelCompleteUI.OnReturnToMenuButtonPressed += OnReturnToMainMenu;

        // Subscribe to level failed UI events
        LevelFailedUI.OnRetryButtonPressed += RestartLevel;
        LevelFailedUI.OnReturnToMenuButtonPressed += OnReturnToMainMenu;
        LevelFailedUI.OnSkipLevelAdCompleted += OnSkipLevelAdCompleted;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from level complete UI events
        LevelCompleteUI.OnContinueButtonPressed -= OnContinueToNextLevel;
        LevelCompleteUI.OnReturnToMenuButtonPressed -= OnReturnToMainMenu;

        // Unsubscribe from level failed UI events
        LevelFailedUI.OnRetryButtonPressed -= RestartLevel;
        LevelFailedUI.OnReturnToMenuButtonPressed -= OnReturnToMainMenu;
        LevelFailedUI.OnSkipLevelAdCompleted -= OnSkipLevelAdCompleted;
    }
    
    /// <summary>
    /// Preload a level (instantiate prefab, disable input)
    /// Called by LoadingScreen before showing main menu
    /// </summary>
    public void PreloadLevel(LevelConfig config)
    {
        if (config == null || config.LevelPrefab == null)
        {
            Debug.LogError("Cannot preload level - config or prefab is null");
            return;
        }
        
        // Clear any existing level
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
        }
        
        // Instantiate level prefab
        currentLevelInstance = Instantiate(config.LevelPrefab);
        currentLevelInstance.name = config.LevelName ?? "Level";
        
        // Get LevelController
        currentLevel = currentLevelInstance.GetComponent<LevelController>();
        if (currentLevel == null)
        {
            Debug.LogError("Level prefab doesn't have LevelController component!");
            return;
        }
        
        // Set level reference in ConnectionManager
        if (connectionManager != null)
        {
            connectionManager.CurrentLevel = currentLevel;
        }
        
        // Initialize energy from level config
        startingEnergy = config.StartingEnergy;
        currentEnergy = startingEnergy;
        
        // Store level config reference
        currentLevelConfig = config;
        
        // Reset restart attempts when loading a new level
        currentLevelRestartAttempts = 0;
        
        // Disable input
        gameplayEnabled = false;

        MapShaderController mapShaderController = FindFirstObjectByType<MapShaderController>();
        if (mapShaderController != null)
        {
            mapShaderController.SetLevelController(currentLevel);
        }
        
        Debug.Log($"Preloaded level: {config.LevelName} with starting energy: {startingEnergy}");
        
        // Force energy recalculation after level load to include producers and any initial state
        if (connectionManager != null)
        {
            connectionManager.RecalculateTotalEnergy();
        }
    }
    
    /// <summary>
    /// Start gameplay - enable input and interactions
    /// Called when player presses Start button
    /// </summary>
    public void StartGame()
    {
        if (currentLevel == null)
        {
            Debug.LogError("Cannot start game - no level loaded");
            return;
        }
        
        gameplayEnabled = true;
        Debug.Log("Game started - input enabled");

        // Trigger level started analytics event
        if (currentLevelConfig != null)
        {
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                int levelIndex = levelsManager.GetCurrentLevelNumber() - 1; // Convert to 0-indexed
                string levelName = currentLevelConfig.LevelName ?? $"Level_{levelIndex + 1}";
                GameEvents.LevelStarted(levelIndex, levelName);
            }
        }

        // Show hints when level starts
        if (currentLevel != null)
        {
            currentLevel.ShowHints(true);
            
            // Notify all nodes that the game has started
            foreach (var node in currentLevel.GetAllNodes())
            {
                if (node != null)
                {
                    node.OnGameStarted();
                }
            }
        }
        
        // Start monster spawning
        MonsterAiManager monsterManager = MonsterAiManager.Instance;
        if (monsterManager != null)
        {
            monsterManager.SetLevel(currentLevel);
            monsterManager.StartSpawning();
        }
    }
    
    /// <summary>
    /// Handle node drag start - begin connection preview
    /// </summary>
    public void OnNodeDragStart(BaseNode node)
    {
        if (!gameplayEnabled) return;
        
        dragStartNode = node;
        isDragging = true;
        
        // Visual feedback
        dragStartNode.OnSelect();
        
        Debug.Log($"Started dragging from node: {node.NodeID}");
    }
    
    /// <summary>
    /// Handle node drag - update ghost line
    /// </summary>
    public void OnNodeDrag(BaseNode node)
    {
        if (!gameplayEnabled || !isDragging || dragStartNode == null) return;
        
        // Get mouse position in world space
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        
        // Determine ghost line state and position
        GhostLineState state = GhostLineState.Neutral;
        Vector3 targetPosition = mouseWorldPos;
        
        if (currentHoveredNode != null && currentHoveredNode != dragStartNode)
        {
            // Hovering over a node - check if connection is valid
            bool isValid = connectionManager.ValidateConnection(dragStartNode, currentHoveredNode);
            state = isValid ? GhostLineState.Valid : GhostLineState.Invalid;
            targetPosition = currentHoveredNode.transform.position;
        }
        
        // Update ghost line
        connectionManager.ShowGhostLine(dragStartNode.transform.position, targetPosition, state);
    }
    
    /// <summary>
    /// Handle node drag end - attempt to create connection
    /// </summary>
    public void OnNodeDragEnd(BaseNode node)
    {
        if (!gameplayEnabled || !isDragging) return;
        
        // Hide ghost line
        connectionManager.HideGhostLine();
        
        // Deselect start node
        if (dragStartNode != null)
        {
            dragStartNode.OnDeselect();
        }
        
        // Check if we ended on a valid target node
        if (currentHoveredNode != null && currentHoveredNode != dragStartNode)
        {
            // Try to create connection
            bool success = connectionManager.CreateConnection(dragStartNode, currentHoveredNode);
            
            if (success)
            {
                Debug.Log($"Created connection from {dragStartNode.NodeID} to {currentHoveredNode.NodeID}");
                // Check win condition after creating connection
                CheckWinCondition();
            }
        }
        else
        {
            Debug.Log("Drag cancelled - no valid target node");
        }
        
        // Reset drag state
        dragStartNode = null;
        isDragging = false;
    }
    
    /// <summary>
    /// Handle node hover enter - track potential target
    /// </summary>
    public void OnNodeHoverEnter(BaseNode node)
    {
        currentHoveredNode = node;
    }
    
    /// <summary>
    /// Handle node hover exit - clear potential target
    /// </summary>
    public void OnNodeHoverExit(BaseNode node)
    {
        if (currentHoveredNode == node)
        {
            currentHoveredNode = null;
        }
    }
    
    /// <summary>
    /// Get mouse position in world space on the node plane
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        // Get mouse position
        Vector3 mousePos = Input.mousePosition;
        
        // Use the drag start node's Y position as the plane
        float planeY = dragStartNode != null ? dragStartNode.transform.position.y : 0f;
        
        // Create a plane at the node level
        Plane plane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
        
        // Cast ray from camera through mouse position
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        // Find intersection with plane
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        // Fallback: return drag start position
        return dragStartNode != null ? dragStartNode.transform.position : Vector3.zero;
    }
    
    /// <summary>
    /// Check if all consumers are connected to at least one producer AND fully activated
    /// Uses BFS from each consumer backwards through incoming connections
    /// </summary>
    public bool CheckWinCondition()
    {
        if (currentLevel == null || !gameplayEnabled) return false;
        
        List<ConsumerNode> consumers = currentLevel.GetConsumerNodes();
        
        if (consumers.Count == 0)
        {
            Debug.LogWarning("No consumer nodes in level");
            return false;
        }
        
        // Check each consumer - must be both connected AND fully delivered (activated)
        foreach (ConsumerNode consumer in consumers)
        {
            // Check if consumer is connected to a producer
            if (!IsConsumerConnectedToProducer(consumer))
            {
                // At least one consumer is not connected
                // Still check fail condition before returning
                CheckFailCondition();
                return false;
            }
            
            // Check if consumer is fully delivered (activated)
            if (!consumer.IsFullyDelivered)
            {
                // At least one consumer is not activated yet
                // Still check fail condition before returning
                CheckFailCondition();
                return false;
            }
        }
        
        // All consumers are connected AND activated!
        Debug.Log("WIN! All consumers connected to producers and fully activated");
        OnLevelComplete();
        return true;
    }

    /// <summary>
    /// Check if all producers are captured by monsters
    /// </summary>
    public bool CheckFailCondition()
    {
        if (currentLevel == null || !gameplayEnabled) return false;

        List<BaseNode> allNodes = currentLevel.GetAllNodes();
        int producerCount = 0;
        int capturedProducerCount = 0;

        foreach (BaseNode node in allNodes)
        {
            if (node is ProducerNode)
            {
                producerCount++;
                if (node.IsCaptured)
                {
                    capturedProducerCount++;
                }
            }
        }

        if (producerCount > 0 && capturedProducerCount == producerCount)
        {
            Debug.Log("FAIL! All producers captured by monsters");
            OnLevelFailed();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Called when level is failed
    /// </summary>
    private void OnLevelFailed()
    {
        gameplayEnabled = false;

        // Trigger level failed analytics event
        if (currentLevelConfig != null)
        {
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                int levelIndex = levelsManager.GetCurrentLevelNumber() - 1; // Convert to 0-indexed
                string levelName = currentLevelConfig.LevelName ?? $"Level_{levelIndex + 1}";
                string failReason = "All producers captured";
                GameEvents.LevelFailed(levelIndex, levelName, failReason);
            }
        }

        // Stop spawning monsters
        MonsterAiManager monsterManager = MonsterAiManager.Instance;
        if (monsterManager != null)
        {
            monsterManager.StopSpawning();
            
            // Trigger falling down animation on all active monsters (same as when they capture a node)
            foreach (Monster monster in monsterManager.ActiveMonsters)
            {
                if (monster != null && !monster.IsDead && !monster.IsGoalCompleted)
                {
                    MonsterAiController aiController = monster.GetComponent<MonsterAiController>();
                    if (aiController != null)
                    {
                        aiController.TriggerFallingDown();
                    }
                }
            }
        }

        if (levelFailedUI != null)
        {
            int reward = currentLevelConfig != null ? currentLevelConfig.CoinReward : 0;
            int energyReward = currentLevelConfig != null ? currentLevelConfig.EnergySphereReward : 0;

            // Apply perks
            float coinBonus = PerksManager.Instance.GetTotalBonus(PerkType.CoinRewardPercent);
            float energyBonus = PerksManager.Instance.GetTotalBonus(PerkType.EnergySphereRewardPercent);
            
            reward = Mathf.RoundToInt(reward * (1f + coinBonus / 100f));
            energyReward = Mathf.RoundToInt(energyReward * (1f + energyBonus / 100f));

            levelFailedUI.Show(reward, energyReward);
        }
        else
        {
            Debug.LogWarning("LevelFailedUI not found in scene!");
        }

        // Hide gameplay UI
        if (gameplayUI != null)
        {
            gameplayUI.Hide();
        }
    }

    private void OnSkipLevelAdCompleted()
    {
        // Mark level as completed and return to main menu
        // Note: Skip rewards (50% of perked reward) are already added by LevelFailedUI.OnSkipLevelButtonClick()
        // so we don't need to add them again here
        
        if (currentLevelConfig != null)
        {
            // Complete level (increments CurrentLevel)
            GameManager.Instance.CompleteLevel();
            
            // Return to main menu
            OnReturnToMainMenu();
        }
    }
    
    /// <summary>
    /// Check if a specific consumer is connected to any producer
    /// Uses BFS backwards through incoming connections
    /// </summary>
    private bool IsConsumerConnectedToProducer(ConsumerNode consumer)
    {
        HashSet<BaseNode> visited = new HashSet<BaseNode>();
        Queue<BaseNode> queue = new Queue<BaseNode>();
        
        // Start BFS from consumer
        queue.Enqueue(consumer);
        visited.Add(consumer);
        
        while (queue.Count > 0)
        {
            BaseNode current = queue.Dequeue();
            
            // Check if we reached a producer
            if (current is ProducerNode)
            {
                return true;
            }
            
            // Explore incoming connections (backwards traversal)
            foreach (Connection conn in current.IncomingConnections)
            {
                BaseNode fromNode = conn.FromNode;
                
                if (fromNode != null && !visited.Contains(fromNode))
                {
                    visited.Add(fromNode);
                    queue.Enqueue(fromNode);
                }
            }
        }
        
        // No producer found
        return false;
    }
    
    /// <summary>
    /// Reset level - fully unload and reload the level prefab
    /// </summary>
    public void ResetLevel()
    {
        if (currentLevelConfig == null)
        {
            Debug.LogError("Cannot reset level - currentLevelConfig is null");
            return;
        }

        // Store config reference before unloading
        LevelConfig config = currentLevelConfig;
        
        // Fully unload current level
        UnloadLevel();
        
        // Reset camera force (in case it was moved)
        CameraController cameraController = CameraController.Instance;
        if (cameraController != null)
        {
            cameraController.ResetCameraForce();
        }

        // Reload the level
        PreloadLevel(config);
        
        OnLevelReset?.Invoke();
        Debug.Log($"Level reset and reloaded: {config.LevelName}");
    }
    
    /// <summary>
    /// Called when level is complete
    /// </summary>
    private void OnLevelComplete()
    {
        gameplayEnabled = false;
        
        // Trigger level completed analytics event
        if (currentLevelConfig != null)
        {
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                int levelIndex = levelsManager.GetCurrentLevelNumber() - 1; // Convert to 0-indexed
                string levelName = currentLevelConfig.LevelName ?? $"Level_{levelIndex + 1}";
                int score = 0; // Score system not implemented yet
                int stars = 0; // Stars system not implemented yet
                GameEvents.LevelCompleted(levelIndex, levelName, score, stars);
            }
        }
        
        // Stop spawning and kill all monsters to prevent them from capturing nodes after level completion
        MonsterAiManager monsterManager = MonsterAiManager.Instance;
        if (monsterManager != null)
        {
            monsterManager.StopSpawning();
            monsterManager.KillAllMonsters();
        }

        // Notify UI and other systems immediately that the level is won
        OnLevelWon?.Invoke();
        
        // Play level complete animation (color return) before proceeding
        StartCoroutine(PlayLevelCompleteAnimation());
    }
    
    /// <summary>
    /// Play level complete animation: return color to environment, play sound and particles
    /// </summary>
    private IEnumerator PlayLevelCompleteAnimation()
    {
        // Get MapShaderController and start color return animation
        MapShaderController mapShaderController = FindFirstObjectByType<MapShaderController>();
        if (mapShaderController != null)
        {
            StartCoroutine(mapShaderController.RemoveGrayscaleFromAllMaterials(5f));
        }
        
        // Play sound effect
        if (levelCompleteColorReturnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(levelCompleteColorReturnSound);
        }
        
        // Activate particle system from LevelController
        if (currentLevel != null)
        {
            currentLevel.ActivateLevelCompleteParticles();
        }
        
        // Safety check: Kill any monsters that might have spawned during the transition
        // This ensures no monsters can spawn after level completion
        yield return new WaitForSeconds(0.5f);
        MonsterAiManager monsterManager = MonsterAiManager.Instance;
        if (monsterManager != null)
        {
            monsterManager.KillAllMonsters();
        }
        
        // Wait for animation to complete (remaining time)
        yield return new WaitForSeconds(3.2f);
        
        // Now proceed with boss fight or level complete screen
        if (currentLevelConfig != null && currentLevelConfig.IsBossFight)
        {
            // Start boss fight
            BossFightManager bossFightManager = BossFightManager.Instance;
            if (bossFightManager != null)
            {
                bossFightManager.StartBossFight(currentLevel, currentLevelConfig);
            }
            else
            {
                Debug.LogError("GameController: BossFightManager not found! Cannot start boss fight.");
                // Fall back to normal level complete
                ShowLevelCompleteScreen(0, 0);
            }
        }
        else
        {
            // Normal level complete flow
            ShowLevelCompleteScreen(0, 0);
        }
    }
    
    /// <summary>
    /// Show level complete screen (called after boss fight or for normal levels)
    /// </summary>
    public void ShowLevelCompleteScreen(int bossBaseReward = 0, int bossEnergyReward = 0, float multiplier = 1f, bool bossDefeated = false)
    {
        int coinsEarned = 0;
        int nextLevel = 1;
        
        // Get coin reward and level info
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager != null)
        {
            LevelConfig config = levelsManager.GetCurrentLevelConfig();
            if (config != null)
            {
                coinsEarned = config.CoinReward;
                int energySpheresEarned = config.EnergySphereReward;
                
                // Calculate boss final reward
                int bossFinalReward = bossDefeated ? (int)(bossBaseReward * multiplier) : 0;
                int bossFinalEnergyReward = bossDefeated ? (int)(config.BossEnergySphereReward * multiplier) : 0;
                
                // Use a copy of monster bounty for UI then reset it
                int finalMonsterBounty = monsterCoinsEarned;
                monsterCoinsEarned = 0;

            // Apply perks
            float coinBonus = PerksManager.Instance.GetTotalBonus(PerkType.CoinRewardPercent);
            float energyBonus = PerksManager.Instance.GetTotalBonus(PerkType.EnergySphereRewardPercent);
                float perkMultiplier = 1f + coinBonus / 100f;
                float energyPerkMultiplier = 1f + energyBonus / 100f;

                // Award all coins (regular level + boss bounty + monster bounty)
                int totalCoinsAwarded = Mathf.RoundToInt((coinsEarned + bossFinalReward + finalMonsterBounty) * perkMultiplier);
                GameManager.Instance.AddCoins(totalCoinsAwarded);

                // Award energy spheres
                int totalEnergyAwarded = Mathf.RoundToInt((energySpheresEarned + bossFinalEnergyReward) * energyPerkMultiplier);
                GameManager.Instance.AddEnergySpheres(totalEnergyAwarded);
                
                // Complete level (increments CurrentLevel)
                GameManager.Instance.CompleteLevel();
                
                // Get next level number for display
                nextLevel = levelsManager.GetCurrentLevelNumber();
                
                // Get earned puzzle pieces from current level config
                System.Collections.Generic.List<string> puzzlePieces = config.PuzzlePieceRewards;
                
                // Hide gameplay UI
                if (gameplayUI != null)
                {
                    gameplayUI.Hide();
                }
            
                if (levelCompleteUI != null)
                {
                    // Pass coinsEarned + finalMonsterBounty as the base reward to show in UI
                    // Note: The UI shows the base rewards, while the actual awarded amount includes perks.
                    // If we want the UI to show the perk-adjusted rewards, we should pass the adjusted values.
                    // The requirement says "when we sho an dscualte wareda r we simply mutuoply them".
                    // So I will pass the perk-adjusted values to the UI too.
                    levelCompleteUI.Show(Mathf.RoundToInt((coinsEarned + finalMonsterBounty) * perkMultiplier), config.IsBossFight, Mathf.RoundToInt(energySpheresEarned * energyPerkMultiplier), nextLevel, Mathf.RoundToInt(bossBaseReward * perkMultiplier), Mathf.RoundToInt(bossEnergyReward * energyPerkMultiplier), multiplier, bossDefeated, puzzlePieces);
                }
                else
                {
                    Debug.LogWarning("LevelCompleteUI not found in scene!");
                }
            }
        }
    }
    
    /// <summary>
    /// Fallback method for boss fight if boss is not found
    /// </summary>
    public void OnLevelCompleteFallback()
    {
        ShowLevelCompleteScreen(0, 0);
    }
    
    /// <summary>
    /// Unload current level
    /// </summary>
    public void UnloadLevel()
    {
        // Stop monster spawning and clear all monsters
        MonsterAiManager monsterManager = MonsterAiManager.Instance;
        if (monsterManager != null)
        {
            monsterManager.StopSpawning();
            monsterManager.ClearAllMonsters();
        }
        
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
        
        // Clean up drag state and clear all connections
        if (connectionManager != null)
        {
            connectionManager.ClearAllConnections();
            connectionManager.HideGhostLine();
        }
        
        if (dragStartNode != null)
        {
            dragStartNode.OnDeselect();
            dragStartNode = null;
        }
        isDragging = false;
        currentHoveredNode = null;
        
        currentLevel = null;
        gameplayEnabled = false;
        
        // Reset rewards tracking
        monsterCoinsEarned = 0;
    }
    
    /// <summary>
    /// Handle continue to next level from LevelCompleteUI
    /// </summary>
    private void OnContinueToNextLevel()
    {
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager != null)
        {
            // Check if there are more levels
            if (levelsManager.HasMoreLevels())
            {
                // Get next level config (CompleteLevel already incremented CurrentLevel)
                LevelConfig nextLevelConfig = levelsManager.GetCurrentLevelConfig();
                
                if (nextLevelConfig != null)
                {
                    // Unload current level
                    UnloadLevel();
                    
                    // Reset camera force (in case it was moved during boss fight)
                    CameraController cameraController = CameraController.Instance;
                    if (cameraController != null)
                    {
                        cameraController.ResetCameraForce();
                    }
                    
                    // Preload and start next level
                    PreloadLevel(nextLevelConfig);
                    
                    // Force energy recalculation after level load to include producers and any initial connections
                    if (connectionManager != null)
                    {
                        connectionManager.RecalculateTotalEnergy();
                    }
                    
                    StartGame();
                    
                    // Show gameplay UI
                    GameplayUI gameplayUI = FindFirstObjectByType<GameplayUI>();
                    if (gameplayUI != null)
                    {
                        gameplayUI.Show();
                    }
                }
            }
            else
            {
                // No more levels - return to main menu
                Debug.Log("All levels completed!");
                OnReturnToMainMenu();
            }
        }
    }
    
    /// <summary>
    /// Restart level - reset and start again
    /// </summary>
    public void RestartLevel()
    {
        // Trigger level restarted analytics event before resetting
        if (currentLevelConfig != null)
        {
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                int levelIndex = levelsManager.GetCurrentLevelNumber() - 1; // Convert to 0-indexed
                string levelName = currentLevelConfig.LevelName ?? $"Level_{levelIndex + 1}";
                currentLevelRestartAttempts++;
                GameEvents.LevelRestarted(levelIndex, levelName, currentLevelRestartAttempts);
            }
        }
        
        ResetLevel();
        StartGame();
    }

    /// <summary>
    /// Handle return to main menu from LevelCompleteUI or GameplayUI
    /// </summary>
    public void OnReturnToMainMenu()
    {
        // Unload current level
        UnloadLevel();
        
        // Reset camera force (in case it was moved during boss fight)
        CameraController cameraController = CameraController.Instance;
        if (cameraController != null)
        {
            cameraController.ResetCameraForce();
        }
        
        // Hide gameplay UI
        if (gameplayUI != null)
        {
            gameplayUI.Hide();
        }
        else
        {
            GameplayUI foundGameplayUI = FindFirstObjectByType<GameplayUI>(FindObjectsInactive.Include);
            if (foundGameplayUI != null)
            {
                foundGameplayUI.Hide();
            }
        }
        
        // Show main menu (include inactive objects in search)
        MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (mainMenu != null)
        {
            // Preload current level for menu background (GetCurrentLevelConfig returns the level to play next)
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                LevelConfig currentLevelConfig = levelsManager.GetCurrentLevelConfig();
                if (currentLevelConfig != null)
                {
                    PreloadLevel(currentLevelConfig);
                }
            }
            
            // Ensure main menu is active and visible
            mainMenu.gameObject.SetActive(true);
            mainMenu.Show();
        }
        else
        {
            Debug.LogError("GameController: MainMenuUI not found! Cannot return to main menu.");
        }
    }
    
    private void OnDestroy()
    {
        Debug.Log("GameController: OnDestroy");
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

