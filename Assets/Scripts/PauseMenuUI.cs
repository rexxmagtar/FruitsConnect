using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private Button effectsToggleButton;
    
    [Header("Toggle Icons")]
    [SerializeField] private Image musicToggleIcon;
    [SerializeField] private Image effectsToggleIcon;
    
    [Header("Icon Sprites")]
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite effectsOnSprite;
    [SerializeField] private Sprite effectsOffSprite;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    
    // Events
    public static event System.Action OnResumeButtonPressed;
    public static event System.Action OnRestartButtonPressed;
    public static event System.Action OnReturnToMenuButtonPressed;
    
    // State
    private CanvasGroup canvasGroup;
    private bool isVisible = false;
    
    private void Awake()
    {
        // Get or add canvas group for fade effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Subscribe to button events
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }
        
        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.AddListener(OnMusicToggleClicked);
        }
        
        if (effectsToggleButton != null)
        {
            effectsToggleButton.onClick.AddListener(OnEffectsToggleClicked);
        }
        
        // Subscribe to audio settings events
        AudioSettings.OnMusicToggled += OnMusicToggled;
        AudioSettings.OnEffectsToggled += OnEffectsToggled;
    }
    
    private void Start()
    {
        // Initially hide the pause menu
        gameObject.SetActive(false);
        
        // Initialize UI with current audio settings
        UpdateMusicIcon(AudioSettings.Instance.IsMusicEnabled());
        UpdateEffectsIcon(AudioSettings.Instance.IsEffectsEnabled());
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from button events
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(OnResumeClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
        
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
        }
        
        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.RemoveListener(OnMusicToggleClicked);
        }
        
        if (effectsToggleButton != null)
        {
            effectsToggleButton.onClick.RemoveListener(OnEffectsToggleClicked);
        }
        
        // Unsubscribe from audio settings events
        AudioSettings.OnMusicToggled -= OnMusicToggled;
        AudioSettings.OnEffectsToggled -= OnEffectsToggled;
    }
    
    /// <summary>
    /// Show the pause menu
    /// </summary>
    public void Show()
    {
        if (isVisible) return;
        
        UpdateMusicIcon(AudioSettings.Instance.IsMusicEnabled());
        UpdateEffectsIcon(AudioSettings.Instance.IsEffectsEnabled());
        gameObject.SetActive(true);
        isVisible = true;
        StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// Hide the pause menu
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        StartCoroutine(FadeOut());
    }
    
    /// <summary>
    /// Fade in animation using unscaled time
    /// </summary>
    private System.Collections.IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time for pause menu
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
    }
    
    /// <summary>
    /// Fade out animation using unscaled time
    /// </summary>
    private System.Collections.IEnumerator FadeOut()
    {
        float startAlpha = canvasGroup.alpha;
        canvasGroup.interactable = false;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time for pause menu
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Handle resume button click
    /// </summary>
    private void OnResumeClicked()
    {
        OnResumeButtonPressed?.Invoke();
    }
    
    /// <summary>
    /// Handle restart button click
    /// </summary>
    private void OnRestartClicked()
    {
        OnRestartButtonPressed?.Invoke();
    }
    
    /// <summary>
    /// Handle return to menu button click
    /// </summary>
    private void OnReturnToMenuClicked()
    {
        OnReturnToMenuButtonPressed?.Invoke();
    }
    
    /// <summary>
    /// Toggle pause menu visibility
    /// </summary>
    public void Toggle()
    {
        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
    
    /// <summary>
    /// Check if pause menu is visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Handle music toggle button click
    /// </summary>
    private void OnMusicToggleClicked()
    {
        AudioSettings.Instance.ToggleMusic();
    }
    
    /// <summary>
    /// Handle effects toggle button click
    /// </summary>
    private void OnEffectsToggleClicked()
    {
        AudioSettings.Instance.ToggleEffects();
    }
    
    /// <summary>
    /// Handle music toggle event
    /// </summary>
    private void OnMusicToggled(bool isEnabled)
    {
        UpdateMusicIcon(isEnabled);
    }
    
    /// <summary>
    /// Handle effects toggle event
    /// </summary>
    private void OnEffectsToggled(bool isEnabled)
    {
        UpdateEffectsIcon(isEnabled);
    }
    
    /// <summary>
    /// Update music icon based on state
    /// </summary>
    private void UpdateMusicIcon(bool isEnabled)
    {
        if (musicToggleIcon != null)
        {
            musicToggleIcon.sprite = isEnabled ? musicOnSprite : musicOffSprite;
        }
    }
    
    /// <summary>
    /// Update effects icon based on state
    /// </summary>
    private void UpdateEffectsIcon(bool isEnabled)
    {
        if (effectsToggleIcon != null)
        {
            effectsToggleIcon.sprite = isEnabled ? effectsOnSprite : effectsOffSprite;
        }
    }
}

