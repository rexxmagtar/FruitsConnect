using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using System;
using System.Threading.Tasks;

namespace AuthServices
{
    public class GooglePlayServicesController : MonoBehaviour
    {
        private const string LEADERBOARD_ID = "CgkIsae3s_QEEAIQAQ";

        public static GooglePlayServicesController Instance { get; private set; }

        public bool IsAuthenticated => PlayGamesPlatform.Instance.IsAuthenticated();
        public bool AreFeaturesEnabled { get; private set; } = false;

        public event Action<bool> OnAuthenticationComplete;
        public event Action<string> OnError;
        public event Action<bool> OnFeaturesStateChanged;

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

        public async Task Initialize(){
             try
            {
                
                PlayGamesPlatform.Activate();
                // var result = await SignInAsync();
                // if (result)
                // {
                //     UpdateFeaturesState(true);
                // }
                // else
                // {
                //     Debug.LogWarning("Automatic sign-in failed. Play Games Services features will be disabled.");
                //     UpdateFeaturesState(false);
                // }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during automatic sign-in: {e.Message}");
                UpdateFeaturesState(false);
            }
        }

        private void UpdateFeaturesState(bool enabled)
        {
            Debug.Log("UpdateFeaturesState: " + enabled);
            AreFeaturesEnabled = enabled;
            OnFeaturesStateChanged?.Invoke(enabled);
        }

        public async Task<bool> SignInAsync(bool mannual = false)
        {
            try
            {
                var tcs = new TaskCompletionSource<bool>();
                
                if(mannual){
                       PlayGamesPlatform.Instance.ManuallyAuthenticate((result) =>
                {
                    if (result == SignInStatus.Success)
                    {
                        Debug.Log("Successfully authenticated with Google Play Services");
                        OnAuthenticationComplete?.Invoke(true);
                    }
                    else
                    {
                        Debug.LogWarning("Failed to authenticate with Google Play Services");
                        OnAuthenticationComplete?.Invoke(false);
                    }
                    tcs.SetResult(result == SignInStatus.Success);
                    });
                }
                else{
                    PlayGamesPlatform.Instance.Authenticate((result) =>
                    {
                        if (result == SignInStatus.Success)
                    {
                        Debug.Log("Successfully authenticated with Google Play Services");
                        OnAuthenticationComplete?.Invoke(true);
                    }
                    else
                    {
                        Debug.LogWarning("Failed to authenticate with Google Play Services");
                        OnAuthenticationComplete?.Invoke(false);
                    }
                        tcs.SetResult(result == SignInStatus.Success);
                    });
                }

                var result = await tcs.Task;
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Authentication error: {e.Message}");
                return false;
            }
        }

        // public async Task<Result> SignOutAsync()
        // {
        //     try
        //     {
        //         if (IsAuthenticated)
        //         {
        //             PlayGamesPlatform.Instance.Sign();
        //             Debug.Log("Signed out from Google Play Services");
        //             return Result.Success();
        //         }
        //         return Result.Failure("User is not authenticated");
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError($"Sign out error: {e.Message}");
        //         return Result.Failure($"Sign out failed: {e.Message}");
        //     }
        // }


    }

    // Result types for better error handling
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(string error) => new Result(false, error);
    }

    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }

        private Result(bool isSuccess, T value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null);
        public static Result<T> Failure(string error) => new Result<T>(false, default, error);
    }
}

