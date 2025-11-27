using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startButton;

    [SerializeField] private Button settingsButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private TextMeshProUGUI startButtonText;
    
    
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

        gameObject.SetActive(false);
    }


    private void OnGameInitialized()
    {
        UpdateUI();
        
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
        
        // Show initially
        Show();
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
        
        // Invoke event
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
    
    private void OnDestroy()
    {
        // // Unsubscribe from events
        // if (SkinStoreManager.Instance != null)
        // {
        //     SkinStoreManager.Instance.OnSkinSelected -= OnSkinSelected;
        // }
        GameManager.OnGameInitialized -= OnGameInitialized;
    }
    /// <summary>
    /// Update level display
    /// </summary>
    public void UpdateLevelDisplay(int level)
    {
        Debug.Log("UpdateLevelDisplay: " + level);
        currentLevel = level;
        UpdateUI();
    }
    
    /// <summary>
    /// Update UI elements
    /// </summary>
    private void UpdateUI()
    {
        // // Update start button text
        // if (startButtonText != null)
        // {
        //     // Check if current level is a bonus level
        //     LevelConfig levelConfig = LevelsManager.Instance?.GetLevelConfig(currentLevel);
        //     if (levelConfig != null && levelConfig.IsBonus)
        //     {
        //         startButtonText.text = "Bonus level";
        //     }
        //     else
        //     {
        //         startButtonText.text = string.Format(startButtonTextFormat, currentLevel);
        //     }
        // }
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
}
