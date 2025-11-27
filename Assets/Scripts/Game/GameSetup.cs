using UnityEngine;

/// <summary>
/// Helper component to setup game objects and initial configuration
/// Place this on a GameObject in your scene to initialize the game systems
/// </summary>
public class GameSetup : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private LevelsManager levelsManager;
    [SerializeField] private GameController gameController;
    [SerializeField] private ConnectionManager connectionManager;
    
    [Header("UI References")]
    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private GameplayUI gameplayUI;
    [SerializeField] private LoadingScreenUI loadingScreenUI;
    
    private void Awake()
    {
        // Ensure all required managers exist
        EnsureManagerExists<LevelsManager>(ref levelsManager, "LevelsManager");
        EnsureManagerExists<GameController>(ref gameController, "GameController");
        EnsureManagerExists<ConnectionManager>(ref connectionManager, "ConnectionManager");
    }
    
    private void EnsureManagerExists<T>(ref T manager, string name) where T : MonoBehaviour
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<T>();
            
            if (manager == null)
            {
                GameObject managerObj = new GameObject(name);
                manager = managerObj.AddComponent<T>();
                Debug.Log($"Created {name}");
            }
        }
    }
}

