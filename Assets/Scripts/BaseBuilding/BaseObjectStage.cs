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
    
    private Material stageMaterial;
    private AudioSource audioSource;
    private float currentDisplayedProgress = 0f;
    private DG.Tweening.Tween progressTween;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // InitializeMaterial is no longer needed as we update in SetProgress
    // but we can keep it for an initial state if desired.
    private void Start()
    {
        // Removed SetProgress(0f) as it was overriding the initial state set by BaseBuildingUI
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
            }
        }

        // Handle particles during building
        if (targetProgress > 0 && targetProgress < 1f)
        {
            if (buildParticles != null && !buildParticles.isPlaying)
            {
                buildParticles.Play();
            }
        }
        else
        {
            if (buildParticles != null && buildParticles.isPlaying)
            {
                buildParticles.Stop();
            }
        }

        // Handle completion particles
        if (targetProgress >= 1f && completionParticles != null)
        {
            completionParticles.Play();
            if (completionSound != null)
            {
                audioSource.PlayOneShot(completionSound);
            }
        }
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
