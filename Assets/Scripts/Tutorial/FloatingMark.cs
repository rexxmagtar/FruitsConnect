using UnityEngine;
using DG.Tweening;

namespace Tutorial
{
    /// <summary>
    /// Simple script to make a GameObject float up and down in a loop.
    /// Starting from its initial position.
    /// </summary>
    public class FloatingMark : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float floatDistance = 0.5f;
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private Ease easeType = Ease.InOutQuad;
        [SerializeField] private bool useLocalPosition = true;

        private void Start()
        {
            if (useLocalPosition)
            {
                transform.DOLocalMoveY(transform.localPosition.y + floatDistance, duration)
                    .SetEase(easeType)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                transform.DOMoveY(transform.position.y + floatDistance, duration)
                    .SetEase(easeType)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void OnDestroy()
        {
            // Kill any active tweens on this object to prevent memory leaks
            transform.DOKill();
        }
    }
}
