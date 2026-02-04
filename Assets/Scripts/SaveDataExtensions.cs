using UnityEngine;
using DataRepository;

/// <summary>
/// Extension methods and helper class for working with SaveData through ProgressSaveManager
/// Provides convenient access to common SaveData operations
/// </summary>
public static class SaveDataExtensions
{
    /// <summary>
    /// Get current SaveData instance
    /// </summary>
    public static SaveData GetSaveData()
    {
        return ProgressSaveManager<SaveData>.Instance.GetGameData();
    }
    
    /// <summary>
    /// Save current data to disk
    /// </summary>
    public static void Save()
    {
        ProgressSaveManager<SaveData>.Instance.SaveGameData();
    }
    
    /// <summary>
    /// Get current level index (0-based)
    /// </summary>
    public static int GetCurrentLevel()
    {
        return GetSaveData().CurrentLevel;
    }
    
    /// <summary>
    /// Set current level index
    /// </summary>
    public static void SetCurrentLevel(int level)
    {
        var saveData = GetSaveData();
        saveData.CurrentLevel = level;
        Save();
    }
    
    /// <summary>
    /// Get total coins
    /// </summary>
    public static int GetTotalCoins()
    {
        return GetSaveData().TotalCoins;
    }

    /// <summary>
    /// Get total energy spheres
    /// </summary>
    public static int GetTotalEnergySpheres()
    {
        return GetSaveData().TotalEnergySpheres;
    }
    
    /// <summary>
    /// Set total coins
    /// </summary>
    public static void SetTotalCoins(int coins)
    {
        var saveData = GetSaveData();
        saveData.TotalCoins = coins;
        Save();
    }

    /// <summary>
    /// Set total energy spheres
    /// </summary>
    public static void SetTotalEnergySpheres(int spheres)
    {
        var saveData = GetSaveData();
        saveData.TotalEnergySpheres = spheres;
        Save();
    }
    
    /// <summary>
    /// Add coins to total
    /// </summary>
    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        
        var saveData = GetSaveData();
        saveData.TotalCoins += amount;
        Save();
        
