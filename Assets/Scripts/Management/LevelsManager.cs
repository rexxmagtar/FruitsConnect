using UnityEngine;
using DataRepository;

/// <summary>
/// Manages level progression and provides level configs
/// </summary>
public class LevelsManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private LevelsConfig levelsConfig;
    
    // Singleton
    private static LevelsManager _instance;
    public static LevelsManager Instance => _instance;
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Initialize the levels manager
    /// </summary>
    public void Initialize()
    {
        if (levelsConfig == null)
        {
            Debug.LogError("LevelsConfig not assigned in LevelsManager!");
            return;
        }
        
        Debug.Log($"LevelsManager initialized with {GetTotalLevels()} levels");
    }
    
    /// <summary>
    /// Get the current level config based on player progress
    /// </summary>
    public LevelConfig GetCurrentLevelConfig()
    {
        if (levelsConfig == null)
        {
            Debug.LogError("LevelsConfig not assigned!");
            return null;
        }
        
        // Get current level from save data
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        int currentLevelIndex = saveData.CurrentLevel;
        
        return GetLevelConfig(currentLevelIndex);
    }
    
    /// <summary>
    /// Get level config by index
    /// </summary>
    public LevelConfig GetLevelConfig(int index)
    {
        if (levelsConfig == null)
        {
            Debug.LogError("LevelsConfig not assigned!");
            return null;
        }
        
        return levelsConfig.GetLevelConfig(index);
    }
    
    /// <summary>
    /// Complete current level and move to next
    /// </summary>
    public void CompleteLevel()
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        int currentLevel = saveData.CurrentLevel;
        int totalLevels = GetTotalLevels();
        
        // Move to next level if available
        if (currentLevel + 1 < totalLevels)
        {
            saveData.CurrentLevel = currentLevel + 1;
            ProgressSaveManager<SaveData>.Instance.SaveGameData();
            
            Debug.Log($"Level complete! Moving to level {saveData.CurrentLevel + 1}");
        }
        else
        {
            Debug.Log("All levels completed!");
        }
    }
    
    /// <summary>
    /// Get total number of levels
    /// </summary>
    public int GetTotalLevels()
    {
        if (levelsConfig == null)
        {
            return 0;
        }
        
        return levelsConfig.GetTotalLevels();
    }
    
    /// <summary>
    /// Get current level number (1-indexed for display)
    /// </summary>
    public int GetCurrentLevelNumber()
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        return saveData.CurrentLevel + 1; // +1 for display (1-indexed)
    }
    
    /// <summary>
    /// Check if there are more levels available
    /// </summary>
    public bool HasMoreLevels()
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        return saveData.CurrentLevel + 1 < GetTotalLevels();
    }
    
    /// <summary>
    /// Reset progress to first level (for testing)
    /// </summary>
    public void ResetProgress()
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        saveData.CurrentLevel = 0;
        ProgressSaveManager<SaveData>.Instance.SaveGameData();
        
        Debug.Log("Progress reset to level 1");
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

