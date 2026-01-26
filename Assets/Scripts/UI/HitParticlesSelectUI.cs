using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DataRepository;
using TMPro;
using DG.Tweening;

namespace UI
{
    public class HitParticlesSelectUI : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private List<Transform> stageContainers = new List<Transform>();
        [SerializeField] private List<GameObject> stageLockers = new List<GameObject>();
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private RectTransform particleParent;

        [Header("Particle Effects")]
        [SerializeField] private Sprite shinyCircleSprite;
        [SerializeField] private int shinyCircleCount = 12;
        [SerializeField] private float shinyCircleAnimationDuration = 1.2f;
        [SerializeField] private float shinyCircleMinSize = 0.4f;
        [SerializeField] private float shinyCircleMaxSize = 1.0f;

        [Header("Prefabs")]
        [SerializeField] private HitParticleUiButton buttonPrefab;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip purchaseSound;
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip buttonClickSound;

        public event System.Action OnClosed;

        private List<HitParticleUiButton> _buttons = new List<HitParticleUiButton>();

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => 
                {
                    PlayButtonClickSound();
                    gameObject.SetActive(false);
                    OnClosed?.Invoke();
                });
            }
        }

        public void PlayButtonClickSound()
        {
            if (audioSource != null && buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }

        public void PlayPurchaseSound()
        {
            if (audioSource != null && purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }
        }

        public void PlaySelectSound()
        {
            if (audioSource != null && selectSound != null)
            {
                audioSource.PlayOneShot(selectSound);
            }
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            ClearButtons();
            UpdateBalance();
            
            var allParticles = HitParticlesManager.Instance.GetAllParticles();
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            int currentLevel = saveData.CurrentLevel;

          

            foreach (var particle in allParticles)
            {
                int stageIndex = particle.stage;
                if (stageIndex >= 0 && stageIndex < stageContainers.Count)
                {
                    var container = stageContainers[stageIndex];
                    if (container != null)
                    {
                        var btn = Instantiate(buttonPrefab, container);
                        bool isAccessible = stageIndex == 0 || currentLevel >= stageIndex * 15;
                        btn.Initialize(particle, this, isAccessible);
                        _buttons.Add(btn);
                    }
                }
            }

              // Update stage lockers visibility
            // Stage 0: default, Stage 1: level 15, Stage 2: level 30...
            for (int i = 0; i < stageLockers.Count; i++)
            {
                bool isStageAccessible = i == 0 || currentLevel >= i * 15;
                if (stageLockers[i] != null){
                    stageLockers[i].SetActive(!isStageAccessible);
                    stageLockers[i].transform.SetAsLastSibling();
                }
            }
        }

        public void RefreshAllButtons()
        {
            UpdateBalance();
            foreach (var btn in _buttons)
            {
                btn.UpdateUI();
            }
        }

        public void TriggerPurchaseEffect(RectTransform targetRect)
        {
            StartCoroutine(SpawnShinyCircleParticles(targetRect));
        }

        private IEnumerator SpawnShinyCircleParticles(RectTransform targetRect)
        {
            if (shinyCircleSprite == null || targetRect == null) yield break;

            RectTransform parent = particleParent != null ? particleParent : GetComponent<RectTransform>();
            if (parent == null) yield break;

            // Get world space corners of the target rect
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            
            // Calculate rect bounds
            float left = corners[0].x;
            float right = corners[2].x;
            float bottom = corners[0].y;
            float top = corners[2].y;
            float width = right - left;
            float height = top - bottom;
            Vector2 centerPosition = targetRect.position;

            for (int i = 0; i < shinyCircleCount; i++)
            {
                // Get random position along the rect edges
                Vector2 spawnPosition = GetRandomPositionOnRectEdge(left, right, bottom, top, width, height);
                
                // Create shiny circle GameObject
                GameObject circleObj = new GameObject("ShinyCircleParticle");
                RectTransform circleRect = circleObj.AddComponent<RectTransform>();
                Image circleImage = circleObj.AddComponent<Image>();

                // Ensure particle doesn't affect layout
                LayoutElement layoutElement = circleObj.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                
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

        private void UpdateBalance()
        {
            if (balanceText != null)
            {
                balanceText.text = GameManager.Instance.GetCoins().ToString();
            }
        }

        private void ClearButtons()
        {
            foreach (var btn in _buttons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _buttons.Clear();
        }
    }
}
