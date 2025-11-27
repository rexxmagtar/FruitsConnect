using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace WindowManager
{
    public class AnimatedButton : Button
    {
        [Header("Animation Settings")]
        [SerializeField] private float scaleAmount = 0.9f;
        [SerializeField] private float animationDuration = 0.1f;
        [SerializeField] private Ease animationEase = Ease.OutQuart;
        [SerializeField] private bool animateOnPress = true;
        [SerializeField] private bool animateOnRelease = true;
        
        private Vector3 originalScale;
        private Tween currentTween;
        private Tween externalTween;
        private bool isAnimating = false;
        private bool hasExternalTween = false;

        protected override void Awake()
        {
            base.Awake();
            originalScale = transform.localScale;
        }

        protected override void OnDestroy()
        {
            // Kill any running tweens to prevent memory leaks
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }
            if (externalTween != null && externalTween.IsActive())
            {
                externalTween.Kill();
            }
            base.OnDestroy();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            
            if (animateOnPress && IsInteractable())
            {
                // Pause external tween if it exists
                if (hasExternalTween && externalTween != null && externalTween.IsActive())
                {
                    externalTween.Pause();
                }
                
                AnimateScale(scaleAmount);
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            
            if (animateOnRelease && IsInteractable())
            {
                AnimateScale(1f);
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            
            // Reset scale when pointer exits in case it was pressed
            if (IsInteractable())
            {
                AnimateScale(1f);
            }
        }

        private void AnimateScale(float targetScale)
        {
            if (isAnimating)
            {
                // Kill current tween if one is already running
                if (currentTween != null && currentTween.IsActive())
                {
                    currentTween.Kill();
                }
            }

            isAnimating = true;
            Vector3 targetScaleVector = originalScale * targetScale;
            
            currentTween = transform.DOScale(targetScaleVector, animationDuration)
                .SetEase(animationEase)
                .OnComplete(() => {
                    isAnimating = false;
                    currentTween = null;
                    
                    // Resume external tween if it exists and we're returning to normal scale
                    if (targetScale >= 1f && hasExternalTween && externalTween != null && externalTween.IsActive())
                    {
                        externalTween.Play();
                    }
                });
        }

        // Public method to manually trigger the click animation
        public void PlayClickAnimation()
        {
            if (!IsInteractable()) return;
            
            // Create a quick scale down and back up animation
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(originalScale * scaleAmount, animationDuration / 2))
                   .Append(transform.DOScale(originalScale, animationDuration / 2))
                   .SetEase(animationEase);
        }

        // Public method to reset the button scale
        public void ResetScale()
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
            }
            transform.localScale = originalScale;
            isAnimating = false;
        }

        // Public method to set an external tween (like pulse animation)
        public void SetExternalTween(Tween tween)
        {
            // Kill any existing external tween
            if (externalTween != null && externalTween.IsActive())
            {
                externalTween.Kill();
            }
            
            externalTween = tween;
            hasExternalTween = tween != null;
        }

        // Public method to clear external tween
        public void ClearExternalTween()
        {
            if (externalTween != null && externalTween.IsActive())
            {
                externalTween.Kill();
            }
            externalTween = null;
            hasExternalTween = false;
        }

        // Public method to pause external tween
        public void PauseExternalTween()
        {
            if (hasExternalTween && externalTween != null && externalTween.IsActive())
            {
                externalTween.Pause();
            }
        }

        // Public method to resume external tween
        public void ResumeExternalTween()
        {
            if (hasExternalTween && externalTween != null && externalTween.IsActive())
            {
                externalTween.Play();
            }
        }
    }
} 