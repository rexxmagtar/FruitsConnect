using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Data structure for a single fruit progress configuration
/// </summary>
[System.Serializable]
public class ProgressFruitData
{
    [Tooltip("Total number of levels needed to reach 100% for this fruit (typically 10)")]
    [SerializeField] public int totalLevelsCount = 10;
    
    [Tooltip("Reference to the fruit sprite to display")]
    [SerializeField] public Sprite fruitSprite;
}

/// <summary>
/// Configuration ScriptableObject for fruit progress indicators
/// Defines which fruit sprites to show at different level ranges
/// </summary>
[CreateAssetMenu(fileName = "FruitsProgressIndicatorConfig", menuName = "Fruit Connect/Fruits Progress Indicator Config")]
public class FruitsProgressIndicatorConfig : ScriptableObject
{
    [Header("Fruit Progress Data")]
    [Tooltip("Ordered list of fruit progress configurations. Each fruit represents a 10-level block.")]
    [SerializeField] private List<ProgressFruitData> fruitProgressData = new List<ProgressFruitData>();
    
    /// <summary>
    /// Get the active fruit data based on current level (1-indexed)
    /// </summary>
    /// <param name="currentLevel">Current level number (1-indexed for display)</param>
    /// <returns>ProgressFruitData for the active fruit, or null if invalid</returns>
    public ProgressFruitData GetActiveFruitData(int currentLevel)
    {
        if (fruitProgressData == null || fruitProgressData.Count == 0)
        {
            Debug.LogWarning("FruitsProgressIndicatorConfig: No fruit progress data configured!");
            return null;
        }
        
        if (currentLevel < 1)
        {
            Debug.LogWarning($"FruitsProgressIndicatorConfig: Invalid level {currentLevel}. Must be >= 1");
            return null;
        }
        
        // Calculate which fruit block this level belongs to (0-indexed)
        // Level 1-10 = fruit 0, Level 11-20 = fruit 1, etc.
        int fruitIndex = (currentLevel - 1) / 10;
        
        // Clamp to available fruits (use last fruit if level exceeds available fruits)
        if (fruitIndex >= fruitProgressData.Count)
        {
            fruitIndex = fruitProgressData.Count - 1;
        }
        
        return fruitProgressData[fruitIndex];
    }
    
    /// <summary>
    /// Calculate progress percentage for the current level within its fruit block
    /// </summary>
    /// <param name="currentLevel">Current level number (1-indexed for display)</param>
    /// <returns>Progress percentage (0-100), or -1 if invalid</returns>
    public float GetProgressPercentage(int currentLevel)
    {
        ProgressFruitData fruitData = GetActiveFruitData(currentLevel);
        if (fruitData == null)
        {
            return -1f;
        }
        
        // Calculate which level within the current fruit block (1-10)
        // Level 1 = 1st in block, Level 11 = 1st in block, Level 12 = 2nd in block, etc.
        int levelsCompletedInBlock = ((currentLevel - 1) % 10) + 1;
        
        // Calculate percentage
        float percentage = (levelsCompletedInBlock / (float)fruitData.totalLevelsCount) * 100f;
        
        // Clamp to 0-100
        return Mathf.Clamp(percentage, 0f, 100f);
    }
    
    /// <summary>
    /// Get the number of levels completed in the current fruit block
    /// </summary>
    /// <param name="currentLevel">Current level number (1-indexed for display)</param>
    /// <returns>Number of levels completed in current block, or -1 if invalid</returns>
    public int GetLevelsCompletedInBlock(int currentLevel)
    {
        if (currentLevel < 1)
        {
            return -1;
        }
        
        // Calculate which level within the current fruit block (1-10)
        return ((currentLevel - 1) % 10) + 1;
    }
    
    /// <summary>
    /// Get total levels count for the active fruit
    /// </summary>
    /// <param name="currentLevel">Current level number (1-indexed for display)</param>
    /// <returns>Total levels count, or -1 if invalid</returns>
    public int GetTotalLevelsForActiveFruit(int currentLevel)
    {
        ProgressFruitData fruitData = GetActiveFruitData(currentLevel);
        if (fruitData == null)
        {
            return -1;
        }
        
        return fruitData.totalLevelsCount;
    }
}
