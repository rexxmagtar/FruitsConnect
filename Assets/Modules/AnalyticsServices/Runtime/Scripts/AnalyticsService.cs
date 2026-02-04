using UnityEngine;
using Firebase.Analytics;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using System.Threading.Tasks;
using System;

namespace AnalyticsServices
{
public class AnalyticsService : MonoBehaviour
{

    // Registration date tracking key (same as AnalyticsManager)
    private const string REGISTRATION_DATE_KEY = "Analytics_RegistrationDate";

    private static AnalyticsService instance;
    public static AnalyticsService Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<AnalyticsService>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            InitializeRegistrationDate();
            
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Initialize or get registration date (first launch date)
    /// </summary>
    private void InitializeRegistrationDate()
    {
        if (!PlayerPrefs.HasKey(REGISTRATION_DATE_KEY))
        {
            // First launch - save current date
            string dateString = System.DateTime.Now.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(REGISTRATION_DATE_KEY, dateString);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Get days since registration (for direct calls from other systems)
    /// </summary>
    private int GetDaysSinceRegistration()
    {
        string dateString = PlayerPrefs.GetString(REGISTRATION_DATE_KEY, "");
        
        if (string.IsNullOrEmpty(dateString))
        {
            // Fallback - initialize if missing
            InitializeRegistrationDate();
            dateString = PlayerPrefs.GetString(REGISTRATION_DATE_KEY, "");
        }
        
        if (System.DateTime.TryParse(dateString, out System.DateTime regDate))
        {
            System.TimeSpan difference = System.DateTime.Now - regDate;
            return Mathf.Max(0, (int)difference.TotalDays);
        }
        
        // Fallback to 0 if parsing fails
        return 0;
    }

    public async Task Initialize(){

        bool firebaseInitialized = false;   
        
         FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            try{
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            }
            else
            {
                Debug.LogWarning($"Firebase initialization failed: {dependencyStatus}");
            }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during Firebase initialization: {e.Message} + {e.StackTrace}");
            }
            finally{
                firebaseInitialized = true;
            }
        });

        while(!firebaseInitialized){
            await Task.Delay(100);
        }

    }

    public void TrackSessionStart()
    {
        Parameter[] parameters = {
            new Parameter("game_name", "PuzzleTask"),
            new Parameter("game_version", Application.version)
        };
        FirebaseAnalytics.LogEvent("session_start", parameters);
    }

