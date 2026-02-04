using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using Io.AppMetrica;
#endif

namespace AnalyticsServices
{
    public static class AppMetricaActivator
    {
        // AppMetrica API Key - can be set via SetApiKey() or stored in PlayerPrefs
        private static string _apiKey = "77f224ba-5982-4a3c-9bac-849f0609ab71";
        private const string API_KEY_PREFS_KEY = "AppMetrica_APIKey";
        private static bool _isActivated = false;
        
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Activate()
        {
            // Try to activate on startup
            TryActivate();
        }
        
        private static void TryActivate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_isActivated)
            {
                return; // Already activated
            }
            
            // Get API key from static field or PlayerPrefs
            if (string.IsNullOrEmpty(_apiKey))
            {
                _apiKey = PlayerPrefs.GetString(API_KEY_PREFS_KEY, "");
            }
            
            // Only activate if API key is set
            if (string.IsNullOrEmpty(_apiKey))
            {
                // Don't log warning here as AnalyticsService might set it later
                // Activation will be retried when API key is set via SetApiKey()
                return;
            }
            
            try
            {
                // Create config with location tracking disabled
                var config = new AppMetricaConfig(_apiKey)
                {
                    FirstActivationAsUpdate = !IsFirstLaunch(),
                    LocationTracking = false // Disable geo tracking
                };
                
                AppMetrica.Activate(config);
                _isActivated = true;
                Debug.Log("AppMetrica activated successfully with geo tracking disabled");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to activate AppMetrica: {e.Message}");
            }
#else
            Debug.LogWarning("AppMetrica is only supported on Android and iOS platforms");
#endif
        }

        private static bool IsFirstLaunch()
        {
            const string FIRST_LAUNCH_KEY = "AppMetrica_FirstLaunch";
            
            // Check if this is the first launch by checking PlayerPrefs
            if (!PlayerPrefs.HasKey(FIRST_LAUNCH_KEY))
            {
                // First launch - mark it and return true
                PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
                PlayerPrefs.Save();
                return true;
            }
            
            // Not first launch
            return false;
        }
    }
}
