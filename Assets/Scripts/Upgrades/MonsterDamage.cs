/// <summary>
/// Upgradable parameter for damage dealt to monsters
/// </summary>
public class MonsterDamage : UpgradableParam
{
    private const float BASE_DAMAGE = 1.0f; // Base damage per hit
    
    protected override float GetBaseValue()
    {
        return BASE_DAMAGE;
    }
    
    /// <summary>
    /// Calculate physical damage value
    /// Formula: baseDamage * (1 + PowerValue / 1000)
    /// </summary>
    public override float CalculatePhysicalValue()
    {
        float multiplier = 1f + ((float)powerValue / 1000f);
        return BASE_DAMAGE * multiplier;
    }
    
    /// <summary>
    /// Get current damage multiplier (for use in Monster/Boss classes)
    /// </summary>
    public float GetDamageMultiplier()
    {
        return 1f + ((float)powerValue / 1000f);
    }
}
