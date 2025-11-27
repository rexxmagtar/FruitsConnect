# StoreServices Module

This module provides in-app purchase functionality for the game, supporting multiple store platforms.

## Features

- **StoreManager**: Main store controller that manages different store implementations
- **UnityIAPStoreManager**: Unity IAP implementation for Google Play and other platforms
- **IStoreController**: Interface defining store operations
- **ProductInfo**: Data structure for product information

## Supported Platforms

- Google Play Store (Unity IAP)
- Rustore (planned)
- Default fallback to Unity IAP

## Usage

```csharp
// Initialize store
await StoreServices.StoreManager.Instance.Initialize();

// Purchase a product
bool success = await StoreServices.StoreManager.Instance.PurchaseAsync("product_id");

// Check if product is owned
bool isOwned = await StoreServices.StoreManager.Instance.IsProductPurchasedAsync("product_id");

// Get product information
var product = await StoreServices.StoreManager.Instance.GetProductInfoAsync("product_id");
```

## Events

- `OnInitializedEvent`: Fired when store initialization completes
- `OnPurchaseCompleteEvent`: Fired when a purchase succeeds
- `OnPurchaseFailedEvent`: Fired when a purchase fails

## Dependencies

- Unity.Purchasing
- Unity.Services.Core
