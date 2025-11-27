# Compliance Service Module

GDPR and COPPA compliance management for mobile games.

## Features

- **GDPR Compliance** - European data protection compliance
- **COPPA Compliance** - US Children's Online Privacy Protection Act
- **Automatic Region Detection** - Timezone-based region detection
- **Consent Dialogs** - User-friendly UI with audio feedback
- **Age Verification** - Date-based age verification system
- **IronSource Integration** - Automatic SDK metadata updates

## Installation

This module is already included in your project under `Assets/Modules/ComplianceService`.

## Dependencies

- IronSource SDK (for metadata updates)

## Usage

### GDPR Manager

```csharp
using ComplianceService;

// Check and show GDPR consent if needed
bool gdprConsent = await GDPRManager.Instance.CheckAndShowGDPRConsentAsync();

// Check if user is in Europe
bool isEuropean = GDPRManager.Instance.IsUserInEurope();

// Check if consent has been shown
bool shown = GDPRManager.Instance.HasConsentBeenShown();

// Get consent status
bool consented = GDPRManager.Instance.HasUserConsented();
```

### Child Age Manager

```csharp
using ComplianceService;

// Check and show age verification if needed
bool isChild = await ChildAgeManager.Instance.CheckAndShowChildAgeDialogAsync();

// Check if user is in USA
bool isUSA = ChildAgeManager.Instance.IsUserInUSA();

// Check if age has been verified
bool verified = ChildAgeManager.Instance.HasChildAgeBeenShown();

// Get child status
bool isChildUser = ChildAgeManager.Instance.IsChild();
```

### Integration with Ads

```csharp
// Initialize compliance before ads
if (GDPRManager.Instance.IsUserInEurope())
{
    bool gdprConsent = await GDPRManager.Instance.CheckAndShowGDPRConsentAsync();
    IronSource.Agent.setConsent(gdprConsent);
}

if (ChildAgeManager.Instance.IsUserInUSA())
{
    bool isChild = await ChildAgeManager.Instance.CheckAndShowChildAgeDialogAsync();
    IronSource.Agent.setMetaData("is_child_directed", isChild ? "true" : "false");
}
```

## Version

1.0.0

