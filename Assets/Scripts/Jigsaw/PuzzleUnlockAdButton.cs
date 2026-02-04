using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using AdsServices;
using System.Collections.Generic;
using DG.Tweening;

namespace JigsawSystem
{
    public class PuzzleUnlockAdButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private GameObject loadingContainer;
        [SerializeField] private GameObject clickContainer;
        [SerializeField] private RectTransform loadingIcon;
        [SerializeField] private RectTransform adIconContainer;
        
        [Header("Settings")]
        [SerializeField] private float rotationSpeed = 180f;

        [Header("Pulse Animation")]
        [SerializeField] private float pulseScale = 1.15f;
        [SerializeField] private float pulseDuration = 0.25f;
        [SerializeField] private float pauseDuration = 1.5f;

        private string puzzleId;
        private int pieceIndex;
        private PuzzleSolveUI solveUI;
        private bool isShowingAd = false;
        private Coroutine pulseCoroutine;

        public void Initialize(string puzzleId, int pieceIndex, PuzzleSolveUI ui)
        {
            this.puzzleId = puzzleId;
            this.pieceIndex = pieceIndex;
            this.solveUI = ui;

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
            
            // Ensure we have a LayoutElement
            if (GetComponent<LayoutElement>() == null)
            {
                gameObject.AddComponent<LayoutElement>();
            }

            UpdateState();
            StartPulseAnimation();
        }

        private void OnDisable()
        {
            StopPulseAnimation();
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

        private void Update()
        {
            if (loadingContainer != null && loadingContainer.activeSelf)
            {
                if (loadingIcon != null)
                {
                    loadingIcon.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
                }
            }

            // Periodically check ad readiness if not ready
            if (!isShowingAd)
            {
                UpdateState();
            }
        }

        private void UpdateState()
        {
            bool isReady = AdsManager.Instance.IsRewardedAdReady();
            
            if (loadingContainer != null) loadingContainer.SetActive(!isReady);
            if (clickContainer != null) clickContainer.SetActive(isReady);
            if (button != null) button.interactable = isReady;
        }

        private async void OnButtonClick()
        {
            if (isShowingAd) return;
            
            isShowingAd = true;
            if (button != null) button.interactable = false;

            bool success = await AdsManager.Instance.ShowRewardedAdAsync();
            
            if (success)
            {
                // Award the piece immediately before showing UI and refreshing
                // This ensures the piece is saved before Refresh() reads CollectedPieces
                string pieceId = puzzleId + "_" + pieceIndex;
                string awardedId = JigsawPuzzleManager.Instance.AwardPiece(pieceId);
                
                if (!string.IsNullOrEmpty(awardedId))
                {
                    // Show earned UI (this will display the piece that was just awarded)
                    if (PuzzlePieceEarnedUI.Instance != null)
                    {
                        PuzzlePieceEarnedUI.Instance.Show(new List<string> { awardedId });
                    }
                    
                    // Refresh the solve UI - now the piece is already saved, so it will show correctly
                    if (solveUI != null)
                    {
                        solveUI.RefreshFromAd();
                    }
                }
            }
            else
            {
                isShowingAd = false;
                UpdateState();
            }
        }
    }
}
