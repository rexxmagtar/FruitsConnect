# Input Controller Module

Cross-platform mobile and desktop input management using Unity's new Input System.

## Features

- **Touch & Mouse Input** - Unified interface for touch and mouse
- **Pinch-to-Zoom** - Two-finger pinch gesture support
- **Drag Detection** - Configurable drag sensitivity
- **Touch Zones** - Restrict input to specific screen areas
- **Event-Driven** - Clean event-based architecture
- **Static Access** - Convenient static methods for quick access

## Installation

This module is already included in your project under `Assets/Modules/InputController`.

## Dependencies

- Unity Input System package

## Usage

```csharp
using InputController;

// Check if user is touching/clicking
if (MobileInputController.IsAnyInputActiveStatic)
{
    // Handle input
}

// Subscribe to events
MobileInputController.Instance.OnTouchBegan += () => {
    Debug.Log("Touch began!");
};

MobileInputController.Instance.OnTouchEnded += () => {
    Debug.Log("Touch ended!");
};

MobileInputController.Instance.OnDragDelta += (delta) => {
    Debug.Log($"Drag delta: {delta}");
};

MobileInputController.Instance.OnZoomChanged += (zoomDelta) => {
    Debug.Log($"Zoom: {zoomDelta}");
};

// Configure touch zones
MobileInputController.Instance.SetTouchRange(minY: 0.1f, maxY: 0.8f);

// Configure sensitivity
MobileInputController.Instance.SetZoomSensitivity(touchSens: 0.5f, mouseSens: 1f);
```

## Version

1.0.0

