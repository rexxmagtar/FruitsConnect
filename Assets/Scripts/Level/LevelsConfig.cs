using UnityEngine;

[CreateAssetMenu(fileName = "LevelsConfig", menuName = "Fruit Connect/Levels Config")]
public class LevelsConfig : ScriptableObject
{
    [Header("All Levels")]
    [SerializeField] private LevelConfig[] levels;
    
    /// <summary>
    /// Get level config by index
    /// </summary>
    public LevelConfig GetLevelConfig(int index)
    {
        if (index < 0 || index >= levels.Length)
        {
            Debug.LogError($"Level index {index} out of range. Total levels: {levels.Length}");
            return null;
        }
        
        return levels[index];
    }
    
    /// <summary>
    /// Get total number of levels
    /// </summary>
    public int GetTotalLevels()
    {
        return levels != null ? levels.Length : 0;
    }
}

