using System;
using System.Threading.Tasks;
using UnityEngine;
using AnalyticsServices;
using ComplianceService;
using AuthServices;
using DataRepository;
using AdsServices;

public class SaveData
{
    public int CurrentLevel { get; set; }
    public int TotalCoins { get; set; }
    public bool IsAdEnabled { get; set; }
}

public class GameManager : MonoBehaviour
{
    [Header("Game Manager Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    
    // Singleton pattern
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }
    
    // Events
    public static event Action OnGameInitialized;
    public static event Action<string> OnInitializationFailed;
    public static event Action<float> OnInitializationProgress;
    
    // State
    public bool IsInitialized { get; private set; }
    public bool IsInitializing { get; private set; }
    public string LastError { get; private set; }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private async void Start()
    {
        await InitializeGame();
    }
    
    /// <summary>
    /// Main game initialization method that initializes all modules in the correct order
    /// </summary>
    public async Task InitializeGame()
    {
        if (IsInitialized || IsInitializing)
        {
            LogDebug("Game already initialized or initializing");
            return;
        }
        
        IsInitializing = true;
        LogDebug("Starting game initialization...");
        
        try
        {
            // 1. Analytics - Initialize first for tracking
            await InitializeAnalytics();
            
            // 2. GDPR/COPPA Compliance - Required before any data collection
            await InitializeCompliance();
            
            // 3. Authentication - Required for user-specific data
            await InitializeAuthentication();
            
            // 4. Save Progress - Load or create user progress
            await InitializeSaveProgress();
            
            // 5. Audio Settings - Initialize after save progress
            await InitializeAudioSettings();
            
            // 5. Ads - Initialize after compliance and auth
            await InitializeAds();

            // ProgressSaveManager<SaveData>.Instance.SyncWithCloud();
            
            
            // Mark as initialized
            OnInitializationProgress?.Invoke(1.0f);
            IsInitialized = true;
            IsInitializing = false;
            
            LogDebug("Game initialization completed successfully!");
            OnGameInitialized?.Invoke();
        }
        catch (Exception ex)
        {
            IsInitializing = false;
            LastError = ex.Message;
            LogError($"Game initialization failed: {ex.Message}");
            OnInitializationFailed?.Invoke(ex.Message);
        }
    }
    
    /// <summary>
    /// Initialize Analytics module
    /// </summary>
    private async Task InitializeAnalytics()
    {
        LogDebug("Initializing Analytics...");
        OnInitializationProgress?.Invoke(0.1f);
        
        try
        {
            // AnalyticsManager uses singleton pattern and initializes itself
            // We just need to access the instance to ensure it's ready
            var analyticsInstance = AnalyticsService.Instance;
            if (analyticsInstance == null)
            {
                LogError("AnalyticsManager singleton not available");
                throw new Exception("AnalyticsManager singleton not available");
            }

            await analyticsInstance.Initialize();
            
            LogDebug("Analytics initialized successfully");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize Analytics: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Initialize GDPR/COPPA Compliance
    /// </summary>
    private async Task InitializeCompliance()
    {
        LogDebug("Initializing Compliance...");
        OnInitializationProgress?.Invoke(0.2f);

        await GDPRManager.Instance.CheckAndShowGDPRConsentAsync();
        await ChildAgeManager.Instance.CheckAndShowChildAgeDialogAsync();
        try
        {
            // Check GDPR compliance for European users
            if (GDPRManager.Instance.IsUserInEurope())
            {
                LogDebug("User is in Europe, checking GDPR consent...");
                bool gdprConsent = await GDPRManager.Instance.CheckAndShowGDPRConsentAsync();
                LogDebug($"GDPR consent result: {gdprConsent}");
            }
            
            // Check COPPA compliance for US users
            if (ChildAgeManager.Instance.IsUserInUSA())
            {
                LogDebug("User is in USA, checking COPPA compliance...");
                bool isChild = await ChildAgeManager.Instance.CheckAndShowChildAgeDialogAsync();
                LogDebug($"COPPA child status: {isChild}");
            }
            
            LogDebug("Compliance initialized successfully");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize Compliance: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Initialize Authentication
    /// </summary>
    private async Task InitializeAuthentication()
    {
        LogDebug("Initializing Authentication...");
        OnInitializationProgress?.Invoke(0.3f);
        
        try
        {
            // Initialize Firebase Auth
             FirebaseAuthManager.Instance.Initialize();
            await FirebaseAuthManager.Instance.Auth();
            
            if (FirebaseAuthManager.Instance.IsAuthenticated)
            {
                LogDebug($"User authenticated: {FirebaseAuthManager.Instance.CurrentUser?.DisplayName}");
            }
            else
            {
                LogDebug("User not authenticated (anonymous or failed)");
            }
            
            LogDebug("Authentication initialized successfully");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize Authentication: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Initialize Save Progress system
    /// </summary>
    private Task InitializeSaveProgress()
    {
        LogDebug("Initializing Save Progress...");
        OnInitializationProgress?.Invoke(0.4f);
        
        try
        {
            // Initialize ProgressSaveManager
            var progressManager = ProgressSaveManager<SaveData>.Instance;

            progressManager.Initialize();
            
            // Load existing save data or create new
            bool hasExistingSave = progressManager.HasData;
            
            if (!hasExistingSave)
            {
                LogDebug("Creating new save data...");
                progressManager.CreateNewSaveData();
            }
            
            // Log current progress
            var saveData = progressManager.GetGameData();
            LogDebug($"Save data loaded - Level: {saveData.CurrentLevel}, Ads Enabled: {saveData.IsAdEnabled}");
            
            LogDebug("Save Progress initialized successfully");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize Save Progress: {ex.Message}");
            throw;
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Initialize Ads system
    /// </summary>
    private async Task InitializeAds()
    {
        LogDebug("Initializing Ads...");
        OnInitializationProgress?.Invoke(0.5f);
        
        try
        {

            // Initialize ads (this will handle GDPR/COPPA compliance internally)
            await AdsManager.Instance.Initialize();
            
            LogDebug("Ads initialized successfully");
        }
        catch (Exception ex)
        {
            LogError($"Failed to initialize Ads: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Initialize Audio Settings
    /// </summary>
    private Task InitializeAudioSettings()
    {
        LogDebug("Initializing Audio Settings...");
        AudioSettings.Instance.Initialize();
        OnInitializationProgress?.Invoke(0.6f);
        return Task.CompletedTask;
    }
    
    
    
    /// <summary>
    /// Restart the game initialization process
    /// </summary>
    public async Task RestartInitialization()
    {
        LogDebug("Restarting game initialization...");
        IsInitialized = false;
        IsInitializing = false;
        LastError = null;
        await InitializeGame();
    }
    
    /// <summary>
    /// Get initialization status
    /// </summary>
    public string GetInitializationStatus()
    {
        if (IsInitialized)
            return "Initialized";
        if (IsInitializing)
            return "Initializing...";
        if (!string.IsNullOrEmpty(LastError))
            return $"Failed: {LastError}";
        return "Not Started";
    }
    
    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GameManager] {message}");
        }
    }
    
    /// <summary>
    /// Error logging helper
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[GameManager] {message}");
    }
    
    /// <summary>
    /// Add coins to player's total
    /// </summary>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        
        var progressManager = ProgressSaveManager<SaveData>.Instance;
        var saveData = progressManager.GetGameData();
        
        saveData.TotalCoins += amount;
        progressManager.SaveGameData();
        
        LogDebug($"Added {amount} coins. Total: {saveData.TotalCoins}");
    }
    
    /// <summary>
    /// Complete current level and move to next
    /// </summary>
    public void CompleteLevel()
    {
        LevelsManager levelsManager = LevelsManager.Instance;
        if (levelsManager != null)
        {
            levelsManager.CompleteLevel();
            LogDebug("Level completed!");
        }
    }
    
    /// <summary>
    /// Get current coin count
    /// </summary>
    public int GetCoins()
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        return saveData.TotalCoins;
    }
    
    private void OnDestroy()
    {
        // Cleanup if needed
    }
}
