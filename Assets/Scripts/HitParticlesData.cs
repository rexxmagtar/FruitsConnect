using UnityEngine;

[CreateAssetMenu(fileName = "HitParticlesData", menuName = "FruitsConnect/HitParticlesData")]
public class HitParticlesData : ScriptableObject
{
    public string id;
    public string displayName;
    public GameObject particlePrefab;
    public GameObject helperSpiritPrefab;
    public int stage;
    public Sprite effectSprite;
    public string price;
    public int damagePowerValue;
    public int connectionSpeedValue;
}
