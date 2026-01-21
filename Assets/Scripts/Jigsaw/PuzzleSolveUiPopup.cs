using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

namespace JigsawSystem
{
    public class PuzzleSolveUiPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image fullImage;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private RectTransform rewardIcon;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float imageScaleDuration = 0.6f;
        [SerializeField] private int coinCount = 10;
        [SerializeField] private float coinAnimationDuration = 1.0f;
        
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

        public void Show(JigsawPuzzleData data)
        {
            currentPuzzle = data;
            fullImage.sprite = data.fullImage;
            rewardText.text = "+" + data.completionReward.ToString();
            
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
            yield return StartCoroutine(AnimateCoins());

            yield return new WaitForSeconds(1.0f);
            
            // Close on click
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }

            panel.SetActive(false);
        }

        private IEnumerator AnimateCoins()
        {
            // Find balance icon in MainMenuUI
            MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
            if (mainMenu == null) yield break;

            RectTransform target = mainMenu.GetBalanceIconTransform();
            Vector3 targetPos = target != null ? target.position : Vector3.zero;

            for (int i = 0; i < coinCount; i++)
            {
                GameObject coin = Instantiate(coinPrefab, transform);
                coin.transform.position = rewardIcon.position;
                
                // Random arc or direct fly
                Vector3 randomOffset = Random.insideUnitSphere * 50f;
                coin.transform.DOMove(rewardIcon.position + randomOffset, 0.2f).OnComplete(() => {
                    // Fly to target
                    // If we can't find target, just fade out
                    coin.transform.DOMove(targetPos, coinAnimationDuration).SetEase(Ease.InBack).OnComplete(() => {
                        if (audioSource != null && coinSound != null)
                            audioSource.PlayOneShot(coinSound);
                        Destroy(coin);
                    });
                });

                yield return new WaitForSeconds(0.05f);
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
