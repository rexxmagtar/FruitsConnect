using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages object pooling for boss hit effects
/// </summary>
public class BossHitEffectPool : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Config containing all hit effect prefabs")]
    [SerializeField] private BossHitEffectConfig config;
    
    [Tooltip("Number of instances to create per prefab type")]
    [SerializeField] private int poolSizePerPrefab = 3;
    
    // Pool structure: Dictionary mapping prefab to list of pooled instances
    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
    
    // Track which instances are currently active
    private Dictionary<GameObject, GameObject> activeInstances = new Dictionary<GameObject, GameObject>();
    
    // Container for pooled objects
    private Transform poolContainer;
    
    // Singleton instance
    private static BossHitEffectPool _instance;
    public static BossHitEffectPool Instance => _instance;
    
    private void Awake()
    {
        // Singleton setup
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Create pool container
        if (poolContainer == null)
        {
            GameObject containerObj = new GameObject("HitEffectPoolContainer");
            containerObj.transform.SetParent(transform);
            poolContainer = containerObj.transform;
        }

        Initialize();
    }
    
    /// <summary>
    /// Initialize pools - creates 3 instances of each prefab from config
    /// </summary>
    public void Initialize()
    {
        if (config == null)
        {
            Debug.LogError("BossHitEffectPool: Config is not assigned! Cannot initialize pool.");
            return;
        }
        
        if (config.PrefabCount == 0)
        {
            Debug.LogWarning("BossHitEffectPool: Config has no prefabs assigned!");
            return;
        }
        
        // Clear existing pools
        ClearPools();
        
        // Create pool for each prefab
        foreach (GameObject prefab in config.HitEffectPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning("BossHitEffectPool: Found null prefab in config, skipping.");
                continue;
            }
            
            List<GameObject> pool = new List<GameObject>();
            
            // Create poolSizePerPrefab instances
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject instance = Instantiate(prefab, poolContainer);
                instance.name = $"{prefab.name}_Pooled_{i}";
                instance.SetActive(false);
                
                // Set pool reference on HitEffectPrefab component
                HitEffectPrefab hitEffect = instance.GetComponent<HitEffectPrefab>();
                if (hitEffect != null)
                {
                    hitEffect.SetPool(this);
                }
                
                pool.Add(instance);
            }
            
            pools[prefab] = pool;
        }
        
        Debug.Log($"BossHitEffectPool: Initialized {pools.Count} pools with {poolSizePerPrefab} instances each.");
    }
    
    /// <summary>
    /// Get a random available effect from pool and activate it
    /// </summary>
    public GameObject GetRandomEffect()
    {
        if (pools.Count == 0)
        {
            Debug.LogWarning("BossHitEffectPool: Pools not initialized! Call Initialize() first.");
            return null;
        }
        
        // Get all available prefabs
        List<GameObject> availablePrefabs = pools.Keys.ToList();
        if (availablePrefabs.Count == 0)
        {
            Debug.LogWarning("BossHitEffectPool: No prefabs available in pools!");
            return null;
        }
        
        // Pick a random prefab
        GameObject randomPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
        
        // Get an available instance from that prefab's pool
        return GetEffectFromPool(randomPrefab);
    }
    
    /// <summary>
    /// Get an effect from a specific prefab's pool
    /// </summary>
    private GameObject GetEffectFromPool(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"BossHitEffectPool: No pool found for prefab {prefab.name}");
            return null;
        }
        
        List<GameObject> pool = pools[prefab];
        
        // Find first inactive instance
        GameObject availableInstance = pool.FirstOrDefault(instance => instance != null && !instance.activeSelf);
        
        if (availableInstance == null)
        {
            // All instances are active, create a new one (expand pool dynamically)
            Debug.LogWarning($"BossHitEffectPool: All instances of {prefab.name} are active, creating additional instance.");
            availableInstance = Instantiate(prefab, poolContainer);
            availableInstance.name = $"{prefab.name}_Pooled_Dynamic_{pool.Count}";
            
            HitEffectPrefab hitEffect = availableInstance.GetComponent<HitEffectPrefab>();
            if (hitEffect != null)
            {
                hitEffect.SetPool(this);
            }
            
            pool.Add(availableInstance);
        }
        
        // Track active instance
        activeInstances[availableInstance] = prefab;
        
        return availableInstance;
    }
    
    /// <summary>
    /// Return an effect to the pool (deactivate and reset)
    /// </summary>
    public void ReturnToPool(GameObject effect)
    {
        if (effect == null) return;
        
        // Reset effect state
        HitEffectPrefab hitEffect = effect.GetComponent<HitEffectPrefab>();
        if (hitEffect != null)
        {
            hitEffect.ResetEffect();
        }
        
        // Deactivate
        effect.SetActive(false);
        
        // Remove from active tracking
        if (activeInstances.ContainsKey(effect))
        {
            activeInstances.Remove(effect);
        }
    }
    
    /// <summary>
    /// Clear all pools
    /// </summary>
    private void ClearPools()
    {
        foreach (var pool in pools.Values)
        {
            foreach (var instance in pool)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }
        
        pools.Clear();
        activeInstances.Clear();
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        
        ClearPools();
    }
}
