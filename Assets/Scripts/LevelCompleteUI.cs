using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using DataRepository;


public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private TextMeshProUGUI coinsEarnedText;
    [SerializeField] private TextMeshProUGUI totalCoinsText;
    [SerializeField] private RectTransform coinIconTransform;
    [SerializeField] private RectTransform balanceIconTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float buttonFadeInDuration = 0.5f;
    
    [Header("Text Settings")]
    [SerializeField] private string levelCompleteMessage = "Level Complete!";
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem celebrationEffect;
    [SerializeField] private AudioClip celebrationSound;
    [SerializeField] private AudioClip buttonClickSound;
    
    [Header("Coin Animation Settings")]
    [SerializeField] private int coinAnimationCount = 5;
    [SerializeField] private float coinAnimationDuration = 1.5f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float coinSpawnInterval = 0.1f;
    [SerializeField] private float coinReachThreshold = 10f;
    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private Vector2 coinParticleSize = new Vector2(30f, 30f);
    
    // Events
    public static event System.Action OnContinueButtonPressed;
    public static event System.Action OnReturnToMenuButtonPressed;
    
    // State
    private CanvasGroup canvasGroup;
     [SerializeField]private AudioSource audioSource;
    private bool isVisible = false;
    private int coinsEarned = 0;
    private int coinsCollectedInAnimation = 0;
    private int displayedBalance = 0;
    private int nextLevelNumber = 1;
    private bool isAnimating = false;
    
    private void Awake()
    {
        // Get or add canvas group for fade effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Get or add audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Setup continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClick);
        }
        
        // Setup return to menu button
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClick);
        }
    }
    
    private void Start()
    {
    }
    
    /// <summary>
    /// Show the level complete UI
    /// </summary>
    public void Show(int coinsEarned = 0, int nextLevel = 1)
    {
        if (isVisible) return;
        
        this.coinsEarned = coinsEarned;
        this.nextLevelNumber = nextLevel;
        gameObject.SetActive(true);
        StartCoroutine(ShowAnimation());
    }
    
    /// <summary>
    /// Hide the level complete UI
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        StartCoroutine(HideAnimation());
    }
    
    /// <summary>
    /// Get all Image components from a button and its children
    /// </summary>
    private Image[] GetAllImagesFromButton(Button button)
    {
        if (button == null) return new Image[0];
        return button.GetComponentsInChildren<Image>(true);
    }
    
    /// <summary>
    /// Get all TextMeshProUGUI components from a button and its children
    /// </summary>
    private TextMeshProUGUI[] GetAllTextsFromButton(Button button)
    {
        if (button == null) return new TextMeshProUGUI[0];
        return button.GetComponentsInChildren<TextMeshProUGUI>(true);
    }
    
    /// <summary>
    /// Set alpha for all images in an array and return original alpha values
    /// </summary>
    private float[] SetImagesAlpha(Image[] images, float alpha)
    {
        if (images == null) return new float[0];
        
        float[] originalAlphas = new float[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
            {
                originalAlphas[i] = images[i].color.a;
                Color color = images[i].color;
                color.a = alpha;
                images[i].color = color;
            }
        }
        return originalAlphas;
    }
    
    /// <summary>
    /// Set alpha for all text components in an array and return original alpha values
    /// </summary>
    private float[] SetTextsAlpha(TextMeshProUGUI[] texts, float alpha)
    {
        if (texts == null) return new float[0];
        
        float[] originalAlphas = new float[texts.Length];
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                originalAlphas[i] = texts[i].color.a;
                Color color = texts[i].color;
                color.a = alpha;
                texts[i].color = color;
            }
        }
        return originalAlphas;
    }
    
    /// <summary>
    /// Fade in all images in an array
    /// </summary>
    private IEnumerator FadeInImages(Image[] images, float[] originalAlphas, float duration)
    {
        if (images == null || images.Length == 0 || originalAlphas == null) yield break;
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            
            for (int i = 0; i < images.Length && i < originalAlphas.Length; i++)
            {
                if (images[i] != null)
                {
                    Color color = images[i].color;
                    color.a = alpha * originalAlphas[i];
                    images[i].color = color;
                }
            }
            
            yield return null;
        }
        
        // Ensure final alpha is correct
        for (int i = 0; i < images.Length && i < originalAlphas.Length; i++)
        {
            if (images[i] != null)
            {
                Color color = images[i].color;
                color.a = originalAlphas[i];
                images[i].color = color;
            }
        }
    }
    
    /// <summary>
    /// Fade in all text components in an array
    /// </summary>
    private IEnumerator FadeInTexts(TextMeshProUGUI[] texts, float[] originalAlphas, float duration)
    {
        if (texts == null || texts.Length == 0 || originalAlphas == null) yield break;
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            
            for (int i = 0; i < texts.Length && i < originalAlphas.Length; i++)
            {
                if (texts[i] != null)
                {
                    Color color = texts[i].color;
                    color.a = alpha * originalAlphas[i];
                    texts[i].color = color;
                }
            }
            
            yield return null;
        }
        
        // Ensure final alpha is correct
        for (int i = 0; i < texts.Length && i < originalAlphas.Length; i++)
        {
            if (texts[i] != null)
            {
                Color color = texts[i].color;
                color.a = originalAlphas[i];
                texts[i].color = color;
            }
        }
    }
    
    /// <summary>
    /// Show animation sequence
    /// </summary>
    private IEnumerator ShowAnimation()
    {
        isVisible = true;
        isAnimating = true;
        
        // Get all images and texts from buttons
        Image[] continueButtonImages = GetAllImagesFromButton(continueButton);
        Image[] menuButtonImages = GetAllImagesFromButton(returnToMenuButton);
        TextMeshProUGUI[] continueButtonTexts = GetAllTextsFromButton(continueButton);
        TextMeshProUGUI[] menuButtonTexts = GetAllTextsFromButton(returnToMenuButton);
        
        // Set buttons to fully transparent and store original alpha values
        float[] continueButtonImageAlphas = SetImagesAlpha(continueButtonImages, 0f);
        float[] menuButtonImageAlphas = SetImagesAlpha(menuButtonImages, 0f);
        float[] continueButtonTextAlphas = SetTextsAlpha(continueButtonTexts, 0f);
        float[] menuButtonTextAlphas = SetTextsAlpha(menuButtonTexts, 0f);
        
        // Disable buttons until animations complete
        if (continueButton != null)
        {
            continueButton.interactable = false;
        }
        if (returnToMenuButton != null)
        {
            returnToMenuButton.interactable = false;
        }
        
        // Update continue button text based on next level
        UpdateContinueButtonText();

        coinsEarnedText.text = $"+{coinsEarned}";
        
        // Reset state
        canvasGroup.alpha = 0f;
        
        // Play celebration sound
        if (celebrationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(celebrationSound);
        }
        
        // Play particle effect
        if (celebrationEffect != null)
        {
            celebrationEffect.Play();
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
        
        // Start coin animation if coins were earned
        if (coinsEarned > 0)
        {
            int currentBalance = ProgressSaveManager<SaveData>.Instance.GetCoins() - coinsEarned;
            yield return StartCoroutine(AnimateCoins(coinsEarned, currentBalance));
        }
        
        // All animations complete, fade in buttons (images and texts simultaneously for both buttons)
        StartCoroutine(FadeInImages(continueButtonImages, continueButtonImageAlphas, buttonFadeInDuration));
        StartCoroutine(FadeInTexts(continueButtonTexts, continueButtonTextAlphas, buttonFadeInDuration));
        StartCoroutine(FadeInImages(menuButtonImages, menuButtonImageAlphas, buttonFadeInDuration));
        StartCoroutine(FadeInTexts(menuButtonTexts, menuButtonTextAlphas, buttonFadeInDuration));
        
        // Wait for fade-in to complete
        yield return new WaitForSeconds(buttonFadeInDuration);
        
        // Enable buttons
        isAnimating = false;
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }
        if (returnToMenuButton != null)
        {
            returnToMenuButton.interactable = true;
        }
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
    /// Handle continue button click
    /// </summary>
    private void OnContinueButtonClick()
    {
        // Play button click sound
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        Hide();
        // Invoke event
        OnContinueButtonPressed?.Invoke();
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
    /// Update level information
    /// </summary>
    public void UpdateLevelInfo(int completedLevel, int nextLevel)
    {
        if (levelCompleteText != null)
        {
            levelCompleteText.text = levelCompleteMessage;
        }
    }
    
    /// <summary>
    /// Set custom level complete message
    /// </summary>
    public void SetLevelCompleteMessage(string message)
    {
        levelCompleteMessage = message;
        if (levelCompleteText != null)
        {
            levelCompleteText.text = message;
        }
    }
    
    /// <summary>
    /// Enable or disable the continue button
    /// </summary>
    public void SetContinueButtonEnabled(bool enabled)
    {
        if (continueButton != null)
        {
            continueButton.interactable = enabled;
        }
    }
    
    /// <summary>
    /// Update continue button text based on next level type
    /// </summary>
    private void UpdateContinueButtonText()
    {
        if (continueButtonText == null) return;
        
        // Check if next level is a bonus level
        LevelConfig nextLevelConfig = LevelsManager.Instance?.GetLevelConfig(nextLevelNumber);

        continueButtonText.text = "Continue";
        
    }
    
    /// <summary>
    /// Check if UI is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Play celebration effect
    /// </summary>
    public void PlayCelebrationEffect()
    {
        if (celebrationEffect != null)
        {
            celebrationEffect.Play();
        }
        
        if (celebrationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(celebrationSound);
        }
    }
    
    /// <summary>
    /// Stop celebration effect
    /// </summary>
    public void StopCelebrationEffect()
    {
        if (celebrationEffect != null)
        {
            celebrationEffect.Stop();
        }
    }
    
    /// <summary>
    /// Animate coins flying from earned position to balance position
    /// </summary>
    private IEnumerator AnimateCoins(int coinsEarned, int currentBalance)
    {
        // Display coins earned text
        if (coinsEarnedText != null)
        {
            coinsEarnedText.text = $"+{coinsEarned}";
        }
        
        // Display current balance before addition
        if (totalCoinsText != null)
        {
            totalCoinsText.text = currentBalance.ToString();
        }
        
        // Check if we have required references
        if (coinIconTransform == null || balanceIconTransform == null)
        {
            Debug.LogWarning("[LevelCompleteUI] Coin icon or balance icon transform not assigned. Skipping coin animation.");
            // Still update the balance text
            if (totalCoinsText != null)
            {
                totalCoinsText.text = ProgressSaveManager<SaveData>.Instance.GetCoins().ToString();
            }
            yield break;
        }
        
        // Calculate number of coins to spawn (use coinAnimationCount or coinsEarned, whichever is smaller)
        int coinsToSpawn = Mathf.Min(coinAnimationCount, coinsEarned);
        int coinsPerParticle = coinsEarned / coinsToSpawn;
        int remainingCoins = coinsEarned % coinsToSpawn;
        
        // Get world positions
        Vector3 startPos = coinIconTransform.position;
        Vector3 endPos = balanceIconTransform.position;
        
        // Initialize animation state
        coinsCollectedInAnimation = 0;
        displayedBalance = currentBalance;
        
        // List to track active coin particles
        System.Collections.Generic.List<GameObject> activeCoins = new System.Collections.Generic.List<GameObject>();
        
        // Spawn coins with intervals
        for (int i = 0; i < coinsToSpawn; i++)
        {
            int coinsForThisParticle = coinsPerParticle + (i < remainingCoins ? 1 : 0);
            
            // Create coin particle
            GameObject coinParticle = null;
            if (coinPrefab != null)
            {
                coinParticle = Instantiate(coinPrefab, transform);
            }
            else
            {
                // Create a simple sprite if no prefab is assigned
                coinParticle = new GameObject("CoinParticle");
                coinParticle.transform.SetParent(transform);
                Image image = coinParticle.AddComponent<Image>();
                image.color = Color.yellow;
                RectTransform rectTransform = coinParticle.GetComponent<RectTransform>();
                rectTransform.sizeDelta = coinParticleSize;
            }
            
            coinParticle.transform.position = startPos;
            activeCoins.Add(coinParticle);
            
            // Start coroutine to animate this coin particle
            StartCoroutine(AnimateCoinParticle(coinParticle, startPos, endPos, coinsForThisParticle));
            
            // Wait before spawning next coin
            if (i < coinsToSpawn - 1)
            {
                yield return new WaitForSeconds(coinSpawnInterval);
            }
        }
        
        // Wait for all coins to be collected
        while (activeCoins.Count > 0)
        {
            activeCoins.RemoveAll(coin => coin == null);
            yield return null;
        }
        
        // Ensure final balance is correct
        if (totalCoinsText != null)
        {
            totalCoinsText.text = ProgressSaveManager<SaveData>.Instance.GetCoins().ToString();
        }
    }
    
    /// <summary>
    /// Animate a single coin particle flying to balance position
    /// </summary>
    private IEnumerator AnimateCoinParticle(GameObject coinParticle, Vector3 startPos, Vector3 endPos, int coinValue)
    {
        float elapsed = 0f;
        bool hasReached = false;
        
        while (elapsed < coinAnimationDuration && coinParticle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / coinAnimationDuration;
            
            // Move coin toward balance position
            coinParticle.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // Check if coin has reached balance position
            if (!hasReached && Vector3.Distance(coinParticle.transform.position, endPos) <= coinReachThreshold)
            {
                hasReached = true;
                coinsCollectedInAnimation += coinValue;
                displayedBalance = displayedBalance + coinValue;
                
                // Play collect sound
                if (coinCollectSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(coinCollectSound);
                }
                
                // Update balance text
                if (totalCoinsText != null)
                {
                    totalCoinsText.text = displayedBalance.ToString();
                }
                
                // Destroy coin particle
                Destroy(coinParticle);
                yield break;
            }
            
            yield return null;
        }
        
        // If coin didn't reach (shouldn't happen), destroy it anyway
        if (coinParticle != null)
        {
            coinsCollectedInAnimation += coinValue;
            displayedBalance = displayedBalance + coinValue;
            
            // Play collect sound
            if (coinCollectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(coinCollectSound);
            }
            
            // Update balance text
            if (totalCoinsText != null)
            {
                totalCoinsText.text = displayedBalance.ToString();
            }
            
            Destroy(coinParticle);
        }
    }
}
