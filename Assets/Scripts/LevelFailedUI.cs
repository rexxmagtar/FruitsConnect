using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LevelFailedUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelFailedText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnToMenuButton;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Header("Text Settings")]
    [SerializeField] private string levelFailedMessage = "Level Failed!";
    
    [Header("Visual Effects")]
    [SerializeField] private AudioClip failSound;
    [SerializeField] private AudioClip buttonClickSound;
    
    // Events
    public static event System.Action OnRetryButtonPressed;
    public static event System.Action OnReturnToMenuButtonPressed;
    
    // State
    private CanvasGroup canvasGroup;
    [SerializeField] private AudioSource audioSource;
    private bool isVisible = false;
    
    private void Awake()
    {
        // Get or add canvas group for fade effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Setup retry button
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClick);
        }
        
        // Setup return to menu button
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClick);
        }
    }
    
    private void Start()
    {
        // Hide initially
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Show the level failed UI
    /// </summary>
    public void Show()
    {
        if (isVisible) return;
        
        gameObject.SetActive(true);
        StartCoroutine(ShowAnimation());
    }
    
    /// <summary>
    /// Hide the level failed UI
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
        
        // Play fail sound
        if (failSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(failSound);
        }
        
        // Fade in
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
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
    /// Handle retry button click
    /// </summary>
    private void OnRetryButtonClick()
    {
        // Play button click sound
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        Hide();
        // Invoke event
        OnRetryButtonPressed?.Invoke();
    }
    
    /// <summary>
    /// Handle return to menu button click
    /// </summary>
    private void OnReturnToMenuButtonClick()
    {
        // Play button click sound
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        Hide();
        // Invoke event
        OnReturnToMenuButtonPressed?.Invoke();
    }
    
    /// <summary>
    /// Set custom level failed message
    /// </summary>
    public void SetLevelFailedMessage(string message)
    {
        levelFailedMessage = message;
        if (levelFailedText != null)
        {
            levelFailedText.text = message;
        }
    }
    
    /// <summary>
    /// Enable or disable the retry button
    /// </summary>
    public void SetRetryButtonEnabled(bool enabled)
    {
        if (retryButton != null)
        {
            retryButton.interactable = enabled;
        }
    }
    
    /// <summary>
    /// Check if UI is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
}

