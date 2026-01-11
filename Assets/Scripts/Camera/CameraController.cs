using UnityEngine;
using System.Collections;

/// <summary>
/// Controls camera movement and transitions
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float transitionDuration = 1.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    private Camera mainCamera;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isTransitioning = false;
    
    // Singleton
    private static CameraController _instance;
    public static CameraController Instance => _instance;
    
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
        
        // Get main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        // Store original position and rotation
        if (mainCamera != null)
        {
            originalPosition = mainCamera.transform.position;
            originalRotation = mainCamera.transform.rotation;
        }
    }
    
    /// <summary>
    /// Move camera to a specific position and rotation
    /// </summary>
    public void MoveToPosition(Vector3 targetPosition, Vector3 targetRotation, float duration = -1f)
    {
        if (mainCamera == null)
        {
            Debug.LogError("CameraController: Main camera not found!");
            return;
        }
        
        if (isTransitioning)
        {
            StopAllCoroutines();
        }
        
        float transitionTime = duration > 0f ? duration : transitionDuration;
        StartCoroutine(MoveCameraCoroutine(targetPosition, Quaternion.Euler(targetRotation), transitionTime));
    }
    
    /// <summary>
    /// Move camera to look at a target position
    /// </summary>
    public void MoveToLookAt(Vector3 targetPosition, float distance, float height, float duration = -1f)
    {
        if (mainCamera == null)
        {
            Debug.LogError("CameraController: Main camera not found!");
            return;
        }
        
        // Calculate camera position
        Vector3 cameraPosition = targetPosition + Vector3.back * distance + Vector3.up * height;
        
        // Calculate rotation to look at target
        Vector3 direction = (targetPosition - cameraPosition).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        MoveToPosition(cameraPosition, targetRotation.eulerAngles, duration);
    }
    
    /// <summary>
    /// Restore camera to original position and rotation
    /// </summary>
    public void RestoreOriginalPosition(float duration = -1f)
    {
        if (mainCamera == null)
        {
            Debug.LogError("CameraController: Main camera not found!");
            return;
        }
        
        float transitionTime = duration > 0f ? duration : transitionDuration;
        StartCoroutine(MoveCameraCoroutine(originalPosition, originalRotation, transitionTime));
    }
    
    /// <summary>
    /// Coroutine to smoothly move camera
    /// </summary>
    private IEnumerator MoveCameraCoroutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        isTransitioning = true;
        
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float curveValue = transitionCurve.Evaluate(t);
            
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
            
            yield return null;
        }
        
        // Ensure we're exactly at target
        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
        
        isTransitioning = false;
    }
    
    /// <summary>
    /// Store current camera position as original (for restoration)
    /// </summary>
    public void StoreCurrentPositionAsOriginal()
    {
        if (mainCamera != null)
        {
            originalPosition = mainCamera.transform.position;
            originalRotation = mainCamera.transform.rotation;
        }
    }
    
    /// <summary>
    /// Check if camera is currently transitioning
    /// </summary>
    public bool IsTransitioning => isTransitioning;
    
    /// <summary>
    /// Reset camera force - stop any transitions and reset to original position immediately
    /// </summary>
    public void ResetCameraForce()
    {
        if (mainCamera == null) return;
        
        // Stop any ongoing transitions
        if (isTransitioning)
        {
            StopAllCoroutines();
            isTransitioning = false;
        }
        
        // Immediately restore to original position
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalPosition;
            mainCamera.transform.rotation = originalRotation;
        }
    }
}
