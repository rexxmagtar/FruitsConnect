using UnityEngine;
using UnityEngine.UI;
using DataRepository;
using TMPro;

/// <summary>
/// UI overlay for gameplay - displays level info, progress, and controls
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI coinDisplayText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button pauseButton;
    
    [Header("Win Screen")]
    [SerializeField] private GameObject winScreenPanel;
    [SerializeField] private TextMeshProUGUI coinsEarnedText;
    [SerializeField] private Button nextLevelButton;
    
    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        // Get canvas group if not assigned
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Setup button listeners
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClick);
        }
        
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseButtonClick);
        }
        
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
        }
        
        // Hide win screen initially
        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to events
        GameController.OnLevelWon += OnLevelWon;
        GameController.OnLevelReset += OnLevelReset;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        GameController.OnLevelWon -= OnLevelWon;
        GameController.OnLevelReset -= OnLevelReset;
    }
    
    private void Start()
    {
        // Initialize UI
        UpdateDisplay();
        
        // Hide initially
        Hide();
    }
    
    /// <summary>
    /// Show the gameplay UI
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Hide the gameplay UI
    /// </summary>
    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    /// <summary>
    /// Update all UI displays
    /// </summary>
    public void UpdateDisplay()
    {
        UpdateLevelDisplay();
        UpdateCoinDisplay();
        UpdateProgressDisplay();
    }
    
    /// <summary>
    /// Update level number display
    /// </summary>
    private void UpdateLevelDisplay()
    {
        if (levelNumberText == null) return;
        
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        levelNumberText.text = $"Level {saveData.CurrentLevel + 1}";
    }
    
    /// <summary>
    /// Update coin display
    /// </summary>
    private void UpdateCoinDisplay()
    {
        if (coinDisplayText == null) return;
        
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        coinDisplayText.text = $"Coins: {saveData.TotalCoins}";
    }
    
    /// <summary>
    /// Update progress display (X/Y shops connected)
    /// </summary>
    public void UpdateProgressDisplay()
    {
        if (progressText == null) return;
        
        GameController controller = GameController.Instance;
        if (controller == null || controller.CurrentLevel == null)
        {
            progressText.text = "0/0";
            return;
        }
        
        var consumers = controller.CurrentLevel.GetConsumerNodes();
        int total = consumers.Count;
        int connected = 0;
        
        // Count connected consumers
        foreach (var consumer in consumers)
        {
            if (consumer.IncomingConnections.Count > 0)
            {
                connected++;
            }
        }
        
        progressText.text = $"{connected}/{total} Shops";
    }
    
    /// <summary>
    /// Show win screen with coins earned
    /// </summary>
    public void ShowWinScreen(int coinsEarned)
    {
        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(true);
        }
        
        if (coinsEarnedText != null)
        {
            coinsEarnedText.text = $"+{coinsEarned} Coins!";
        }
    }
    
    /// <summary>
    /// Handle reset button click
    /// </summary>
    private void OnResetButtonClick()
    {
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.ResetLevel();
        }
    }
    
    /// <summary>
    /// Handle pause button click
    /// </summary>
    private void OnPauseButtonClick()
    {
        // Show pause menu (you can implement this later)
        Debug.Log("Pause button clicked");
        
        // For now, just find and show existing PauseMenuUI if it exists
        PauseMenuUI pauseMenu = FindObjectOfType<PauseMenuUI>(true);
        if (pauseMenu != null)
        {
            pauseMenu.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Handle next level button click
    /// </summary>
    private void OnNextLevelButtonClick()
    {
        // Hide win screen
        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(false);
        }
        
        // Load next level
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager != null)
        {
            LevelConfig nextLevel = levelsManager.GetCurrentLevelConfig();
            if (nextLevel != null)
            {
                // Unload current level
                GameController.Instance.UnloadLevel();
                
                // Preload and start next level
                GameController.Instance.PreloadLevel(nextLevel);
                GameController.Instance.StartGame();
                
                // Update display
                UpdateDisplay();
            }
            else
            {
                // No more levels - return to main menu
                Debug.Log("No more levels! Returning to main menu");
                Hide();
                
                MainMenuUI mainMenu = FindObjectOfType<MainMenuUI>(true);
                if (mainMenu != null)
                {
                    mainMenu.Show();
                }
            }
        }
    }
    
    /// <summary>
    /// Called when level is won
    /// </summary>
    private void OnLevelWon()
    {
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager != null)
        {
            LevelConfig config = levelsManager.GetCurrentLevelConfig();
            if (config != null)
            {
                ShowWinScreen(config.CoinReward);
            }
        }
        
        // Update displays
        UpdateCoinDisplay();
    }
    
    /// <summary>
    /// Called when level is reset
    /// </summary>
    private void OnLevelReset()
    {
        // Hide win screen if showing
        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(false);
        }
        
        // Update progress
        UpdateProgressDisplay();
    }
    
    private void Update()
    {
        // Update progress display continuously (optional - could be optimized)
        UpdateProgressDisplay();
    }
}

