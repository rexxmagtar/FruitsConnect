using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space healthbar displayed above boss showing health tiles
/// Similar to MonsterHealthBar but works with Boss
/// </summary>
[RequireComponent(typeof(Canvas))]
public class BossHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slider healthSlider;
    
    [SerializeField] private Boss boss;
    private float maxHealth;
    
    private void Awake()
    {
        // Get canvas if not assigned
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }
        
        // Setup canvas as world space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }
        
    }
    
    private void Start()
    {
        if (boss != null)
        {
            Initialize(boss);
        }
        else
        {
            Debug.LogError("BossHealthBar: Boss reference not found!");
        }
    }
    
    /// <summary>
    /// Initialize healthbar with boss reference
    /// </summary>
    public void Initialize(Boss bossRef)
    {
        boss = bossRef;
        
        if (boss == null)
        {
            Debug.LogError("BossHealthBar: Cannot initialize with null boss!");
            return;
        }
        
        maxHealth = boss.MaxHealth;
        
        // Setup slider if not assigned
        if (healthSlider == null)
        {
            CreateHealthSlider();
        }
        
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = maxHealth;
        }
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Create health slider if not assigned
    /// </summary>
    private void CreateHealthSlider()
    {
        // Try to find existing slider
        healthSlider = GetComponentInChildren<Slider>();
        
        if (healthSlider == null)
        {
            // Create slider GameObject
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(transform);
            sliderObj.transform.localPosition = Vector3.zero;
            
            // Add RectTransform
            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 0.2f);
            rect.anchoredPosition = Vector2.zero;
            
            // Add Slider component
            healthSlider = sliderObj.AddComponent<Slider>();
            
            // Create background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            healthSlider.targetGraphic = bgImage;
            
            // Create fill area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            fillAreaRect.anchoredPosition = Vector2.zero;
            
            // Create fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = Color.red;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            healthSlider.fillRect = fillRect;
        }
    }
    
    /// <summary>
    /// Update healthbar display based on current health
    /// </summary>
    public void UpdateDisplay()
    {
        if (boss == null || healthSlider == null) return;
        
        float currentHealth = boss.CurrentHealth;
        healthSlider.value = currentHealth;
        
        // Hide healthbar when health reaches zero
        if (currentHealth <= 0)
        {
            Hide();
        }
    }
    
    /// <summary>
    /// Hide the healthbar
    /// </summary>
    public void Hide()
    {
        if (canvas != null)
        {
            canvas.enabled = false;
        }
        else if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }
    
    private void LateUpdate()
    {
        // Update position to stay above boss
        if (boss != null)
        {
            // Face camera
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup is handled automatically by Unity
    }
}
