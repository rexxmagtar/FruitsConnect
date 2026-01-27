using UnityEngine;

/// <summary>
/// Forwards mouse/tap events from a child collider to the main Boss script.
/// Attach this to any child object of the Boss that has its own collider.
/// </summary>
public class BossHitbox : MonoBehaviour
{
    [SerializeField] private Boss boss;

    private void Awake()
    {
        // Auto-find boss in parents if not assigned
        if (boss == null)
        {
            boss = GetComponentInParent<Boss>();
        }
        
        // Ensure this object has a collider
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"BossHitbox on {gameObject.name} missing Collider! Hit detection won't work.");
        }
    }

    /// <summary>
    /// Called when the user clicks or taps on this specific collider
    /// </summary>
    private void OnMouseDown()
    {
        if (boss != null)
        {
            // Forward the hit event to the main boss script
            boss.OnMouseDown();
        }
    }
}
