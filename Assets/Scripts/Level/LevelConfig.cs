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
    
    [Header("Boss Fight")]
    [Tooltip("If true, level will trigger a boss fight after completion instead of showing level complete screen")]
    [SerializeField] private bool isBossFight = false;
    
    [Tooltip("Gold reward for defeating the boss (only awarded if boss is killed in time)")]
    [SerializeField] private int bossGoldReward = 50;
    
    [Tooltip("Time limit in seconds to defeat the boss")]
    [SerializeField] private float bossFightTimeLimit = 30f;
    
    [Header("Jigsaw Puzzle Rewards")]
    [Tooltip("List of puzzle piece IDs to reward. Use 'random' for a random missing piece. ID format: puzzleId_pieceIndex (e.g. puzzle1_0)")]
    [SerializeField] private System.Collections.Generic.List<string> puzzlePieceRewards = new System.Collections.Generic.List<string> { "random" };
    
    public GameObject LevelPrefab => levelPrefab;
    public int CoinReward => coinReward;
    public string LevelName => levelName;
    public int StartingEnergy => startingEnergy;
    public GameObject ConnectionAnimationPrefab => connectionAnimationPrefab;
    public bool IsBossFight => isBossFight;
    public int BossGoldReward => bossGoldReward;
    public float BossFightTimeLimit => bossFightTimeLimit;
    public System.Collections.Generic.List<string> PuzzlePieceRewards => puzzlePieceRewards;
}

