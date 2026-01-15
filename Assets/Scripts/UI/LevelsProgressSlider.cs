using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the levels progress slider UI component
/// Displays player progress through 5-level blocks with animated cells
/// Shows completion status and updates slider value
/// </summary>
public class LevelsProgressSlider : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Array of 5 progress cells (cell 5 should be BossProgressCell)")]
    [SerializeField] private ProgressCell[] progressCells = new ProgressCell[5];
    
    [Tooltip("UI Slider component to show progress")]
    [SerializeField] private Slider progressSlider;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    /// <summary>
    /// Update progress display based on current level
    /// </summary>
    /// <param name="currentLevel">Current level number (1-indexed)</param>
    public void UpdateProgress(int currentLevel)
    {
        if (currentLevel < 1)
        {
            Debug.LogError($"LevelsProgressSlider: Invalid level {currentLevel}. Must be >= 1");
            return;
        }
        
        // Calculate block start and position within block
        int blockStartLevel = GetBlockStartLevel(currentLevel);
        int positionInBlock = GetPositionInBlock(currentLevel);
        
        if (showDebugLogs)
        {
            Debug.Log($"LevelsProgressSlider: Level {currentLevel} -> Block starts at {blockStartLevel}, Position {positionInBlock}/5");
        }
        
        // Update each cell state
        for (int i = 0; i < progressCells.Length; i++)
        {
            if (progressCells[i] == null)
            {
                Debug.LogWarning($"LevelsProgressSlider: Progress cell {i + 1} is not assigned!");
                continue;
            }
            
            // Calculate the level this cell represents
            int cellLevel = blockStartLevel + i;
            
            // Determine cell state
            CellState cellState;
            if (cellLevel < currentLevel)
            {
                cellState = CellState.Complete;
            }
            else if (cellLevel == currentLevel)
            {
                cellState = CellState.Current;
            }
            else
            {
                cellState = CellState.Incomplete;
            }
            
            // Set the state
            progressCells[i].SetState(cellState);
            
            if (showDebugLogs)
            {
                Debug.Log($"  Cell {i + 1} (Level {cellLevel}): {cellState}");
            }
        }
        
        // Update slider value (0.0 to 1.0 based on position in block)
        if (progressSlider != null)
        {
            float sliderValue = positionInBlock / 5f;
            if(positionInBlock == 1){
                sliderValue = 0f;
            }
            progressSlider.value = sliderValue;
            
            if (showDebugLogs)
            {
                Debug.Log($"  Slider value: {sliderValue:F2} ({positionInBlock}/5)");
            }
        }
        else
        {
            Debug.LogWarning("LevelsProgressSlider: Progress Slider not assigned!");
        }
    }
    
    /// <summary>
    /// Get the starting level of the block containing the current level
    /// </summary>
    /// <param name="currentLevel">Current level (1-indexed)</param>
    /// <returns>Starting level of the block (1-indexed)</returns>
    private int GetBlockStartLevel(int currentLevel)
    {
        // Convert to 0-indexed for calculation
        int levelIndex = currentLevel - 1;
        
        // Calculate which block (0-indexed)
        int blockIndex = levelIndex / 5;
        
        // Calculate block start (0-indexed)
        int blockStartIndex = blockIndex * 5;
        
        // Convert back to 1-indexed
        return blockStartIndex + 1;
    }
    
    /// <summary>
    /// Get position within the current 5-level block (1-5)
    /// </summary>
    /// <param name="currentLevel">Current level (1-indexed)</param>
    /// <returns>Position in block (1-5)</returns>
    private int GetPositionInBlock(int currentLevel)
    {
        // Convert to 0-indexed for calculation
        int levelIndex = currentLevel - 1;
        
        // Get position in block (0-4)
        int position = levelIndex % 5;
        
        // Convert to 1-indexed (1-5)
        return position + 1;
    }
    
    /// <summary>
    /// Check if a specific level is a boss level
    /// Uses LevelConfig.IsBossFight property
    /// </summary>
    /// <param name="levelIndex">Level index (0-indexed)</param>
    /// <returns>True if level has boss fight</returns>
    private bool IsBossLevel(int levelIndex)
    {
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager == null)
        {
            Debug.LogWarning("LevelsProgressSlider: LevelsManager not found!");
            return false;
        }
        
        LevelConfig levelConfig = levelsManager.GetLevelConfig(levelIndex);
        if (levelConfig == null)
        {
            // Level doesn't exist yet
            return false;
        }
        
        return levelConfig.IsBossFight;
    }
    
    /// <summary>
    /// Validation in editor
    /// </summary>
    private void OnValidate()
    {
        // Check that exactly 5 cells are assigned
        if (progressCells == null || progressCells.Length != 5)
        {
            Debug.LogError($"LevelsProgressSlider on {gameObject.name}: Must have exactly 5 progress cells assigned!", this);
        }
        else
        {
            // Check that cell 5 (index 4) is a BossProgressCell
            if (progressCells[4] != null && !(progressCells[4] is BossProgressCell))
            {
                Debug.LogWarning($"LevelsProgressSlider on {gameObject.name}: Cell 5 should be a BossProgressCell for proper boss level visualization!", this);
            }
            
            // Check for null cells
            for (int i = 0; i < progressCells.Length; i++)
            {
                if (progressCells[i] == null)
                {
                    Debug.LogWarning($"LevelsProgressSlider on {gameObject.name}: Progress cell {i + 1} is not assigned!", this);
                }
            }
        }
        
        if (progressSlider == null)
        {
            Debug.LogWarning($"LevelsProgressSlider on {gameObject.name}: Progress Slider not assigned!", this);
        }
    }
}