        Debug.Log($"[SaveData] Added {amount} coins. Total: {saveData.TotalCoins}");
    }

    /// <summary>
    /// Add energy spheres to total
    /// </summary>
    public static void AddEnergySpheres(int amount)
    {
        if (amount <= 0) return;

        var saveData = GetSaveData();
        saveData.TotalEnergySpheres += amount;
        Save();

        Debug.Log($"[SaveData] Added {amount} energy spheres. Total: {saveData.TotalEnergySpheres}");
    }
    
    /// <summary>
    /// Remove coins from total (for purchases, etc.)
    /// </summary>
    public static bool RemoveCoins(int amount)
    {
        if (amount <= 0) return false;
        
        var saveData = GetSaveData();
        
        if (saveData.TotalCoins < amount)
        {
            Debug.LogWarning($"[SaveData] Not enough coins. Have: {saveData.TotalCoins}, Need: {amount}");
            return false;
        }
        
        saveData.TotalCoins -= amount;
        Save();
        
        Debug.Log($"[SaveData] Removed {amount} coins. Total: {saveData.TotalCoins}");
        return true;
    }

    /// <summary>
    /// Remove energy spheres from total (saves to disk immediately)
    /// </summary>
    public static bool RemoveEnergySpheres(int amount)
    {
        if (amount <= 0) return false;

        var saveData = GetSaveData();

        if (saveData.TotalEnergySpheres < amount)
        {
            Debug.LogWarning($"[SaveData] Not enough energy spheres. Have: {saveData.TotalEnergySpheres}, Need: {amount}");
            return false;
        }

        saveData.TotalEnergySpheres -= amount;
        Save();

        Debug.Log($"[SaveData] Removed {amount} energy spheres. Total: {saveData.TotalEnergySpheres}");
        return true;
    }

    /// <summary>
    /// Remove energy spheres from total (updates in memory only, does not save to disk)
    /// Use this for frequent updates in building UI. Call SaveGameData() manually when needed.
    /// </summary>
    public static bool RemoveEnergySpheresInMemory(int amount)
    {
        if (amount <= 0) return false;

        var saveData = GetSaveData();

        if (saveData.TotalEnergySpheres < amount)
        {
            Debug.LogWarning($"[SaveData] Not enough energy spheres. Have: {saveData.TotalEnergySpheres}, Need: {amount}");
            return false;
        }

        saveData.TotalEnergySpheres -= amount;
        // Don't call Save() - data is updated in memory only

        Debug.Log($"[SaveData] Removed {amount} energy spheres (in memory). Total: {saveData.TotalEnergySpheres}");
        return true;
    }
    
    /// <summary>
    /// Complete current level and move to next
    /// </summary>
    public static void CompleteCurrentLevel()
    {
        var saveData = GetSaveData();
        saveData.CurrentLevel++;
        Save();
        
        Debug.Log($"[SaveData] Level completed! Now on level {saveData.CurrentLevel + 1}");
    }

    /// <summary>
    /// Get base level
    /// </summary>
    public static int GetBaseLevel()
    {
        return GetSaveData().BaseLevel;
    }

    /// <summary>
    /// Set base level
    /// </summary>
    public static void SetBaseLevel(int level)
    {
        var saveData = GetSaveData();
        saveData.BaseLevel = level;
        Save();
    }

    /// <summary>
    /// Get progress within the current base stage (energy spent)
    /// </summary>
    public static int GetBaseStageProgress()
    {
        return GetSaveData().BaseStageProgress;
    }

    /// <summary>
    /// Set progress within the current base stage (saves to disk immediately)
    /// </summary>
    public static void SetBaseStageProgress(int progress)
    {
        var saveData = GetSaveData();
        saveData.BaseStageProgress = progress;
        Save();
    }

    /// <summary>
    /// Set progress within the current base stage (updates in memory only, does not save to disk)
    /// Use this for frequent updates in building UI. Call SaveGameData() manually when needed.
    /// </summary>
    public static void SetBaseStageProgressInMemory(int progress)
    {
        var saveData = GetSaveData();
        saveData.BaseStageProgress = progress;
        // Don't call Save() - data is updated in memory only
    }
    
    /// <summary>
    /// Get whether ads are enabled
    /// </summary>
    public static bool IsAdEnabled()
    {
        return GetSaveData().IsAdEnabled;
    }
    
    /// <summary>
    /// Set ad enabled status
    /// </summary>
    public static void SetAdEnabled(bool enabled)
    {
        var saveData = GetSaveData();
        saveData.IsAdEnabled = enabled;
        Save();
    }
    
    /// <summary>
    /// Reset all progress (for testing or reset button)
    /// </summary>
    public static void ResetProgress()
    {
        var saveData = GetSaveData();
        saveData.CurrentLevel = 0;
        saveData.TotalCoins = 0;
        Save();
        
        Debug.Log("[SaveData] Progress reset to default");
    }
    
    /// <summary>
    /// Get current level number for display (1-based)
    /// </summary>
    public static int GetCurrentLevelNumber()
    {
        return GetCurrentLevel() + 1;
    }
    
    /// <summary>
    /// Check if player has enough coins
    /// </summary>
    public static bool HasEnoughCoins(int amount)
    {
        return GetTotalCoins() >= amount;
    }

    /// <summary>
    /// Get or initialize registration date (first launch date)
    /// </summary>
    public static System.DateTime GetRegistrationDate()
    {
        const string REGISTRATION_DATE_KEY = "RegistrationDate";
        string dateString = PlayerPrefs.GetString(REGISTRATION_DATE_KEY, "");
        
        if (string.IsNullOrEmpty(dateString))
        {
            // First launch - save current date
            System.DateTime now = System.DateTime.Now;
            dateString = now.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(REGISTRATION_DATE_KEY, dateString);
            PlayerPrefs.Save();
            return now;
        }
        
        // Parse saved date
        if (System.DateTime.TryParse(dateString, out System.DateTime regDate))
        {
            return regDate;
        }
        
        // Fallback to current date if parsing fails
        System.DateTime fallback = System.DateTime.Now;
        PlayerPrefs.SetString(REGISTRATION_DATE_KEY, fallback.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
        return fallback;
    }

    /// <summary>
    /// Get days since registration
    /// </summary>
    public static int GetDaysSinceRegistration()
    {
        System.DateTime regDate = GetRegistrationDate();
        System.TimeSpan difference = System.DateTime.Now - regDate;
        return Mathf.Max(0, difference.Days);
    }
}

/// <summary>
/// Static helper class for easy access to SaveData operations
/// Use this instead of directly accessing ProgressSaveManager
/// </summary>
public static class SaveDataHelper
{
    // Shortcuts to extension methods for even easier access
    public static SaveData Data => SaveDataExtensions.GetSaveData();
    public static int CurrentLevel => SaveDataExtensions.GetCurrentLevel();
    public static int CurrentLevelNumber => SaveDataExtensions.GetCurrentLevelNumber();
    public static int TotalCoins => SaveDataExtensions.GetTotalCoins();
    public static int TotalEnergySpheres => SaveDataExtensions.GetTotalEnergySpheres();
    public static int BaseLevel => SaveDataExtensions.GetBaseLevel();
    public static int BaseStageProgress => SaveDataExtensions.GetBaseStageProgress();
    public static bool AdsEnabled => SaveDataExtensions.IsAdEnabled();
    
    public static void Save() => SaveDataExtensions.Save();
    public static void AddCoins(int amount) => SaveDataExtensions.AddCoins(amount);
    public static void AddEnergySpheres(int amount) => SaveDataExtensions.AddEnergySpheres(amount);
    public static bool RemoveCoins(int amount) => SaveDataExtensions.RemoveCoins(amount);
    public static bool RemoveEnergySpheres(int amount) => SaveDataExtensions.RemoveEnergySpheres(amount);
    public static void CompleteLevel() => SaveDataExtensions.CompleteCurrentLevel();
    public static bool HasCoins(int amount) => SaveDataExtensions.HasEnoughCoins(amount);
    public static void SetBaseLevel(int level) => SaveDataExtensions.SetBaseLevel(level);
    public static void SetBaseStageProgress(int progress) => SaveDataExtensions.SetBaseStageProgress(progress);
}

