# Data Repository Module

Encrypted data persistence with cloud sync support via Firebase Firestore.

## Features

- **Local Encrypted Storage** - AES encrypted save files
- **Cloud Sync** - Firebase Firestore integration with conflict resolution
- **Data Integrity** - SHA-256 hashing for data validation
- **Multiple Save Managers** - Game progress and treasure data systems
- **Editor Tools** - Data clear tool for development

## Installation

This module is already included in your project under `Assets/Modules/DataRepository`.

## Dependencies

- Firebase Firestore SDK
- Newtonsoft.Json

## Usage

### Progress Save Manager

```csharp
using DataRepository;

// Initialize
ProgressSaveManager.Instance.Initialize();

// Save/Load data
ProgressSaveManager.Instance.SetCoins(100);
int coins = ProgressSaveManager.Instance.GetCoins();

ProgressSaveManager.Instance.CompleteLevel(5);
int currentLevel = ProgressSaveManager.Instance.GetCurrentLevel();

// Cloud sync
await ProgressSaveManager.Instance.SyncWithCloud();

// Force upload to cloud
await ProgressSaveManager.Instance.SyncWithCloud(forceSync: true);
```

### Treasure Data Save Manager

```csharp
using DataRepository;

// Initialize
TreasureDataSaveManager.Instance.Initialize();

// Track treasure spawns
TreasureDataSaveManager.Instance.AddSpawnedTreasure("treasure_001");
List<string> spawned = TreasureDataSaveManager.Instance.GetSpawnedTreasures();

// Last spawn time
DateTime lastSpawn = TreasureDataSaveManager.Instance.GetLastSpawnTime();
TreasureDataSaveManager.Instance.SetLastSpawnTimeToNow();
```

## Version

1.0.0

