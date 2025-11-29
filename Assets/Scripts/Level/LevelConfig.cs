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
    
    [Header("Energy System")]
    [SerializeField] private int startingEnergy = 5;
    
    [Header("Connection Animation")]
    [Tooltip("Prefab for objects that animate along connection lines (spawns at node A, moves to node B)")]
    [SerializeField] private GameObject connectionAnimationPrefab;
    
    public GameObject LevelPrefab => levelPrefab;
    public int CoinReward => coinReward;
    public string LevelName => levelName;
    public int StartingEnergy => startingEnergy;
    public GameObject ConnectionAnimationPrefab => connectionAnimationPrefab;
}

