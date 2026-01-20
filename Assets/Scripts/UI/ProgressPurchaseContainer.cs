using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI container for displaying and purchasing upgrades
/// Shows power value, level, steps progress, and purchase button
/// </summary>
public class ProgressPurchaseContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI nextUpgradePowerImproveValue;
    [SerializeField] private Slider stepsProgressSlider;
    [SerializeField] private TextMeshProUGUI paramNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Image[] imagesToGrayWhenCantAfford;
    
    [Header("Display Format")]
    [SerializeField] private string powerFormat = "Power: {0}";
    [SerializeField] private string levelFormat = "Level {0}";
    [SerializeField] private string improveValueFormat = "+{0}";
    [SerializeField] private string priceFormat = "{0} coins";
    
    [Header("Audio")]
    [SerializeField] private AudioClip stepPurchaseSound;
    [SerializeField] private AudioClip levelPurchaseSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem levelUpParticles;
    
    [Header("Animation Settings")]
    [SerializeField] private float sliderAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve sliderAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color grayedOutColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    private UpgradableParam currentParam;
    private Coroutine sliderAnimationCoroutine;
    private Color[] originalImageColors; // Store original colors for restoration
    
    private void Awake()
    {
        // Setup button click handler
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseButtonClick);
        }
        
        // Setup slider
        if (stepsProgressSlider != null)
        {
            stepsProgressSlider.minValue = 0f;
            stepsProgressSlider.maxValue = 4f;
        }
        
        // Get or add AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        
        // Initialize animation curve if not set
        if (sliderAnimationCurve == null || sliderAnimationCurve.keys.Length == 0)
        {
            sliderAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
        
        // Store original colors of images before any modifications
        StoreOriginalImageColors();
    }
    
    /// <summary>
    /// Store the original colors of all images that will be grayed out
    /// This must be called before any color modifications
    /// </summary>
    private void StoreOriginalImageColors()
    {
        if (imagesToGrayWhenCantAfford == null || imagesToGrayWhenCantAfford.Length == 0) return;
        
        originalImageColors = new Color[imagesToGrayWhenCantAfford.Length];
        for (int i = 0; i < imagesToGrayWhenCantAfford.Length; i++)
        {
            if (imagesToGrayWhenCantAfford[i] != null)
            {
                originalImageColors[i] = imagesToGrayWhenCantAfford[i].color;
            }
        }
    }
    
    /// <summary>
    /// Initialize container with upgradable parameter
    /// </summary>
    public void Initialize(UpgradableParam param)
    {
        currentParam = param;
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update all UI elements
    /// </summary>
    public void UpdateDisplay()
    {
        if (currentParam == null) return;
        
        // Update power text
        if (powerText != null)
        {
            powerText.text = string.Format(powerFormat, currentParam.PowerValue);
        }

        // Update power improvement text
        UpdatePowerImprovementText();
        
        // Update level text
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, currentParam.Level);
        }
        
        // Update steps progress slider (without animation on initial display)
        if (stepsProgressSlider != null)
        {
            // Only set directly if not animating
            if (sliderAnimationCoroutine == null)
            {
                stepsProgressSlider.value = currentParam.CurrentLevelStep;
            }
        }
        
        // Update parameter name
        if (paramNameText != null && currentParam.Config != null)
        {
            paramNameText.text = currentParam.Config.ParamName;
        }
        
        // Update price text
        if (priceText != null)
        {
            int price = currentParam.GetUpgradePrice();
            priceText.text = string.Format(priceFormat, price);
        }
        
        // Update button interactability
        bool canAfford = currentParam.CanPurchaseUpgrade();
        if (purchaseButton != null)
        {
            purchaseButton.interactable = canAfford;
        }
        
        // Update image colors based on affordability
        UpdateImageColors(canAfford);
    }
    
    /// <summary>
    /// Update image colors based on whether upgrade can be afforded
    /// </summary>
    private void UpdateImageColors(bool canAfford)
    {
        if (imagesToGrayWhenCantAfford == null || imagesToGrayWhenCantAfford.Length == 0) return;
        
        // Ensure original colors are stored (safety check in case Awake wasn't called yet)
        if (originalImageColors == null || originalImageColors.Length != imagesToGrayWhenCantAfford.Length)
        {
            StoreOriginalImageColors();
        }
        
        Color targetColor = canAfford ? normalColor : grayedOutColor;
        
        for (int i = 0; i < imagesToGrayWhenCantAfford.Length; i++)
        {
            if (imagesToGrayWhenCantAfford[i] != null)
            {
                // If can afford, restore original color, otherwise use grayed out color
                if (canAfford && originalImageColors != null && i < originalImageColors.Length)
                {
                    imagesToGrayWhenCantAfford[i].color = originalImageColors[i];
                }
                else
                {
                    imagesToGrayWhenCantAfford[i].color = targetColor;
                }
            }
        }
    }

    /// <summary>
    /// Update the text showing the power increase for the next upgrade
    /// </summary>
    private void UpdatePowerImprovementText()
    {
        if (nextUpgradePowerImproveValue != null && currentParam != null)
        {
            int nextIncrement = currentParam.GetNextPowerIncrement();
            nextUpgradePowerImproveValue.text = string.Format(improveValueFormat, nextIncrement);
        }
    }
    
    /// <summary>
    /// Handle purchase button click
    /// </summary>
    private void OnPurchaseButtonClick()
    {
        if (currentParam == null)
        {
            Debug.LogWarning("ProgressPurchaseContainer: No parameter assigned!");
            return;
        }
        
        PlayerProgressController controller = PlayerProgressController.Instance;
        if (controller == null)
        {
            Debug.LogError("ProgressPurchaseContainer: PlayerProgressController not found!");
            return;
        }
        
        // Store current step to detect level completion
        int previousStep = currentParam.CurrentLevelStep;
        bool wasLevelCompletion = (previousStep == 3);
        
        // Attempt purchase
        bool success = controller.PurchaseUpgrade(currentParam);
        
        if (success)
        {
            // Refresh display after successful purchase with level completion flag
            RefreshAfterPurchase(wasLevelCompletion);
        }
        else
        {
            Debug.Log("ProgressPurchaseContainer: Purchase failed - not enough coins or other error");
            // Still update display to reflect button state
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Refresh UI after successful purchase
    /// </summary>
    public void RefreshAfterPurchase(bool isLevelCompletion = false)
    {
        if (currentParam == null) return;
        
        // Update power text
        if (powerText != null)
        {
            powerText.text = string.Format(powerFormat, currentParam.PowerValue);
        }

        // Update power improvement text
        UpdatePowerImprovementText();
        
        // Update level text
        if (levelText != null)
        {
            levelText.text = string.Format(levelFormat, currentParam.Level);
        }
        
        // Update price text
        if (priceText != null)
        {
            int price = currentParam.GetUpgradePrice();
            priceText.text = string.Format(priceFormat, price);
        }
        
        // Update button interactability and image colors
        bool canAfford = currentParam.CanPurchaseUpgrade();
        if (purchaseButton != null)
        {
            purchaseButton.interactable = canAfford;
        }
        
        // Update image colors based on affordability
        UpdateImageColors(canAfford);
        
        // Animate slider smoothly to new step value
        if (stepsProgressSlider != null)
        {
            AnimateSliderToValue(currentParam.CurrentLevelStep, isLevelCompletion);
        }
        
        // Play sound based on purchase type
        PlayPurchaseSound(isLevelCompletion);
        
        // Play particles if level completion
        if (isLevelCompletion)
        {
            PlayLevelUpParticles();
        }
    }
    
    /// <summary>
    /// Animate slider smoothly to target value
    /// </summary>
    private void AnimateSliderToValue(float targetValue, bool isLevelCompletion = false)
    {
        if (stepsProgressSlider == null) return;
        
        // Stop any existing animation
        if (sliderAnimationCoroutine != null)
        {
            StopCoroutine(sliderAnimationCoroutine);
        }
        
        sliderAnimationCoroutine = StartCoroutine(AnimateSliderCoroutine(targetValue, isLevelCompletion));
    }
    
    /// <summary>
    /// Coroutine to animate slider smoothly
    /// </summary>
    private IEnumerator AnimateSliderCoroutine(float targetValue, bool isLevelCompletion)
    {
        float startValue = stepsProgressSlider.value;
        float elapsedTime = 0f;
        
        if (isLevelCompletion)
        {
            // Disable button during level completion animation
            if (purchaseButton != null)
            {
                purchaseButton.interactable = false;
            }

            // Animate to max value first
            float maxValue = stepsProgressSlider.maxValue;
            while (elapsedTime < sliderAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / sliderAnimationDuration;
                float curveValue = sliderAnimationCurve.Evaluate(t);
                
                float currentValue = Mathf.Lerp(startValue, maxValue, curveValue);
                stepsProgressSlider.value = currentValue;
                
                yield return null;
            }
            
            // Snap to zero (the new target value)
            stepsProgressSlider.value = targetValue;
            
            // Re-enable button based on current affordability after animation completes
            UpdateAffordability();
        }
        else
        {
            while (elapsedTime < sliderAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / sliderAnimationDuration;
                float curveValue = sliderAnimationCurve.Evaluate(t);
                
                float currentValue = Mathf.Lerp(startValue, targetValue, curveValue);
                stepsProgressSlider.value = currentValue;
                
                yield return null;
            }
            
            // Ensure final value is set
            stepsProgressSlider.value = targetValue;
        }
        
        sliderAnimationCoroutine = null;
    }
    
    /// <summary>
    /// Play purchase sound based on purchase type
    /// </summary>
    private void PlayPurchaseSound(bool isLevelCompletion)
    {
        if (audioSource == null) return;
        
        AudioClip clipToPlay = isLevelCompletion ? levelPurchaseSound : stepPurchaseSound;
        
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }
    
    /// <summary>
    /// Play level up particle effect
    /// </summary>
    private void PlayLevelUpParticles()
    {
        if (levelUpParticles != null)
        {
            levelUpParticles.Play();
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to upgrade purchased event
        PlayerProgressController.OnUpgradePurchased += OnUpgradePurchased;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from event
        PlayerProgressController.OnUpgradePurchased -= OnUpgradePurchased;
    }
    
    /// <summary>
    /// Handle upgrade purchased event (from any parameter)
    /// </summary>
    private void OnUpgradePurchased(UpgradableParam param, bool isLevelCompletion)
    {
        // Refresh if this container's parameter was purchased
        if (param == currentParam)
        {
            RefreshAfterPurchase(isLevelCompletion);
        }
        else
        {
            // If another parameter was purchased, update affordability (balance changed)
            UpdateAffordability();
        }
    }
    
    /// <summary>
    /// Update button and image states based on current affordability
    /// Called when balance changes (e.g., after another upgrade purchase)
    /// </summary>
    public void UpdateAffordability()
    {
        if (currentParam == null) return;
        
        bool canAfford = currentParam.CanPurchaseUpgrade();
        
        // Update button interactability
        if (purchaseButton != null)
        {
            purchaseButton.interactable = canAfford;
        }
        
        // Update image colors
        UpdateImageColors(canAfford);
    }
}
