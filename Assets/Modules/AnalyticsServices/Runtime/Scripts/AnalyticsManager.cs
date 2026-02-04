using UnityEngine;
using System.Collections.Generic;
using System;
using Core;


namespace AnalyticsServices
{
    public class AnalyticsManager : MonoBehaviour
    {
        private static AnalyticsManager _instance;
        public static AnalyticsManager Instance => _instance;
        
        // Level time tracking
        private DateTime levelStartTime;
        private int currentLevelIndex = -1;
        
        // Registration date tracking key
        private const string REGISTRATION_DATE_KEY = "Analytics_RegistrationDate";
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeRegistrationDate();
                SubscribeToEvents();
            }
            else
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
                string dateString = DateTime.Now.ToString("yyyy-MM-dd");
                PlayerPrefs.SetString(REGISTRATION_DATE_KEY, dateString);
                PlayerPrefs.Save();
            }
        }
        
        /// <summary>
        /// Get days since registration
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
            
            if (DateTime.TryParse(dateString, out DateTime regDate))
            {
                TimeSpan difference = DateTime.Now - regDate;
                return Mathf.Max(0, (int)difference.TotalDays);
            }
            
            // Fallback to 0 if parsing fails
            return 0;
        }
        
        private void SubscribeToEvents()
        {
            // Level Events
            GameEvents.OnLevelStarted += OnLevelStarted;
            GameEvents.OnLevelCompleted += OnLevelCompleted;
            GameEvents.OnLevelFailed += OnLevelFailed;
            GameEvents.OnLevelRestarted += OnLevelRestarted;
            
            // Gameplay Events
            GameEvents.OnTruckFilled += OnTruckFilled;
            GameEvents.OnPerfectTruckFill += OnPerfectTruckFill;
            
            // UI Events
            GameEvents.OnGamePaused += OnGamePaused;
            GameEvents.OnGameResumed += OnGameResumed;
            
            // Monetization Events
            GameEvents.OnAdWatched += OnAdWatched;
            GameEvents.OnNoAdsPurchaseAttempted += OnNoAdsPurchaseAttempted;
            GameEvents.OnPurchaseCompleted += OnPurchaseCompleted;
            
            // Error Events
            GameEvents.OnGameplayError += OnGameplayError;
        }
        
        private void UnsubscribeFromEvents()
        {
            // Level Events
            GameEvents.OnLevelStarted -= OnLevelStarted;
            GameEvents.OnLevelCompleted -= OnLevelCompleted;
            GameEvents.OnLevelFailed -= OnLevelFailed;
            GameEvents.OnLevelRestarted -= OnLevelRestarted;
            
            // Gameplay Events
            GameEvents.OnTruckFilled -= OnTruckFilled;
            GameEvents.OnPerfectTruckFill -= OnPerfectTruckFill;
            
            // UI Events
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnGameResumed -= OnGameResumed;
            
            // Monetization Events
            GameEvents.OnAdWatched -= OnAdWatched;
            GameEvents.OnNoAdsPurchaseAttempted -= OnNoAdsPurchaseAttempted;
            GameEvents.OnPurchaseCompleted -= OnPurchaseCompleted;
            
            // Error Events
            GameEvents.OnGameplayError -= OnGameplayError;
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        // Event handlers
        private void OnLevelStarted(int levelIndex, string levelName)
        {
            // Track level start time
            levelStartTime = DateTime.Now;
            currentLevelIndex = levelIndex;
            
            // Get level number (1-based for display)
            int levelNumber = levelIndex + 1;
            int daysSinceReg = GetDaysSinceRegistration();
            
            // Track with new method
            AnalyticsService.Instance.TrackLevelStart(levelNumber, daysSinceReg);
            Debug.Log($"Analytics: Level Started - {levelName} (Index: {levelIndex}, Level: {levelNumber})");
        }
        
        private void OnLevelCompleted(int levelIndex, string levelName, int score, int stars)
        {
            // Calculate time spent
            int timeSpent = CalculateTimeSpent();
            int levelNumber = levelIndex + 1;
            int daysSinceReg = GetDaysSinceRegistration();
            
            // Track with new method
            AnalyticsService.Instance.TrackLevelComplete(levelNumber, timeSpent, daysSinceReg);
            Debug.Log($"Analytics: Level Completed - {levelName} | Level: {levelNumber} | Score: {score} | Stars: {stars} | Time: {timeSpent}s");
        }
        
        private void OnLevelFailed(int levelIndex, string levelName, string failReason)
        {
            // Calculate time spent
            int timeSpent = CalculateTimeSpent();
            int levelNumber = levelIndex + 1;
            int daysSinceReg = GetDaysSinceRegistration();
            
            // Track with new method
            AnalyticsService.Instance.TrackLevelFail(levelNumber, failReason, timeSpent, daysSinceReg);
            Debug.Log($"Analytics: Level Failed - {levelName} | Level: {levelNumber} | Reason: {failReason} | Time: {timeSpent}s");
        }

        /// <summary>
        /// Calculate time spent on current level in seconds
        /// </summary>
        private int CalculateTimeSpent()
        {
            if (levelStartTime == default(DateTime))
                return 0;
            
            TimeSpan timeSpan = DateTime.Now - levelStartTime;
            return (int)timeSpan.TotalSeconds;
        }
        
        private void OnLevelRestarted(int levelIndex, string levelName, int attemptNumber)
        {
            Debug.Log($"Analytics: Level Restarted - {levelName} (Attempt: {attemptNumber})");
        }
        
        private void OnTruckFilled(int levelIndex, string truckType, int particlesInTruck, int truckCapacity, float fillPercentage)
        {
            Debug.Log($"Analytics: Truck Filled - {truckType} | Fill: {fillPercentage:F1}%");
        }
        
        private void OnPerfectTruckFill(int levelIndex, string truckType, int particlesUsed)
        {
            // var parameters = new Dictionary<string, object>
            // {
            //     { "level_index", levelIndex },
            //     { "truck_type", truckType },
            //     { "particles_used", particlesUsed }
            // };
            
            // AnalyticsService.Instance.LogEvent("perfect_truck_fill", parameters);
            Debug.Log($"Analytics: Perfect Truck Fill - {truckType}");
        }
        
        private void OnGamePaused(int levelIndex)
        {
            var parameters = new Dictionary<string, object>
            {
                { "level_index", levelIndex }
            };
            
            // AnalyticsService.Instance.LogEvent("game_paused", parameters);
        }
        
        private void OnGameResumed(int levelIndex)
        {
            var parameters = new Dictionary<string, object>
            {
                { "level_index", levelIndex }
            };
            
            // AnalyticsService.Instance.LogEvent("game_resumed", parameters);
        }
        
        private void OnAdWatched(string adType, string placement, int levelIndex)
        {
            var parameters = new Dictionary<string, object>
            {
                { "ad_type", adType },
                { "placement", placement },
                { "level_index", levelIndex }
            };
            
            // AnalyticsService.Instance.LogEvent("ad_watched", parameters);
        }
        
        private void OnNoAdsPurchaseAttempted(int levelIndex)
        {
            var parameters = new Dictionary<string, object>
            {
                { "level_index", levelIndex }
            };
            
            // AnalyticsService.Instance.LogEvent("no_ads_purchase_attempted", parameters);
        }
        
        private void OnPurchaseCompleted(string productId, string productName, double price, string currency)
        {
            // Track purchase analytics
            int daysSinceReg = GetDaysSinceRegistration();
            AnalyticsService.Instance.TrackPurchase(productId, productName, price, currency, daysSinceReg);
            Debug.Log($"Analytics: Purchase Completed - {productName} ({productId}) | Price: {price} {currency}");
        }
        
        private void OnGameplayError(string errorType, string errorMessage, int levelIndex)
        {
            var parameters = new Dictionary<string, object>
            {
                { "error_type", errorType },
                { "error_message", errorMessage },
                { "level_index", levelIndex }
            };
            
            // AnalyticsService.Instance.LogEvent("gameplay_error", parameters);
        }
    }
} 