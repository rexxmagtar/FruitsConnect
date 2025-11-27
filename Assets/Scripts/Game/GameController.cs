using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main game controller - handles level loading, gameplay, and win conditions
/// </summary>
public class GameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConnectionManager connectionManager;
    [SerializeField] private GameplayUI gameplayUI;
    [SerializeField] private LevelCompleteUI levelCompleteUI;
    
    [Header("Gameplay")]
    [SerializeField] private bool gameplayEnabled = false;
    
    // Singleton
    private static GameController _instance;
    public static GameController Instance => _instance;
    
    // Current level
    private LevelController currentLevel;
    private GameObject currentLevelInstance;
    
    // Node selection for creating connections
    private BaseNode selectedNode;
    
    // Events
    public static event System.Action OnLevelWon;
    public static event System.Action OnLevelReset;
    
    public LevelController CurrentLevel => currentLevel;
    public bool GameplayEnabled => gameplayEnabled;
    
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
    }
    
    private void OnDisable()
    {
        // Unsubscribe from level complete UI events
        LevelCompleteUI.OnContinueButtonPressed -= OnContinueToNextLevel;
        LevelCompleteUI.OnReturnToMenuButtonPressed -= OnReturnToMainMenu;
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
        
        // Disable input
        gameplayEnabled = false;
        
        Debug.Log($"Preloaded level: {config.LevelName}");
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
    }
    
    /// <summary>
    /// Handle node click for creating connections
    /// </summary>
    public void OnNodeClicked(BaseNode node)
    {
        if (!gameplayEnabled) return;
        
        if (selectedNode == null)
        {
            // First node selected
            selectedNode = node;
            selectedNode.OnSelect();
            Debug.Log($"Selected node: {node.NodeID}");
        }
        else
        {
            // Second node clicked - attempt to create connection
            if (selectedNode == node)
            {
                // Clicked same node - deselect
                selectedNode.OnDeselect();
                selectedNode = null;
                Debug.Log("Deselected node");
            }
            else
            {
                // Try to create connection
                bool success = connectionManager.CreateConnection(selectedNode, node);
                
                // Deselect first node
                selectedNode.OnDeselect();
                selectedNode = null;
                
                if (success)
                {
                    // Check win condition after creating connection
                    CheckWinCondition();
                }
            }
        }
    }
    
    /// <summary>
    /// Check if all consumers are connected to at least one producer
    /// Uses BFS from each consumer backwards through incoming connections
    /// </summary>
    public bool CheckWinCondition()
    {
        if (currentLevel == null) return false;
        
        List<ConsumerNode> consumers = currentLevel.GetConsumerNodes();
        
        if (consumers.Count == 0)
        {
            Debug.LogWarning("No consumer nodes in level");
            return false;
        }
        
        // Check each consumer
        foreach (ConsumerNode consumer in consumers)
        {
            if (!IsConsumerConnectedToProducer(consumer))
            {
                // At least one consumer is not connected
                return false;
            }
        }
        
        // All consumers are connected!
        Debug.Log("WIN! All consumers connected to producers");
        OnLevelComplete();
        return true;
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
    /// Reset level - clear all connections
    /// </summary>
    public void ResetLevel()
    {
        if (connectionManager != null)
        {
            connectionManager.ClearAllConnections();
        }
        
        // Deselect any selected node
        if (selectedNode != null)
        {
            selectedNode.OnDeselect();
            selectedNode = null;
        }
        
        OnLevelReset?.Invoke();
        Debug.Log("Level reset");
    }
    
    /// <summary>
    /// Called when level is complete
    /// </summary>
    private void OnLevelComplete()
    {
        gameplayEnabled = false;
        
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
                
                // Award coins
                GameManager.Instance.AddCoins(coinsEarned);
                
                // Complete level (increments CurrentLevel)
                GameManager.Instance.CompleteLevel();
                
                // Get next level number for display
                nextLevel = levelsManager.GetCurrentLevelNumber();
            }
        }
        
        // Hide gameplay UI

        if (gameplayUI != null)
        {
            gameplayUI.Hide();
        }
    

        if (levelCompleteUI != null)
        {
            levelCompleteUI.Show(coinsEarned, nextLevel);
        }
        else
        {
            Debug.LogWarning("LevelCompleteUI not found in scene!");
        }
        
        OnLevelWon?.Invoke();
    }
    
    /// <summary>
    /// Unload current level
    /// </summary>
    public void UnloadLevel()
    {
        if (currentLevelInstance != null)
        {
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
        
        currentLevel = null;
        selectedNode = null;
        gameplayEnabled = false;
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
                    
                    // Preload and start next level
                    PreloadLevel(nextLevelConfig);
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
    /// Handle return to main menu from LevelCompleteUI
    /// </summary>
    private void OnReturnToMainMenu()
    {
        // Unload current level
        UnloadLevel();
        
        // Hide gameplay UI
        GameplayUI gameplayUI = FindFirstObjectByType<GameplayUI>();
        if (gameplayUI != null)
        {
            gameplayUI.Hide();
        }
        
        // Show main menu
        MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
        if (mainMenu != null)
        {
            // Preload current level for menu background
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                LevelConfig currentLevelConfig = levelsManager.GetCurrentLevelConfig();
                if (currentLevelConfig != null)
                {
                    PreloadLevel(currentLevelConfig);
                }
            }
            
            mainMenu.Show();
        }
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

