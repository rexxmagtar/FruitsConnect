using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages boss fight flow - transitions, timer, win/loss conditions
/// </summary>
public class BossFightManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossFightUI bossFightUI;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform bossCameraViewTransform;
    
    [Header("Boss Fight Settings")]
    [SerializeField] private float bossAlertDisplayDuration = 2.5f;
    [SerializeField] private float cameraTransitionDuration = 1.5f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip bossFightAlarmSound;
    [SerializeField] private AudioClip bossFightMusic;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource musicAudioSource;
    
    // Singleton
    private static BossFightManager _instance;
    public static BossFightManager Instance => _instance;
    
    // State
    private Boss currentBoss;
    private LevelController currentLevel;
    private LevelConfig currentLevelConfig;
    private bool isBossFightActive = false;
    private float timeRemaining;
    private float timeLimit;
    private List<GameObject> hiddenObjects = new List<GameObject>();
    private List<GameObject> fadeableObjects = new List<GameObject>();
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool terrainWasVisible = true;
    private Coroutine fadeAwayCoroutine;
    
    // Properties
    public bool IsBossFightActive => isBossFightActive;
    public float TimeRemaining => timeRemaining;
    public float TimeLimit => timeLimit;
    public Boss CurrentBoss => currentBoss;
    
    private void Awake()
    {
        // Singleton setup
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
        
        // Get or find CameraController
        if (cameraController == null)
        {
            cameraController = CameraController.Instance;
            if (cameraController == null)
            {
                GameObject cameraObj = new GameObject("CameraController");
                cameraController = cameraObj.AddComponent<CameraController>();
            }
        }
        
        // Get or find BossFightUI
        if (bossFightUI == null)
        {
            bossFightUI = FindFirstObjectByType<BossFightUI>();
        }
        
        // Get or add AudioSource for alarm sound if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        
        // Get or add AudioSource for boss fight music if not assigned
        if (musicAudioSource == null)
        {
            // Try to find existing AudioSource that's not the alarm one
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                musicAudioSource = sources[1]; // Use second AudioSource if multiple exist
            }
            else
            {
                // Create new AudioSource for music
                musicAudioSource = gameObject.AddComponent<AudioSource>();
                musicAudioSource.playOnAwake = false;
                musicAudioSource.loop = true; // Boss fight music should loop
            }
        }
    }
    
    private void Update()
    {
        if (isBossFightActive)
        {
            UpdateTimer();
        }
    }
    
    /// <summary>
    /// Start boss fight sequence
    /// </summary>
    public void StartBossFight(LevelController level, LevelConfig config)
    {
        if (isBossFightActive)
        {
            Debug.LogWarning("BossFightManager: Boss fight already active!");
            return;
        }
        
        currentLevel = level;
        currentLevelConfig = config;
        
        // Find boss in level
        currentBoss = FindBossInLevel(level);
        if (currentBoss == null)
        {
            Debug.LogError("BossFightManager: No boss found in level! Cannot start boss fight.");
            // Fall back to normal level complete
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                gameController.OnLevelCompleteFallback();
            }
            return;
        }
        
        // Get time limit from config
        timeLimit = config.BossFightTimeLimit;
        timeRemaining = timeLimit;
        
        // Start transition sequence
        StartCoroutine(BossFightTransitionSequence());
    }
    
    /// <summary>
    /// Find boss in level
    /// </summary>
    private Boss FindBossInLevel(LevelController level)
    {
        if (level == null) return null;
        
        // Search for Boss component in level
        Boss boss = level.GetComponentInChildren<Boss>();
        if (boss == null)
        {
            // Search in all nodes
            List<BaseNode> nodes = level.GetAllNodes();
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    boss = node.GetComponentInChildren<Boss>();
                    if (boss != null) break;
                }
            }
        }
        
        // If still not found, search entire scene
        if (boss == null)
        {
            boss = FindFirstObjectByType<Boss>();
        }
        
        return boss;
    }
    
    /// <summary>
    /// Transition sequence: hide map, move camera, show alert, start fight
    /// </summary>
    private IEnumerator BossFightTransitionSequence()
    {
        // Store original camera position
        if (cameraController != null)
        {
            cameraController.StoreCurrentPositionAsOriginal();
        }
        
        // Step 1: Collect fadeable elements
        CollectFadeableElements();
        
        // Step 2: Show boss alert UI immediately (before camera transition completes)
        if (bossFightUI != null)
        {
            bossFightUI.ShowBossAlert();
        }
        
        // Play boss fight alarm sound
        PlayBossFightAlarmSound();
        
        // Wait 1 second to hear the alarm sound, then start boss fight music
        yield return new WaitForSeconds(1f);
        StartBossFightMusic();
        
        // Step 3: Move camera to boss view
        if (cameraController != null)
        {
            if (bossCameraViewTransform != null)
            {
                // Use predefined transform for camera position and rotation
                cameraController.MoveToPosition(
                    bossCameraViewTransform.position,
                    bossCameraViewTransform.rotation.eulerAngles,
                    cameraTransitionDuration
                );
            }
            else
            {
                // Fallback: calculate position relative to boss
                if (currentBoss != null)
                {
                    Vector3 bossPosition = currentBoss.transform.position;
                    Vector3 cameraPosition = bossPosition + Vector3.back * 8f + Vector3.up * 5f;
                    Vector3 cameraRotation = Quaternion.LookRotation(bossPosition - cameraPosition).eulerAngles;
                    cameraController.MoveToPosition(cameraPosition, cameraRotation, cameraTransitionDuration);
                }
                Debug.LogWarning("BossFightManager: BossCameraViewTransform not assigned. Using fallback camera position.");
            }
        }
        
        // Step 4: Start fade-away animation 1 second after camera transition starts (runs in parallel, doesn't block)
        yield return new WaitForSeconds(0.4f);
        if (fadeAwayCoroutine != null)
        {
            StopCoroutine(fadeAwayCoroutine);
        }
        fadeAwayCoroutine = StartCoroutine(FadeAwayAllMapElements());
        
        // Wait for remaining camera transition time
        yield return new WaitForSeconds(cameraTransitionDuration - 1f);
        
        // Wait for alert display
        yield return new WaitForSeconds(bossAlertDisplayDuration);
        
        // Step 4: Hide alert, show fight UI, start boss fight
        if (bossFightUI != null)
        {
            bossFightUI.HideBossAlert();
            bossFightUI.ShowFightUI();
        }
        
        // Initialize boss for fight
        if (currentBoss != null)
        {
            currentBoss.StartBossFight();
        }
        
        // Subscribe to boss events
        Boss.OnBossDied += OnBossDied;
        Boss.OnBossEscaped += OnBossEscaped;
        
        // Start fight
        isBossFightActive = true;
        
        Debug.Log("Boss fight started!");
    }
    
    /// <summary>
    /// Collect all map elements that need to be faded out (nodes, terrain, vegetation)
    /// </summary>
    private void CollectFadeableElements()
    {
        fadeableObjects.Clear();
        hiddenObjects.Clear();
        
        if (currentLevel == null) return;
        
        // Collect all nodes (except boss)
        List<BaseNode> nodes = currentLevel.GetAllNodes();
        foreach (var node in nodes)
        {
            if (node != null && node.gameObject != currentBoss.gameObject)
            {
                fadeableObjects.Add(node.gameObject);
            }
        }
        
        // Collect base terrain plane (TerrainMeshRenderer)
        if (currentLevel != null && currentLevel.TerrainMeshRenderer != null)
        {
            terrainWasVisible = currentLevel.TerrainMeshRenderer.enabled;
            fadeableObjects.Add(currentLevel.TerrainMeshRenderer.gameObject);
        }
        
        // Collect all vegetation objects under "Terrain" parent
        Transform terrainParent = currentLevel.transform.Find("Terrain");
        if (terrainParent == null)
        {
            // Try to find by name in all children
            Transform[] allChildren = currentLevel.transform.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name == "Terrain")
                {
                    terrainParent = child;
                    break;
                }
            }
        }
        
        if (terrainParent != null)
        {
            // Get all direct children of Terrain parent (vegetation objects)
            for (int i = 0; i < terrainParent.childCount; i++)
            {
                Transform child = terrainParent.GetChild(i);
                if (child != null && child.gameObject != currentBoss.gameObject)
                {
                    fadeableObjects.Add(child.gameObject);
                }
            }
        }
        
        // Hide all connections and stop their animations (these don't fade, just hide immediately)
        ConnectionManager connectionManager = ConnectionManager.Instance;
        if (connectionManager != null)
        {
            List<Connection> connections = connectionManager.GetActiveConnections();
            foreach (var connection in connections)
            {
                if (connection != null)
                {
                    // Stop animation and hide animated objects
                    connection.StopAnimationAndHideObjects();
                    connection.gameObject.SetActive(false);
                    hiddenObjects.Add(connection.gameObject);
                }
            }
        }
        
        // Hide all connection placeholders (these don't fade, just hide immediately)
        if (currentLevel != null)
        {
            GraphVisualizer graphVisualizer = currentLevel.GetGraphVisualizer();
            if (graphVisualizer != null)
            {
                graphVisualizer.SetShowPlaceholders(false);
            }
        }
        
        // Hide all monsters (these don't fade, just hide immediately)
        MonsterAiManager monsterManager = MonsterAiManager.Instance;
        if (monsterManager != null)
        {
            Monster[] monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                if (monster != null && monster.gameObject != currentBoss.gameObject)
                {
                    monster.gameObject.SetActive(false);
                    hiddenObjects.Add(monster.gameObject);
                }
            }
        }
        
        // Hide gameplay UI
        GameplayUI gameplayUI = FindFirstObjectByType<GameplayUI>();
        if (gameplayUI != null)
        {
            gameplayUI.Hide();
        }
    }
    
    /// <summary>
    /// Fade away all map elements (nodes, terrain, vegetation) smoothly
    /// Uses UnlitAlphaFade shader procedurally for materials that don't support alpha
    /// </summary>
    private IEnumerator FadeAwayAllMapElements()
    {
        if (fadeableObjects.Count == 0)
        {
            yield break;
        }
        
        float fadeDuration = 1.5f; // Fade duration in seconds
        float elapsedTime = 0f;
        
        // Load the UnlitAlphaFade shader
        Shader fadeShader = Shader.Find("Custom/UnlitAlphaFade");
        if (fadeShader == null)
        {
            Debug.LogError("BossFightManager: UnlitAlphaFade shader not found! Cannot perform fade animation.");
            // Just deactivate objects
            foreach (GameObject obj in fadeableObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
            fadeableObjects.Clear();
            yield break;
        }
        
        // Store renderers, original materials, and fade materials
        Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        Dictionary<Renderer, Material[]> fadeMaterials = new Dictionary<Renderer, Material[]>();
        List<Material> createdFadeMaterials = new List<Material>(); // For cleanup
        
        // Create fade materials for all renderers
        foreach (GameObject obj in fadeableObjects)
        {
            if (obj == null) continue;
            
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                
                // Store original materials
                Material[] originalMats = renderer.sharedMaterials;
                originalMaterials[renderer] = originalMats;
                
                // Create fade materials using UnlitAlphaFade shader
                Material[] fadeMats = new Material[originalMats.Length];
                for (int i = 0; i < originalMats.Length; i++)
                {
                    Material originalMat = originalMats[i];
                    if (originalMat == null)
                    {
                        fadeMats[i] = null;
                        continue;
                    }
                    
                    // Create new material with fade shader
                    Material fadeMat = new Material(fadeShader);
                    fadeMat.name = originalMat.name + "_Fade";
                    
                    // Copy main texture if available
                    if (originalMat.HasProperty("_MainTex"))
                    {
                        Texture mainTex = originalMat.GetTexture("_MainTex");
                        if (mainTex != null)
                        {
                            fadeMat.SetTexture("_MainTex", mainTex);
                        }
                        
                        // Copy texture tiling (scale) and offset
                        Vector2 tiling = originalMat.GetTextureScale("_MainTex");
                        Vector2 offset = originalMat.GetTextureOffset("_MainTex");
                        fadeMat.SetTextureScale("_MainTex", tiling);
                        fadeMat.SetTextureOffset("_MainTex", offset);
                    }
                    
                    // Set initial color (full alpha)
                    Color originalColor = Color.white;
                    if (originalMat.HasProperty("_Color"))
                    {
                        originalColor = originalMat.color;
                    }
                    fadeMat.SetColor("_Color", originalColor);
                    
                    fadeMats[i] = fadeMat;
                    createdFadeMaterials.Add(fadeMat);
                }
                
                fadeMaterials[renderer] = fadeMats;
                
                // Apply fade materials to renderer
                renderer.materials = fadeMats;
            }
        }
        
        if (fadeMaterials.Count == 0)
        {
            Debug.LogWarning("BossFightManager: No renderers found for fade animation!");
            // Just deactivate objects
            foreach (GameObject obj in fadeableObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
            fadeableObjects.Clear();
            yield break;
        }
        
        // Fade out animation
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            
            // Smooth fade using ease-out curve
            t = 1f - Mathf.Pow(1f - t, 2f);
            
            // Update all fade materials' alpha
            foreach (var kvp in fadeMaterials)
            {
                Renderer renderer = kvp.Key;
                Material[] fadeMats = kvp.Value;
                
                if (renderer == null) continue;
                
                foreach (Material fadeMat in fadeMats)
                {
                    if (fadeMat == null) continue;
                    
                    // Get current color and fade alpha
                    Color currentColor = fadeMat.GetColor("_Color");
                    currentColor.a = 1f - t; // Fade from 1 to 0
                    fadeMat.SetColor("_Color", currentColor);
                }
            }
            
            yield return null;
        }
        
        // Ensure final state (fully transparent)
        foreach (var kvp in fadeMaterials)
        {
            Renderer renderer = kvp.Key;
            Material[] fadeMats = kvp.Value;
            
            if (renderer == null) continue;
            
            foreach (Material fadeMat in fadeMats)
            {
                if (fadeMat == null) continue;
                
                Color transparentColor = fadeMat.GetColor("_Color");
                transparentColor.a = 0f;
                fadeMat.SetColor("_Color", transparentColor);
            }
        }
        
        // Wait a frame to ensure final state is rendered
        yield return null;
        
        // Deactivate all objects
        foreach (GameObject obj in fadeableObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        
        // Clean up created fade materials
        foreach (Material fadeMat in createdFadeMaterials)
        {
            if (fadeMat != null)
            {
                Destroy(fadeMat);
            }
        }
        
        fadeableObjects.Clear();
        originalMaterials.Clear();
        fadeMaterials.Clear();
        createdFadeMaterials.Clear();
    }
    
    /// <summary>
    /// Restore all hidden map elements
    /// </summary>
    private void RestoreMapElements()
    {
        foreach (var obj in hiddenObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        hiddenObjects.Clear();
        
        // Restore terrain renderer visibility from LevelController if available
        if (currentLevel != null && currentLevel.TerrainMeshRenderer != null)
        {
            currentLevel.TerrainMeshRenderer.enabled = terrainWasVisible;
        }
    }
    
    /// <summary>
    /// Update timer countdown
    /// </summary>
    private void UpdateTimer()
    {
        if (!isBossFightActive) return;
        
        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(0f, timeRemaining);
        
        // Update UI
        if (bossFightUI != null)
        {
            bossFightUI.UpdateTimer(timeRemaining, timeLimit);
        }
        
        // Check if time ran out
        if (timeRemaining <= 0f && currentBoss != null && !currentBoss.IsDead && !currentBoss.HasEscaped)
        {
            // Trigger boss escape
            currentBoss.Escape();
        }
    }
    
    /// <summary>
    /// Handle boss death
    /// </summary>
    private void OnBossDied(Boss boss)
    {
        if (boss != currentBoss) return;
        
        isBossFightActive = false;
        
        // Award gold
        if (currentLevelConfig != null)
        {
            int goldReward = currentLevelConfig.BossGoldReward;
            GameManager.Instance.AddCoins(goldReward);
            Debug.Log($"Boss defeated! Awarded {goldReward} gold.");
        }
        
        // Wait for death animation, then show level complete
        StartCoroutine(EndBossFightSequence(true));
    }
    
    /// <summary>
    /// Handle boss escape
    /// </summary>
    private void OnBossEscaped(Boss boss)
    {
        if (boss != currentBoss) return;
        
        isBossFightActive = false;
        
        Debug.Log("Boss escaped! Time ran out.");
        
        // Wait for escape animation, then show level complete
        StartCoroutine(EndBossFightSequence(false));
    }
    
    /// <summary>
    /// End boss fight sequence: show level complete screen directly (no camera return animation)
    /// </summary>
    private IEnumerator EndBossFightSequence(bool bossDefeated)
    {
        // Wait a bit for animations
        yield return new WaitForSeconds(2f);
        
        // Stop boss fight music and resume ambient music
        StopBossFightMusic();
        
        // Hide fight UI
        if (bossFightUI != null)
        {
            bossFightUI.HideFightUI();
        }
        
        // Unsubscribe from boss events
        Boss.OnBossDied -= OnBossDied;
        Boss.OnBossEscaped -= OnBossEscaped;
        
        // Show level complete screen directly (no camera return, no map restoration)
        GameController gameController = GameController.Instance;
        if (gameController != null)
        {
            gameController.ShowLevelCompleteScreen();
        }
        
        // Reset state
        currentBoss = null;
        currentLevel = null;
        currentLevelConfig = null;
    }
    
    /// <summary>
    /// Play boss fight alarm sound
    /// </summary>
    private void PlayBossFightAlarmSound()
    {
        if (bossFightAlarmSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bossFightAlarmSound);
        }
    }
    
    /// <summary>
    /// Start playing boss fight music (replaces ambient music)
    /// </summary>
    private void StartBossFightMusic()
    {
        // Stop ambient music
        AmbienceSound ambienceSound = AmbienceSound.Instance;
        if (ambienceSound != null)
        {
            ambienceSound.StopAmbience();
        }
        
        // Play boss fight music
        if (bossFightMusic != null && musicAudioSource != null)
        {
            musicAudioSource.clip = bossFightMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
            Debug.Log("Boss fight music started");
        }
        else if (bossFightMusic == null)
        {
            Debug.LogWarning("BossFightManager: Boss fight music clip not assigned!");
        }
    }
    
    /// <summary>
    /// Stop boss fight music and resume ambient music
    /// </summary>
    private void StopBossFightMusic()
    {
        // Stop boss fight music
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
            Debug.Log("Boss fight music stopped");
        }
        
        // Resume ambient music
        AmbienceSound ambienceSound = AmbienceSound.Instance;
        if (ambienceSound != null)
        {
            ambienceSound.PlayAmbience();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        Boss.OnBossDied -= OnBossDied;
        Boss.OnBossEscaped -= OnBossEscaped;
        
        // Stop fade coroutine if running
        if (fadeAwayCoroutine != null)
        {
            StopCoroutine(fadeAwayCoroutine);
        }
        
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
