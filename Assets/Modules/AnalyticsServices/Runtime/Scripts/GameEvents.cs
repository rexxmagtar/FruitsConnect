using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core
{
    public static class GameEvents
    {
        // Level Events
        public static event Action<int, string> OnLevelStarted;
        public static event Action<int, string, int, int> OnLevelCompleted;
        public static event Action<int, string, string> OnLevelFailed;
        public static event Action<int, string, int> OnLevelRestarted;
        
        // Gameplay Events
        public static event Action<int, string, int, int, float> OnTruckFilled;
        public static event Action<int, string, int> OnPerfectTruckFill;
        
        // UI Events
        public static event Action<int> OnGamePaused;
        public static event Action<int> OnGameResumed;
        
        // Monetization Events
        public static event Action<string, string, int> OnAdWatched;
        public static event Action<int> OnNoAdsPurchaseAttempted;
        
        // Error Events
        public static event Action<string, string, int> OnGameplayError;
        
        // Trigger methods
        public static void LevelStarted(int levelIndex, string levelName)
        {
            OnLevelStarted?.Invoke(levelIndex, levelName);
        }
        
        public static void LevelCompleted(int levelIndex, string levelName, int score, int stars)
        {
            OnLevelCompleted?.Invoke(levelIndex, levelName, score, stars);
        }
        
        public static void LevelFailed(int levelIndex, string levelName, string failReason)
        {
            OnLevelFailed?.Invoke(levelIndex, levelName, failReason);
        }
        
        public static void LevelRestarted(int levelIndex, string levelName, int attemptNumber)
        {
            OnLevelRestarted?.Invoke(levelIndex, levelName, attemptNumber);
        }
        
        // Gameplay Events
        public static void TruckFilled(int levelIndex, string truckType, int particlesInTruck, int truckCapacity, float fillPercentage)
        {
            OnTruckFilled?.Invoke(levelIndex, truckType, particlesInTruck, truckCapacity, fillPercentage);
        }
        
        public static void PerfectTruckFill(int levelIndex, string truckType, int particlesUsed)
        {
            OnPerfectTruckFill?.Invoke(levelIndex, truckType, particlesUsed);
        }
        
        // UI Events
        public static void GamePaused(int levelIndex)
        {
            OnGamePaused?.Invoke(levelIndex);
        }
        
        public static void GameResumed(int levelIndex)
        {
            OnGameResumed?.Invoke(levelIndex);
        }
        
        // Monetization Events
        public static void AdWatched(string adType, string placement, int levelIndex)
        {
            OnAdWatched?.Invoke(adType, placement, levelIndex);
        }
        
        public static void NoAdsPurchaseAttempted(int levelIndex)
        {
            OnNoAdsPurchaseAttempted?.Invoke(levelIndex);
        }
        
        // Error Events
        public static void GameplayError(string errorType, string errorMessage, int levelIndex)
        {
            OnGameplayError?.Invoke(errorType, errorMessage, levelIndex);
        }
    }
} 