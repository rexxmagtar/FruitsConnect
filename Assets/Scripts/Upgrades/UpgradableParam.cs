using UnityEngine;
using DataRepository;

/// <summary>
/// Abstract base class for all upgradable parameters
/// Manages level, steps, and stored power value
/// Power value is calculated once at purchase and stored permanently
/// </summary>
public abstract class UpgradableParam
{
    protected int level;
    protected int currentLevelStep;
    protected int powerValue; // Stored value - calculated once at purchase, never recalculated (always int)
    protected UpgradableParamConfig config;
    
    public int Level => level;
    public int CurrentLevelStep => currentLevelStep;
    public int PowerValue => powerValue; // Returns stored value directly (always int)
    public UpgradableParamConfig Config => config;
    
    /// <summary>
    /// Initialize parameter with config and saved values
    /// </summary>
    public void Initialize(UpgradableParamConfig config, int savedLevel, int savedSteps, int savedPowerValue)
    {
        this.config = config;
        this.level = savedLevel;
        this.currentLevelStep = savedSteps;
        this.powerValue = savedPowerValue; // Use stored value, don't recalculate
    }
    
    /// <summary>
    /// Get current power value (stored, not calculated)
    /// </summary>
    public int GetPowerValue()
    {
        return powerValue;
    }
    
    /// <summary>
    /// Calculate price for next step purchase
    /// </summary>
    public int GetUpgradePrice()
    {
        if (config == null || config.PriceConfig == null)
        {
            Debug.LogError("UpgradableParam: Config or PriceConfig is null!");
            return 0;
        }
        
        return config.PriceConfig.GetPrice(level);
    }
    
    /// <summary>
    /// Check if player can afford next upgrade
    /// </summary>
    public bool CanPurchaseUpgrade()
    {
        var manager = ProgressSaveManager<SaveData>.Instance;
        if (manager == null) return false;
        
        int price = GetUpgradePrice();
        return manager.HasEnoughCoins(price);
    }
    
    /// <summary>
    /// Purchase next upgrade step
    /// Calculates power increment and adds to stored power value
    /// </summary>
    public bool PurchaseUpgrade()
    {
        if (config == null || config.PowerConfig == null)
        {
            Debug.LogError("UpgradableParam: Config or PowerConfig is null!");
            return false;
        }
        
        // Check if this is a level completion (step 3 -> 4)
        bool isLevelCompletion = (currentLevelStep == 3);
        
        // Calculate power increment using config (returns int, rounded)
        int powerIncrement = config.PowerConfig.GetPowerIncrement(level, isLevelCompletion);
        
        // Add increment to stored power value
        powerValue += powerIncrement;
        
        // Update level and steps
        if (isLevelCompletion)
        {
            level++;
            currentLevelStep = 0;
        }
        else
        {
            currentLevelStep++;
        }
        
        return true;
    }
    
    /// <summary>
    /// Get base value for physical calculation
    /// </summary>
    protected abstract float GetBaseValue();
    
    /// <summary>
    /// Convert stored power value to actual game value
    /// </summary>
    public abstract float CalculatePhysicalValue();
}
