using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using DataRepository;

namespace JigsawSystem
{
    public class PuzzleSolveUiPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image fullImage;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private RectTransform rewardIcon;
        [SerializeField] private TextMeshProUGUI totalCoinsText;
        [SerializeField] private RectTransform balanceIconTransform;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float imageScaleDuration = 0.6f;
        [SerializeField] private int coinCount = 10;
        [SerializeField] private float coinAnimationDuration = 1.0f;
        [SerializeField] private float coinSpawnInterval = 0.1f;
        [SerializeField] private float coinReachThreshold = 10f;
        [SerializeField] private Vector2 coinParticleSize = new Vector2(30f, 30f);
        
        [Header("Particle Effects")]
        [SerializeField] private Sprite starSprite;
        [SerializeField] private int starCount = 15;
        [SerializeField] private float starAnimationDuration = 1.5f;
        [SerializeField] private float starMinSize = 0.5f;
        [SerializeField] private float starMaxSize = 1.2f;
        [SerializeField] private RectTransform particleParent;
        
        [Header("Sounds")]
        [SerializeField] private AudioClip solveSound;
        [SerializeField] private AudioClip coinSound;
        [SerializeField] private AudioSource audioSource;

        private JigsawPuzzleData currentPuzzle;
        private int coinsCollectedInAnimation = 0;
        private int displayedBalance = 0;

        public void Show(JigsawPuzzleData data)
        {
            currentPuzzle = data;
            fullImage.sprite = data.fullImage;
            rewardText.text = "+" + data.completionReward.ToString();
            
            // Set initial balance display
            int currentBalance = ProgressSaveManager<SaveData>.Instance.GetCoins();
            int initialBalance = currentBalance - data.completionReward;
            if (totalCoinsText != null)
            {
                totalCoinsText.text = initialBalance.ToString();
            }
            displayedBalance = initialBalance;
            
            panel.SetActive(true);
            StartCoroutine(ShowSequence());
        }

        private IEnumerator ShowSequence()
        {
            if (audioSource != null && solveSound != null)
                audioSource.PlayOneShot(solveSound);

            canvasGroup.alpha = 0;
            fullImage.transform.localScale = Vector3.zero;

            // Fade in panel
            canvasGroup.DOFade(1, fadeInDuration);
            
            // Scale up image with bounce
            fullImage.transform.DOScale(1.2f, imageScaleDuration).SetEase(Ease.OutBack);
            
            yield return new WaitForSeconds(imageScaleDuration);
            
            // Return to normal scale
            fullImage.transform.DOScale(1f, 0.2f);
            
            // Spawn star particles
            StartCoroutine(SpawnStarParticles());
            
            yield return new WaitForSeconds(0.5f);

            // Coin animation
            yield return StartCoroutine(AnimateCoins(currentPuzzle.completionReward, displayedBalance));

            yield return new WaitForSeconds(1.0f);
            
            // Close on click
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }

