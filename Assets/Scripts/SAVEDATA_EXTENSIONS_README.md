# SaveData Extension Methods - Usage Guide

## Overview

Two new extension classes have been created to make working with SaveData easier throughout the game:

1. **SaveDataExtensions.cs** - Static helper methods for SaveData operations
2. **ProgressSaveManagerExtensions.cs** - Extension methods for ProgressSaveManager<SaveData>

## Files Created

- `Assets/Scripts/SaveDataExtensions.cs`
- `Assets/Scripts/ProgressSaveManagerExtensions.cs`

## Usage Examples

### Option 1: Using SaveDataHelper (Simplest)

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    private void Example()
    {
        // Get current values
        int coins = SaveDataHelper.TotalCoins;
        int level = SaveDataHelper.CurrentLevel;
        int levelDisplay = SaveDataHelper.CurrentLevelNumber; // 1-based for UI
        
        // Modify data
        SaveDataHelper.AddCoins(10);
        SaveDataHelper.CompleteLevel();
        
        // Check conditions
        if (SaveDataHelper.HasCoins(100))
        {
            SaveDataHelper.RemoveCoins(100);
            // Buy something
        }
    }
}
```

### Option 2: Using SaveDataExtensions

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    private void Example()
    {
        // Get current values
        int coins = SaveDataExtensions.GetTotalCoins();
        int level = SaveDataExtensions.GetCurrentLevel();
        
        // Modify data
        SaveDataExtensions.AddCoins(50);
        SaveDataExtensions.CompleteCurrentLevel();
        SaveDataExtensions.Save(); // Explicit save
    }
}
```

### Option 3: Using ProgressSaveManager Extension Methods

```csharp
using UnityEngine;
using DataRepository;

public class MyScript : MonoBehaviour
{
    private void Example()
    {
        var manager = ProgressSaveManager<SaveData>.Instance;
        
        // These are extension methods - work directly on the manager
        int coins = manager.GetCoins();
        int level = manager.GetCurrentLevel();
        
        manager.AddCoins(25);
        manager.CompleteLevel();
        
        // Check if has enough coins
        if (manager.HasEnoughCoins(100))
        {
            manager.RemoveCoins(100);
        }
    }
}
```

## Available Methods

### Coin Operations

```csharp
// Get
int coins = SaveDataHelper.TotalCoins;
int coins = SaveDataExtensions.GetTotalCoins();
int coins = manager.GetCoins();

// Add
SaveDataHelper.AddCoins(10);
SaveDataExtensions.AddCoins(10);
manager.AddCoins(10);

// Remove (returns false if not enough)
bool success = SaveDataHelper.RemoveCoins(10);
bool success = SaveDataExtensions.RemoveCoins(10);
bool success = manager.RemoveCoins(10);

// Check
bool hasEnough = SaveDataHelper.HasCoins(100);
bool hasEnough = SaveDataExtensions.HasEnoughCoins(100);
bool hasEnough = manager.HasEnoughCoins(100);
```

### Level Operations

```csharp
// Get current level (0-based)
int level = SaveDataHelper.CurrentLevel;
int level = SaveDataExtensions.GetCurrentLevel();
int level = manager.GetCurrentLevel();

// Get level for display (1-based)
int display = SaveDataHelper.CurrentLevelNumber;
int display = SaveDataExtensions.GetCurrentLevelNumber();
int display = manager.GetCurrentLevelNumber();

// Complete level and advance
SaveDataHelper.CompleteLevel();
SaveDataExtensions.CompleteCurrentLevel();
manager.CompleteLevel();

// Set level directly
SaveDataExtensions.SetCurrentLevel(5);
manager.SetCurrentLevel(5);
```

### Other Operations

```csharp
// Ads
bool adsEnabled = SaveDataHelper.AdsEnabled;
SaveDataExtensions.SetAdEnabled(true);
manager.SetAdEnabled(true);

// Manual save
SaveDataExtensions.Save();
manager.SaveGameData(); // Built-in method

// Reset all progress
SaveDataExtensions.ResetProgress();
```

## Integration with Existing Code

### LevelCompleteUI

The `LevelCompleteUI.cs` already uses extension methods:

```csharp
// This now works because of ProgressSaveManagerExtensions
int currentBalance = ProgressSaveManager<SaveData>.Instance.GetCoins();
```

### GameController Integration

The GameController now properly shows LevelCompleteUI when level is won:

```csharp
// In OnLevelComplete():
// 1. Awards coins
// 2. Completes level (increments CurrentLevel)
// 3. Hides GameplayUI
// 4. Shows LevelCompleteUI with coins earned and next level number
```

### Button Handlers

When player clicks buttons in LevelCompleteUI:

- **Continue Button**: Loads next level automatically
- **Return to Menu Button**: Returns to main menu with current level preloaded

## Benefits

✅ **Cleaner Code**: No need to repeatedly type `ProgressSaveManager<SaveData>.Instance.GetGameData()`

✅ **Type Safety**: Extension methods provide compile-time checking

✅ **Consistency**: Same interface across all code

✅ **Backward Compatible**: Existing code using `manager.GetCoins()` continues to work

✅ **Auto-Save**: Methods automatically save after modifications

## Migration from Old Code

### Before:
```csharp
var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
saveData.TotalCoins += 10;
ProgressSaveManager<SaveData>.Instance.SaveGameData();
```

### After:
```csharp
SaveDataHelper.AddCoins(10);
// or
SaveDataExtensions.AddCoins(10);
// or
ProgressSaveManager<SaveData>.Instance.AddCoins(10);
```

## Recommended Usage

For **new code**, use `SaveDataHelper` - it's the simplest:

```csharp
if (SaveDataHelper.HasCoins(50))
{
    SaveDataHelper.RemoveCoins(50);
    UnlockFeature();
}
```

For **existing code** that already uses `ProgressSaveManager<SaveData>.Instance`, the extension methods work automatically:

```csharp
var manager = ProgressSaveManager<SaveData>.Instance;
int coins = manager.GetCoins(); // Extension method - works automatically!
```

## Testing

All methods include proper validation:
- `RemoveCoins()` returns `false` if insufficient coins
- `AddCoins()` ignores negative amounts
- All modifications are automatically saved

## Complete Game Flow

### Level Complete Flow:

1. Player completes level
2. `GameController.CheckWinCondition()` returns `true`
3. `GameController.OnLevelComplete()` is called:
   - Awards coins via `GameManager.AddCoins()`
   - Completes level via `GameManager.CompleteLevel()`
   - Hides `GameplayUI`
   - Shows `LevelCompleteUI` with earned coins
4. Player sees animated coin collection
5. Player clicks Continue or Return to Menu
6. GameController handles button action:
   - **Continue**: Load next level, start game
   - **Menu**: Return to main menu

### Data Flow:

```
GameController.OnLevelComplete()
    ↓
GameManager.AddCoins(amount)
    ↓
SaveDataExtensions.AddCoins(amount)
    ↓
ProgressSaveManager<SaveData>.GetGameData().TotalCoins += amount
    ↓
ProgressSaveManager<SaveData>.SaveGameData()
    ↓
Data saved to disk (encrypted)
```

## Notes

- All save operations are **automatic** - you don't need to manually call `Save()`
- Extension methods are in the global namespace - no special `using` required
- Works with existing `LevelCompleteUI` and `LevelFailedUI` without modifications
- Thread-safe for async operations (managed by ProgressSaveManager)

---

**Created:** November 27, 2025  
**Status:** ✅ Complete and Integrated

