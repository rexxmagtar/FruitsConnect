using UnityEngine;
using System.Collections.Generic;
using Core;

namespace AnalyticsServices
{
    public class AnalyticsManager : MonoBehaviour
    {
        private static AnalyticsManager _instance;
        public static AnalyticsManager Instance => _instance;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                SubscribeToEvents();
            }
            else
            {
                Destroy(gameObject);
            }
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
            AnalyticsService.Instance.TrackLevelStart(levelName);
            Debug.Log($"Analytics: Level Started - {levelName} (Index: {levelIndex})");
        }
        
        private void OnLevelCompleted(int levelIndex, string levelName, int score, int stars)
        {
            AnalyticsService.Instance.TrackLevelComplete(levelName);
            Debug.Log($"Analytics: Level Completed - {levelName} | Score: {score} | Stars: {stars}");
        }
        
        private void OnLevelFailed(int levelIndex, string levelName, string failReason)
        {
            // AnalyticsService.Instance.TrackLevelFailed(levelName, failReason);
            Debug.Log($"Analytics: Level Failed - {levelName} | Reason: {failReason}");
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