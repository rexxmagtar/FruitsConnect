using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Derived class for boss level progress cells
/// Extends ProgressCell with additional color animation for the Current state
/// Animates Image component color from red to orange and back
/// </summary>
public class BossProgressCell : ProgressCell
{
    [Header("Boss Cell Settings")]
    [Tooltip("Image component to animate color on (should be part of Current state object)")]
    [SerializeField] private Image colorAnimationImage;
    
    [Tooltip("Starting color for animation (Red)")]
    [SerializeField] private Color colorStartRed = new Color(1f, 0f, 0f, 1f); // #FF0000
    
    [Tooltip("Target color for animation (Orange)")]
    [SerializeField] private Color colorEndOrange = new Color(1f, 0.647f, 0f, 1f); // #FFA500
    
    [Tooltip("Duration for one complete color cycle (red->orange->red)")]
    [SerializeField] private float colorAnimationDuration = 1.0f;
    
    // Color animation tracking
    private Sequence colorSequence;
    
    /// <summary>
    /// Override to add color animation on top of base pulse animation
    /// </summary>
    protected override void PlayCurrentAnimation()
    {
        // Call base class to play scale pulse animation
        base.PlayCurrentAnimation();
        
        // Add color animation if image is assigned
        if (colorAnimationImage != null)
        {
            PlayColorAnimation();
        }
        else
        {
            Debug.LogWarning($"BossProgressCell on {gameObject.name}: Color Animation Image not assigned!", this);
        }
    }
    
    /// <summary>
    /// Play color animation loop
    /// </summary>
    private void PlayColorAnimation()
    {
        if (colorAnimationImage == null) return;
        
        // Reset to start color
        colorAnimationImage.color = colorStartRed;
        
        // Create color animation sequence
        colorSequence = DOTween.Sequence();
        
        // Animate from red to orange
        colorSequence.Append(colorAnimationImage.DOColor(colorEndOrange, colorAnimationDuration / 2f).SetEase(Ease.InOutSine));
        
        // Animate from orange back to red
        colorSequence.Append(colorAnimationImage.DOColor(colorStartRed, colorAnimationDuration / 2f).SetEase(Ease.InOutSine));
        
        // Loop infinitely
        colorSequence.SetLoops(-1, LoopType.Restart);
    }
    
    /// <summary>
    /// Stop color animation
    /// </summary>
    private void StopColorAnimation()
    {
        if (colorSequence != null)
        {
            colorSequence.Kill();
            colorSequence = null;
        }
        
        // Reset color if image exists
        if (colorAnimationImage != null)
        {
            colorAnimationImage.color = colorStartRed;
        }
    }
    
    /// <summary>
    /// Override OnDestroy to cleanup color animation
    /// </summary>
    protected override void OnDestroy()
    {
        StopColorAnimation();
        base.OnDestroy();
    }
    
    /// <summary>
    /// Override to stop both scale and color animations
    /// We need to expose a way to stop animations when state changes
    /// </summary>
    public new void SetState(CellState state)
    {
        // Stop color animation when changing state
        StopColorAnimation();
        
        // Call base SetState
        base.SetState(state);
    }
    
    /// <summary>
    /// Validation in editor
    /// </summary>
    protected override void OnValidate()
    {
        base.OnValidate();
        
        if (colorAnimationImage == null)
        {
            Debug.LogWarning($"BossProgressCell on {gameObject.name}: Color Animation Image not assigned!", this);
        }
    }
}
