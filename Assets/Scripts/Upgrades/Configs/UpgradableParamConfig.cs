using UnityEngine;

/// <summary>
/// Enum to identify parameter types
/// </summary>
public enum UpgradableParamType
{
    ConnectionSpeed,
    MonsterDamage
}

/// <summary>
/// Main configuration container for an upgradable parameter
/// References price and power configs, and provides display information
/// </summary>
[CreateAssetMenu(fileName = "UpgradableParamConfig", menuName = "Upgrades/Param Config")]
public class UpgradableParamConfig : ScriptableObject
{
    [Header("Config References")]
    [Tooltip("Price configuration for this parameter")]
    [SerializeField] private UpgradePriceConfig priceConfig;
    
    [Tooltip("Power configuration for this parameter")]
    [SerializeField] private UpgradePowerConfig powerConfig;
    
    [Header("Display Settings")]
    [Tooltip("Display name for UI")]
    [SerializeField] private string paramName = "Upgrade";
    
    [Tooltip("Parameter type identifier")]
    [SerializeField] private UpgradableParamType paramType;
    
    public UpgradePriceConfig PriceConfig => priceConfig;
    public UpgradePowerConfig PowerConfig => powerConfig;
    public string ParamName => paramName;
    public UpgradableParamType ParamType => paramType;
}
