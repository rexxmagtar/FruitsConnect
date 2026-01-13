using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;
using DataRepository;
using DG.Tweening;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startButton;

    [SerializeField] private Button settingsButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI balanceText;
    
    [Header("Upgrade Containers")]
    [SerializeField] private ProgressPurchaseContainer connectionSpeedContainer;
    [SerializeField] private ProgressPurchaseContainer monsterDamageContainer;
    
    [Header("Boss Alert")]
    [SerializeField] private GameObject bossAlertContainer;
    [SerializeField] private float bossAlertScaleMin = 0.9f;
    [SerializeField] private float bossAlertScaleMax = 1.1f;
    [SerializeField] private float bossAlertScaleDuration = 1.0f;
    
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Header("Text Settings")]
    [SerializeField] private string startButtonTextFormat = "Start Level {0}";
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem backgroundEffect;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip backgroundMusic;
    
    [SerializeField] private SettingsUI settingsUI;
    // Events
    public static event System.Action OnStartButtonPressed;
    
    // State
    private CanvasGroup canvasGroup;
     [SerializeField]private AudioSource audioSource;
    private bool isVisible = false;
    private int currentLevel = 1;
    private Tween bossAlertScaleTween;
    
    private void Awake()
    {
        // Get or add canvas group for fade effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        
        // Setup start button
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClick);
        }
        
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
        }
        


    }
    
    private void Start()
    {
        GameManager.OnGameInitialized += OnGameInitialized;
        
        // Subscribe to upgrade purchased event to update balance
        PlayerProgressController.OnUpgradePurchased += OnUpgradePurchased;

        gameObject.SetActive(false);
    }


    private void OnGameInitialized()
    {
        // Note: Loading screen will handle showing the menu after level preload
        // Just prepare UI
        UpdateUI();
        
        // Update balance display
        UpdateBalanceDisplay();
        
        // Initialize upgrade containers
        InitializeUpgradeContainers();
        
        // Update upgrade visibility based on level completion
        UpdateUpgradeVisibility();
        
        // Update upgrade affordability
        UpdateUpgradeAffordability();
        
        // Start background music
        if (backgroundMusic != null && audioSource != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        // Start background effect
        if (backgroundEffect != null)
        {
            backgroundEffect.Play();
        }
    }
    
    /// <summary>
    /// Initialize upgrade purchase containers
    /// </summary>
    private void InitializeUpgradeContainers()
    {
        PlayerProgressController controller = PlayerProgressController.Instance;
        if (controller == null)
        {
            Debug.LogWarning("MainMenuUI: PlayerProgressController not found! Upgrade containers will not be initialized.");
            return;
        }
        
        // Initialize Connection Speed container
        if (connectionSpeedContainer != null)
        {
            ConnectionSpeed csParam = controller.GetConnectionSpeedParam();
            if (csParam != null)
            {
                connectionSpeedContainer.Initialize(csParam);
            }
        }
        
        // Initialize Monster Damage container
        if (monsterDamageContainer != null)
        {
            MonsterDamage mdParam = controller.GetMonsterDamageParam();
            if (mdParam != null)
            {
                monsterDamageContainer.Initialize(mdParam);
            }
        }
    }

    
    /// <summary>
    /// Show the main menu UI
    /// </summary>
    public void Show()
    {
        if (isVisible) return;
        
        gameObject.SetActive(true);
        StartCoroutine(ShowAnimation());
    }
    
    /// <summary>
    /// Hide the main menu UI
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        StartCoroutine(HideAnimation());
    }
    
    /// <summary>
    /// Show animation sequence
    /// </summary>
    private IEnumerator ShowAnimation()
    {
        isVisible = true;
        
        // Reset state
        canvasGroup.alpha = 0f;
        
        // Update balance when showing menu
        UpdateBalanceDisplay();
        
        // Update upgrade visibility based on level completion
        UpdateUpgradeVisibility();
        
        // Update upgrade affordability (balance may have changed after completing level)
        UpdateUpgradeAffordability();
        
        // Update boss alert visibility based on next level
        UpdateBossAlertVisibility();
        
        // Fade in
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        
        // Apply current skin to ball preview
        ApplyCurrentSkinToPreview();
    }
    
    /// <summary>
    /// Hide animation sequence
    /// </summary>
    private IEnumerator HideAnimation()
    {
        isVisible = false;
        
        // Fade out
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Handle start button click
    /// </summary>
    private void OnStartButtonClick()
    {
        // Play button click sound
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        // Hide main menu
        Hide();
        
        // Start the game
        GameController gameController = GameController.Instance;
        if (gameController != null)
        {
            gameController.StartGame();
        }
        
        // Show gameplay UI
        GameplayUI gameplayUI = FindFirstObjectByType<GameplayUI>();
        if (gameplayUI != null)
        {
            gameplayUI.gameObject.SetActive(true);
            gameplayUI.Show();
        }
        
        // Invoke event for any other listeners
        OnStartButtonPressed?.Invoke();
    }
    
    private void OnSettingsButtonClick()
    {
        // Play button click sound
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        settingsUI.gameObject.SetActive(true);
    }
    
    private void OnShopButtonClick()
    {
        // Play button click sound
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
    }
    
    /// <summary>
    /// Apply current skin to ball preview
    /// </summary>
    private void ApplyCurrentSkinToPreview()
    {
        // // Find the ball from the loaded level platform
        // LevelPlatform levelPlatform = FindFirstObjectByType<LevelPlatform>();
        // if (levelPlatform == null) return;
        
        // GameObject ballPreview = levelPlatform.GetCurrentBall();
        // if (ballPreview == null) return;
        
        // SkinStoreManager storeManager = SkinStoreManager.Instance;
        // if (storeManager == null) return;
        
        // Material skinMaterial = storeManager.GetCurrentSkinMaterial();
        // if (skinMaterial == null) return;
        
        // BallController ballController = ballPreview.GetComponent<BallController>();
        // if (ballController != null)
        // {
        //     ballController.SetSkin(skinMaterial);
        // }
        // else
        // {
        //     // If no BallController, try to apply material directly
        //     Renderer renderer = ballPreview.GetComponent<Renderer>();
        //     if (renderer != null)
        //     {
        //         renderer.material = skinMaterial;
        //     }
        // }
    }
    
    /// <summary>
    /// Handle skin selected event
    /// </summary>
    private void OnSkinSelected(string skinId)
    {
        ApplyCurrentSkinToPreview();
    }
    
    /// <summary>
    /// Update balance display with current coins
    /// </summary>
    private void UpdateBalanceDisplay()
    {
        if (balanceText == null) return;
        
        DataRepository.ProgressSaveManager<SaveData> manager = DataRepository.ProgressSaveManager<SaveData>.Instance;
        if (manager != null)
        {
            int coins = manager.GetCoins();
            balanceText.text = coins.ToString();
        }
        else
        {
            balanceText.text = "0";
        }
    }
    
    /// <summary>
    /// Handle upgrade purchased event - update balance display and refresh upgrade containers
    /// </summary>
    private void OnUpgradePurchased(UpgradableParam param, bool isLevelCompletion)
    {
        UpdateBalanceDisplay();
        
        // Update affordability states for both containers (balance changed)
        if (connectionSpeedContainer != null)
        {
            connectionSpeedContainer.UpdateAffordability();
        }
        if (monsterDamageContainer != null)
        {
            monsterDamageContainer.UpdateAffordability();
        }
    }
    
    /// <summary>
    /// Update level display
    /// </summary>
    public void UpdateLevelDisplay(int level)
    {
        Debug.Log("UpdateLevelDisplay: " + level);
        currentLevel = level;
        UpdateUI();
        UpdateUpgradeVisibility();
        UpdateBossAlertVisibility();
    }
    
    /// <summary>
    /// Update UI elements
    /// </summary>
    private void UpdateUI()
    {
        // Update start button text with current level
        if (startButtonText != null)
        {
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                int levelNumber = levelsManager.GetCurrentLevelNumber();
                startButtonText.text = string.Format(startButtonTextFormat, levelNumber);
            }
        }
    }
    
    
    /// <summary>
    /// Set start button text
    /// </summary>
    public void SetStartButtonText(string text)
    {
        if (startButtonText != null)
        {
            startButtonText.text = text;
        }
    }
    
    
    /// <summary>
    /// Enable or disable the start button
    /// </summary>
    public void SetStartButtonEnabled(bool enabled)
    {
        if (startButton != null)
        {
            startButton.interactable = enabled;
        }
    }
    
    
    /// <summary>
    /// Check if UI is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Play background music
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && audioSource != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    
    /// <summary>
    /// Start background effect
    /// </summary>
    public void StartBackgroundEffect()
    {
        if (backgroundEffect != null)
        {
            backgroundEffect.Play();
        }
    }
    
    /// <summary>
    /// Stop background effect
    /// </summary>
    public void StopBackgroundEffect()
    {
        if (backgroundEffect != null)
        {
            backgroundEffect.Stop();
        }
    }
    
    /// <summary>
    /// Update upgrade containers affordability based on current balance
    /// </summary>
    private void UpdateUpgradeAffordability()
    {
        // Update affordability states for both containers (balance may have changed)
        if (connectionSpeedContainer != null)
        {
            connectionSpeedContainer.UpdateAffordability();
        }
        if (monsterDamageContainer != null)
        {
            monsterDamageContainer.UpdateAffordability();
        }
    }
    
    /// <summary>
    /// Update upgrade containers visibility based on level completion
    /// Upgrades are hidden until user has completed first 2 levels
    /// </summary>
    private void UpdateUpgradeVisibility()
    {
        // Get current level (0-indexed: 0 = level 1, 1 = level 2, 2 = level 3, etc.)
        // User needs to complete levels 1 and 2, so CurrentLevel should be >= 2
        int currentLevelIndex = 0;
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager != null)
        {
            // GetCurrentLevelNumber returns 1-indexed, so subtract 1 to get index
            currentLevelIndex = levelsManager.GetCurrentLevelNumber() - 1;
        }
        else
        {
            // Fallback: try to get from save data directly
            var manager = ProgressSaveManager<SaveData>.Instance;
            if (manager != null)
            {
                currentLevelIndex = manager.GetGameData().CurrentLevel;
            }
        }
        
        // Show upgrades only if user has completed first 2 levels (CurrentLevel >= 2)
        bool shouldShowUpgrades = currentLevelIndex >= 2;
        
        // Update visibility of upgrade containers
        if (connectionSpeedContainer != null)
        {
            connectionSpeedContainer.gameObject.SetActive(shouldShowUpgrades);
        }
        
        if (monsterDamageContainer != null)
        {
            monsterDamageContainer.gameObject.SetActive(shouldShowUpgrades);
        }
        
        Debug.Log($"UpdateUpgradeVisibility: CurrentLevel={currentLevelIndex}, ShowUpgrades={shouldShowUpgrades}");
    }
    
    /// <summary>
    /// Update boss alert container visibility based on next level boss status
    /// Shows with scale animation loop if next level has boss, hides if no boss
    /// </summary>
    private void UpdateBossAlertVisibility()
    {
        if (bossAlertContainer == null) return;
        
        // Get next level config
        LevelsManager levelsManager = LevelsManager.Instance;
        bool hasBoss = false;
        
        if (levelsManager != null)
        {
            LevelConfig nextLevelConfig = levelsManager.GetCurrentLevelConfig();
            if (nextLevelConfig != null)
            {
                hasBoss = nextLevelConfig.IsBossFight;
            }
        }
        
        // Stop any existing animation
        if (bossAlertScaleTween != null)
        {
            bossAlertScaleTween.Kill();
            bossAlertScaleTween = null;
        }
        
        if (hasBoss)
        {
            // Show boss alert with scale animation loop
            bossAlertContainer.SetActive(true);
            StartBossAlertAnimation();
        }
        else
        {
            // Hide boss alert
            bossAlertContainer.SetActive(false);
        }
    }
    
    /// <summary>
    /// Start scale animation loop for boss alert
    /// </summary>
    private void StartBossAlertAnimation()
    {
        if (bossAlertContainer == null) return;
        
        RectTransform rectTransform = bossAlertContainer.GetComponent<RectTransform>();
        if (rectTransform == null) return;
        
        // Reset scale to min
        rectTransform.localScale = Vector3.one * bossAlertScaleMin;
        
        // Create scale animation loop
        bossAlertScaleTween = rectTransform.DOScale(Vector3.one * bossAlertScaleMax, bossAlertScaleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
    
    /// <summary>
    /// Stop boss alert animation
    /// </summary>
    private void StopBossAlertAnimation()
    {
        if (bossAlertScaleTween != null)
        {
            bossAlertScaleTween.Kill();
            bossAlertScaleTween = null;
        }
    }
    
    private void OnDestroy()
    {
        // Stop boss alert animation
        StopBossAlertAnimation();
        
        // // Unsubscribe from events
        // if (SkinStoreManager.Instance != null)
        // {
        //     SkinStoreManager.Instance.OnSkinSelected -= OnSkinSelected;
        // }
        GameManager.OnGameInitialized -= OnGameInitialized;
        PlayerProgressController.OnUpgradePurchased -= OnUpgradePurchased;
    }
}
