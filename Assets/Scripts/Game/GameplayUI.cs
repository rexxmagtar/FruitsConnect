using UnityEngine;
using UnityEngine.UI;
using DataRepository;
using TMPro;

/// <summary>
/// UI overlay for gameplay - displays level info, energy balance, and controls
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI energyBalanceText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button pauseButton;
    
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
        UpdateEnergyDisplay();
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
    /// Update player energy balance display
    /// </summary>
    private void UpdateEnergyDisplay()
    {
        if (energyBalanceText == null) return;
        
        GameController controller = GameController.Instance;
        if (controller == null)
        {
            energyBalanceText.text = "Energy: 0";
            return;
        }
        
        int currentEnergy = controller.GetCurrentEnergy();
        energyBalanceText.text = $"Energy: {currentEnergy}";
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
        PauseMenuUI pauseMenu = FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu != null)
        {
            pauseMenu.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Called when level is won
    /// </summary>
    private void OnLevelWon()
    {
        // LevelCompleteUI handles the win screen, so we don't need to do anything here
        // Just update energy display in case it changed
        UpdateEnergyDisplay();
    }
    
    /// <summary>
    /// Called when level is reset
    /// </summary>
    private void OnLevelReset()
    {
        // Update energy display after reset
        UpdateEnergyDisplay();
    }
    
    private void Update()
    {
        // Update energy display continuously to reflect changes
        UpdateEnergyDisplay();
    }
}

