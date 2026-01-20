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
    }
}