            panel.SetActive(false);
        }

        private IEnumerator AnimateCoins(int coinsToAnimate, int currentBalance)
        {
            // Use default icon if none provided
            RectTransform sourceIcon = rewardIcon;
            RectTransform targetIcon = balanceIconTransform;

            // Check if we have required references
            if (sourceIcon == null || targetIcon == null)
            {
                Debug.LogWarning("[PuzzleSolveUiPopup] Reward icon or balance icon transform not assigned. Skipping coin animation.");
                // Still update the balance text
                if (totalCoinsText != null)
                {
                    totalCoinsText.text = (currentBalance + coinsToAnimate).ToString();
                }
                yield break;
            }
            
            // Calculate number of coins to spawn
            int coinsToSpawn = Mathf.Min(coinCount, coinsToAnimate);
            int coinsPerParticle = coinsToAnimate / coinsToSpawn;
            int remainingCoins = coinsToAnimate % coinsToSpawn;
            
            // Get world positions
            Vector3 startPos = sourceIcon.position;
            Vector3 endPos = targetIcon.position;
            
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

                // Ensure particle doesn't affect layout
                LayoutElement layoutElement = coinParticle.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = coinParticle.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                
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
                totalCoinsText.text = (currentBalance + coinsToAnimate).ToString();
            }
        }

        private IEnumerator AnimateCoinParticle(GameObject coinParticle, Vector3 startPos, Vector3 endPos, int coinValue)
        {
            float elapsed = 0f;
            bool hasReached = false;
            
            // Optional: add a small random arc like in the original implementation
            Vector3 midPos = (startPos + endPos) / 2f + (Vector3)Random.insideUnitCircle * 100f;

            while (elapsed < coinAnimationDuration && coinParticle != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / coinAnimationDuration;
                
                // Move coin toward balance position with a slight curve if desired
                // For simplicity and "same logic", let's use Lerp or a simple curve
                // LevelCompleteUI uses Lerp: coinParticle.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                // Let's use a simple Lerp to match LevelCompleteUI exactly
                coinParticle.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                // Check if coin has reached balance position
                if (!hasReached && Vector3.Distance(coinParticle.transform.position, endPos) <= coinReachThreshold)
                {
                    hasReached = true;
                    coinsCollectedInAnimation += coinValue;
                    displayedBalance = displayedBalance + coinValue;
                    
                    // Play collect sound
                    if (audioSource != null && coinSound != null)
                    {
                        audioSource.PlayOneShot(coinSound);
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
                
                if (audioSource != null && coinSound != null)
                {
                    audioSource.PlayOneShot(coinSound);
                }
                
                if (totalCoinsText != null)
                {
                    totalCoinsText.text = displayedBalance.ToString();
                }
                
                Destroy(coinParticle);
            }
        }

        private IEnumerator SpawnStarParticles()
        {
            if (starSprite == null) yield break;

            RectTransform parent = particleParent != null ? particleParent : panel.GetComponent<RectTransform>();
            if (parent == null) yield break;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            if (fullImage == null) yield break;
            RectTransform imageRect = fullImage.rectTransform;
            
            // Get world space corners of the image rect
            Vector3[] corners = new Vector3[4];
            imageRect.GetWorldCorners(corners);
            
            // Calculate rect bounds
            float left = corners[0].x;
            float right = corners[2].x;
            float bottom = corners[0].y;
            float top = corners[2].y;
            float width = right - left;
            float height = top - bottom;
            Vector2 centerPosition = imageRect.position;

            for (int i = 0; i < starCount; i++)
            {
                // Get random position along the rect edges
                Vector2 spawnPosition = GetRandomPositionOnRectEdge(left, right, bottom, top, width, height);
                
                // Create star GameObject
                GameObject starObj = new GameObject("StarParticle");
                RectTransform starRect = starObj.AddComponent<RectTransform>();
                Image starImage = starObj.AddComponent<Image>();

                // Ensure particle doesn't affect layout
                LayoutElement layoutElement = starObj.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                
                starImage.sprite = starSprite;
                starImage.SetNativeSize();
                
                starRect.SetParent(parent, false);
                starRect.position = spawnPosition;
                starRect.localScale = Vector3.zero;

                // Calculate direction outward from center
                Vector2 direction = (spawnPosition - centerPosition).normalized;
                float distance = Random.Range(100f, 300f);
                Vector2 targetPosition = spawnPosition + direction * distance;

                // Random size
                float randomSize = Random.Range(starMinSize, starMaxSize);
                Vector3 targetScale = Vector3.one * randomSize;

                // Animate star
                starRect.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack);
                starRect.DOMove(targetPosition, starAnimationDuration).SetEase(Ease.OutQuad);
                
                // Fade out
                CanvasGroup starCanvasGroup = starObj.AddComponent<CanvasGroup>();
                starCanvasGroup.alpha = 1f;
                starCanvasGroup.DOFade(0f, starAnimationDuration).SetDelay(starAnimationDuration * 0.5f);
                
                // Rotate
                starRect.DORotate(new Vector3(0, 0, Random.Range(-360f, 360f)), starAnimationDuration, RotateMode.FastBeyond360);

                // Destroy after animation
                starRect.DOScale(Vector3.zero, 0.2f).SetDelay(starAnimationDuration).OnComplete(() => {
                    Destroy(starObj);
                });

                yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
            }
        }

        private Vector2 GetRandomPositionOnRectEdge(float left, float right, float bottom, float top, float width, float height)
        {
            // Randomly choose which edge (0=top, 1=right, 2=bottom, 3=left)
            int edge = Random.Range(0, 4);
            
            switch (edge)
            {
                case 0: // Top edge
                    return new Vector2(Random.Range(left, right), top);
                case 1: // Right edge
                    return new Vector2(right, Random.Range(bottom, top));
                case 2: // Bottom edge
                    return new Vector2(Random.Range(left, right), bottom);
                case 3: // Left edge
                    return new Vector2(left, Random.Range(bottom, top));
                default:
                    return new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);
            }
        }
    }
}
