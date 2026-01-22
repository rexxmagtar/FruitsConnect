using UnityEngine;

[CreateAssetMenu(fileName = "HitParticlesData", menuName = "FruitsConnect/HitParticlesData")]
public class HitParticlesData : ScriptableObject
{
    public string id;
    public GameObject particlePrefab;
    public int stage;
    public Sprite effectSprite;
    public int price;
}
