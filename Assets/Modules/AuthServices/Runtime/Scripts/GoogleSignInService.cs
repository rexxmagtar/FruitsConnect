using UnityEngine;
using Google;
using System;
using System.Threading.Tasks;
using GoogleSignInUser = Google.GoogleSignInUser;

namespace AuthServices
{
    /// <summary>
    /// Dedicated service for handling Google Sign-In functionality
    /// Separated from Google Play Services for better organization
    /// </summary>
    public class GoogleSignInService : MonoBehaviour
    {
        public static GoogleSignInService Instance { get; private set; }

        [Header("Google Sign-In Configuration")]
        [SerializeField] private string webClientId = ""; // Set this in inspector
        [SerializeField] private bool requestEmail = true;
        [SerializeField] private bool requestProfile = true;
        [SerializeField] private bool requestIdToken = true;
        [SerializeField] private bool autoLinkToFirebase = true;

        // Properties
        public bool IsSignedIn { get; private set; } = false;
        public GoogleSignInUser CurrentUser { get; private set; } = null;

        // Events
        public event Action<GoogleSignInUser> OnSignInComplete;
        public event Action<string> OnSignInError;
        public event Action OnSignOutComplete;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGoogleSignIn();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes Google Sign-In with the configured settings
        /// </summary>
        private void InitializeGoogleSignIn()
        {
            try
            {
                // Configure Google Sign-In
                var configuration = new GoogleSignInConfiguration
                {
                    WebClientId = webClientId,
                    RequestEmail = requestEmail,
                    RequestProfile = requestProfile,
                    RequestIdToken = requestIdToken,
                    UseGameSignIn = false // We want regular Google Sign-In, not Play Games
                };
                
                GoogleSignIn.Configuration = configuration;
                GoogleSignIn.DefaultInstance.EnableDebugLogging(true);
                
                Debug.Log("Google Sign-In Service initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Google Sign-In Service: {e.Message}");
            }
        }

        /// <summary>
        /// Signs in to Google account with UI
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SignInAsync()
        {
            try
            {
                Debug.Log("Starting Google Sign-In...");
                
                var signInTask = GoogleSignIn.DefaultInstance.SignIn();
                var user = await signInTask;
                
                if (user != null)
                {
                    CurrentUser = user;
                    IsSignedIn = true;
                    
                    Debug.Log($"Google Sign-In successful! User: {user.DisplayName} ({user.Email})");
                    OnSignInComplete?.Invoke(user);
                    
                    // Automatically link to Firebase if enabled
                    if (autoLinkToFirebase)
                    {
                        await LinkToFirebaseIfAvailable();
                    }
                    
                    return true;
                }
                else
                {
                    Debug.LogWarning("Google Sign-In returned null user");
                    OnSignInError?.Invoke("Sign-in returned null user");
                    return false;
                }
            }
            catch (GoogleSignIn.SignInException e)
            {
                Debug.LogError($"Google Sign-In failed with status {e.Status}: {e.Message}");
                OnSignInError?.Invoke($"Sign-in failed: {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unexpected error during Google Sign-In: {e.Message}");
                OnSignInError?.Invoke($"Unexpected error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to sign in silently (without UI) to Google account
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SignInSilentlyAsync()
        {
            try
            {
                Debug.Log("Attempting silent Google Sign-In...");
                
                var signInTask = GoogleSignIn.DefaultInstance.SignInSilently();
                var user = await signInTask;
                
                if (user != null)
                {
                    CurrentUser = user;
                    IsSignedIn = true;
                    
                    Debug.Log($"Silent Google Sign-In successful! User: {user.DisplayName} ({user.Email})");
                    OnSignInComplete?.Invoke(user);
                    
                    // Automatically link to Firebase if enabled
                    if (autoLinkToFirebase)
                    {
                        await LinkToFirebaseIfAvailable();
                    }
                    
                    return true;
                }
                else
                {
                    Debug.Log("Silent Google Sign-In failed - user needs to sign in manually");
                    return false;
                }
            }
            catch (GoogleSignIn.SignInException e)
            {
                Debug.LogWarning($"Silent Google Sign-In failed with status {e.Status}: {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unexpected error during silent Google Sign-In: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Signs out from Google account
        /// </summary>
        public void SignOut()
        {
            try
            {
                GoogleSignIn.DefaultInstance.SignOut();
                CurrentUser = null;
                IsSignedIn = false;
                
                Debug.Log("Successfully signed out from Google account");
                OnSignOutComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error signing out from Google: {e.Message}");
            }
        }

        /// <summary>
        /// Disconnects from Google account (revokes all access)
        /// </summary>
        public void Disconnect()
        {
            try
            {
                GoogleSignIn.DefaultInstance.Disconnect();
                CurrentUser = null;
                IsSignedIn = false;
                
                Debug.Log("Successfully disconnected from Google account");
                OnSignOutComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error disconnecting from Google: {e.Message}");
            }
        }

        /// <summary>
        /// Automatically links Google Sign-In to Firebase if FirebaseAuthManager is available
        /// </summary>
        private async Task<bool> LinkToFirebaseIfAvailable()
        {
            try
            {
                // Check if FirebaseAuthManager is available
                if (FirebaseAuthManager.Instance != null)
                {
                    Debug.Log("FirebaseAuthManager found. Attempting to link Google Sign-In to Firebase...");
                    bool firebaseLinkSuccess = await FirebaseAuthManager.Instance.SignInWithGoogleSignIn();
                    
                    if (firebaseLinkSuccess)
                    {
                        Debug.Log("Successfully linked Google Sign-In to Firebase");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("Failed to link Google Sign-In to Firebase");
                        return false;
                    }
                }
                else
                {
                    Debug.Log("FirebaseAuthManager not found. Skipping Firebase linking.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error linking to Firebase: {e.Message}");
                return false;
            }
        }

        // Helper methods for accessing user data
        public string GetUserEmail()
        {
            return CurrentUser?.Email;
        }

        public string GetUserDisplayName()
        {
            return CurrentUser?.DisplayName;
        }

        public string GetUserIdToken()
        {
            return CurrentUser?.IdToken;
        }

        public string GetUserAuthCode()
        {
            return CurrentUser?.AuthCode;
        }

        public string GetUserId()
        {
            return CurrentUser?.UserId;
        }

        public Uri GetUserImageUrl()
        {
            return CurrentUser?.ImageUrl;
        }

        /// <summary>
        /// Manually triggers Firebase linking (useful when auto-linking is disabled)
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> LinkToFirebase()
        {
            if (!IsSignedIn)
            {
                Debug.LogWarning("Cannot link to Firebase: User is not signed in to Google");
                return false;
            }

            return await LinkToFirebaseIfAvailable();
        }

        /// <summary>
        /// Checks if the service is properly configured
        /// </summary>
        /// <returns>True if configured, false otherwise</returns>
        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(webClientId);
        }

        /// <summary>
        /// Updates the configuration at runtime
        /// </summary>
        /// <param name="newWebClientId">New Web Client ID</param>
        /// <param name="newRequestEmail">Whether to request email</param>
        /// <param name="newRequestProfile">Whether to request profile</param>
        /// <param name="newRequestIdToken">Whether to request ID token</param>
        public void UpdateConfiguration(string newWebClientId, bool newRequestEmail = true, bool newRequestProfile = true, bool newRequestIdToken = true)
        {
            webClientId = newWebClientId;
            requestEmail = newRequestEmail;
            requestProfile = newRequestProfile;
            requestIdToken = newRequestIdToken;

            // Reinitialize with new configuration
            InitializeGoogleSignIn();
        }
    }
}

