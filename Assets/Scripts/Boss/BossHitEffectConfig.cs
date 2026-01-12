using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Configuration for boss hit effects - stores references to all hit effect prefabs
/// </summary>
[CreateAssetMenu(fileName = "BossHitEffectConfig", menuName = "Fruit Connect/Boss Hit Effect Config")]
public class BossHitEffectConfig : ScriptableObject
{
    [Header("Hit Effect Prefabs")]
    [Tooltip("List of hit effect prefabs that can be spawned when boss is hit")]
    [SerializeField] private List<GameObject> hitEffectPrefabs = new List<GameObject>();
    
    /// <summary>
    /// Get all hit effect prefabs
    /// </summary>
    public List<GameObject> HitEffectPrefabs => hitEffectPrefabs;
    
    /// <summary>
    /// Get count of available prefabs
    /// </summary>
    public int PrefabCount => hitEffectPrefabs != null ? hitEffectPrefabs.Count : 0;
    
    /// <summary>
    /// Get a random prefab from the list
    /// </summary>
    public GameObject GetRandomPrefab()
    {
        if (hitEffectPrefabs == null || hitEffectPrefabs.Count == 0)
        {
            Debug.LogWarning("BossHitEffectConfig: No hit effect prefabs assigned!");
            return null;
        }
        
        return hitEffectPrefabs[Random.Range(0, hitEffectPrefabs.Count)];
    }
}
