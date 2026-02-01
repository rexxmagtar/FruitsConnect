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
        [SerializeField] private Image rewardIconImage;
        [SerializeField] private RectTransform rewardIcon;
        [SerializeField] private TextMeshProUGUI totalBalanceText;
        [SerializeField] private RectTransform balanceIconTransform;
        [SerializeField] private Sprite coinIconSprite;
        [SerializeField] private Sprite energySphereIconSprite;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject energySpherePrefab;
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
        private int collectedInAnimation = 0;
        private int displayedBalance = 0;

        public void Show(JigsawPuzzleData data)
        {
            currentPuzzle = data;
            fullImage.sprite = data.fullImage;
            rewardText.text = "+" + data.completionReward.ToString();
            
            // Set reward icon
            if (rewardIconImage != null)
            {
                rewardIconImage.sprite = data.rewardType == PuzzleRewardType.Coins ? coinIconSprite : energySphereIconSprite;
            }

            // Set initial balance display
            int initialBalance = 0;
            if (data.rewardType == PuzzleRewardType.Coins)
            {
                int currentBalance = SaveDataHelper.TotalCoins;
                initialBalance = currentBalance - data.completionReward;
            }
            else
            {
                int currentBalance = SaveDataHelper.TotalEnergySpheres;
                initialBalance = currentBalance - data.completionReward;
            }

            if (totalBalanceText != null)
            {
                totalBalanceText.text = initialBalance.ToString();
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

            // Coin/Energy animation
            GameObject prefabToUse = currentPuzzle.rewardType == PuzzleRewardType.Coins ? coinPrefab : energySpherePrefab;
            yield return StartCoroutine(AnimateRewards(currentPuzzle.completionReward, displayedBalance, prefabToUse));

            yield return new WaitForSeconds(1.0f);
            
            // Close on click
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }

            panel.SetActive(false);
        }

        private IEnumerator AnimateRewards(int amountToAnimate, int currentBalance, GameObject prefab)
        {
            // Use default icon if none provided
            RectTransform sourceIcon = rewardIcon;
            RectTransform targetIcon = balanceIconTransform;

            // Check if we have required references
            if (sourceIcon == null || targetIcon == null)
            {
                Debug.LogWarning("[PuzzleSolveUiPopup] Reward icon or balance icon transform not assigned. Skipping animation.");
                // Still update the balance text
                if (totalBalanceText != null)
                {
                    totalBalanceText.text = (currentBalance + amountToAnimate).ToString();
                }
                yield break;
            }
            
            // Calculate number of particles to spawn
            int particlesToSpawn = Mathf.Min(coinCount, amountToAnimate);
            int valuePerParticle = amountToAnimate / particlesToSpawn;
            int remainingValue = amountToAnimate % particlesToSpawn;
            
            // Get world positions
            Vector3 startPos = sourceIcon.position;
            Vector3 endPos = targetIcon.position;
            
            // Initialize animation state
            collectedInAnimation = 0;
            displayedBalance = currentBalance;
            
            // List to track active particles
            System.Collections.Generic.List<GameObject> activeParticles = new System.Collections.Generic.List<GameObject>();
            
            // Spawn particles with intervals
            for (int i = 0; i < particlesToSpawn; i++)
            {
                int valueForThisParticle = valuePerParticle + (i < remainingValue ? 1 : 0);
                
                // Create particle
                GameObject particle = null;
                if (prefab != null)
                {
                    particle = Instantiate(prefab, transform);
                }
                else
                {
                    // Create a simple sprite if no prefab is assigned
                    particle = new GameObject("RewardParticle");
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
                StartCoroutine(AnimateParticle(particle, startPos, endPos, valueForThisParticle));
                
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
            if (totalBalanceText != null)
            {
                totalBalanceText.text = (currentBalance + amountToAnimate).ToString();
            }
        }

        private IEnumerator AnimateParticle(GameObject particle, Vector3 startPos, Vector3 endPos, int value)
        {
            float elapsed = 0f;
            bool hasReached = false;
            
            while (elapsed < coinAnimationDuration && particle != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / coinAnimationDuration;
                
                particle.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                // Check if particle has reached target position
                if (!hasReached && Vector3.Distance(particle.transform.position, endPos) <= coinReachThreshold)
                {
                    hasReached = true;
                    displayedBalance = displayedBalance + value;
                    
                    // Play collect sound
                    if (audioSource != null && coinSound != null)
                    {
                        audioSource.PlayOneShot(coinSound);
                    }
                    
                    // Update balance text
                    if (totalBalanceText != null)
                    {
                        totalBalanceText.text = displayedBalance.ToString();
                    }
                    
                    // Destroy particle
                    Destroy(particle);
                    yield break;
                }
                
                yield return null;
            }
            
            if (particle != null)
            {
                displayedBalance = displayedBalance + value;
                
                if (audioSource != null && coinSound != null)
                {
                    audioSource.PlayOneShot(coinSound);
                }
                
                if (totalBalanceText != null)
                {
                    totalBalanceText.text = displayedBalance.ToString();
                }
                
                Destroy(particle);
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
