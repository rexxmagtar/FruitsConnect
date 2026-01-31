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
    [SerializeField] private TextMeshProUGUI totalCoinsText;
    [SerializeField] private RectTransform coinIconTransform;
    [SerializeField] private RectTransform balanceIconTransform;
    [SerializeField] private RectTransform adIconContainer;
    [SerializeField] private GameObject loadingContainer;
    [SerializeField] private RectTransform loadingIcon;
    
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
    private Coroutine pulseCoroutine;
    private bool isShowingAd = false;
    private int displayedBalance = 0;
    
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
    public void Show(int levelReward = 0)
    {
        if (isVisible) return;
        
        this.coinsEarned = (int)(levelReward * 0.3f);
        gameObject.SetActive(true);

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
        if (adIconContainer != null)
        {
            pulseCoroutine = StartCoroutine(PulseSequenceRoutine());
        }
    }

    private void StopPulseAnimation()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        if (adIconContainer != null)
        {
            adIconContainer.DOKill();
            adIconContainer.localScale = Vector3.one;
        }
    }

    private IEnumerator PulseSequenceRoutine()
    {
        while (true)
        {
            // Pulse 1
            yield return adIconContainer.DOScale(pulseScale, pulseDuration).SetEase(Ease.OutQuad).WaitForCompletion();
            yield return adIconContainer.DOScale(1f, pulseDuration).SetEase(Ease.InQuad).WaitForCompletion();

            // Pulse 2
            yield return adIconContainer.DOScale(pulseScale, pulseDuration).SetEase(Ease.OutQuad).WaitForCompletion();
            yield return adIconContainer.DOScale(1f, pulseDuration).SetEase(Ease.InQuad).WaitForCompletion();

            // Pause
            yield return new WaitForSeconds(pauseDuration);
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

        int finalBalanceTotal = ProgressSaveManager<SaveData>.Instance.GetCoins();
        int initialBalance = finalBalanceTotal - coinsEarned;

        if (totalCoinsText != null) totalCoinsText.text = initialBalance.ToString();

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
            yield return StartCoroutine(AnimateCoins(coinsEarned, initialBalance));
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

    private IEnumerator AnimateCoins(int coinsToAnimate, int currentBalance)
    {
        if (coinIconTransform == null || balanceIconTransform == null)
        {
            if (totalCoinsText != null) totalCoinsText.text = (currentBalance + coinsToAnimate).ToString();
            yield break;
        }

        int coinsToSpawn = Mathf.Min(coinAnimationCount, coinsToAnimate);
        int coinsPerParticle = coinsToAnimate / coinsToSpawn;
        int remainingCoins = coinsToAnimate % coinsToSpawn;
        
        Vector3 startPos = coinIconTransform.position;
        Vector3 endPos = balanceIconTransform.position;
        
        displayedBalance = currentBalance;
        List<GameObject> activeCoins = new List<GameObject>();
        
        for (int i = 0; i < coinsToSpawn; i++)
        {
            int coinsForThisParticle = coinsPerParticle + (i < remainingCoins ? 1 : 0);
            
            GameObject coinParticle = null;
            if (coinPrefab != null)
            {
                coinParticle = Instantiate(coinPrefab, transform);
            }
            else
            {
                coinParticle = new GameObject("CoinParticle");
                coinParticle.transform.SetParent(transform);
                Image image = coinParticle.AddComponent<Image>();
                image.color = Color.yellow;
                RectTransform rectTransform = coinParticle.GetComponent<RectTransform>();
                rectTransform.sizeDelta = coinParticleSize;
            }

            LayoutElement layoutElement = coinParticle.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = coinParticle.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            
            coinParticle.transform.position = startPos;
            activeCoins.Add(coinParticle);
            
            StartCoroutine(AnimateCoinParticle(coinParticle, startPos, endPos, coinsForThisParticle));
            
            if (i < coinsToSpawn - 1)
            {
                yield return new WaitForSeconds(coinSpawnInterval);
            }
        }
        
        while (activeCoins.Count > 0)
        {
            activeCoins.RemoveAll(coin => coin == null);
            yield return null;
        }
        
        if (totalCoinsText != null)
        {
            totalCoinsText.text = (currentBalance + coinsToAnimate).ToString();
        }
    }

    private IEnumerator AnimateCoinParticle(GameObject coinParticle, Vector3 startPos, Vector3 endPos, int coinValue)
    {
        float elapsed = 0f;
        while (elapsed < coinAnimationDuration && coinParticle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / coinAnimationDuration;
            coinParticle.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            if (Vector3.Distance(coinParticle.transform.position, endPos) <= coinReachThreshold)
            {
                displayedBalance += coinValue;
                if (coinCollectSound != null && audioSource != null) audioSource.PlayOneShot(coinCollectSound);
                if (totalCoinsText != null) totalCoinsText.text = displayedBalance.ToString();
                Destroy(coinParticle);
                yield break;
            }
            yield return null;
        }
        
        if (coinParticle != null)
        {
            displayedBalance += coinValue;
            if (coinCollectSound != null && audioSource != null) audioSource.PlayOneShot(coinCollectSound);
            if (totalCoinsText != null) totalCoinsText.text = displayedBalance.ToString();
            Destroy(coinParticle);
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

        bool success = await AdsManager.Instance.ShowRewardedAdAsync();

        if (success)
        {
            Hide();
            OnSkipLevelAdCompleted?.Invoke();
        }
        else
        {
            isShowingAd = false;
            if (skipLevelButton != null) skipLevelButton.interactable = true;
        }
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

