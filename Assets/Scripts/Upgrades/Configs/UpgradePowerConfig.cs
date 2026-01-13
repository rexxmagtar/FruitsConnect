using UnityEngine;

/// <summary>
/// Configuration for upgrade power value calculation
/// Uses AnimationCurve to determine power multiplier based on level
/// </summary>
[CreateAssetMenu(fileName = "UpgradePowerConfig", menuName = "Upgrades/Power Config")]
public class UpgradePowerConfig : ScriptableObject
{
    [Header("Power Settings")]
    [Tooltip("Base power increase per step")]
    [SerializeField] private float basePowerIncrease = 10f;
    
    [Tooltip("Power multiplier curve based on current level. X-axis = level, Y-axis = multiplier")]
    [SerializeField] private AnimationCurve powerMultiplierCurve = AnimationCurve.Linear(0f, 1f, 10f, 2f);
    
    [Tooltip("Multiplier applied when completing all 4 steps (leveling up)")]
    [SerializeField] private float fullLevelIncrementMultiplier = 1.5f;
    
    public float BasePowerIncrease => basePowerIncrease;
    public AnimationCurve PowerMultiplierCurve => powerMultiplierCurve;
    public float FullLevelIncrementMultiplier => fullLevelIncrementMultiplier;
    
    /// <summary>
    /// Calculate power increment for purchasing next step at given level
    /// Returns int (rounded) to ensure power is always an integer
    /// </summary>
    public int GetPowerIncrement(int level, bool isLevelCompletion = false)
    {
        float multiplier = powerMultiplierCurve.Evaluate(level);
        float increment = basePowerIncrease * multiplier;
        
        if (isLevelCompletion)
        {
            increment *= fullLevelIncrementMultiplier;
        }
        
        // Round to nearest int to ensure power is always an integer
        return Mathf.RoundToInt(increment);
    }
}
