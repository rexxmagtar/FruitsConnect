# AdsServices Module

This module provides advertisement functionality for the game, including rewarded ads and banner ads.

## Features

- **AdsManager**: Main ads controller managing ad initialization and display
- **Rewarded Ads**: Support for rewarded video ads
- **Banner Ads**: Support for banner advertisements
- **GDPR/COPPA Compliance**: Automatic consent handling

## Ad Types

- **Rewarded Video Ads**: For rewards and revives
- **Banner Ads**: Displayed at top of screen

## Usage

```csharp
// Initialize ads
await AdsServices.AdsManager.Instance.Initialize();

// Show rewarded ad
bool success = await AdsServices.AdsManager.Instance.ShowRewardedAdAsync();

// Show rewarded ad for revive
bool success = await AdsServices.AdsManager.Instance.ShowRewardedAdForRevive();

// Banner ad controls
AdsServices.AdsManager.Instance.ShowBannerAd();
AdsServices.AdsManager.Instance.HideBannerAd();

// Check ad availability
bool isReady = AdsServices.AdsManager.Instance.IsRewardedAdReady();
bool isBannerReady = AdsServices.AdsManager.Instance.IsBannerReady();
```

## Compliance

The module automatically handles:
- GDPR consent for European users
- COPPA compliance for US users
- Ad consent management

## Dependencies

- Unity.Services.LevelPlay
- IronSource SDK (for rewarded ads)
# AdsServices
