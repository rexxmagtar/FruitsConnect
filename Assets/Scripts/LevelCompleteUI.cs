using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using JigsawSystem;
using DataRepository;
using DG.Tweening;
using AdsServices;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private Button continueButton; // Deprecated - kept for backward compatibility but hidden
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private TextMeshProUGUI continueButtonText; // Deprecated - kept for backward compatibility but hidden
    [SerializeField] private TextMeshProUGUI coinsEarnedText;
    [SerializeField] private TextMeshProUGUI energySpheresEarnedText;
    [SerializeField] private TextMeshProUGUI totalCoinsText;
    [SerializeField] private TextMeshProUGUI totalEnergySpheresText;
    [SerializeField] private GameObject energyBalanceContainer;
    [SerializeField] private RectTransform coinIconTransform;
    [SerializeField] private RectTransform energySphereIconTransform;
    [SerializeField] private RectTransform balanceIconTransform;
    [SerializeField] private RectTransform energyBalanceIconTransform;
    
    [Header("Boss Reward References")]
    [SerializeField] private GameObject bossRewardContainer;
    [SerializeField] private TextMeshProUGUI bossBaseRewardText;
    [SerializeField] private TextMeshProUGUI bossEnergyRewardText;
    [SerializeField] private TextMeshProUGUI bossMultiplierText;
    [SerializeField] private GameObject bossBountyLocker;
    [SerializeField] private RectTransform bossRewardIconTransform;
    [SerializeField] private RectTransform bossEnergyRewardIconTransform;
    
    [Header("Double Rewards Button")]
    [SerializeField] private Button doubleRewardsButton;
    [SerializeField] private GameObject doubleRewardsLoadingContainer;
    [SerializeField] private GameObject doubleRewardsClickContainer;
    [SerializeField] private RectTransform doubleRewardsLoadingIcon;
    [SerializeField] private RectTransform doubleRewardsAdIconContainer;
    [SerializeField] private float doubleRewardsRotationSpeed = 180f;
    [SerializeField] private float doubleRewardsPulseScale = 1.15f;
    [SerializeField] private float doubleRewardsPulseDuration = 0.25f;
    [SerializeField] private float doubleRewardsPauseDuration = 1.5f;
    
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

    [Header("Confetti Settings")]
    [SerializeField] private int confettiCount = 50;
    [SerializeField] private int confettiBurstCount = 3;
    [SerializeField] private float confettiSpawnDuration = 2f;
    [SerializeField] private float confettiAnimationDuration = 3f;
    [SerializeField] private Vector2 confettiMinSize = new Vector2(10f, 10f);
    [SerializeField] private Vector2 confettiMaxSize = new Vector2(20f, 20f);
    [SerializeField] private float confettiSpawnZoneSize = 50f;
    [SerializeField] private Color[] confettiColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };
    [SerializeField] private RectTransform confettiParent;
    
    [Header("Coin Animation Settings")]
    [SerializeField] private int coinAnimationCount = 5;
    [SerializeField] private float coinAnimationDuration = 1.5f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject energySpherePrefab;
    [SerializeField] private float coinSpawnInterval = 0.1f;
    [SerializeField] private float coinReachThreshold = 10f;
    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private Vector2 coinParticleSize = new Vector2(30f, 30f);
    
    [Header("Fruit Progress Indicator")]
    [SerializeField] private FruitsProgressIndicatorConfig progressConfig;
    [SerializeField] private List<Image> fruitImages = new List<Image>();
    [SerializeField] private Image filledImage;
    [SerializeField] private TextMeshProUGUI progressPercentText;
    [SerializeField] private RectTransform progressContainer;
    [SerializeField] private AudioClip progressCompleteSound;
    [SerializeField] private float progressAnimationDuration = 1f;
    [SerializeField] private float progressCompleteScaleDuration = 0.5f;
    [SerializeField] private float progressCompleteScaleAmount = 1.2f;
    
    // Events
    public static event System.Action OnContinueButtonPressed; // Deprecated - kept for backward compatibility
    public static event System.Action OnReturnToMenuButtonPressed;
    
    // State
    private CanvasGroup canvasGroup;
     [SerializeField]private AudioSource audioSource;
    private bool isVisible = false;
    private int coinsEarned = 0;
    private int energySpheresEarned = 0;
    private int coinsCollectedInAnimation = 0;
    private int energySpheresCollectedInAnimation = 0;
    private int nextLevelNumber = 1;
    private bool isAnimating = false;
    private System.Collections.Generic.List<string> earnedPuzzlePieces;
    
    // Boss Reward State
    private bool isBossLevel = false;
    private int bossBaseReward = 0;
    private int bossEnergyReward = 0;
    private float bossMultiplier = 1f;
    private bool isBossDefeated = false;
    private int bossTotalReward = 0;
    private int bossTotalEnergyReward = 0;
    
    // Fruit Progress State
    private float currentProgressPercent = 0f;
    
    // Double Rewards State
    private bool hasRewardsBeenDoubled = false;
    private bool isShowingAd = false;
    private Coroutine doubleRewardsPulseCoroutine;
    private int originalCoinsEarned = 0;
    private int originalEnergySpheresEarned = 0;
    private int originalBossTotalReward = 0;
    private int originalBossTotalEnergyReward = 0;
    private Tween doubleRewardsPulseTween;
    
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
        
        // Setup continue button (deprecated - hide it)
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
        
        // Setup return to menu button
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClick);
        }
        
        // Setup double rewards button
        if (doubleRewardsButton != null)
        {
            doubleRewardsButton.onClick.AddListener(OnDoubleRewardsButtonClick);
            
            // Ensure button is active but hidden using CanvasGroup
            CanvasGroup doubleRewardsCanvasGroup = doubleRewardsButton.GetComponent<CanvasGroup>();
            if (doubleRewardsCanvasGroup == null)
            {
                doubleRewardsCanvasGroup = doubleRewardsButton.gameObject.AddComponent<CanvasGroup>();
            }
            // Hide button but keep it active for autolayout
            doubleRewardsCanvasGroup.alpha = 0f;
            doubleRewardsCanvasGroup.interactable = false;
            doubleRewardsCanvasGroup.blocksRaycasts = false;
        }

        // Try to find bossRewardIconTransform if not assigned
        if (bossRewardIconTransform == null && bossRewardContainer != null)
        {
            Transform bossIcon = bossRewardContainer.transform.Find("BossIcon");
            if (bossIcon != null)
            {
                bossRewardIconTransform = bossIcon.GetComponent<RectTransform>();
            }
            else
            {
                // Fallback: look for anything with "Icon" or "Coin" in name
                foreach (Transform child in bossRewardContainer.transform)
                {
                    if (child.name.Contains("Icon") || child.name.Contains("Coin"))
                    {
                        bossRewardIconTransform = child.GetComponent<RectTransform>();
                        break;
                    }
                }
            }
        }
    }
    
    private void Start()
    {
    }
    
    /// <summary>
    /// Show the level complete UI
    /// </summary>
    public void Show(int coinsEarned = 0, bool isBossLevel = false, int energySpheresEarned = 0, int nextLevel = 1, int bossBaseReward = 0, int bossEnergyReward = 0, float bossMultiplier = 1f, bool bossDefeated = false, System.Collections.Generic.List<string> puzzlePieces = null)
    {
        if (isVisible) return;
        
        this.coinsEarned = coinsEarned;
        this.energySpheresEarned = energySpheresEarned;
        this.nextLevelNumber = nextLevel;
        this.isBossLevel = isBossLevel;
        this.bossBaseReward = bossBaseReward;
        this.bossEnergyReward = bossEnergyReward;
        this.bossMultiplier = bossMultiplier;
        this.isBossDefeated = bossDefeated;
        this.earnedPuzzlePieces = puzzlePieces;
        this.bossTotalReward = isBossDefeated ? (int)(bossBaseReward * bossMultiplier) : 0;
        this.bossTotalEnergyReward = isBossDefeated ? (int)(bossEnergyReward * bossMultiplier) : 0;
        
        // Store original rewards for doubling
        this.originalCoinsEarned = coinsEarned;
        this.originalEnergySpheresEarned = energySpheresEarned;
        this.originalBossTotalReward = this.bossTotalReward;
        this.originalBossTotalEnergyReward = this.bossTotalEnergyReward;
        this.hasRewardsBeenDoubled = false;
        this.isShowingAd = false;

        // Hide energy UI if level < 15
        bool isEnergyUnlocked = nextLevelNumber > 15;
        if (energySpheresEarnedText != null) energySpheresEarnedText.gameObject.SetActive(isEnergyUnlocked);
        if (energySphereIconTransform != null) energySphereIconTransform.gameObject.SetActive(isEnergyUnlocked);
        
        if (bossEnergyRewardText != null) bossEnergyRewardText.gameObject.SetActive(isEnergyUnlocked && isBossLevel);
        if (bossEnergyRewardIconTransform != null) bossEnergyRewardIconTransform.gameObject.SetActive(isEnergyUnlocked && isBossLevel);
        
        if (energyBalanceContainer != null) energyBalanceContainer.SetActive(isEnergyUnlocked);
        
        gameObject.SetActive(true);

        // Prepare review if level 5, 15, 25... completed
        int completedLevel = nextLevelNumber - 1;
        if (completedLevel > 0 && completedLevel % 10 == 5)
        {
            if (Management.ReviewManager.Instance != null)
            {
                Management.ReviewManager.Instance.PrepareReview();
            }
        }

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
    /// Hide the return to menu button by fading it out
    /// </summary>
    private void HideMenuButton()
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.interactable = false;
            
            CanvasGroup btnGroup = returnToMenuButton.GetComponent<CanvasGroup>();
            if (btnGroup == null)
            {
                btnGroup = returnToMenuButton.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Fade out the button
            btnGroup.DOFade(0f, buttonFadeInDuration).OnComplete(() => {
                if (btnGroup != null)
                {
                    btnGroup.blocksRaycasts = false;
                }
            });
        }
    }
    
    /// <summary>
    /// Fade out all images in an array
    /// </summary>
    private IEnumerator FadeOutImages(Image[] images, float[] originalAlphas, float duration)
    {
        if (images == null || images.Length == 0 || originalAlphas == null) yield break;
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            
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
        
        // Ensure final alpha is 0
        for (int i = 0; i < images.Length && i < originalAlphas.Length; i++)
        {
            if (images[i] != null)
            {
                Color color = images[i].color;
                color.a = 0f;
                images[i].color = color;
            }
        }
    }
    
    /// <summary>
    /// Fade out all text components in an array
    /// </summary>
    private IEnumerator FadeOutTexts(TextMeshProUGUI[] texts, float[] originalAlphas, float duration)
    {
        if (texts == null || texts.Length == 0 || originalAlphas == null) yield break;
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            
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
        
        // Ensure final alpha is 0
        for (int i = 0; i < texts.Length && i < originalAlphas.Length; i++)
        {
            if (texts[i] != null)
            {
                Color color = texts[i].color;
                color.a = 0f;
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
        
        // Hide menu button initially using CanvasGroup (like LevelFailedUI does)
        if (returnToMenuButton != null)
        {
            returnToMenuButton.interactable = false;
            
            CanvasGroup btnGroup = returnToMenuButton.GetComponent<CanvasGroup>();
            if (btnGroup == null)
            {
                btnGroup = returnToMenuButton.gameObject.AddComponent<CanvasGroup>();
            }
            btnGroup.alpha = 0f;
            btnGroup.interactable = false;
            btnGroup.blocksRaycasts = false;
        }

        // Initialize texts to zero
        if (coinsEarnedText != null) coinsEarnedText.text = "+0";
        if (energySpheresEarnedText != null) energySpheresEarnedText.text = "+0";
        if (bossBaseRewardText != null) bossBaseRewardText.text = "0";
        if (bossEnergyRewardText != null) bossEnergyRewardText.text = "0";
        if (bossMultiplierText != null) 
        {
            bossMultiplierText.text = $"Time multiplier \n x{bossMultiplier:F2}";
            bossMultiplierText.gameObject.SetActive(false);
            bossMultiplierText.transform.localScale = Vector3.zero;
        }
        
        // Setup boss container and locker
        if (bossRewardContainer != null) bossRewardContainer.SetActive(isBossLevel);
        if (bossBountyLocker != null) bossBountyLocker.SetActive(isBossLevel && !isBossDefeated);
        
        // Hide double rewards button initially using alpha (button stays active for autolayout)
        if (doubleRewardsButton != null)
        {
            CanvasGroup doubleRewardsCanvasGroup = doubleRewardsButton.GetComponent<CanvasGroup>();
            if (doubleRewardsCanvasGroup == null)
            {
                doubleRewardsCanvasGroup = doubleRewardsButton.gameObject.AddComponent<CanvasGroup>();
            }
            doubleRewardsCanvasGroup.alpha = 0f;
            doubleRewardsCanvasGroup.interactable = false;
            doubleRewardsCanvasGroup.blocksRaycasts = false;
        }
        
        // Initialize fruit progress immediately (before any animations)
        InitializeFruitProgress();

        int finalBalanceTotal = SaveDataExtensions.GetTotalCoins();
        int initialBalance = finalBalanceTotal - (coinsEarned + bossTotalReward);

        int finalEnergyBalanceTotal = SaveDataExtensions.GetTotalEnergySpheres();
        int initialEnergyBalance = finalEnergyBalanceTotal - (energySpheresEarned + bossTotalEnergyReward);

        if (totalCoinsText != null) totalCoinsText.text = initialBalance.ToString();
        if (totalEnergySpheresText != null) totalEnergySpheresText.text = initialEnergyBalance.ToString();
        
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

        // Spawn UI confetti and wait for it to finish spawning before money animations
        yield return StartCoroutine(SpawnConfettiParticlesRoutine());

        // Sequence: Regular Reward
        if (coinsEarned > 0)
        {
            yield return StartCoroutine(AnimateTextCount(coinsEarnedText, 0, coinsEarned, 0.5f, "+"));
        }
        if (energySpheresEarned > 0)
        {
            yield return StartCoroutine(AnimateTextCount(energySpheresEarnedText, 0, energySpheresEarned, 0.5f, "+"));
        }

        
        
        // Sequence: Money Animations
        int totalCollectedSoFar = 0;
        
        // Basic money fly
        if (coinsEarned > 0)
        {
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(AnimateCurrency(coinsEarned, initialBalance, coinIconTransform, balanceIconTransform, totalCoinsText, coinPrefab, coinsEarnedText));
            totalCollectedSoFar += coinsEarned;
        }

        // Basic energy fly
        if (energySpheresEarned > 0)
        {
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(AnimateCurrency(energySpheresEarned, initialEnergyBalance, energySphereIconTransform, energyBalanceIconTransform, totalEnergySpheresText, energySpherePrefab, energySpheresEarnedText));
        }

        // Sequence: Boss Reward
        if (isBossLevel)
        {
            yield return new WaitForSeconds(0.2f);
            if (bossBaseReward > 0) yield return StartCoroutine(AnimateTextCount(bossBaseRewardText, 0, bossBaseReward, 0.5f));
            if (bossEnergyReward > 0) yield return StartCoroutine(AnimateTextCount(bossEnergyRewardText, 0, bossEnergyReward, 0.5f));

            if (isBossDefeated)
            {
                yield return new WaitForSeconds(0.2f);
                // Scale up multiplier
                if (bossMultiplierText != null)
                {
                    bossMultiplierText.gameObject.SetActive(true);
                    float scaleElapsed = 0f;
                    float scaleDuration = 0.3f;
                    while (scaleElapsed < scaleDuration)
                    {
                        scaleElapsed += Time.deltaTime;
                        bossMultiplierText.transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, scaleElapsed / scaleDuration);
                        yield return null;
                    }
                    bossMultiplierText.transform.localScale = Vector3.one;
                }

                yield return new WaitForSeconds(0.1f);
                // Increase base text amount to total boss reward
                if (bossTotalReward > 0) yield return StartCoroutine(AnimateTextCount(bossBaseRewardText, bossBaseReward, bossTotalReward, 0.5f));
                if (bossTotalEnergyReward > 0) yield return StartCoroutine(AnimateTextCount(bossEnergyRewardText, bossEnergyReward, bossTotalEnergyReward, 0.5f));
            }
        }

        // Boss money fly
        if (isBossLevel && bossTotalReward > 0)
        {
            yield return new WaitForSeconds(0.5f); // Wait a bit more between animations
            yield return StartCoroutine(AnimateCurrency(bossTotalReward, initialBalance + totalCollectedSoFar, bossRewardIconTransform, balanceIconTransform, totalCoinsText, coinPrefab, bossBaseRewardText));
        }

        // Boss energy fly
        if (isBossLevel && bossTotalEnergyReward > 0)
        {
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(AnimateCurrency(bossTotalEnergyReward, initialEnergyBalance + energySpheresEarned, bossEnergyRewardIconTransform, energyBalanceIconTransform, totalEnergySpheresText, energySpherePrefab, bossEnergyRewardText));
        }
        
        
        
        // Animate fruit progress delta (initial progress was already set)
        yield return StartCoroutine(AnimateFruitProgress());

        yield return new WaitForSeconds(0.3f);
        

        // Show puzzle piece earned popups if any
        if (earnedPuzzlePieces != null && earnedPuzzlePieces.Count > 0)
        {
            JigsawSystem.PuzzlePieceEarnedUI.Instance.Show(earnedPuzzlePieces);
            
            // Wait for puzzle piece window to close before showing buttons
            if (JigsawSystem.PuzzlePieceEarnedUI.Instance != null)
            {
                while (JigsawSystem.PuzzlePieceEarnedUI.Instance.IsVisible)
                {
                    yield return null;
                }
            }
        }

        // Show spirit unlock UI if level 5 was just completed
        if (nextLevelNumber - 1 == 5)
        {
            if (UI.ShoSpiritsUnlockUI.Instance != null)
            {
                UI.ShoSpiritsUnlockUI.Instance.Show();
            }
        }

        // All reward animations complete - show double rewards button first
        InitializeDoubleRewardsButton();
        isAnimating = false;
        
        // Wait 2 seconds before showing continue button
        yield return new WaitForSeconds(2f);
        
        // Now fade in menu button using CanvasGroup (like LevelFailedUI does)
        if (returnToMenuButton != null)
        {
            CanvasGroup btnGroup = returnToMenuButton.GetComponent<CanvasGroup>();
            if (btnGroup == null)
            {
                btnGroup = returnToMenuButton.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Set initial state
            btnGroup.alpha = 0f;
            btnGroup.blocksRaycasts = true;
            
            // Fade in the button
            btnGroup.DOFade(1f, buttonFadeInDuration).OnComplete(() => {
                if (btnGroup != null)
                {
                    btnGroup.interactable = true;
                }
            });
        }
        
        // Wait for fade-in to complete
        yield return new WaitForSeconds(buttonFadeInDuration);

        // Enable button
        if (returnToMenuButton != null)
        {
            returnToMenuButton.interactable = true;
        }

        // Show review if prepared (Level 5, 15, 25...)
        int completedLevel = nextLevelNumber - 1;
        if (completedLevel > 0 && completedLevel % 10 == 5)
        {
            if (Management.ReviewManager.Instance != null)
            {
                Management.ReviewManager.Instance.ShowReview();
            }
        }
    }

    /// <summary>
    /// Animate text counting from start to end
    /// </summary>
    private IEnumerator AnimateTextCount(TextMeshProUGUI text, int start, int end, float duration, string prefix = "")
    {
        if (text == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            int current = (int)Mathf.Lerp(start, end, elapsed / duration);
            text.text = prefix + current.ToString();
            yield return null;
        }
        text.text = prefix + end.ToString();
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
    /// Spawn UI confetti particles with a fountain trajectory from bottom corners
    /// </summary>
    private IEnumerator SpawnConfettiParticlesRoutine()
    {
        RectTransform parent = confettiParent != null ? confettiParent : (RectTransform)transform;
        int totalBursts = confettiCount / confettiBurstCount;
        if (totalBursts <= 0) totalBursts = 1;
        float interval = confettiSpawnDuration / totalBursts;
        
        for (int i = 0; i < totalBursts; i++)
        {
            // Spawn multiple particles from both sides in each burst
            for (int j = 0; j < confettiBurstCount; j++)
            {
                SpawnSingleConfetti(parent, true);  // Left side
                SpawnSingleConfetti(parent, false); // Right side
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnSingleConfetti(RectTransform parent, bool isLeft)
    {
        GameObject confettiObj = new GameObject("Confetti");
        confettiObj.transform.SetParent(parent, false);
        
        RectTransform rect = confettiObj.AddComponent<RectTransform>();
        Image img = confettiObj.AddComponent<Image>();
        
        // Random color
        if (confettiColors != null && confettiColors.Length > 0)
        {
            img.color = confettiColors[Random.Range(0, confettiColors.Length)];
        }
        else
        {
            img.color = new Color(Random.value, Random.value, Random.value, 1f);
        }
        
        // Random size
        float sizeX = Random.Range(confettiMinSize.x, confettiMaxSize.x);
        float sizeY = Random.Range(confettiMinSize.y, confettiMaxSize.y);
        rect.sizeDelta = new Vector2(sizeX, sizeY);
        
        LayoutElement le = confettiObj.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        img.raycastTarget = false;
        
        // Use Anchors to ensure it stays in the corners regardless of screen size
        if (isLeft)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
        }
        else
        {
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
        }

        // Randomize spawn within anchor zone
        float zoneOffset = confettiSpawnZoneSize;
        float startX = isLeft ? Random.Range(0, zoneOffset) : Random.Range(-zoneOffset, 0);
        float startY = Random.Range(0, zoneOffset);
        rect.anchoredPosition = new Vector2(startX, startY);
        
        // Random initial rotation
        rect.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
        
        float duration = Random.Range(confettiAnimationDuration * 0.8f, confettiAnimationDuration * 1.2f);
        
        // Peak and End positions relative to the anchor
        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        
        // Horizontal distance: how far across the screen to travel
        float horizontalTravel = isLeft ? Random.Range(parentWidth * 0.3f, parentWidth * 0.8f) : Random.Range(-parentWidth * 0.8f, -parentWidth * 0.3f);
        
        // Vertical peaks
        float peakY = Random.Range(parentHeight * 0.4f, parentHeight * 0.9f);
        float endY = -100f; // Below the anchor (off screen)

        // Sequence for the trajectory
        Sequence trajectory = DOTween.Sequence();
        
        // Horizontal movement: burst towards center
        trajectory.Join(rect.DOAnchorPosX(startX + horizontalTravel, duration).SetEase(Ease.OutQuad));
        
        // Vertical movement: burst UP, then fall DOWN
        Sequence verticalSeq = DOTween.Sequence();
        verticalSeq.Append(rect.DOAnchorPosY(peakY, duration * 0.4f).SetEase(Ease.OutQuad));
        verticalSeq.Append(rect.DOAnchorPosY(endY, duration * 0.6f).SetEase(Ease.InQuad));
        trajectory.Join(verticalSeq);
        
        // Rotation and flipping
        rect.DORotate(new Vector3(0, 0, Random.Range(-720f, 720f)), duration, RotateMode.FastBeyond360);
        rect.DOScaleX(0, duration / 4).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        
        // Fade and Cleanup
        CanvasGroup cg = confettiObj.AddComponent<CanvasGroup>();
        cg.DOFade(0, 0.5f).SetDelay(duration - 0.5f).OnComplete(() => {
            if (confettiObj != null) Destroy(confettiObj);
        });
    }
    
    /// <summary>
    /// Animate currency flying from earned position to balance position
    /// </summary>
    private IEnumerator AnimateCurrency(int amountToAnimate, int currentBalance, RectTransform sourceIcon, RectTransform targetIcon, TextMeshProUGUI balanceText, GameObject prefab, TextMeshProUGUI earnedText = null)
    {
        // Display amount earned text
        if (earnedText != null)
        {
            string prefix = "+";
            earnedText.text = prefix + amountToAnimate.ToString();
        }
        
        // Display current balance before addition
        if (balanceText != null)
        {
            balanceText.text = currentBalance.ToString();
        }
        
        // Check if we have required references
        if (sourceIcon == null || targetIcon == null)
        {
            Debug.LogWarning("[LevelCompleteUI] Source or target icon transform not assigned. Skipping animation.");
            // Still update the balance text
            if (balanceText != null)
            {
                balanceText.text = (currentBalance + amountToAnimate).ToString();
            }
            yield break;
        }
        
        // Calculate number of particles to spawn (use coinAnimationCount or amountToAnimate, whichever is smaller)
        int particlesToSpawn = Mathf.Min(coinAnimationCount, amountToAnimate);
        int amountPerParticle = amountToAnimate / particlesToSpawn;
        int remainingAmount = amountToAnimate % particlesToSpawn;
        
        // Get world positions
        Vector3 startPos = sourceIcon.position;
        Vector3 endPos = targetIcon.position;
        
        // Initialize animation state
        int localDisplayedBalance = currentBalance;
        
        // List to track active particles
        System.Collections.Generic.List<GameObject> activeParticles = new System.Collections.Generic.List<GameObject>();
        
        // Spawn particles with intervals
        for (int i = 0; i < particlesToSpawn; i++)
        {
            int amountForThisParticle = amountPerParticle + (i < remainingAmount ? 1 : 0);
            
            // Create particle
            GameObject particle = null;
            if (prefab != null)
            {
                particle = Instantiate(prefab, transform);
            }
            else
            {
                // Create a simple sprite if no prefab is assigned
                particle = new GameObject("CurrencyParticle");
                particle.transform.SetParent(transform);
                Image image = particle.AddComponent<Image>();
                image.color = Color.yellow;
                RectTransform rectTransform = particle.GetComponent<RectTransform>();
                rectTransform.sizeDelta = coinParticleSize;
            }

            // Ensure particle doesn't affect layout
            LayoutElement layoutElement = particle.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = particle.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            
            particle.transform.position = startPos;
            activeParticles.Add(particle);
            
            // Start coroutine to animate this particle
            StartCoroutine(AnimateCurrencyParticle(particle, startPos, endPos, amountForThisParticle, balanceText, (v) => {
                localDisplayedBalance += v;
                return localDisplayedBalance;
            }));
            
            // Wait before spawning next particle
            if (i < particlesToSpawn - 1)
            {
                yield return new WaitForSeconds(coinSpawnInterval);
            }
        }
        
        // Wait for all particles to be collected
        while (activeParticles.Count > 0)
        {
            activeParticles.RemoveAll(p => p == null);
            yield return null;
        }
        
        // Ensure final balance is correct
        if (balanceText != null)
        {
            balanceText.text = (currentBalance + amountToAnimate).ToString();
        }
    }
    
    /// <summary>
    /// Animate a single currency particle flying to balance position
    /// </summary>
    private IEnumerator AnimateCurrencyParticle(GameObject particle, Vector3 startPos, Vector3 endPos, int particleValue, TextMeshProUGUI balanceText, System.Func<int, int> updateBalance)
    {
        float elapsed = 0f;
        bool hasReached = false;
        
        while (elapsed < coinAnimationDuration && particle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / coinAnimationDuration;
            
            // Move particle toward balance position
            particle.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // Check if particle has reached balance position
            if (!hasReached && Vector3.Distance(particle.transform.position, endPos) <= coinReachThreshold)
            {
                hasReached = true;
                int newBalance = updateBalance(particleValue);
                
                if (balanceText == totalCoinsText) coinsCollectedInAnimation += particleValue;
                else if (balanceText == totalEnergySpheresText) energySpheresCollectedInAnimation += particleValue;

                // Play collect sound
                if (coinCollectSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(coinCollectSound);
                }
                
                // Update balance text
                if (balanceText != null)
                {
                    balanceText.text = newBalance.ToString();
                }
                
                // Destroy particle
                Destroy(particle);
                yield break;
            }
            
            yield return null;
        }
        
        // If particle didn't reach (shouldn't happen), destroy it anyway
        if (particle != null)
        {
            int newBalance = updateBalance(particleValue);
            
            if (balanceText == totalCoinsText) coinsCollectedInAnimation += particleValue;
            else if (balanceText == totalEnergySpheresText) energySpheresCollectedInAnimation += particleValue;

            // Play collect sound
            if (coinCollectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(coinCollectSound);
            }
            
            // Update balance text
            if (balanceText != null)
            {
                balanceText.text = newBalance.ToString();
            }
            
            Destroy(particle);
        }
    }
    
    /// <summary>
    /// Initialize fruit progress indicator immediately (called at start of ShowAnimation)
    /// </summary>
    private void InitializeFruitProgress()
    {
        if (progressConfig == null)
        {
            return; // Silently fail - config might not be assigned yet
        }
        
        // Get the level that was just completed (1-indexed for display)
        // Note: nextLevelNumber is the next level to play, so the completed level is nextLevelNumber - 1
        int completedLevel = nextLevelNumber-1;
        
        // Ensure we have a valid level (at least 1)
        if (completedLevel < 1)
        {
            completedLevel = 1;
        }
        
        // Calculate progress BEFORE completing the current level (previous level)
        int previousLevel = completedLevel - 1;

        // Get active fruit data for the completed level
        ProgressFruitData fruitData = progressConfig.GetActiveFruitData(completedLevel);
        if (fruitData == null)
        {
            return; // Silently fail - might not have fruit data configured
        }
        
        // Check if previous level and completed level are in the same fruit block
        // Calculate progress percentage BEFORE completing current level
        float startProgress;

        if(previousLevel % 10 == 0){
        startProgress = 0f;
        }
        else{
            startProgress = progressConfig.GetProgressPercentage(previousLevel);
        }
        
        // Set fruit sprite on all fruit images
        if (fruitData.fruitSprite != null)
        {
            foreach (Image fruitImage in fruitImages)
            {
                if (fruitImage != null)
                {
                    fruitImage.sprite = fruitData.fruitSprite;
                }
            }
        }
        
        // Initialize progress display with the previous progress value
        if (filledImage != null)
        {
            // Ensure filled image is set to Filled type
            filledImage.type = Image.Type.Filled;
            filledImage.fillAmount = startProgress / 100f; // Convert percentage to 0-1 range
        }
        
        if (progressPercentText != null)
        {
            progressPercentText.text = $"{Mathf.RoundToInt(startProgress)}%";
        }
        
        // Update current progress state
        currentProgressPercent = startProgress;
    }
    
    /// <summary>
    /// Animate fruit progress delta (initial progress should already be set)
    /// </summary>
    private IEnumerator AnimateFruitProgress()
    {
        if (progressConfig == null)
        {
            Debug.LogWarning("[LevelCompleteUI] FruitsProgressIndicatorConfig not assigned!");
            yield break;
        }
        
        // Get the level that was just completed (1-indexed for display)
        int completedLevel = nextLevelNumber - 1;
        
        // Ensure we have a valid level (at least 1)
        if (completedLevel < 1)
        {
            completedLevel = 1;
        }
        
        // Calculate progress percentage AFTER completing current level
        float targetProgress = progressConfig.GetProgressPercentage(completedLevel);
        if (targetProgress < 0f)
        {
            Debug.LogWarning($"[LevelCompleteUI] Invalid progress percentage for level {completedLevel}");
            yield break;
        }
        
        // Animate progress fill and text simultaneously (in parallel)
        StartCoroutine(AnimateProgressFill(targetProgress));
        StartCoroutine(AnimateProgressText(targetProgress));
        
        // Wait for animations to complete (both have the same duration)
        yield return new WaitForSeconds(progressAnimationDuration);
        
        // Check if we reached 100%
        if (targetProgress >= 100f)
        {
            yield return StartCoroutine(PlayProgressCompleteAnimation());
        }
    }
    
    /// <summary>
    /// Animate progress fill image from current to target percentage
    /// </summary>
    private IEnumerator AnimateProgressFill(float targetPercent)
    {
        if (filledImage == null) yield break;
        
        float startFill = filledImage.fillAmount;
        float targetFill = targetPercent / 100f; // Convert percentage to 0-1 range
        float elapsed = 0f;
        
        while (elapsed < progressAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / progressAnimationDuration;
            filledImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);
            yield return null;
        }
        
        filledImage.fillAmount = targetFill;
        currentProgressPercent = targetPercent;
    }
    
    /// <summary>
    /// Animate progress percentage text from current to target
    /// </summary>
    private IEnumerator AnimateProgressText(float targetPercent)
    {
        if (progressPercentText == null) yield break;
        
        float startPercent = currentProgressPercent;
        float elapsed = 0f;
        
        while (elapsed < progressAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / progressAnimationDuration;
            float currentPercent = Mathf.Lerp(startPercent, targetPercent, t);
            progressPercentText.text = $"{Mathf.RoundToInt(currentPercent)}%";
            yield return null;
        }
        
        progressPercentText.text = $"{Mathf.RoundToInt(targetPercent)}%";
    }
    
    /// <summary>
    /// Play scale animation and sound when progress reaches 100%
    /// </summary>
    private IEnumerator PlayProgressCompleteAnimation()
    {
        if (progressContainer == null) yield break;
        
        // Play sound
        if (progressCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(progressCompleteSound);
        }
        
        Vector3 originalScale = progressContainer.localScale;
        float elapsed = 0f;
        
        // Scale up
        while (elapsed < progressCompleteScaleDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (progressCompleteScaleDuration / 2f);
            float scale = Mathf.Lerp(1f, progressCompleteScaleAmount, t);
            progressContainer.localScale = originalScale * scale;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < progressCompleteScaleDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (progressCompleteScaleDuration / 2f);
            float scale = Mathf.Lerp(progressCompleteScaleAmount, 1f, t);
            progressContainer.localScale = originalScale * scale;
            yield return null;
        }
        
        // Ensure final scale is correct
        progressContainer.localScale = originalScale;
    }
    
    /// <summary>
    /// Initialize double rewards button state and animations
    /// </summary>
    private void InitializeDoubleRewardsButton()
    {
        if (doubleRewardsButton == null) return;
        
        // Show the button using CanvasGroup (button is already active)
        CanvasGroup doubleRewardsCanvasGroup = doubleRewardsButton.GetComponent<CanvasGroup>();
        if (doubleRewardsCanvasGroup == null)
        {
            doubleRewardsCanvasGroup = doubleRewardsButton.gameObject.AddComponent<CanvasGroup>();
        }
        doubleRewardsCanvasGroup.alpha = 1f;
        doubleRewardsCanvasGroup.interactable = true;
        doubleRewardsCanvasGroup.blocksRaycasts = true;
        
        UpdateDoubleRewardsButtonState();
        StartDoubleRewardsPulseAnimation();
    }
    
    /// <summary>
    /// Update double rewards button state based on ad readiness
    /// </summary>
    private void UpdateDoubleRewardsButtonState()
    {
        if (hasRewardsBeenDoubled)
        {
            // Hide button if rewards already doubled using alpha (button stays active for autolayout)
            if (doubleRewardsButton != null)
            {
                CanvasGroup doubleRewardsCanvasGroup = doubleRewardsButton.GetComponent<CanvasGroup>();
                if (doubleRewardsCanvasGroup == null)
                {
                    doubleRewardsCanvasGroup = doubleRewardsButton.gameObject.AddComponent<CanvasGroup>();
                }
                doubleRewardsCanvasGroup.alpha = 0f;
                doubleRewardsCanvasGroup.interactable = false;
                doubleRewardsCanvasGroup.blocksRaycasts = false;
            }
            return;
        }
        
        bool isReady = AdsManager.Instance != null && AdsManager.Instance.IsRewardedAdReady();
        
        if (doubleRewardsLoadingContainer != null) 
            doubleRewardsLoadingContainer.SetActive(!isReady);
        if (doubleRewardsClickContainer != null) 
            doubleRewardsClickContainer.SetActive(isReady);
        
        // Update button and CanvasGroup interactability
        if (doubleRewardsButton != null)
        {
            doubleRewardsButton.interactable = isReady;
            
            // Also update CanvasGroup interactable to allow button clicks
            CanvasGroup doubleRewardsCanvasGroup = doubleRewardsButton.GetComponent<CanvasGroup>();
            if (doubleRewardsCanvasGroup != null)
            {
                doubleRewardsCanvasGroup.interactable = isReady;
            }
        }
    }
    
    /// <summary>
    /// Start pulse animation for double rewards button
    /// </summary>
    private void StartDoubleRewardsPulseAnimation()
    {
        StopDoubleRewardsPulseAnimation();
        if (doubleRewardsButton == null || hasRewardsBeenDoubled) return;
        
        RectTransform buttonRect = doubleRewardsButton.GetComponent<RectTransform>();
        if (buttonRect == null) return;
        
        // Create a looping pulse sequence
        Sequence pulseSequence = DOTween.Sequence();
        pulseSequence.Append(buttonRect.DOScale(doubleRewardsPulseScale, doubleRewardsPulseDuration).SetEase(Ease.OutQuad));
        pulseSequence.Append(buttonRect.DOScale(1f, doubleRewardsPulseDuration).SetEase(Ease.InQuad));
        pulseSequence.Append(buttonRect.DOScale(doubleRewardsPulseScale, doubleRewardsPulseDuration).SetEase(Ease.OutQuad));
        pulseSequence.Append(buttonRect.DOScale(1f, doubleRewardsPulseDuration).SetEase(Ease.InQuad));
        pulseSequence.AppendInterval(doubleRewardsPauseDuration);
        pulseSequence.SetLoops(-1); // Loop infinitely
        
        doubleRewardsPulseTween = pulseSequence;
        
        // Register with AnimatedButton if it exists
        WindowManager.AnimatedButton animatedButton = doubleRewardsButton.GetComponent<WindowManager.AnimatedButton>();
        if (animatedButton != null)
        {
            animatedButton.SetExternalTween(pulseSequence);
        }
    }
    
    /// <summary>
    /// Stop pulse animation for double rewards button
    /// </summary>
    private void StopDoubleRewardsPulseAnimation()
    {
        if (doubleRewardsPulseCoroutine != null)
        {
            StopCoroutine(doubleRewardsPulseCoroutine);
            doubleRewardsPulseCoroutine = null;
        }
        
        // Clear external tween from AnimatedButton if it exists
        if (doubleRewardsButton != null)
        {
            WindowManager.AnimatedButton animatedButton = doubleRewardsButton.GetComponent<WindowManager.AnimatedButton>();
            if (animatedButton != null)
            {
                animatedButton.ClearExternalTween();
            }
            
            RectTransform buttonRect = doubleRewardsButton.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.DOKill();
                buttonRect.localScale = Vector3.one;
            }
        }
        
        if (doubleRewardsPulseTween != null && doubleRewardsPulseTween.IsActive())
        {
            doubleRewardsPulseTween.Kill();
            doubleRewardsPulseTween = null;
        }
    }
    
    /// <summary>
    /// Update double rewards button state periodically
    /// </summary>
    private void Update()
    {
        // Rotate loading icon if visible
        if (doubleRewardsLoadingContainer != null && doubleRewardsLoadingContainer.activeSelf)
        {
            if (doubleRewardsLoadingIcon != null)
            {
                doubleRewardsLoadingIcon.Rotate(Vector3.forward, -doubleRewardsRotationSpeed * Time.deltaTime);
            }
        }

        // Periodically check ad readiness if not ready and not showing ad
        if (!isShowingAd && !hasRewardsBeenDoubled)
        {
            UpdateDoubleRewardsButtonState();
        }
    }
    
    /// <summary>
    /// Handle double rewards button click
    /// </summary>
    private async void OnDoubleRewardsButtonClick()
    {
        if (isShowingAd || hasRewardsBeenDoubled) return;
        if (AdsManager.Instance == null) return;
        
        isShowingAd = true;
        if (doubleRewardsButton != null) 
            doubleRewardsButton.interactable = false;

        bool success = await AdsManager.Instance.ShowRewardedAdAsync();
        
        if (success)
        {
            hasRewardsBeenDoubled = true;
            StopDoubleRewardsPulseAnimation();
            
            // Hide button using alpha (button stays active for autolayout)
            if (doubleRewardsButton != null)
            {
                CanvasGroup doubleRewardsCanvasGroup = doubleRewardsButton.GetComponent<CanvasGroup>();
                if (doubleRewardsCanvasGroup == null)
                {
                    doubleRewardsCanvasGroup = doubleRewardsButton.gameObject.AddComponent<CanvasGroup>();
                }
                doubleRewardsCanvasGroup.alpha = 0f;
                doubleRewardsCanvasGroup.interactable = false;
                doubleRewardsCanvasGroup.blocksRaycasts = false;
            }
            
            // Hide return to menu button immediately when ad is watched
            HideMenuButton();
            
            // Double the rewards
            int doubledCoinsEarned = originalCoinsEarned * 2;
            int doubledEnergySpheresEarned = originalEnergySpheresEarned * 2;
            int doubledBossReward = originalBossTotalReward * 2;
            int doubledBossEnergyReward = originalBossTotalEnergyReward * 2;
            
            // Calculate additional currency to award
            int additionalCoins = originalCoinsEarned;
            int additionalEnergySpheres = originalEnergySpheresEarned;
            int additionalBossReward = originalBossTotalReward;
            int additionalBossEnergyReward = originalBossTotalEnergyReward;
            
            // Add additional currency to player's balance
            GameManager.Instance.AddCoins(additionalCoins + additionalBossReward);
            GameManager.Instance.AddEnergySpheres(additionalEnergySpheres + additionalBossEnergyReward);
            
            // Start animation for doubling rewards (will auto-continue after animation)
            StartCoroutine(AnimateDoubleRewards(doubledCoinsEarned, doubledEnergySpheresEarned, doubledBossReward, doubledBossEnergyReward, additionalCoins, additionalEnergySpheres, additionalBossReward, additionalBossEnergyReward));
        }
        else
        {
            isShowingAd = false;
            UpdateDoubleRewardsButtonState();
        }
    }
    
    /// <summary>
    /// Animate doubling of rewards
    /// </summary>
    private IEnumerator AnimateDoubleRewards(int doubledCoinsEarned, int doubledEnergySpheresEarned, int doubledBossReward, int doubledBossEnergyReward, int additionalCoins, int additionalEnergySpheres, int additionalBossReward, int additionalBossEnergyReward)
    {
        // Get current balances (already includes original rewards + additional rewards from doubling)
        int currentBalance = SaveDataExtensions.GetTotalCoins();
        int currentEnergyBalance = SaveDataExtensions.GetTotalEnergySpheres();

        // Calculate balance before doubling (subtract the additional currency we just added)
        int balanceBeforeDoubling = currentBalance - (additionalCoins + additionalBossReward);
        int energyBalanceBeforeDoubling = currentEnergyBalance - (additionalEnergySpheres + additionalBossEnergyReward);
        
        // Animate level reward doubling
        if (originalCoinsEarned > 0 && coinsEarnedText != null)
        {
            yield return StartCoroutine(AnimateTextCount(coinsEarnedText, originalCoinsEarned, doubledCoinsEarned, 0.5f, "+"));
            this.coinsEarned = doubledCoinsEarned;
        }
        if (originalEnergySpheresEarned > 0 && energySpheresEarnedText != null)
        {
            yield return StartCoroutine(AnimateTextCount(energySpheresEarnedText, originalEnergySpheresEarned, doubledEnergySpheresEarned, 0.5f, "+"));
            this.energySpheresEarned = doubledEnergySpheresEarned;
        }
        
        // Animate boss reward doubling
        if (originalBossTotalReward > 0 && bossBaseRewardText != null)
        {
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(AnimateTextCount(bossBaseRewardText, originalBossTotalReward, doubledBossReward, 0.5f));
            this.bossTotalReward = doubledBossReward;
        }
        if (originalBossTotalEnergyReward > 0 && bossEnergyRewardText != null)
        {
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(AnimateTextCount(bossEnergyRewardText, originalBossTotalEnergyReward, doubledBossEnergyReward, 0.5f));
            this.bossTotalEnergyReward = doubledBossEnergyReward;
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // Animate currency flying for level reward
        if (additionalCoins > 0)
        {
            if (totalCoinsText != null) totalCoinsText.text = balanceBeforeDoubling.ToString();
            yield return StartCoroutine(AnimateCurrency(additionalCoins, balanceBeforeDoubling, coinIconTransform, balanceIconTransform, totalCoinsText, coinPrefab, null));
        }
        if (additionalEnergySpheres > 0)
        {
            if (totalEnergySpheresText != null) totalEnergySpheresText.text = energyBalanceBeforeDoubling.ToString();
            yield return StartCoroutine(AnimateCurrency(additionalEnergySpheres, energyBalanceBeforeDoubling, energySphereIconTransform, energyBalanceIconTransform, totalEnergySpheresText, energySpherePrefab, null));
        }
        
        // Animate currency flying for boss reward
        if (additionalBossReward > 0 && bossRewardIconTransform != null)
        {
            yield return new WaitForSeconds(0.3f);
            int balanceAfterLevel = balanceBeforeDoubling + additionalCoins;
            if (totalCoinsText != null) totalCoinsText.text = balanceAfterLevel.ToString();
            yield return StartCoroutine(AnimateCurrency(additionalBossReward, balanceAfterLevel, bossRewardIconTransform, balanceIconTransform, totalCoinsText, coinPrefab, null));
        }
        if (additionalBossEnergyReward > 0 && bossEnergyRewardIconTransform != null)
        {
            yield return new WaitForSeconds(0.3f);
            int energyBalanceAfterLevel = energyBalanceBeforeDoubling + additionalEnergySpheres;
            if (totalEnergySpheresText != null) totalEnergySpheresText.text = energyBalanceAfterLevel.ToString();
            yield return StartCoroutine(AnimateCurrency(additionalBossEnergyReward, energyBalanceAfterLevel, bossEnergyRewardIconTransform, energyBalanceIconTransform, totalEnergySpheresText, energySpherePrefab, null));
        }
        
        // Wait a brief moment after animation completes
        yield return new WaitForSeconds(0.5f);
        
        // Automatically continue after double reward animation finishes
        OnReturnToMenuButtonClick();
    }
    
    /// <summary>
    /// Cleanup when disabled
    /// </summary>
    private void OnDisable()
    {
        StopDoubleRewardsPulseAnimation();
    }
}
