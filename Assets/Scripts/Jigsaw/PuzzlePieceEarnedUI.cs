using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace JigsawSystem
{
    public class PuzzlePieceEarnedUI : MonoBehaviour
    {
        private static PuzzlePieceEarnedUI _instance;
        public static PuzzlePieceEarnedUI Instance => _instance;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Image pieceImage;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float scaleDuration = 0.5f;

        [Header("Particle Effects")]
        [SerializeField] private Sprite shinyCircleSprite;
        [SerializeField] private int shinyCircleCount = 12;
        [SerializeField] private float shinyCircleAnimationDuration = 1.2f;
        [SerializeField] private float shinyCircleMinSize = 0.4f;
        [SerializeField] private float shinyCircleMaxSize = 1.0f;
        [SerializeField] private RectTransform particleParent;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pieceEarnedSound;

        private bool isWaitingForClick = false;
        private List<string> pendingPieceIds = new List<string>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                panel.SetActive(false);

                if (audioSource == null)
                {
                    audioSource = GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = gameObject.AddComponent<AudioSource>();
                    }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Show(List<string> pieceIds)
        {
            pendingPieceIds.AddRange(pieceIds);
            if (!panel.activeSelf)
            {
                StartCoroutine(ShowSequence());
            }
        }

        private IEnumerator ShowSequence()
        {
            panel.SetActive(true);

            while (pendingPieceIds.Count > 0)
            {
                string pieceId = pendingPieceIds[0];
                pendingPieceIds.RemoveAt(0);

                // Award the piece through manager
                string awardedId = JigsawPuzzleManager.Instance.AwardPiece(pieceId);
                if (string.IsNullOrEmpty(awardedId)) continue;

                // Setup UI
                string[] parts = awardedId.Split('_');
                string puzzleId = parts[0];
                int pieceIndex = int.Parse(parts[1]);

                var puzzleData = JigsawPuzzleManager.Instance.Config.GetPuzzle(puzzleId);
               
                    pieceImage.sprite = puzzleData.pieces[pieceIndex];
                    int collected = JigsawPuzzleManager.Instance.GetCollectedPieceCount(puzzleId);
                    progressText.text = $"{collected}/9";
                

                // Animate In
                if (audioSource != null && pieceEarnedSound != null)
                {
                    audioSource.PlayOneShot(pieceEarnedSound);
                }
                
                // Spawn shiny circle particles
                StartCoroutine(SpawnShinyCircleParticles());
                
                yield return StartCoroutine(AnimateIn());

                // Wait for click
                isWaitingForClick = true;
                while (isWaitingForClick)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        isWaitingForClick = false;
                    }
                    yield return null;
                }

                // Animate Out
                yield return StartCoroutine(AnimateOut());
            }

            panel.SetActive(false);
        }

        private IEnumerator AnimateIn()
        {
            canvasGroup.alpha = 0;
            pieceImage.transform.localScale = Vector3.zero;
            
            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                canvasGroup.alpha = t;
                pieceImage.transform.localScale = Vector3.one * Mathf.Lerp(0, 1.2f, elapsed / scaleDuration);
                yield return null;
            }
            
            pieceImage.transform.localScale = Vector3.one;
            canvasGroup.alpha = 1;
        }

        private IEnumerator AnimateOut()
        {
            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1 - (elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0;
        }

        private IEnumerator SpawnShinyCircleParticles()
        {
            if (shinyCircleSprite == null) yield break;

            RectTransform parent = particleParent != null ? particleParent : panel.GetComponent<RectTransform>();
            if (parent == null) yield break;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            if (pieceImage == null) yield break;
            RectTransform imageRect = pieceImage.rectTransform;
            
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

            for (int i = 0; i < shinyCircleCount; i++)
            {
                // Get random position along the rect edges
                Vector2 spawnPosition = GetRandomPositionOnRectEdge(left, right, bottom, top, width, height);
                
                // Create shiny circle GameObject
                GameObject circleObj = new GameObject("ShinyCircleParticle");
                RectTransform circleRect = circleObj.AddComponent<RectTransform>();
                Image circleImage = circleObj.AddComponent<Image>();
                
                circleImage.sprite = shinyCircleSprite;
                circleImage.SetNativeSize();
                
                circleRect.SetParent(parent, false);
                circleRect.position = spawnPosition;
                circleRect.localScale = Vector3.zero;

                // Calculate direction outward from center
                Vector2 direction = (spawnPosition - centerPosition).normalized;
                float distance = Random.Range(80f, 250f);
                Vector2 targetPosition = spawnPosition + direction * distance;

                // Random size
                float randomSize = Random.Range(shinyCircleMinSize, shinyCircleMaxSize);
                Vector3 targetScale = Vector3.one * randomSize;

                // Animate shiny circle
                circleRect.DOScale(targetScale, 0.25f).SetEase(Ease.OutBack);
                circleRect.DOMove(targetPosition, shinyCircleAnimationDuration).SetEase(Ease.OutQuad);
                
                // Fade out
                CanvasGroup circleCanvasGroup = circleObj.AddComponent<CanvasGroup>();
                circleCanvasGroup.alpha = 1f;
                circleCanvasGroup.DOFade(0f, shinyCircleAnimationDuration).SetDelay(shinyCircleAnimationDuration * 0.4f);
                
                // Rotate
                circleRect.DORotate(new Vector3(0, 0, Random.Range(-180f, 180f)), shinyCircleAnimationDuration, RotateMode.FastBeyond360);

                // Destroy after animation
                circleRect.DOScale(Vector3.zero, 0.2f).SetDelay(shinyCircleAnimationDuration).OnComplete(() => {
                    Destroy(circleObj);
                });

                yield return new WaitForSeconds(Random.Range(0.03f, 0.07f));
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
