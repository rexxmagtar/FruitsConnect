using UnityEngine;
using DataRepository;
using System;

/// <summary>
/// Singleton controller that manages all player upgrade progress
/// Handles save/load, purchases, and provides access to upgrade values
/// </summary>
public class PlayerProgressController : MonoBehaviour
{
    [Header("Config References")]
    [Tooltip("Configuration for Connection Speed upgrade")]
    [SerializeField] private UpgradableParamConfig connectionSpeedConfig;
    
    [Tooltip("Configuration for Monster Damage upgrade")]
    [SerializeField] private UpgradableParamConfig monsterDamageConfig;
    
    // Singleton
    private static PlayerProgressController _instance;
    public static PlayerProgressController Instance => _instance;
    
    // Upgrade parameters
    private ConnectionSpeed connectionSpeed;
    private MonsterDamage monsterDamage;
    
    // Events
    public static event Action<UpgradableParam, bool> OnUpgradePurchased; // bool = isLevelCompletion
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    
    /// <summary>
    /// Initialize upgrade system with configs and load saved data
    /// </summary>
    public void Initialize()
    {
        if (connectionSpeedConfig == null || monsterDamageConfig == null)
        {
            Debug.LogError("PlayerProgressController: Configs not assigned!");
            return;
        }
        
        var manager = ProgressSaveManager<SaveData>.Instance;
        if (manager == null)
        {
            Debug.LogError("PlayerProgressController: ProgressSaveManager not found!");
            return;
        }
        
        // Initialize Connection Speed
        connectionSpeed = new ConnectionSpeed();
        int csLevel = manager.GetConnectionSpeedLevel();
        int csSteps = manager.GetConnectionSpeedSteps();
        int csPower = manager.GetConnectionSpeedPowerValue();
        connectionSpeed.Initialize(connectionSpeedConfig, csLevel, csSteps, csPower);
        
        // Initialize Monster Damage
        monsterDamage = new MonsterDamage();
        int mdLevel = manager.GetMonsterDamageLevel();
        int mdSteps = manager.GetMonsterDamageSteps();
        int mdPower = manager.GetMonsterDamagePowerValue();
        monsterDamage.Initialize(monsterDamageConfig, mdLevel, mdSteps, mdPower);
        
        Debug.Log($"PlayerProgressController initialized - CS: L{csLevel}S{csSteps}P{csPower}, MD: L{mdLevel}S{mdSteps}P{mdPower}");
    }
    
    /// <summary>
    /// Get current connection speed multiplier
    /// </summary>
    public float GetConnectionSpeed()
    {
        if (connectionSpeed == null)
        {
            Debug.LogWarning("PlayerProgressController: ConnectionSpeed not initialized!");
            return 1f; // Default multiplier
        }
        
        return connectionSpeed.GetSpeedMultiplier();
    }
    
    /// <summary>
    /// Get current monster damage multiplier
    /// </summary>
    public float GetMonsterDamage()
    {
        if (monsterDamage == null)
        {
            Debug.LogWarning("PlayerProgressController: MonsterDamage not initialized!");
            return 1f; // Default multiplier
        }
        
        return monsterDamage.GetDamageMultiplier();
    }
    
    /// <summary>
    /// Get ConnectionSpeed parameter instance
    /// </summary>
    public ConnectionSpeed GetConnectionSpeedParam()
    {
        return connectionSpeed;
    }
    
    /// <summary>
    /// Get MonsterDamage parameter instance
    /// </summary>
    public MonsterDamage GetMonsterDamageParam()
    {
        return monsterDamage;
    }
    
    /// <summary>
    /// Attempt to purchase an upgrade
    /// </summary>
    public bool PurchaseUpgrade(UpgradableParam param)
    {
        if (param == null)
        {
            Debug.LogError("PlayerProgressController: Cannot purchase null parameter!");
            return false;
        }
        
        var manager = ProgressSaveManager<SaveData>.Instance;
        if (manager == null)
        {
            Debug.LogError("PlayerProgressController: ProgressSaveManager not found!");
            return false;
        }
        
        // Check if can afford
        if (!param.CanPurchaseUpgrade())
        {
            Debug.Log($"PlayerProgressController: Not enough coins for upgrade. Price: {param.GetUpgradePrice()}, Have: {manager.GetCoins()}");
            return false;
        }
        
        // Get price before purchase
        int price = param.GetUpgradePrice();
        
        // Check if this will be a level completion (step 3 -> 4)
        bool isLevelCompletion = (param.CurrentLevelStep == 3);
        
        // Perform upgrade (calculates and stores new power value)
        if (!param.PurchaseUpgrade())
        {
            Debug.LogError("PlayerProgressController: PurchaseUpgrade failed!");
            return false;
        }
        
        // Deduct coins
        if (!manager.RemoveCoins(price))
        {
            Debug.LogError("PlayerProgressController: Failed to remove coins after purchase!");
            return false;
        }
        
        // Save updated values
        SaveProgress(param);
        
        // Fire event with level completion flag
        OnUpgradePurchased?.Invoke(param, isLevelCompletion);
        
        Debug.Log($"PlayerProgressController: Upgrade purchased! New PowerValue: {param.PowerValue}, Level: {param.Level}, Steps: {param.CurrentLevelStep}, LevelCompletion: {isLevelCompletion}");
        
        return true;
    }
    
    /// <summary>
    /// Save upgrade progress to save manager
    /// </summary>
    private void SaveProgress(UpgradableParam param)
    {
        var manager = ProgressSaveManager<SaveData>.Instance;
        if (manager == null) return;
        
        if (param is ConnectionSpeed)
        {
            manager.SetConnectionSpeedLevel(param.Level);
            manager.SetConnectionSpeedSteps(param.CurrentLevelStep);
            manager.SetConnectionSpeedPowerValue(param.PowerValue);
        }
        else if (param is MonsterDamage)
        {
            manager.SetMonsterDamageLevel(param.Level);
            manager.SetMonsterDamageSteps(param.CurrentLevelStep);
            manager.SetMonsterDamagePowerValue(param.PowerValue);
        }
    }
    
    /// <summary>
    /// Load progress from save manager (called on initialization)
    /// </summary>
    private void LoadProgress()
    {
        // Loading is handled in Initialize() method
        // This method exists for potential future use
    }
}
