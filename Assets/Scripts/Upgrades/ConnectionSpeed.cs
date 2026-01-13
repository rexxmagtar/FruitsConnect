/// <summary>
/// Upgradable parameter for connection animation speed
/// </summary>
public class ConnectionSpeed : UpgradableParam
{
    private const float BASE_SPEED = 2.0f; // Base animation speed in units per second
    
    protected override float GetBaseValue()
    {
        return BASE_SPEED;
    }
    
    /// <summary>
    /// Calculate physical animation speed multiplier
    /// Formula: baseSpeed * (1 + PowerValue / 1000)
    /// </summary>
    public override float CalculatePhysicalValue()
    {
        float multiplier = 1f + ((float)powerValue / 1000f);
        return BASE_SPEED * multiplier;
    }
    
    /// <summary>
    /// Get current animation speed multiplier (for use in Connection class)
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return 1f + ((float)powerValue / 1000f);
    }
}
