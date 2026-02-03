using UnityEngine;
using DG.Tweening;

public class BaseObjectStage : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private string progressPropertyName = "_Progress";
    [SerializeField] private string minHeightPropertyName = "_MinHeight";
    [SerializeField] private string maxHeightPropertyName = "_MaxHeight";
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem buildParticles;
    [SerializeField] private ParticleSystem completionParticles;
    [SerializeField] private AudioClip buildSound;
    [SerializeField] private AudioClip completionSound;

    [Header("Price UI")]
    [SerializeField] private GameObject priceContainer;
    [SerializeField] private TMPro.TextMeshProUGUI priceText;
    
    private Material stageMaterial;
    private AudioSource audioSource;
    private float currentDisplayedProgress = 0f;
    private DG.Tweening.Tween progressTween;
    private Transform mainCameraTransform;
    private Vector3 originalScale;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        
        // Store original scale for animation
        originalScale = transform.localScale;
    }

    public void SetCamera(Camera camera)
    {
        if (camera != null)
        {
            mainCameraTransform = camera.transform;
        }
    }

    private void LateUpdate()
    {
        if (priceContainer != null && priceContainer.activeInHierarchy && mainCameraTransform != null)
        {
            // Look at the camera so the UI is always flat to the screen
            priceContainer.transform.LookAt(priceContainer.transform.position + mainCameraTransform.forward);
        }
    }

    // InitializeMaterial is no longer needed as we update in SetProgress
    // but we can keep it for an initial state if desired.
    private void Start()
    {
        // Removed SetProgress(0f) as it was overriding the initial state set by BaseBuildingUI
    }

    public void SetPriceUIActive(bool active)
    {
        if (priceContainer != null)
        {
            priceContainer.SetActive(active);
        }
    }

    public void UpdatePriceText(int remainingPrice)
    {
        if (priceText != null)
        {
            priceText.text = remainingPrice.ToString();
        }
    }

    /// <summary>
    /// Set the building progress for this stage (0 to 1)
    /// </summary>
    /// <param name="targetProgress">Target progress value</param>
    /// <param name="immediate">If true, skips animation</param>
    /// <param name="duration">Duration of the fill animation</param>
    public void SetProgress(float targetProgress, bool immediate = false, float duration = 0.5f)
    {
        if (meshRenderer != null)
        {
            if (stageMaterial == null) stageMaterial = meshRenderer.material;

            // Always refresh world-space bounds right before setting progress
            Bounds bounds = meshRenderer.bounds;
            stageMaterial.SetFloat(minHeightPropertyName, bounds.min.y - 0.01f);
            stageMaterial.SetFloat(maxHeightPropertyName, bounds.max.y + 0.01f);

            float previousProgress = currentDisplayedProgress;
            
            if (immediate)
            {
                progressTween?.Kill();
                currentDisplayedProgress = targetProgress;
                stageMaterial.SetFloat(progressPropertyName, currentDisplayedProgress);
            }
            else
            {
                progressTween?.Kill();
                progressTween = DOTween.To(() => currentDisplayedProgress, x => {
                    currentDisplayedProgress = x;
                    if (stageMaterial != null) stageMaterial.SetFloat(progressPropertyName, currentDisplayedProgress);
                }, targetProgress, duration).SetEase(Ease.OutQuad);
                
                // Handle completion sound and animation
                // Only trigger when progress transitions from less than 1.0 to 1.0 during gameplay (not initialization)
                // Only check when immediate=false to ensure it only happens when actually building, not when loading
                // Note: Completion particles are handled separately to always play on fully built stages
                if (targetProgress >= 1f && previousProgress < 1f)
                {
                    if (completionSound != null)
                    {
                        audioSource.PlayOneShot(completionSound);
                    }
                    
                    // Play scale up and down animation when stage completes
                    PlayCompletionScaleAnimation();
                }
            }
        }

        // Handle particles during building
        // Start particles as soon as stage becomes current (even at 0% progress)
        if (targetProgress >= 0 && targetProgress < 1f && gameObject.activeInHierarchy)
        {
            if (buildParticles != null && !buildParticles.isPlaying)
            {
                buildParticles.Play();
            }
            // Stop completion particles when building
            if (completionParticles != null && completionParticles.isPlaying)
            {
                completionParticles.Stop();
            }
        }
        else if (targetProgress >= 1f)
        {
            // Stop build particles when completed
            if (buildParticles != null && buildParticles.isPlaying)
            {
                buildParticles.Stop();
            }
            // Always play completion particles on fully built stages
            if (completionParticles != null && !completionParticles.isPlaying)
            {
                completionParticles.Play();
            }
        }
        else
        {
            // Stop all particles when stage is not active
            if (buildParticles != null && buildParticles.isPlaying)
            {
                buildParticles.Stop();
            }
            if (completionParticles != null && completionParticles.isPlaying)
            {
                completionParticles.Stop();
            }
        }
    }
    
    private void PlayCompletionScaleAnimation()
    {
        float scaleUpAmount = 1.15f;
        float scaleUpDuration = 0.3f;
        float scaleDownDuration = 0.2f;
        
        Sequence scaleSequence = DOTween.Sequence();
        scaleSequence.Append(transform.DOScale(originalScale * scaleUpAmount, scaleUpDuration).SetEase(Ease.OutQuad));
        scaleSequence.Append(transform.DOScale(originalScale, scaleDownDuration).SetEase(Ease.InQuad));
    }

    public void PlayBuildEffect()
    {
        if (buildParticles != null && !buildParticles.isPlaying)
        {
            buildParticles.Play();
        }
        
        if (buildSound != null && !audioSource.isPlaying)
        {
            // Note: In a real implementation we might want to loop or handle this differently
            // but for now let's just trigger it.
            audioSource.PlayOneShot(buildSound);
        }
    }
}
