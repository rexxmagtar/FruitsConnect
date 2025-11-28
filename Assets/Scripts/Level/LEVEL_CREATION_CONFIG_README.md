# Level Creation Config Integration

## Overview
The `LevelCreationConfig` ScriptableObject has been integrated into the Level Editor Tool to manage prefabs and templates for level creation.

## What Was Added

### 1. LevelCreationConfig.cs
A new ScriptableObject that stores:
- **Node Prefabs**: Producer, Consumer, and Neutral node prefabs
- **Level Template**: Base template to use when creating new levels
- **Connection Prefab**: Visual representation for connections
- **Grid Settings**: Default spacing, columns, and rows for auto-layout
- **Helper Methods**: `GetNodePrefabByType()` and `IsValid()`

### 2. NodeType Enum
A public enum for identifying node types:
- `Producer` - Starting points (red filled spheres)
- `Consumer` - Endpoints (blue outline spheres)  
- `Neutral` - Pass-through nodes (red outline spheres)

## How to Use

### Setting Up the Config

1. **Create the Config Asset**
   - Go to `Tools > Fruit Connect Level Editor`
   - Click "Find or Create LevelCreationConfig"
   - Or manually: Right-click in Project > `Create > Fruit Connect > Level Creation Config`

2. **Assign Prefabs**
   - Select the LevelCreationConfig asset
   - In the Inspector, assign:
     - Producer Node Prefab
     - Consumer Node Prefab
     - Neutral Node Prefab
     - (Optional) Level Template - base GameObject with common level elements
     - (Optional) Connection Prefab - for visual connections

3. **Configure Grid Settings** (Optional)
   - Default Node Spacing (default: 2.0)
   - Default Grid Columns (default: 5)
   - Default Grid Rows (default: 5)

### Using in Level Editor

1. Open the Level Editor: `Tools > Fruit Connect Level Editor`
2. The config will be auto-loaded if found, or you can assign it manually
3. Click "New Level":
   - If a Level Template is assigned, it will be used as the base
   - Otherwise, an empty level is created
4. Click "Add Node to Scene":
   - If prefabs are assigned, nodes will be instantiated from prefabs
   - Otherwise, primitive spheres will be created as fallback

## Benefits

✅ **Consistent Visuals**: All nodes use the same prefabs with proper materials and settings
✅ **Reusability**: Define prefabs once, use everywhere
✅ **Templates**: Start new levels from a common base (lighting, cameras, etc.)
✅ **Validation**: Config validates that all required prefabs are assigned
✅ **Fallback**: Still works without config (creates primitive spheres)

## Integration Details

### LevelEditorWindow Changes
- Added `levelCreationConfig` field
- New `DrawConfigSection()` - UI for loading/creating config
- Updated `CreateNewLevel()` - Uses level template if available
- Updated `AddNode()` - Instantiates from prefabs if available, falls back to primitives
- New `FindOrCreateLevelCreationConfig()` - Helper to find or create config asset

### Backwards Compatibility
The tool still works without a config - it will create nodes from primitive spheres as before. The config is optional but recommended for production use.

## File Locations

- **Config Script**: `Assets/Scripts/Level/LevelCreationConfig.cs`
- **Editor Tool**: `Assets/Scripts/Editor/LevelEditorWindow.cs`
- **Config Asset**: Create in `Assets/ScriptableObjects/` (recommended)
- **Node Scripts**: `Assets/Scripts/Nodes/` (BaseNode, ProducerNode, ConsumerNode, NeutralNode)

