using UnityEngine;

/// <summary>
/// Configuration for upgrade price calculation
/// Uses AnimationCurve to determine price multiplier based on level
/// </summary>
[CreateAssetMenu(fileName = "UpgradePriceConfig", menuName = "Upgrades/Price Config")]
public class UpgradePriceConfig : ScriptableObject
{
    [Header("Price Settings")]
    [Tooltip("Base price for a single step purchase")]
    [SerializeField] private int basePrice = 100;
    
    [Tooltip("Price multiplier curve based on current level. X-axis = level, Y-axis = multiplier")]
    [SerializeField] private AnimationCurve priceMultiplierCurve = AnimationCurve.Linear(0f, 1f, 10f, 2f);
    
    public int BasePrice => basePrice;
    public AnimationCurve PriceMultiplierCurve => priceMultiplierCurve;
    
    /// <summary>
    /// Calculate price for purchasing next step at given level
    /// </summary>
    public int GetPrice(int level)
    {
        float multiplier = priceMultiplierCurve.Evaluate(level);
        return Mathf.RoundToInt(basePrice * multiplier);
    }
}
