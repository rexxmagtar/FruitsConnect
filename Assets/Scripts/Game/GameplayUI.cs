using UnityEngine;
using UnityEngine.UI;
using DataRepository;
using TMPro;
using DG.Tweening;
using AdsServices; // Assuming this is the namespace for AdsManager

/// <summary>
/// UI overlay for gameplay - displays level info, energy balance, and controls
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI energyBalanceText;
    [SerializeField] private Slider energySlider;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button pauseButton;
    
    [Header("Energy Helper")]
    [SerializeField] private Button helpButton;
    [SerializeField] private Image helpTimerFillImage;
    [SerializeField] private float helpTimerDuration = 10f;
    [SerializeField] private float helpShowInterval = 60f;
    [SerializeField] private float helpRetryInterval = 20f;
    
    [Header("Animation Settings")]
    [SerializeField] private float sliderAnimationSpeed = 5f;
    
    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    private float targetSliderValue;
    private float nextHelpShowTime;
    private float currentHelpTimer;
    private bool isHelpButtonVisible;
    private Tween helpPulseTween;
    
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
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClick);
        }
        
        if (homeButton != null)
        {
            homeButton.onClick.AddListener(OnHomeButtonClick);
        }
        
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPauseButtonClick);
        }

        // Setup help button
        if (helpButton != null)
        {
            helpButton.onClick.AddListener(OnHelpButtonClick);
            helpButton.gameObject.SetActive(false);
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
        
        // Initialize help button timer
        nextHelpShowTime = Time.time + helpShowInterval;
        HideHelpButton(helpShowInterval);

        // Initialize slider if available
        if (energySlider != null)
        {
            GameController controller = GameController.Instance;
            if (controller != null)
            {
                int currentEnergy = controller.GetCurrentEnergy();
                int maxEnergy = controller.GetMaxEnergy();
                float initialValue = maxEnergy > 0 ? (float)currentEnergy / maxEnergy : 0f;
                UpdateSliderValue(initialValue);
            }
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
        UpdateEnergyDisplay();
        UpdateButtonsVisibility();
    }
    
    /// <summary>
    /// Update visibility of control buttons based on current level
    /// </summary>
    private void UpdateButtonsVisibility()
    {
        bool isFirstLevel = false;
        if (LevelsManager.Instance != null)
        {
            isFirstLevel = LevelsManager.Instance.GetCurrentLevelNumber() == 1;
        }

        // Hide restart and home buttons on the first level
        bool showButtons = !isFirstLevel;

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(showButtons);
        }

        if (homeButton != null)
        {
            homeButton.gameObject.SetActive(showButtons);
        }
    }
    
    /// <summary>
    /// Update player energy balance display
    /// </summary>
    private void UpdateEnergyDisplay()
    {
        GameController controller = GameController.Instance;
        if (controller == null)
        {
            if (energyBalanceText != null)
            {
                energyBalanceText.text = "0/0";
            }
            if (energySlider != null)
            {
                UpdateSliderValue(0f);
            }
            return;
        }
        
        int currentEnergy = controller.GetCurrentEnergy();
        int maxEnergy = controller.GetMaxEnergy();
        
        // Update text format: {current}/{max}
        if (energyBalanceText != null)
        {
            energyBalanceText.text = $"{currentEnergy}/{maxEnergy}";
        }
        
        // Update slider target value
        if (energySlider != null && maxEnergy > 0)
        {
            targetSliderValue = (float)currentEnergy / maxEnergy;
        }
        else if (energySlider != null)
        {
            targetSliderValue = 0f;
        }
    }
    
    /// <summary>
    /// Update slider value immediately without animation
    /// </summary>
    private void UpdateSliderValue(float value)
    {
        if (energySlider != null)
        {
            energySlider.value = Mathf.Clamp01(value);
            targetSliderValue = value;
        }
    }
    
    /// <summary>
    /// Handle restart button click
    /// </summary>
    private void OnRestartButtonClick()
    {
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.RestartLevel();
        }
    }

    /// <summary>
    /// Handle home button click
    /// </summary>
    private void OnHomeButtonClick()
    {
        GameController controller = GameController.Instance;
        if (controller != null)
        {
            controller.OnReturnToMainMenu();
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
        
        // Hide help button on level win
        HideHelpButton(helpShowInterval);
    }
    
    /// <summary>
    /// Called when level is reset
    /// </summary>
    private void OnLevelReset()
    {
        // Update energy display after reset
        UpdateEnergyDisplay();

        // Reset help button timer on level reset
        nextHelpShowTime = Time.time + helpShowInterval;
        HideHelpButton(helpShowInterval);
    }
    
    private void Update()
    {
        // Update energy display continuously to reflect changes
        UpdateEnergyDisplay();
        
        // Interpolate slider value toward target
        if (energySlider != null)
        {
            energySlider.value = Mathf.Lerp(energySlider.value, targetSliderValue, sliderAnimationSpeed * Time.deltaTime);
        }

        UpdateHelpButtonLogic();
    }

    /// <summary>
    /// Update logic for the energy help button
    /// </summary>
    private void UpdateHelpButtonLogic()
    {
        if (GameController.Instance == null || !GameController.Instance.GameplayEnabled) return;

        // Only show hint button after level 5
        if (LevelsManager.Instance != null && LevelsManager.Instance.GetCurrentLevelNumber() <= 5)
        {
            if (isHelpButtonVisible)
            {
                HideHelpButton(helpShowInterval);
            }
            return;
        }

        if (!isHelpButtonVisible)
        {
            if (Time.time >= nextHelpShowTime)
            {
                ShowHelpButton();
            }
        }
        else
        {
            currentHelpTimer -= Time.deltaTime;
            if (helpTimerFillImage != null)
            {
                helpTimerFillImage.fillAmount = currentHelpTimer / helpTimerDuration;
            }

            if (currentHelpTimer <= 0)
            {
                HideHelpButton(helpRetryInterval);
            }
        }
    }

    /// <summary>
    /// Show the help button with pulsing animation
    /// </summary>
    private void ShowHelpButton()
    {
        if (helpButton == null) return;

        isHelpButtonVisible = true;
        currentHelpTimer = helpTimerDuration;
        helpButton.gameObject.SetActive(true);
        
        if (helpTimerFillImage != null)
        {
            helpTimerFillImage.fillAmount = 1f;
        }

        // Pulse animation
        helpButton.transform.localScale = Vector3.one;
        helpPulseTween = helpButton.transform.DOScale(1.1f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true); // Ensure it pulses even if paused (though we show it during gameplay)
    }

    /// <summary>
    /// Hide the help button and stop animations
    /// </summary>
    private void HideHelpButton(float nextInterval)
    {
        if (helpButton == null) return;

        isHelpButtonVisible = false;
        nextHelpShowTime = Time.time + nextInterval;
        helpButton.gameObject.SetActive(false);
        
        if (helpPulseTween != null)
        {
            helpPulseTween.Kill();
            helpPulseTween = null;
        }
    }

    /// <summary>
    /// Handle help button click - show rewarded ad
    /// </summary>
    private async void OnHelpButtonClick()
    {
        if (AdsManager.Instance == null) return;

        // Pause game
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Debug.Log("Energy Helper: Showing rewarded ad");
        bool success = await AdsManager.Instance.ShowRewardedAdAsync();

        // Resume game
        Time.timeScale = previousTimeScale;

        if (success)
        {
            Debug.Log("Energy Helper: Ad successful, adding energy");
            if (GameController.Instance != null)
            {
                GameController.Instance.IncrementMaxEnergy();
            }
            UpdateEnergyDisplay();
            HideHelpButton(helpShowInterval); // Wait 1 minute after success
        }
        else
        {
            Debug.Log("Energy Helper: Ad failed or cancelled");
            HideHelpButton(helpRetryInterval); // Retry in 20 seconds
        }
    }
}

