using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using AdsServices;
using DataRepository;

public class LevelFailedUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelFailedText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button skipLevelButton;
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private TextMeshProUGUI coinsEarnedText;
    [SerializeField] private TextMeshProUGUI energySpheresEarnedText;
    [SerializeField] private TextMeshProUGUI totalCoinsText;
    [SerializeField] private TextMeshProUGUI totalEnergySpheresText;
    [SerializeField] private GameObject energyBalanceContainer;
    [SerializeField] private RectTransform coinIconTransform;
    [SerializeField] private RectTransform energySphereIconTransform;
    [SerializeField] private RectTransform balanceIconTransform;
    [SerializeField] private RectTransform energyBalanceIconTransform;
    [SerializeField] private RectTransform adIconContainer;
    [SerializeField] private GameObject loadingContainer;
    [SerializeField] private RectTransform loadingIcon;
    [SerializeField] private TextMeshProUGUI skipRewardText;
    [SerializeField] private TextMeshProUGUI skipEnergyRewardText;
    [SerializeField] private RectTransform skipRewardIconTransform;
    [SerializeField] private RectTransform skipEnergyRewardIconTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float retryButtonDelay = 2.0f;
    
    [Header("Pulse Animation")]
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private float pauseDuration = 1.5f;

    [Header("Coin Animation Settings")]
    [SerializeField] private int coinAnimationCount = 5;
    [SerializeField] private float coinAnimationDuration = 1.5f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject energySpherePrefab;
    [SerializeField] private float coinSpawnInterval = 0.1f;
    [SerializeField] private float coinReachThreshold = 10f;
    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private Vector2 coinParticleSize = new Vector2(30f, 30f);
    
    [Header("Text Settings")]
    [SerializeField] private string levelFailedMessage = "Level Failed!";
    
    [Header("Visual Effects")]
    [SerializeField] private AudioClip failSound;
    [SerializeField] private AudioClip buttonClickSound;
    
    // Events
    public static event System.Action OnRetryButtonPressed;
    public static event System.Action OnSkipLevelAdCompleted;
    public static event System.Action OnReturnToMenuButtonPressed;
    
    // State
    private CanvasGroup canvasGroup;
    [SerializeField] private AudioSource audioSource;
    private bool isVisible = false;
    private int coinsEarned = 0;
    private int energySpheresEarned = 0;
    private int levelReward = 0;
    private int levelEnergyReward = 0;
    private int skipReward = 0;
    private int skipEnergyReward = 0;
    private Coroutine pulseCoroutine;
    private Tween pulseTween;
    private bool isShowingAd = false;
    private int displayedBalance = 0;
    private int displayedEnergyBalance = 0;
    
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

        // Setup skip level button
        if (skipLevelButton != null)
        {
            skipLevelButton.onClick.AddListener(OnSkipLevelButtonClick);
        }
        
        // Setup return to menu button
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClick);
        }
        
        // Setup retry button - ensure it's active but hidden using CanvasGroup
        if (retryButton != null)
        {
            CanvasGroup retryCanvasGroup = retryButton.GetComponent<CanvasGroup>();
            if (retryCanvasGroup == null)
            {
                retryCanvasGroup = retryButton.gameObject.AddComponent<CanvasGroup>();
            }
            // Hide button but keep it active for autolayout
            retryCanvasGroup.alpha = 0f;
            retryCanvasGroup.interactable = false;
            retryCanvasGroup.blocksRaycasts = false;
        }
    }
    
    private void Start()
    {
        // Hide initially
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isVisible && !isShowingAd)
        {
            UpdateAdState();
        }
    }

    private void UpdateAdState()
    {
        bool isReady = AdsManager.Instance.IsRewardedAdReady();
        
        if (loadingContainer != null) loadingContainer.SetActive(!isReady);
        if (skipLevelButton != null) skipLevelButton.interactable = isReady;

        if (loadingContainer != null && loadingContainer.activeSelf)
        {
            if (loadingIcon != null)
            {
                loadingIcon.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void OnDisable()
    {
        StopPulseAnimation();
    }
    
    /// <summary>
    /// Show the level failed UI
    /// </summary>
    public void Show(int levelReward = 0, int levelEnergyReward = 0)
    {
        if (isVisible) return;
        
        this.levelReward = levelReward;
        this.levelEnergyReward = levelEnergyReward;
        this.coinsEarned = (int)(levelReward * 0.3f);
        this.energySpheresEarned = (int)(levelEnergyReward * 0.3f);
        this.skipReward = (int)(levelReward * 0.5f);
        this.skipEnergyReward = (int)(levelEnergyReward * 0.5f);

        // Hide energy UI if level < 8
        int currentLevel = SaveDataExtensions.GetCurrentLevelNumber();
        bool isEnergyUnlocked = currentLevel > 8;
        
        if (energySpheresEarnedText != null) energySpheresEarnedText.gameObject.SetActive(isEnergyUnlocked);
        if (energySphereIconTransform != null) energySphereIconTransform.gameObject.SetActive(isEnergyUnlocked);
        
        if (skipEnergyRewardText != null) skipEnergyRewardText.gameObject.SetActive(isEnergyUnlocked);
        if (skipEnergyRewardIconTransform != null) skipEnergyRewardIconTransform.gameObject.SetActive(isEnergyUnlocked);
        
        if (energyBalanceContainer != null) energyBalanceContainer.SetActive(isEnergyUnlocked);

        gameObject.SetActive(true);
        
        // Update skip reward text
        if (skipRewardText != null)
        {
            skipRewardText.text = "+" + skipReward.ToString();
        }
        if (skipEnergyRewardText != null)
        {
            skipEnergyRewardText.text = "+" + skipEnergyReward.ToString();
        }

        // Hide retry button initially using alpha (button stays active for autolayout)
        if (retryButton != null)
        {
            CanvasGroup retryCanvasGroup = retryButton.GetComponent<CanvasGroup>();
            if (retryCanvasGroup == null)
            {
                retryCanvasGroup = retryButton.gameObject.AddComponent<CanvasGroup>();
            }
            retryCanvasGroup.alpha = 0f;
            retryCanvasGroup.interactable = false;
            retryCanvasGroup.blocksRaycasts = false;
        }

        StartCoroutine(ShowAnimation());
        StartPulseAnimation();
    }
    
    /// <summary>
    /// Hide the level failed UI
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        StopPulseAnimation();
        StartCoroutine(HideAnimation());
    }

    private void StartPulseAnimation()
    {
        StopPulseAnimation();
        if (adIconContainer == null) return;
        
        // Create a looping pulse sequence
        Sequence pulseSequence = DOTween.Sequence();
        pulseSequence.Append(adIconContainer.DOScale(pulseScale, pulseDuration).SetEase(Ease.OutQuad));
        pulseSequence.Append(adIconContainer.DOScale(1f, pulseDuration).SetEase(Ease.InQuad));
        pulseSequence.Append(adIconContainer.DOScale(pulseScale, pulseDuration).SetEase(Ease.OutQuad));
        pulseSequence.Append(adIconContainer.DOScale(1f, pulseDuration).SetEase(Ease.InQuad));
        pulseSequence.AppendInterval(pauseDuration);
        pulseSequence.SetLoops(-1); // Loop infinitely
        
        pulseTween = pulseSequence;
        
        // Register with AnimatedButton if skip button has one
        if (skipLevelButton != null)
        {
            WindowManager.AnimatedButton animatedButton = skipLevelButton.GetComponent<WindowManager.AnimatedButton>();
            if (animatedButton != null)
            {
                animatedButton.SetExternalTween(pulseSequence);
            }
        }
    }

    private void StopPulseAnimation()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Clear external tween from AnimatedButton if it exists
        if (skipLevelButton != null)
        {
            WindowManager.AnimatedButton animatedButton = skipLevelButton.GetComponent<WindowManager.AnimatedButton>();
            if (animatedButton != null)
            {
                animatedButton.ClearExternalTween();
            }
        }
        
        if (adIconContainer != null)
        {
            adIconContainer.DOKill();
            adIconContainer.localScale = Vector3.one;
        }
        
        if (pulseTween != null && pulseTween.IsActive())
        {
            pulseTween.Kill();
            pulseTween = null;
        }
    }
    
    /// <summary>
    /// Show animation sequence
    /// </summary>
    private IEnumerator ShowAnimation()
    {
        isVisible = true;
        
        // Reset state
        canvasGroup.alpha = 0f;
        
        if (coinsEarnedText != null) coinsEarnedText.text = "+0";
        if (energySpheresEarnedText != null) energySpheresEarnedText.text = "+0";

        int finalBalanceTotal = SaveDataExtensions.GetTotalCoins();
        int initialBalance = finalBalanceTotal - coinsEarned;

        int finalEnergyBalanceTotal = SaveDataExtensions.GetTotalEnergySpheres();
        int initialEnergyBalance = finalEnergyBalanceTotal - energySpheresEarned;

        if (totalCoinsText != null) totalCoinsText.text = initialBalance.ToString();
        if (totalEnergySpheresText != null) totalEnergySpheresText.text = initialEnergyBalance.ToString();

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

        // Sequence: Reward
        if (coinsEarned > 0)
        {
            yield return StartCoroutine(AnimateTextCount(coinsEarnedText, 0, coinsEarned, 0.5f, "+"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(AnimateCurrency(coinsEarned, initialBalance, coinIconTransform, balanceIconTransform, totalCoinsText, coinPrefab));
        }

        if (energySpheresEarned > 0)
        {
            yield return StartCoroutine(AnimateTextCount(energySpheresEarnedText, 0, energySpheresEarned, 0.5f, "+"));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(AnimateCurrency(energySpheresEarned, initialEnergyBalance, energySphereIconTransform, energyBalanceIconTransform, totalEnergySpheresText, energySpherePrefab));
        }

        // Show retry button after delay using CanvasGroup
        yield return new WaitForSeconds(retryButtonDelay);
        if (retryButton != null)
        {
            CanvasGroup btnGroup = retryButton.GetComponent<CanvasGroup>();
            if (btnGroup == null)
            {
                btnGroup = retryButton.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Fade in the button
            btnGroup.alpha = 0f;
            btnGroup.blocksRaycasts = true;
            btnGroup.DOFade(1f, 0.5f).OnComplete(() => {
                if (btnGroup != null)
                {
                    btnGroup.interactable = true;
                }
            });
        }
    }

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

    private IEnumerator AnimateCurrency(int amountToAnimate, int currentBalance, RectTransform sourceIcon, RectTransform targetIcon, TextMeshProUGUI balanceText, GameObject prefab)
    {
        if (sourceIcon == null || targetIcon == null)
        {
            if (balanceText != null) balanceText.text = (currentBalance + amountToAnimate).ToString();
            yield break;
        }

        int particlesToSpawn = Mathf.Min(coinAnimationCount, amountToAnimate);
        int amountPerParticle = amountToAnimate / particlesToSpawn;
        int remainingAmount = amountToAnimate % particlesToSpawn;
        
        Vector3 startPos = sourceIcon.position;
        Vector3 endPos = targetIcon.position;
        
        int localDisplayedBalance = currentBalance;
        List<GameObject> activeParticles = new List<GameObject>();
        
        for (int i = 0; i < particlesToSpawn; i++)
        {
            int amountForThisParticle = amountPerParticle + (i < remainingAmount ? 1 : 0);
            
            GameObject particle = null;
            if (prefab != null)
            {
                particle = Instantiate(prefab, transform);
            }
            else
            {
                particle = new GameObject("CurrencyParticle");
                particle.transform.SetParent(transform);
                Image image = particle.AddComponent<Image>();
                image.color = Color.yellow;
                RectTransform rectTransform = particle.GetComponent<RectTransform>();
                rectTransform.sizeDelta = coinParticleSize;
            }

            LayoutElement layoutElement = particle.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = particle.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            
            particle.transform.position = startPos;
            activeParticles.Add(particle);
            
            StartCoroutine(AnimateCurrencyParticle(particle, startPos, endPos, amountForThisParticle, balanceText, (v) => {
                localDisplayedBalance += v;
                return localDisplayedBalance;
            }));
            
            if (i < particlesToSpawn - 1)
            {
                yield return new WaitForSeconds(coinSpawnInterval);
            }
        }
        
        while (activeParticles.Count > 0)
        {
            activeParticles.RemoveAll(p => p == null);
            yield return null;
        }
        
        if (balanceText != null)
        {
            balanceText.text = (currentBalance + amountToAnimate).ToString();
        }
    }

    private IEnumerator AnimateCurrencyParticle(GameObject particle, Vector3 startPos, Vector3 endPos, int particleValue, TextMeshProUGUI balanceText, System.Func<int, int> updateBalance)
    {
        float elapsed = 0f;
        while (elapsed < coinAnimationDuration && particle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / coinAnimationDuration;
            particle.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            if (Vector3.Distance(particle.transform.position, endPos) <= coinReachThreshold)
            {
                int newBalance = updateBalance(particleValue);
                if (coinCollectSound != null && audioSource != null) audioSource.PlayOneShot(coinCollectSound);
                if (balanceText != null) balanceText.text = newBalance.ToString();
                Destroy(particle);
                yield break;
            }
            yield return null;
        }
        
        if (particle != null)
        {
            int newBalance = updateBalance(particleValue);
            if (coinCollectSound != null && audioSource != null) audioSource.PlayOneShot(coinCollectSound);
            if (balanceText != null) balanceText.text = newBalance.ToString();
            Destroy(particle);
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

    private async void OnSkipLevelButtonClick()
    {
        if (isShowingAd) return;

        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }

        isShowingAd = true;
        if (skipLevelButton != null) skipLevelButton.interactable = false;
        
        // Hide and disable retry button immediately when skip is pressed
        HideRetryButton();
        
        // Stop pulse animation
        StopPulseAnimation();

        bool success = await AdsManager.Instance.ShowRewardedAdAsync();

        if (success)
        {
            // Add skip rewards to player's balance
            if (skipReward > 0) GameManager.Instance.AddCoins(skipReward);
            if (skipEnergyReward > 0) GameManager.Instance.AddEnergySpheres(skipEnergyReward);
            
            if (skipReward > 0 || skipEnergyReward > 0)
            {
                // Animate rewards flying from skip reward icons to balance
                StartCoroutine(AnimateSkipReward());
            }
            else
            {
                // No reward, just skip
                Hide();
                OnSkipLevelAdCompleted?.Invoke();
            }
        }
        else
        {
            isShowingAd = false;
            if (skipLevelButton != null) skipLevelButton.interactable = true;
            StartPulseAnimation();
            // Don't show retry button again if ad failed - user can try skip again
        }
    }
    
    /// <summary>
    /// Hide the retry button by fading it out
    /// </summary>
    private void HideRetryButton()
    {
        if (retryButton != null)
        {
            retryButton.interactable = false;
            
            CanvasGroup retryCanvasGroup = retryButton.GetComponent<CanvasGroup>();
            if (retryCanvasGroup == null)
            {
                retryCanvasGroup = retryButton.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Fade out the button
            retryCanvasGroup.DOFade(0f, 0.3f).OnComplete(() => {
                if (retryCanvasGroup != null)
                {
                    retryCanvasGroup.blocksRaycasts = false;
                }
            });
        }
    }
    
    /// <summary>
    /// Animate skip reward currency flying to balance
    /// </summary>
    private IEnumerator AnimateSkipReward()
    {
        // Get current balances before adding skip rewards
        int currentBalance = SaveDataExtensions.GetTotalCoins() - skipReward;
        int currentEnergyBalance = SaveDataExtensions.GetTotalEnergySpheres() - skipEnergyReward;
        
        // Update total currency texts to show balance before skip rewards
        if (totalCoinsText != null) totalCoinsText.text = currentBalance.ToString();
        if (totalEnergySpheresText != null) totalEnergySpheresText.text = currentEnergyBalance.ToString();
        
        // Determine source icons
        RectTransform coinSource = skipRewardIconTransform != null ? skipRewardIconTransform : coinIconTransform;
        RectTransform energySource = skipEnergyRewardIconTransform != null ? skipEnergyRewardIconTransform : energySphereIconTransform;
        
        // Animate currency flying in parallel/sequence
        if (skipReward > 0)
        {
            yield return StartCoroutine(AnimateCurrency(skipReward, currentBalance, coinSource, balanceIconTransform, totalCoinsText, coinPrefab));
        }
        
        if (skipEnergyReward > 0)
        {
            yield return StartCoroutine(AnimateCurrency(skipEnergyReward, currentEnergyBalance, energySource, energyBalanceIconTransform, totalEnergySpheresText, energySpherePrefab));
        }
        
        // Wait a brief moment after animation completes
        yield return new WaitForSeconds(0.5f);
        
        // Now hide and skip
        Hide();
        OnSkipLevelAdCompleted?.Invoke();
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

