# Haptic Controller Module

Cross-platform haptic feedback and vibration system.

## Features

- **Cross-Platform Support** - Android, iOS, and Desktop (gamepad)
- **Multiple Vibration Types** - Single, continuous, and pattern-based
- **Predefined Effects** - Built-in vibration patterns
- **Settings Integration** - Respects user vibration preferences
- **Platform Detection** - Automatic platform-specific implementation

## Installation

This module is already included in your project under `Assets/Modules/HapticController`.

## Dependencies

- Unity Input System (for gamepad support)
- RDG Vibration (for mobile platforms)

## Usage

```csharp
using HapticController;

// Single vibration
HapticController.Instance.PlaySingleVibration();

// Continuous vibration
HapticController.Instance.StartVibration();
HapticController.Instance.StopVibration();

// Predefined effects (mobile only)
HapticController.Instance.PlayPredefinedVibration(PredefinedEffectType.Click);
HapticController.Instance.PlayPredefinedVibration(PredefinedEffectType.HeavyClick);

// Custom patterns (mobile only)
long[] pattern = {0, 100, 50, 100}; // Off-On-Off-On in milliseconds
HapticController.Instance.PlayVibrationPattern(pattern);

// Check support
if (HapticController.Instance.IsVibrationSupported())
{
    // Use haptics
}

// Check platform
var platform = HapticController.Instance.GetPlatformType();
```

## Version

1.0.0

