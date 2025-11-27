using UnityEngine;
using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using Firebase;
using Google;

namespace AuthServices
{
    public class FirebaseAuthManager : MonoBehaviour
    {
        public static FirebaseAuthManager Instance { get; private set; }
        
        private FirebaseAuth auth;
        public FirebaseUser CurrentUser => auth?.CurrentUser;
        public bool IsAuthenticated => CurrentUser != null;

        public event Action<FirebaseUser> OnUserAuthenticated;
        public event Action<string> OnAuthError;
        public event Action OnAuthComplete;

        public bool IsSignedInWithGoogle()
        {
            foreach (var provider in auth.CurrentUser.ProviderData)
            {
                if (provider.ProviderId == GoogleAuthProvider.ProviderId)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsSignedInWithGooglePlayServices()
        {
            foreach (var provider in auth.CurrentUser.ProviderData)
            {
                if (provider.ProviderId == PlayGamesAuthProvider.ProviderId)
                {
                    return true;
                }
            }
            return false;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize()
        {
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged += AuthStateChanged;
        }

        private void AuthStateChanged(object sender, EventArgs e)
        {
            if (auth.CurrentUser != null)
            {
                OnUserAuthenticated?.Invoke(auth.CurrentUser);
            }
        }

        public async Task<bool> Auth()
        {
            try
            {
                Debug.Log("Attempting to authenticate with Firebase");
                bool authSuccess = false;

                if(auth.CurrentUser != null && !auth.CurrentUser.IsAnonymous)
                {
                    authSuccess = true;
                     OnAuthComplete?.Invoke();
                     Debug.Log("User is already authenticated with google play services");
                    return true;
                }

                if(auth.CurrentUser != null && auth.CurrentUser.IsAnonymous){
                     authSuccess = true;
                     OnAuthComplete?.Invoke();
                     Debug.Log("User is already authenticated anonymously");
                    return true;
                }
                
                if(!authSuccess)
                {
                    authSuccess = await SignInAnonymously();
                }

                if (!authSuccess)
                {
                    throw new Exception("Failed to authenticate with Firebase");
                }

                OnAuthComplete?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Authentication failed: {e.Message}");
                OnAuthError?.Invoke(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Signs in to Firebase using Google Sign-In credentials
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SignInWithGoogleSignIn()
        {
            try
            {
                Debug.Log("Attempting to sign in with Google Sign-In");
                
                // Check if user is signed in to Google
                if (GoogleSignInService.Instance == null || !GoogleSignInService.Instance.IsSignedIn)
                {
                    Debug.LogWarning("User is not signed in to Google. Attempting silent sign-in...");
                    bool signInSuccess = await GoogleSignInService.Instance.SignInAsync();
                    if (!signInSuccess)
                    {
                        Debug.LogWarning("Google Sign-In failed");
                        return false;
                    }
                }

                // Get the ID token from Google Sign-In
                string idToken = GoogleSignInService.Instance.GetUserIdToken();
                if (string.IsNullOrEmpty(idToken))
                {
                    Debug.LogError("Failed to get ID token from Google Sign-In");
                    return false;
                }

                Debug.Log("Got ID token from Google Sign-In");

                // Create Firebase credential from Google ID token
                var credential = GoogleAuthProvider.GetCredential(idToken, null);

                // If we have an anonymous user, link the accounts
                if (auth.CurrentUser != null)
                {
                    try
                    {
                        await auth.CurrentUser.LinkWithCredentialAsync(credential);
                        Debug.Log("Successfully linked anonymous account with Google Sign-In account");
                        return true;
                    }
                    catch (Exception linkEx)
                    {
                        Debug.Log($"Error linking accounts: {linkEx.Message}");
                        

                        Debug.Log("Google account is already linked to another Firebase account. Attempting to sign in with that account.");
                            // Sign in with the credential directly instead of linking
                        if(auth.CurrentUser.IsAnonymous)
                        {
                            await auth.CurrentUser.DeleteAsync();
                        }

                        await auth.SignInWithCredentialAsync(credential);
                        return true;
    
                    }
                }
                else
                {
                    // If no anonymous user, sign in directly with Google
                    await auth.SignInWithCredentialAsync(credential);
                    Debug.Log("Successfully signed in to Firebase with Google Sign-In");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error signing in with Google Sign-In: {e.Message}");
                OnAuthError?.Invoke(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Manually triggers Google Sign-In and links to Firebase
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ManualGoogleSignIn()
        {
            try
            {
                Debug.Log("Starting manual Google Sign-In process");
                
                if (GoogleSignInService.Instance == null)
                {
                    Debug.LogError("GoogleSignInService not found!");
                    return false;
                }
                
                // Sign in to Google with UI
                bool googleSignInSuccess = await GoogleSignInService.Instance.SignInAsync();
                if (!googleSignInSuccess)
                {
                    Debug.LogWarning("Manual Google Sign-In failed");
                    return false;
                }

                // Now link to Firebase
                return await SignInWithGoogleSignIn();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during manual Google Sign-In: {e.Message}");
                OnAuthError?.Invoke(e.Message);
                return false;
            }
        }

        public async Task<bool> SignInWithGooglePlayServices()
        {
            try
            {
                Debug.Log("Attempting to sign in with Google Play Services");

                var signInResult = await GooglePlayServicesController.Instance.SignInAsync(true);
                if (!signInResult)
                {
                    Debug.LogWarning("Failed to sign in with Google Play Services");
                    return false;
                }


                // Get Google Play Games credentials
                var tcs = new TaskCompletionSource<string>();
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
                {
                    tcs.SetResult(authCode);
                });
                
                var serverAuthCode = await tcs.Task;
                if (string.IsNullOrEmpty(serverAuthCode))
                {
                    Debug.LogError("Failed to get server auth code from Google Play Services");
                    return false;
                }

                Debug.Log("Server auth code: " + serverAuthCode);

                var credential = PlayGamesAuthProvider.GetCredential(serverAuthCode);

                // If we already have a user, link the accounts
                if (auth.CurrentUser != null)
                {
                    try
                    {
                        await auth.CurrentUser.LinkWithCredentialAsync(credential);
                        Debug.Log("Successfully linked anonymous account with Google Play account");
                    }
                    catch (Exception linkEx)
                    {
                        // Check if the error is due to the credential being already linked to another account

                            Debug.Log("Google Play account is already linked to another account. Attempting to sign in with that account.");
                            // Sign in with the credential directly instead of linking
                           if(auth.CurrentUser.IsAnonymous)
                           {
                            await auth.CurrentUser.DeleteAsync();
                           }
var tcs2 = new TaskCompletionSource<string>();
                             PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
                            {
                                Debug.Log("Server auth code: " + authCode);
                                tcs2.SetResult(authCode);
                            });

                            var serverAuthCode2 = await tcs2.Task;
                            var credential2 = PlayGamesAuthProvider.GetCredential(serverAuthCode2);
                            
                            await auth.SignInWithCredentialAsync(credential2);
                            return true;
                        
                    }
                }
                else
                {
                    // If no user, sign in directly with Google
                    await auth.SignInWithCredentialAsync(credential);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error signing in with Google Play Services: {e.Message}");
                OnAuthError?.Invoke(e.Message);
                return false;
            }
        }

        private async Task<bool> SignInAnonymously()
        {
            try
            {
                var result = await auth.SignInAnonymouslyAsync();
                return result != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error signing in anonymously: {e.Message}");
                OnAuthError?.Invoke(e.Message);
                return false;
            }
        }

        public async Task<bool> SignOut()
        {
            try
            {
                auth.SignOut();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error signing out: {e.Message}");
                OnAuthError?.Invoke(e.Message);
                return false;
            }
        }

        private void OnDestroy()
        {
            if (auth != null)
            {
                auth.StateChanged -= AuthStateChanged;
            }
        }
    }
}

