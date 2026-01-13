using DataRepository;

/// <summary>
/// Extension methods for ProgressSaveManager<SaveData> to add convenience methods
/// This allows calling methods like GetCoins() directly on the manager instance
/// </summary>
public static class ProgressSaveManagerExtensions
{
    /// <summary>
    /// Get total coins
    /// </summary>
    public static int GetCoins(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().TotalCoins;
    }
    
    /// <summary>
    /// Set total coins
    /// </summary>
    public static void SetCoins(this ProgressSaveManager<SaveData> manager, int coins)
    {
        var data = manager.GetGameData();
        data.TotalCoins = coins;
        manager.SaveGameData();
    }
    
    /// <summary>
    /// Add coins
    /// </summary>
    public static void AddCoins(this ProgressSaveManager<SaveData> manager, int amount)
    {
        if (amount <= 0) return;
        
        var data = manager.GetGameData();
        data.TotalCoins += amount;
        manager.SaveGameData();
    }
    
    /// <summary>
    /// Remove coins (returns false if not enough coins)
    /// </summary>
    public static bool RemoveCoins(this ProgressSaveManager<SaveData> manager, int amount)
    {
        if (amount <= 0) return false;
        
        var data = manager.GetGameData();
        
        if (data.TotalCoins < amount)
            return false;
        
        data.TotalCoins -= amount;
        manager.SaveGameData();
        return true;
    }
    
    /// <summary>
    /// Get current level (0-based)
    /// </summary>
    public static int GetCurrentLevel(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().CurrentLevel;
    }
    
    /// <summary>
    /// Set current level
    /// </summary>
    public static void SetCurrentLevel(this ProgressSaveManager<SaveData> manager, int level)
    {
        var data = manager.GetGameData();
        data.CurrentLevel = level;
        manager.SaveGameData();
    }
    
    /// <summary>
    /// Get current level number for display (1-based)
    /// </summary>
    public static int GetCurrentLevelNumber(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().CurrentLevel + 1;
    }
    
    /// <summary>
    /// Complete current level and advance to next
    /// </summary>
    public static void CompleteLevel(this ProgressSaveManager<SaveData> manager)
    {
        var data = manager.GetGameData();
        data.CurrentLevel++;
        manager.SaveGameData();
    }
    
    /// <summary>
    /// Check if player has enough coins
    /// </summary>
    public static bool HasEnoughCoins(this ProgressSaveManager<SaveData> manager, int amount)
    {
        return manager.GetGameData().TotalCoins >= amount;
    }
    
    /// <summary>
    /// Get whether ads are enabled
    /// </summary>
    public static bool IsAdEnabled(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().IsAdEnabled;
    }
    
    /// <summary>
    /// Set ad enabled status
    /// </summary>
    public static void SetAdEnabled(this ProgressSaveManager<SaveData> manager, bool enabled)
    {
        var data = manager.GetGameData();
        data.IsAdEnabled = enabled;
        manager.SaveGameData();
    }
    
    // Connection Speed Upgrade Methods
    public static int GetConnectionSpeedLevel(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().ConnectionSpeedLevel;
    }
    
    public static void SetConnectionSpeedLevel(this ProgressSaveManager<SaveData> manager, int level)
    {
        var data = manager.GetGameData();
        data.ConnectionSpeedLevel = level;
        manager.SaveGameData();
    }
    
    public static int GetConnectionSpeedSteps(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().ConnectionSpeedSteps;
    }
    
    public static void SetConnectionSpeedSteps(this ProgressSaveManager<SaveData> manager, int steps)
    {
        var data = manager.GetGameData();
        data.ConnectionSpeedSteps = steps;
        manager.SaveGameData();
    }
    
    public static int GetConnectionSpeedPowerValue(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().ConnectionSpeedPowerValue;
    }
    
    public static void SetConnectionSpeedPowerValue(this ProgressSaveManager<SaveData> manager, int powerValue)
    {
        var data = manager.GetGameData();
        data.ConnectionSpeedPowerValue = powerValue;
        manager.SaveGameData();
    }
    
    // Monster Damage Upgrade Methods
    public static int GetMonsterDamageLevel(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().MonsterDamageLevel;
    }
    
    public static void SetMonsterDamageLevel(this ProgressSaveManager<SaveData> manager, int level)
    {
        var data = manager.GetGameData();
        data.MonsterDamageLevel = level;
        manager.SaveGameData();
    }
    
    public static int GetMonsterDamageSteps(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().MonsterDamageSteps;
    }
    
    public static void SetMonsterDamageSteps(this ProgressSaveManager<SaveData> manager, int steps)
    {
        var data = manager.GetGameData();
        data.MonsterDamageSteps = steps;
        manager.SaveGameData();
    }
    
    public static int GetMonsterDamagePowerValue(this ProgressSaveManager<SaveData> manager)
    {
        return manager.GetGameData().MonsterDamagePowerValue;
    }
    
    public static void SetMonsterDamagePowerValue(this ProgressSaveManager<SaveData> manager, int powerValue)
    {
        var data = manager.GetGameData();
        data.MonsterDamagePowerValue = powerValue;
        manager.SaveGameData();
    }
}

