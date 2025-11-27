# Auth Services Module

Unified authentication service supporting Firebase Auth, Google Play Games Sign-In, and Google Sign-In.

## Features

- **Firebase Authentication** - Anonymous and provider-based auth
- **Google Play Games Sign-In** - Android game services authentication
- **Google Sign-In** - Cross-platform Google authentication
- **Account Linking** - Seamlessly link anonymous accounts to social providers
- **Multi-Provider Support** - Handle multiple authentication providers

## Installation

This module is already included in your project under `Assets/Modules/AuthServices`.

## Dependencies

- Firebase Auth SDK
- Google Play Games Plugin
- Google Sign-In Unity Plugin

## Usage

```csharp
using AuthServices;

// Initialize Firebase Auth
await FirebaseAuthManager.Instance.Auth();

// Sign in with Google Play Games
await FirebaseAuthManager.Instance.SignInWithGooglePlayGames();

// Sign in with Google Sign-In
await FirebaseAuthManager.Instance.SignInWithGoogleSignIn();

// Check authentication status
if (FirebaseAuthManager.Instance.IsAuthenticated)
{
    var user = FirebaseAuthManager.Instance.CurrentUser;
    Debug.Log($"Signed in as: {user.DisplayName}");
}
```

## Events

```csharp
FirebaseAuthManager.Instance.OnUserAuthenticated += (user) => {
    Debug.Log("User authenticated!");
};

FirebaseAuthManager.Instance.OnAuthError += (error) => {
    Debug.LogError($"Auth error: {error}");
};

FirebaseAuthManager.Instance.OnAuthComplete += () => {
    Debug.Log("Auth process completed");
};
```

## Version

1.0.0

