using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Fruit Connect/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Level References")]
    [SerializeField] private GameObject levelPrefab;
    
    [Header("Rewards")]
    [SerializeField] private int coinReward = 10;
    
    [Header("Info")]
    [SerializeField] private string levelName;
    
    public GameObject LevelPrefab => levelPrefab;
    public int CoinReward => coinReward;
    public string LevelName => levelName;
}

