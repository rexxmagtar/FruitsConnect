using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

        private bool isWaitingForClick = false;
        private List<string> pendingPieceIds = new List<string>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                panel.SetActive(false);
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
    }
}
