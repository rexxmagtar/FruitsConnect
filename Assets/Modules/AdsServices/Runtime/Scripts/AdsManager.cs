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
        private LevelPlayRewardedAd _rewardedAd;

        public string profileId = "226de93e5";
        public string rewardedAdUnitId = "p516a7qwetzb5pli"; // TODO: Update with actual rewarded ad unit ID if different from banner

        public string bannerAdUnitId = "p516a7qwetzb5pli";
        public static AdsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AdsManager>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        private TaskCompletionSource<bool> _rewardedAdCompletionSource;

        private void Awake()
        {
            _instance = this;
        }

        public async Task Initialize()
        {
            Debug.Log("Initializing AdsManager");
            
                // First check GDPR consent
                if(ComplianceService.GDPRManager.Instance.IsUserInEurope())
                {
                    bool gdprConsent = await ComplianceService.GDPRManager.Instance.CheckAndShowGDPRConsentAsync();
                    LevelPlay.SetConsent(gdprConsent);
                }

                // Then check COPPA status  
                if(ComplianceService.ChildAgeManager.Instance.IsUserInUSA())
                {
                    bool isChild = await ComplianceService.ChildAgeManager.Instance.CheckAndShowChildAgeDialogAsync();
                    LevelPlay.SetMetaData("is_child_directed", isChild ? "true" : "false");
                }

                // Initialize LevelPlay after both checks are complete
                LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
                LevelPlay.OnInitFailed += SdkInitializationFailedEvent;


                Debug.Log("Initializing LevelPlay SDK");
                LevelPlay.Init(profileId, null);
        }

        private void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
        {
            Debug.Log("LevelPlay SDK initialization completed");

        //    _bannerAd = new LevelPlayBannerAd(bannerAdUnitId, new LevelPlayBannerAdConfig { DisplayOnLoad = false, Position = LevelPlayBannerPosition.TopCenter });

        //    _bannerAd.LoadAd();

           // Initialize Rewarded
           _rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);
           _rewardedAd.OnAdDisplayed += RewardedVideoOnAdOpenedEvent;
           _rewardedAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
           _rewardedAd.OnAdLoaded += RewardedVideoOnAdAvailable;
           _rewardedAd.OnAdLoadFailed += RewardedVideoOnAdUnavailable;
           _rewardedAd.OnAdDisplayFailed += RewardedVideoOnAdShowFailedEvent;
           _rewardedAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
           _rewardedAd.OnAdClicked += RewardedVideoOnAdClickedEvent;
           _rewardedAd.LoadAd();
        }

        private void SdkInitializationFailedEvent(LevelPlayInitError error)
        {
            Debug.LogError($"LevelPlay SDK initialization failed: {error}");
        }

        public async Task<bool> ShowRewardedAdAsync()
        {
            _rewardedAdCompletionSource = new TaskCompletionSource<bool>();

            #if UNITY_EDITOR
            await Task.Delay(2000);
            _rewardedAdCompletionSource.TrySetResult(true);
            #else
            _rewardedAd?.ShowAd();
            #endif

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

        public async Task<bool> ShowRewardedAdForRevive()
        {
            _rewardedAdCompletionSource = new TaskCompletionSource<bool>();
            
            Debug.Log("Showing rewarded ad for revive");
            _rewardedAd?.ShowAd("revive_screen");
            
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

        private void RewardedVideoOnAdOpenedEvent(LevelPlayAdInfo adInfo)
        {
        }

        private void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo adInfo)
        {
            // Reload ad after closing
            _rewardedAd?.LoadAd();
        }

        private void RewardedVideoOnAdAvailable(LevelPlayAdInfo adInfo)
        {
        }

        private void RewardedVideoOnAdUnavailable(LevelPlayAdError error)
        {
        }


        private void RewardedVideoOnAdShowFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)

        {
            Debug.LogError($"Rewarded video ad show failed: {error}");
            _rewardedAdCompletionSource?.TrySetResult(false);
            // Reload ad after failure
            _rewardedAd?.LoadAd();
        }

        private void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            _rewardedAdCompletionSource?.TrySetResult(true);
        }

        private void RewardedVideoOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
        }

        public bool IsRewardedAdReady()
        {
            #if UNITY_EDITOR
            return true;
            #else
            return _rewardedAd != null && _rewardedAd.IsAdReady();
            #endif
        }
    }
}
