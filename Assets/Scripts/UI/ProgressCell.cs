using UnityEngine;
using DG.Tweening;

/// <summary>
/// Cell state enumeration
/// </summary>
public enum CellState
{
    Complete,
    Current,
    Incomplete
}

/// <summary>
/// Base class for progress cells that display level completion status
/// Manages 3 visual states: Complete, Current, Incomplete
/// Current state includes animated scale pulse effect
/// </summary>
public class ProgressCell : MonoBehaviour
{
    [Header("State GameObjects")]
    [Tooltip("GameObject shown when level is complete")]
    [SerializeField] protected GameObject completeStateObject;
    
    [Tooltip("GameObject shown when this is the current level")]
    [SerializeField] protected GameObject currentStateObject;
    
    [Tooltip("GameObject shown when level is incomplete")]
    [SerializeField] protected GameObject incompleteStateObject;
    
    [Header("Animation Settings")]
    [Tooltip("Duration of each fast pulse in seconds")]
    [SerializeField] protected float fastPulseDuration = 0.2f;
    
    [Tooltip("Duration of slow pulse in seconds")]
    [SerializeField] protected float slowPulseDuration = 0.8f;
    
    [Tooltip("Scale multiplier during pulse (1.0 = normal, 1.15 = 15% larger)")]
    [SerializeField] protected float pulseScale = 1.15f;
    
    // Current state
    protected CellState currentState = CellState.Incomplete;
    
    // Animation tracking
    protected Sequence pulseSequence;
    
    /// <summary>
    /// Set the cell to a specific state
    /// </summary>
    public void SetState(CellState state)
    {
        // Stop any existing animation
        StopAnimation();
        
        currentState = state;
        
        // Deactivate all state objects
        if (completeStateObject != null)
            completeStateObject.SetActive(false);
        if (currentStateObject != null)
            currentStateObject.SetActive(false);
        if (incompleteStateObject != null)
            incompleteStateObject.SetActive(false);
        
        // Activate the appropriate state object
        switch (state)
        {
            case CellState.Complete:
                if (completeStateObject != null)
                    completeStateObject.SetActive(true);
                break;
                
            case CellState.Current:
                if (currentStateObject != null)
                    currentStateObject.SetActive(true);
                PlayCurrentAnimation();
                break;
                
            case CellState.Incomplete:
                if (incompleteStateObject != null)
                    incompleteStateObject.SetActive(true);
                break;
        }
    }
    
    /// <summary>
    /// Play animation for current state
    /// Virtual so derived classes can override
    /// </summary>
    protected virtual void PlayCurrentAnimation()
    {
        // Apply scale to the entire ProgressCell GameObject
        Transform target = transform;
        
        // Reset scale
        target.localScale = Vector3.one;
        
        // Create pulse sequence: 3 fast pulses, then 1 slow pulse, loop
        pulseSequence = DOTween.Sequence();
        
        // Add 3 fast pulses
        for (int i = 0; i < 3; i++)
        {
            pulseSequence.Append(target.DOScale(Vector3.one * pulseScale, fastPulseDuration / 2f).SetEase(Ease.OutQuad));
            pulseSequence.Append(target.DOScale(Vector3.one, fastPulseDuration / 2f).SetEase(Ease.InQuad));
        }
        
        // Add 1 slow pulse
        pulseSequence.Append(target.DOScale(Vector3.one * pulseScale, slowPulseDuration / 2f).SetEase(Ease.OutQuad));
        pulseSequence.Append(target.DOScale(Vector3.one, slowPulseDuration / 2f).SetEase(Ease.InQuad));
        
        // Loop infinitely
        pulseSequence.SetLoops(-1, LoopType.Restart);
    }
    
    /// <summary>
    /// Stop and cleanup animation
    /// </summary>
    protected void StopAnimation()
    {
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
            pulseSequence = null;
        }
        
        // Reset scale of the entire ProgressCell GameObject
        transform.localScale = Vector3.one;
    }
    
    /// <summary>
    /// Get current cell state
    /// </summary>
    public CellState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    protected virtual void OnDestroy()
    {
        StopAnimation();
    }
    
    /// <summary>
    /// Validation in editor
    /// </summary>
    protected virtual void OnValidate()
    {
        if (completeStateObject == null)
        {
            Debug.LogWarning($"ProgressCell on {gameObject.name}: Complete State Object not assigned!", this);
        }
        
        if (currentStateObject == null)
        {
            Debug.LogWarning($"ProgressCell on {gameObject.name}: Current State Object not assigned!", this);
        }
        
        if (incompleteStateObject == null)
        {
            Debug.LogWarning($"ProgressCell on {gameObject.name}: Incomplete State Object not assigned!", this);
        }
    }
}