    /// <summary>
    /// Track level start event
    /// </summary>
    public void TrackLevelStart(int level, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("level", level),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("level_start", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportLevelStart(level, daysSinceReg);
    }

    /// <summary>
    /// Track level complete event
    /// </summary>
    public void TrackLevelComplete(int level, int timeSpent, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("level", level),
            new Parameter("time_spent", timeSpent),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("level_complete", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportLevelComplete(level, timeSpent, daysSinceReg);
    }

    /// <summary>
    /// Track level fail event
    /// </summary>
    public void TrackLevelFail(int level, string reason, int timeSpent, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("level", level),
            new Parameter("reason", reason ?? "unknown"),
            new Parameter("time_spent", timeSpent),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("level_fail", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportLevelFail(level, reason, timeSpent, daysSinceReg);
    }

    // Legacy methods for backward compatibility
    public void TrackLevelStart(string missionId)
    {
        int level = GetLevelNumberFromMissionId(missionId);
        int daysSinceReg = GetDaysSinceRegistration();
        TrackLevelStart(level, daysSinceReg);
    }

    public void TrackLevelComplete(string missionId)
    {
        int level = GetLevelNumberFromMissionId(missionId);
        int daysSinceReg = GetDaysSinceRegistration();
        // Note: time_spent is 0 for legacy calls - should be tracked separately
        TrackLevelComplete(level, 0, daysSinceReg);
    }

    private int GetLevelNumberFromMissionId(string missionId)
    {
        // Try to extract level number from mission ID (e.g., "Level_01" -> 1)
        if (string.IsNullOrEmpty(missionId))
            return 0;

        // Try to find number in the string
        string numberPart = System.Text.RegularExpressions.Regex.Match(missionId, @"\d+").Value;
        if (int.TryParse(numberPart, out int level))
            return level;

        return 0;
    }

    public void TrackLayerRotation(int layerIndex, float angle)
    {
        Parameter[] parameters = {
            new Parameter("layer_index", layerIndex),
            new Parameter("rotation_angle", angle)
        };
        FirebaseAnalytics.LogEvent("layer_rotation", parameters);
    }

    public void TrackHintUsed()
    {
        FirebaseAnalytics.LogEvent("hint_used");
    }

    public void TrackPuzzleReset()
    {
        FirebaseAnalytics.LogEvent("puzzle_reset");
    }

    /// <summary>
    /// Track tutorial start event
    /// </summary>
    public void TrackTutorialStart(string tutorialName, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("tutorial_name", tutorialName),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("tutorial_start", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportTutorialStart(tutorialName, daysSinceReg);
    }

    /// <summary>
    /// Track tutorial complete event
    /// </summary>
    public void TrackTutorialComplete(string tutorialName, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("tutorial_name", tutorialName),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("tutorial_complete", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportTutorialComplete(tutorialName, daysSinceReg);
    }

    public void TrackTutorialStep(string stepName)
    {
        Parameter[] parameters = {
            new Parameter("tutorial_step", stepName)
        };
        FirebaseAnalytics.LogEvent("tutorial_step", parameters);
    }

    // Legacy methods for backward compatibility
    public void TrackTutorialStart()
    {
        TrackTutorialStart("unknown", GetDaysSinceRegistration());
    }

    public void TrackTutorialComplete()
    {
        TrackTutorialComplete("unknown", GetDaysSinceRegistration());
    }
    
    // Overloads for convenience (calculate days_since_reg automatically)
    public void TrackTutorialStart(string tutorialName)
    {
        TrackTutorialStart(tutorialName, GetDaysSinceRegistration());
    }

    public void TrackTutorialComplete(string tutorialName)
    {
        TrackTutorialComplete(tutorialName, GetDaysSinceRegistration());
    }
    
    public void TrackPurchase(string productId, string productName, double price, string currency)
    {
        TrackPurchase(productId, productName, price, currency, GetDaysSinceRegistration());
    }
    
    public void TrackPuzzleComplete(string puzzleId)
    {
        TrackPuzzleComplete(puzzleId, GetDaysSinceRegistration());
    }
    
    public void TrackBaseBuildingNewLevel(int baseLevel)
    {
        TrackBaseBuildingNewLevel(baseLevel, GetDaysSinceRegistration());
    }

    /// <summary>
    /// Track purchase event
    /// </summary>
    public void TrackPurchase(string productId, string productName, double price, string currency, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("product_id", productId),
            new Parameter("product_name", productName),
            new Parameter("price", price),
            new Parameter("currency", currency),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("purchase", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportPurchase(productId, productName, price, currency, daysSinceReg);
    }

    /// <summary>
    /// Track puzzle complete event
    /// </summary>
    public void TrackPuzzleComplete(string puzzleId, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("puzzle_id", puzzleId),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("puzzle_complete", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportPuzzleComplete(puzzleId, daysSinceReg);
    }

    /// <summary>
    /// Track base building new level event
    /// </summary>
    public void TrackBaseBuildingNewLevel(int baseLevel, int daysSinceReg)
    {
        // Firebase Analytics
        Parameter[] firebaseParams = {
            new Parameter("base_level", baseLevel),
            new Parameter("days_since_reg", daysSinceReg)
        };
        FirebaseAnalytics.LogEvent("base_building_new_level", firebaseParams);

        // AppMetrica Analytics
        AppMetricaAnalytics.Instance.ReportBaseBuildingNewLevel(baseLevel, daysSinceReg);
    }
} 
}
