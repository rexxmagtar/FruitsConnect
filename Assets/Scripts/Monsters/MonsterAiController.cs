using UnityEngine;

/// <summary>
/// Controls monster animations using Animator triggers
/// </summary>
[RequireComponent(typeof(Animator))]
public class MonsterAiController : MonoBehaviour
{
    [Header("Animator Reference")]
    [SerializeField] private Animator animator;
    
    [Header("Animation Triggers")]

    private const string TRIGGER_FALLING_DOWN = "FallingDown";
    private const string TRIGGER_GET_HIT = "GetHit";
    private const string TRIGGER_ATTACK = "Attack";
    private const string TRIGGER_DIE = "Die";
    
    private void Awake()
    {
        // Get Animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogError("MonsterAiController: Animator component not found!");
        }
    }
    
    /// <summary>
    /// Set running animation (when moving toward target)
    /// </summary>
    public void SetRunning()
    {
        if (animator != null)
        {
            // Reset other triggers
            animator.ResetTrigger(TRIGGER_FALLING_DOWN);
            
            // Note: "Running" appears to be a state, not a trigger in the controller
            // We'll use SetBool or let the animator handle state transitions
            // If it's a trigger, uncomment below:
            // animator.SetTrigger(TRIGGER_RUNNING);
        }
    }
    
    /// <summary>
    /// Trigger falling down animation (when monster reaches target and captures it)
    /// </summary>
    public void TriggerFallingDown()
    {
        if (animator != null)
        {
            Debug.Log("MonsterAiController: Triggering falling down animation");
            animator.SetTrigger(TRIGGER_FALLING_DOWN);
        }
    }
    
    
    /// <summary>
    /// Trigger get hit animation (when monster takes damage)
    /// </summary>
    public void TriggerGetHit()
    {
        if (animator != null)
        {
            Debug.Log("MonsterAiController: Triggering get hit animation");
            animator.SetTrigger(TRIGGER_GET_HIT);
        }
    }
    
    /// <summary>
    /// Trigger attack animation
    /// </summary>
    public void TriggerAttack()
    {
        if (animator != null)
        {
            Debug.Log("MonsterAiController: Triggering attack animation");
            animator.SetTrigger(TRIGGER_ATTACK);
        }
    }
    
    /// <summary>
    /// Trigger die animation
    /// </summary>
    public void TriggerDie()
    {
        if (animator != null)
        {
            Debug.Log("MonsterAiController: Triggering die animation");
            animator.SetTrigger(TRIGGER_DIE);
        }
    }
    
    /// <summary>
    /// Check if animator is in a specific state
    /// </summary>
    public bool IsInState(string stateName)
    {
        if (animator == null) return false;
        
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }
    
    /// <summary>
    /// Check if animator is currently playing an attack animation
    /// </summary>
    public bool IsAttacking()
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Attack") || stateInfo.IsName("attack") || 
               stateInfo.IsName("Basic_Jump") || stateInfo.IsName("Electrocution_Reaction");
    }
    
    /// <summary>
    /// Check if animator is currently in a dying state
    /// </summary>
    public bool IsDying()
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Die") || stateInfo.IsName("die") || 
               stateInfo.IsName("Dying") || stateInfo.IsName("dying") ||
               stateInfo.IsName("Death") || stateInfo.IsName("death");
    }
    
    /// <summary>
    /// Get the normalized time of the current animation (0-1)
    /// </summary>
    public float GetCurrentAnimationTime()
    {
        if (animator == null) return 0f;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime;
    }
}
