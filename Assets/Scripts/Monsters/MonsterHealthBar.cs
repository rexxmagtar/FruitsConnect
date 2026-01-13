using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space healthbar displayed above monster showing health tiles
/// </summary>
[RequireComponent(typeof(Canvas))]
public class MonsterHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slider healthSlider;
    
    private Monster monster;
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
        
        // Get monster reference from parent
        if (monster == null)
        {
            monster = GetComponentInParent<Monster>();
        }
    }
    
    private void Start()
    {
        if (monster != null)
        {
            Initialize(monster);
        }
        else
        {
            Debug.LogError("MonsterHealthBar: Monster reference not found!");
        }
    }
    
    /// <summary>
    /// Initialize healthbar with monster reference
    /// </summary>
    public void Initialize(Monster monsterRef)
    {
        monster = monsterRef;
        
        if (monster == null)
        {
            Debug.LogError("MonsterHealthBar: Cannot initialize with null monster!");
            return;
        }
        
        maxHealth = monster.MaxHealth;
        
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
        if (monster == null || healthSlider == null) return;
        
        float currentHealth = monster.CurrentHealth;
        healthSlider.value = currentHealth;
    }
    
    private void LateUpdate()
    {
        // Update position to stay above monster
        if (monster != null)
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
