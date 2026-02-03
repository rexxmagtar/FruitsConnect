using System;

public enum PerkType
{
    CoinRewardPercent,
    EnergySphereRewardPercent,
    Damage
}

[Serializable]
public class PerkInfo
{
    public PerkType type;
    public float value;
}
