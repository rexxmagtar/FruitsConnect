using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    
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
        
        // Set alpha to 1 immediately (no fade in)
        canvasGroup.alpha = 1f;
        
        // Show immediately when app starts (before GameManager initialization)
        Show();
    }
    
    private void Start()
    {
        // Subscribe to initialization complete event
        GameManager.OnGameInitialized += OnGameInitialized;
    }
    
    private void OnDestroy()
    {
        GameManager.OnGameInitialized -= OnGameInitialized;
    }
    
    /// <summary>
    /// Called when GameManager finishes initialization
    /// </summary>
    private void OnGameInitialized()
    {
        StartCoroutine(PreloadLevelAndShowMenu());
    }
    
    /// <summary>
    /// Preload current level, then show main menu
    /// </summary>
    private IEnumerator PreloadLevelAndShowMenu()
    {
        // Initialize LevelsManager
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager != null)
        {
            levelsManager.Initialize();
            yield return null;
            
            // Get current level config
            LevelConfig levelConfig = levelsManager.GetCurrentLevelConfig();
            
            if (levelConfig != null)
            {
                // Preload level in GameController
                GameController gameController = GameController.Instance;
                if (gameController != null)
                {
                    gameController.PreloadLevel(levelConfig);
                    yield return null;
                }
            }
        }
        
        // Show main menu
        MainMenuUI mainMenu = FindObjectOfType<MainMenuUI>(true);
        if (mainMenu != null)
        {
            mainMenu.gameObject.SetActive(true);
            mainMenu.Show();
        }
        
        // Hide loading screen
        Hide();
    }
    
    /// <summary>
    /// Show the loading screen (appears instantly, no fade in)
    /// </summary>
    public void Show()
    {
        if (isVisible) return;
        
        isVisible = true;
        gameObject.SetActive(true);
        
        // Set alpha to 1 immediately (no fade in as requested)
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
    }
    
    /// <summary>
    /// Hide the loading screen (fades out)
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        StartCoroutine(FadeOut());
    }
    
    /// <summary>
    /// Fade out animation
    /// </summary>
    private IEnumerator FadeOut()
    {
        isVisible = false;
        
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Set the background image sprite
    /// </summary>
    public void SetBackgroundSprite(Sprite sprite)
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = sprite;
        }
    }
}

