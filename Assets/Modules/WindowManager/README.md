# Window Manager Module

Generic UI framework with screen management, transitions, and animated components.

## Features

- **Generic Screen Management** - Reusable window management without project dependencies
- **Smooth Transitions** - Fade and custom transitions
- **Animated Buttons** - DOTween-powered button animations
- **Base UI Classes** - Reusable UI component base classes
- **Audio Integration** - Built-in sound feedback

## Installation

This module is already included in your project under `Assets/Modules/WindowManager`.

## Dependencies

- DOTween (for animations)

## Usage

### Generic Window Manager

```csharp
using WindowManager;

// Show screen with transition
await WindowManager.Instance.ShowScreen(myScreen, useTransition: true);

// Show screen directly
WindowManager.Instance.ShowScreenDirect(myScreen);

// Hide current screen
WindowManager.Instance.HideCurrentScreen();

// Check if transitioning
if (!WindowManager.Instance.IsTransitioning())
{
    // Safe to show new screen
}
```

### Base UI Classes

```csharp
// Base UI usage
public class MyScreen : BaseUI
{
    public override void Initialize()
    {
        // Setup your UI
    }
    
    public override void Show()
    {
        base.Show();
        // Custom show logic
    }
}

// Animated button
public class MyButton : AnimatedButton
{
    protected override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        // Custom press logic
    }
}
```

### Fade Transitions

```csharp
// Set custom fade transition
WindowManager.Instance.SetFadeTransition(myFadeTransition);

// Configure transition duration
WindowManager.Instance.SetDefaultTransitionDuration(0.5f);
```

## Version

1.0.0

