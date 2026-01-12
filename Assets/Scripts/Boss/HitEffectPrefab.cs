using UnityEngine;
using System.Collections;

/// <summary>
/// Component attached to hit effect prefabs to handle playback
/// </summary>
public class HitEffectPrefab : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("How long the effect should play before auto-deactivating")]
    [SerializeField] private float duration = 1f;
    
    [Header("Component References")]
    [Tooltip("Particle systems to play when effect starts")]
    [SerializeField] private ParticleSystem[] particleSystems;
    
    private Coroutine playCoroutine;
    private BossHitEffectPool pool;
    
    private void Awake()
    {
        // Auto-find particle systems if not assigned
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }
    }
    
    /// <summary>
    /// Set the pool reference for returning effects
    /// </summary>
    public void SetPool(BossHitEffectPool poolRef)
    {
        pool = poolRef;
    }
    
    /// <summary>
    /// Play the hit effect - activates, plays animations/particles, then auto-deactivates
    /// </summary>
    public void Play()
    {
        // Stop any existing play coroutine
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
        }
        
        // Activate GameObject
        gameObject.SetActive(true);
        
        // Play particle systems
        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }
        }
        
        // Start auto-deactivate coroutine
        playCoroutine = StartCoroutine(PlayCoroutine());
    }
    
    /// <summary>
    /// Coroutine that waits for duration then deactivates the effect
    /// </summary>
    private IEnumerator PlayCoroutine()
    {
        yield return new WaitForSeconds(duration);
        
        // Return to pool or deactivate
        if (pool != null)
        {
            pool.ReturnToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
        
        playCoroutine = null;
    }
    
    /// <summary>
    /// Reset effect state when returned to pool
    /// </summary>
    public void ResetEffect()
    {
        // Stop particle systems
        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Stop();
                    ps.Clear();
                }
            }
        }
        
        // Stop coroutine if running
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
    }
}
