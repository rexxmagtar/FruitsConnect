using UnityEngine;
using DG.Tweening;

namespace Tutorial
{
    /// <summary>
    /// Simulates a cursor click and move sequence for tutorials.
    /// 1. Appears at initial pos.
    /// 2. Scales down (simulating click).
    /// 3. Moves to target point while scaled down.
    /// 4. Scales back up.
    /// 5. Fades out.
    /// 6. Loops.
    /// </summary>
    public class CursorLink : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private Transform targetPoint;
        [SerializeField] private Vector3 fallbackOffset = new Vector3(2f, 0f, 0f);

        [Header("Animation Durations")]
        [SerializeField] private float moveDuration = 1.2f;
        [SerializeField] private float clickScaleDuration = 0.2f;
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private float resetDelay = 0.5f;

        [Header("Animation Settings")]
        [SerializeField] private float clickScaleMultiplier = 0.8f;
        [SerializeField] private Ease moveEase = Ease.InOutQuad;
        [SerializeField] private bool useLocalSpace = false;

        private Vector3 startPosition;
        private Vector3 startScale;
        private CanvasGroup canvasGroup;
        private Sequence mainSequence;

        private void Awake()
        {
            // Store initial state
            startPosition = useLocalSpace ? transform.localPosition : transform.position;
            startScale = transform.localScale;
            
            // Try to get CanvasGroup for fading (common in UI tutorials)
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            PlaySequence();
        }

        private void OnDisable()
        {
            mainSequence?.Kill();
        }

        private void PlaySequence()
        {
            mainSequence?.Kill();

            // Reset state
            if (useLocalSpace) transform.localPosition = startPosition;
            else transform.position = startPosition;
            
            transform.localScale = startScale;
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            // Determine target
            Vector3 targetPos;
            if (targetPoint != null)
            {
                targetPos = useLocalSpace ? targetPoint.localPosition : targetPoint.position;
            }
            else
            {
                targetPos = startPosition + fallbackOffset;
            }

            mainSequence = DOTween.Sequence();

            // 1. Initial Scale down (Click)
            mainSequence.Append(transform.DOScale(startScale * clickScaleMultiplier, clickScaleDuration));

            // 2. Move to destination while scaled down
            if (useLocalSpace)
            {
                mainSequence.Append(transform.DOLocalMove(targetPos, moveDuration).SetEase(moveEase));
            }
            else
            {
                mainSequence.Append(transform.DOMove(targetPos, moveDuration).SetEase(moveEase));
            }

            // 3. Scale back up (Release)
            mainSequence.Append(transform.DOScale(startScale, clickScaleDuration));

            // 4. Fade out
            if (canvasGroup != null)
            {
                mainSequence.Append(canvasGroup.DOFade(0f, fadeDuration));
            }
            else
            {
                // If no CanvasGroup, we can scale to zero as a fallback "fade"
                mainSequence.Append(transform.DOScale(Vector3.zero, fadeDuration));
            }

            // 5. Reset and Loop
            mainSequence.AppendInterval(resetDelay);
            mainSequence.OnComplete(() => PlaySequence());
            
            mainSequence.Play();
        }

        private void OnDestroy()
        {
            mainSequence?.Kill();
        }
        
        // Context menu helper to set target point in editor if needed
        [ContextMenu("Set Start Position")]
        private void SetStartPos()
        {
            startPosition = useLocalSpace ? transform.localPosition : transform.position;
            startScale = transform.localScale;
        }
    }
}
