using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace AdsServices
{
    public class AdsManager : MonoBehaviour
    {
        private static AdsManager _instance;

        private LevelPlayBannerAd _bannerAd;

        public static AdsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AdsManager>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        private TaskCompletionSource<bool> _rewardedAdCompletionSource;

        private void Awake()
        {
           
            _instance = this;

            IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedVideoOnAdOpenedEvent;
            IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedVideoOnAdClosedEvent;
            IronSourceRewardedVideoEvents.onAdAvailableEvent += RewardedVideoOnAdAvailable;
            IronSourceRewardedVideoEvents.onAdUnavailableEvent += RewardedVideoOnAdUnavailable;
            IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedVideoOnAdShowFailedEvent;
            IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedVideoOnAdRewardedEvent;
            IronSourceRewardedVideoEvents.onAdClickedEvent += RewardedVideoOnAdClickedEvent;
        }

        public async Task Initialize()
        {
            Debug.Log("Initializing AdsManager");
            
                // First check GDPR consent
                if(ComplianceService.GDPRManager.Instance.IsUserInEurope())
                {
                    bool gdprConsent = await ComplianceService.GDPRManager.Instance.CheckAndShowGDPRConsentAsync();
                    IronSource.Agent.setConsent(gdprConsent);
                }

                // Then check COPPA status  
                if(ComplianceService.ChildAgeManager.Instance.IsUserInUSA())
                {
                    bool isChild = await ComplianceService.ChildAgeManager.Instance.CheckAndShowChildAgeDialogAsync();
                    IronSource.Agent.setMetaData("is_child_directed", isChild ? "true" : "false");
                }

                // Initialize LevelPlay after both checks are complete
                com.unity3d.mediation.LevelPlayAdFormat[] legacyAdFormats = new[] { com.unity3d.mediation.LevelPlayAdFormat.REWARDED };
                LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
                LevelPlay.OnInitFailed += SdkInitializationFailedEvent;


                Debug.Log("Initializing LevelPlay SDK");
                LevelPlay.Init("226de93e5", null, legacyAdFormats);
        }

        private void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
        {
            Debug.Log("LevelPlay SDK initialization completed");
           _bannerAd = new LevelPlayBannerAd("p516a7qwetzb5pli",displayOnLoad: false, position: com.unity3d.mediation.LevelPlayBannerPosition.TopCenter);
           _bannerAd.LoadAd();
        }

        private void SdkInitializationFailedEvent(LevelPlayInitError error)
        {
            Debug.LogError($"LevelPlay SDK initialization failed: {error}");
        }

        public async Task<bool> ShowRewardedAdAsync()
        {
            #if UNITY_EDITOR
            await Task.Delay(2000);
            return true;
            #endif
            _rewardedAdCompletionSource = new TaskCompletionSource<bool>();
            IronSource.Agent.showRewardedVideo();
            return await _rewardedAdCompletionSource.Task;
        }

        public void ShowBannerAd()
        {
            _bannerAd.ShowAd();
        }

        public void HideBannerAd()
        {
            _bannerAd.HideAd();
        }

        public bool IsBannerReady()
        {
            return _bannerAd != null;
        }

        void OnApplicationPause(bool isPaused)
        {
            IronSource.Agent.onApplicationPause(isPaused);
        }

        public async Task<bool> ShowRewardedAdForRevive()
        {
            _rewardedAdCompletionSource = new TaskCompletionSource<bool>();
            
            Debug.Log("Showing rewarded ad for revive");
            IronSource.Agent.showRewardedVideo("revive_screen");
            
            try 
            {
                return await _rewardedAdCompletionSource.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error showing rewarded ad: {e}");
                return false;
            }
        }

        private void RewardedVideoOnAdOpenedEvent(IronSourceAdInfo adInfo)
        {
        }

        private void RewardedVideoOnAdClosedEvent(IronSourceAdInfo adInfo)
        {
        }

        private void RewardedVideoOnAdAvailable(IronSourceAdInfo adInfo)
        {
        }

        private void RewardedVideoOnAdUnavailable()
        {
        }

        private void RewardedVideoOnAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
        {
            Debug.LogError($"Rewarded video ad show failed: {error}");
            _rewardedAdCompletionSource?.TrySetResult(false);
        }

        private void RewardedVideoOnAdRewardedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
        {
            _rewardedAdCompletionSource?.TrySetResult(true);
        }

        private void RewardedVideoOnAdClickedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
        {
        }

        public bool IsRewardedAdReady()
        {
            #if UNITY_EDITOR
            return true;
            #endif
            return IronSource.Agent.isRewardedVideoAvailable();
        }
    }
}
