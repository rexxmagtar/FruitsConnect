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
    
    [Header("Energy System")]
    [SerializeField] private int currentEnergy = 0;
    [SerializeField] private int startingEnergy = 5;
    
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
    
    /// <summary>
    /// Get current player energy
    /// </summary>
    public int GetCurrentEnergy() => currentEnergy;
    
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
        
        // Initialize energy from level config
        startingEnergy = config.StartingEnergy;
        currentEnergy = startingEnergy;
        
        // Store level config reference
        currentLevelConfig = config;
        
        // Disable input
        gameplayEnabled = false;
        
        Debug.Log($"Preloaded level: {config.LevelName} with starting energy: {startingEnergy}");
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
        
        // Check for wall intersection - overrides state to Invalid if wall is hit
        if (connectionManager.CheckWallIntersection(dragStartNode.transform.position, targetPosition))
        {
            state = GhostLineState.Invalid;
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
            connectionManager.HideGhostLine();
        }
        
        // Clear drag state
        if (dragStartNode != null)
        {
            dragStartNode.OnDeselect();
            dragStartNode = null;
        }
        isDragging = false;
        currentHoveredNode = null;
        
        // Reset energy to starting value
        currentEnergy = startingEnergy;
        Debug.Log($"Energy reset to {currentEnergy}");
        
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
        
        // Clean up drag state
        if (connectionManager != null)
        {
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

