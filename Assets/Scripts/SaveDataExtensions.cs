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
    /// Set total coins
    /// </summary>
    public static void SetTotalCoins(int coins)
    {
        var saveData = GetSaveData();
        saveData.TotalCoins = coins;
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
    public static bool AdsEnabled => SaveDataExtensions.IsAdEnabled();
    
    public static void Save() => SaveDataExtensions.Save();
    public static void AddCoins(int amount) => SaveDataExtensions.AddCoins(amount);
    public static bool RemoveCoins(int amount) => SaveDataExtensions.RemoveCoins(amount);
    public static void CompleteLevel() => SaveDataExtensions.CompleteCurrentLevel();
    public static bool HasCoins(int amount) => SaveDataExtensions.HasEnoughCoins(amount);
}

