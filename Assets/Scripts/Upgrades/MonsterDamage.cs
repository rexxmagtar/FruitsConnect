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
    /// Formula: baseDamage * (1 + PowerValue / 3000) * (1 + PerkBonus / 100)
    /// </summary>
    public override float CalculatePhysicalValue()
    {
        int totalPower = powerValue;
        if (HitParticlesManager.Instance != null && HitParticlesManager.Instance.GetCurrentParticle() != null)
        {
            totalPower += HitParticlesManager.Instance.GetCurrentParticle().damagePowerValue;
        }
        float multiplier = 1f + ((float)totalPower / 3000f);

        // Apply perk bonus
        if (PerksManager.Instance != null)
        {
            float bonusPercent = PerksManager.Instance.GetTotalBonus(PerkType.Damage);
            multiplier *= (1f + bonusPercent / 100f);
        }

        return BASE_DAMAGE * multiplier;
    }
    
    /// <summary>
    /// Get current damage multiplier (for use in Monster/Boss classes)
    /// </summary>
    public float GetDamageMultiplier()
    {
        int totalPower = powerValue;
        if (HitParticlesManager.Instance != null && HitParticlesManager.Instance.GetCurrentParticle() != null)
        {
            totalPower += HitParticlesManager.Instance.GetCurrentParticle().damagePowerValue;
        }
        float multiplier = 1f + ((float)totalPower / 3000f);

        // Apply perk bonus
        if (PerksManager.Instance != null)
        {
            float bonusPercent = PerksManager.Instance.GetTotalBonus(PerkType.Damage);
            multiplier *= (1f + bonusPercent / 100f);
        }

        return multiplier;
    }
}
