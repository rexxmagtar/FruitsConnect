using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace UI
{
    public class ShoSpiritsUnlockUI : MonoBehaviour
    {
        private static ShoSpiritsUnlockUI _instance;
        public static ShoSpiritsUnlockUI Instance => _instance;

        [Header("UI References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform particleParent;

        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Particle Effects")]
        [SerializeField] private Sprite shinyCircleSprite;
        [SerializeField] private int shinyCircleCount = 20;
        [SerializeField] private float shinyCircleAnimationDuration = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip unlockSound;
        [SerializeField] private AudioClip buttonClickSound;

        private bool isWaitingForClick = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                if (panel != null) panel.SetActive(false);

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

        public void Show()
        {
            if (panel != null && !panel.activeSelf)
            {
                StartCoroutine(ShowSequence());
            }
        }

        private IEnumerator ShowSequence()
        {
            panel.SetActive(true);
            canvasGroup.alpha = 0;
            
            // Play sound
            if (audioSource != null && unlockSound != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }

            // Fade in
            canvasGroup.DOFade(1, fadeDuration);
            
            // Spawn particles (similar to puzzle piece)
            StartCoroutine(SpawnParticles());

            yield return new WaitForSeconds(fadeDuration);

            // Wait for any touch/click
            isWaitingForClick = true;
            while (isWaitingForClick)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (audioSource != null && buttonClickSound != null)
                    {
                        audioSource.PlayOneShot(buttonClickSound);
                    }
                    isWaitingForClick = false;
                }
                yield return null;
            }

            // Fade out
            canvasGroup.DOFade(0, fadeDuration).OnComplete(() => panel.SetActive(false));
        }

        private IEnumerator SpawnParticles()
        {
            if (shinyCircleSprite == null || particleParent == null) yield break;

            for (int i = 0; i < shinyCircleCount; i++)
            {
                GameObject particle = new GameObject("UnlockParticle");
                RectTransform rect = particle.AddComponent<RectTransform>();
                Image img = particle.AddComponent<Image>();
                img.sprite = shinyCircleSprite;
                img.SetNativeSize();
                
                rect.SetParent(particleParent, false);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.zero;

                float angle = Random.Range(0f, 360f);
                float dist = Random.Range(100f, 400f);
                Vector2 targetPos = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * dist;

                rect.DOScale(Random.Range(0.5f, 1.2f), 0.3f);
                rect.DOAnchorPos(targetPos, shinyCircleAnimationDuration).SetEase(Ease.OutQuad);
                img.DOFade(0, shinyCircleAnimationDuration).SetEase(Ease.InQuad).OnComplete(() => Destroy(particle));

                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}
