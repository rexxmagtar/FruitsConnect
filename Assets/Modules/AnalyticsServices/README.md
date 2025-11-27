# Analytics Services Module

Event-driven analytics system with Firebase Analytics integration.

## Features

- **Event-Driven Architecture** - Centralized event bus
- **Firebase Analytics** - Google Analytics integration
- **Game Events** - Predefined game event tracking
- **Custom Events** - Flexible event logging
- **Error Tracking** - Automatic error event capture

## Installation

This module is already included in your project under `Assets/Modules/AnalyticsServices`.

## Dependencies

- Firebase Analytics SDK

## Usage

```csharp
using AnalyticsServices;

// Track level events
AnalyticsService.Instance.TrackLevelStart("Level_01");
AnalyticsService.Instance.TrackLevelComplete("Level_01");
AnalyticsService.Instance.TrackLevelFailed("Level_01", "TimeUp");

// Track custom events
var parameters = new Dictionary<string, object>
{
    { "score", 1500 },
    { "time", 120 }
};
AnalyticsService.Instance.LogEvent("high_score_achieved", parameters);

// Track errors
AnalyticsService.Instance.LogEvent("gameplay_error", new Dictionary<string, object>
{
    { "error_type", "NullReference" },
    { "error_message", "Object reference not set" }
});
```

## Version

1.0.0

# AnalyticServices
