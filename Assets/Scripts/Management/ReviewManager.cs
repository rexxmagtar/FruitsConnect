using UnityEngine;
using System.Collections;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

namespace Management
{
    /// <summary>
    /// Manages the Google Play In-App Review flow (Android only; no-op on WebGL / other platforms).
    /// </summary>
    public class ReviewManager : MonoBehaviour
    {
        public static ReviewManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ReviewManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ReviewManager");
                        _instance = go.AddComponent<ReviewManager>();
                    }
                }
                return _instance;
            }
        }
        private static ReviewManager _instance;

#if UNITY_ANDROID && !UNITY_EDITOR
        private Google.Play.Review.ReviewManager _reviewManager;
        private PlayReviewInfo _playReviewInfo;
        private Coroutine _prepareCoroutine;
#endif

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Starts preloading the review info.
        /// </summary>
        public void PrepareReview()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _playReviewInfo = null;
            if (_prepareCoroutine != null) StopCoroutine(_prepareCoroutine);
            _prepareCoroutine = StartCoroutine(PrepareCoroutine());
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator PrepareCoroutine()
        {
            _reviewManager = new Google.Play.Review.ReviewManager();
            var requestFlowOperation = _reviewManager.RequestReviewFlow();
            yield return requestFlowOperation;

            if (requestFlowOperation.Error == ReviewErrorCode.NoError)
            {
                _playReviewInfo = requestFlowOperation.GetResult();
            }
            _prepareCoroutine = null;
        }
#endif

        /// <summary>
        /// Shows the review if prepared and no level is running.
        /// </summary>
        public void ShowReview()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(ShowCoroutine());
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator ShowCoroutine()
        {
            // Wait for prepare to finish if it's still running
            while (_prepareCoroutine != null)
            {
                yield return null;
            }

            if (_playReviewInfo != null)
            {
                // Check if a level is currently running
                if (GameController.Instance != null && GameController.Instance.GameplayEnabled)
                {
                    Debug.Log("[ReviewManager] Gameplay is enabled, skipping review.");
                    yield break;
                }

                var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
                yield return launchFlowOperation;
                _playReviewInfo = null;
            }
        }
#endif
    }
}
