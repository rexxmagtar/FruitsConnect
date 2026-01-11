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
    [SerializeField] private Transform healthTilesContainer;
    [SerializeField] private GameObject healthTilePrefab;
    
    [SerializeField] private Color filledColor = Color.red;
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    private Boss boss;
    private Image[] healthTiles;
    private int maxHealth;
    
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
        
        // Get boss reference from parent
        if (boss == null)
        {
            boss = GetComponentInParent<Boss>();
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
        CreateHealthTiles();
        UpdateDisplay();
    }
    
    /// <summary>
    /// Create health tiles based on max health
    /// </summary>
    private void CreateHealthTiles()
    {
        // Clear existing tiles
        if (healthTilesContainer != null)
        {
            foreach (Transform child in healthTilesContainer)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        else
        {
            // Create container if it doesn't exist
            GameObject containerObj = new GameObject("HealthTilesContainer");
            containerObj.transform.SetParent(transform);
            containerObj.transform.localPosition = Vector3.zero;
            healthTilesContainer = containerObj.transform;
        }
        
        // Create health tiles
        healthTiles = new Image[maxHealth];
        float tileSpacing = 0.3f;
        float startX = -(maxHealth - 1) * tileSpacing * 0.5f;
        
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject tileObj;
            
            if (healthTilePrefab != null)
            {
                tileObj = Instantiate(healthTilePrefab, healthTilesContainer);
            }
            else
            {
                // Create default tile if no prefab
                tileObj = new GameObject($"HealthTile_{i}");
                tileObj.transform.SetParent(healthTilesContainer);
                
                // Add Image component
                Image image = tileObj.AddComponent<Image>();
                image.color = filledColor;
                
                // Setup RectTransform
                RectTransform rect = tileObj.GetComponent<RectTransform>();
                if (rect == null)
                {
                    rect = tileObj.AddComponent<RectTransform>();
                }
                rect.sizeDelta = new Vector2(0.2f, 0.2f);
                rect.anchoredPosition = new Vector2(startX + i * tileSpacing, 0);
            }
            
            // Get Image component
            Image tileImage = tileObj.GetComponent<Image>();
            if (tileImage == null)
            {
                tileImage = tileObj.AddComponent<Image>();
            }
            
            healthTiles[i] = tileImage;
            
            // Position tile
            RectTransform tileRect = tileObj.GetComponent<RectTransform>();
            if (tileRect != null)
            {
                tileRect.anchoredPosition = new Vector2(startX + i * tileSpacing, 0);
            }
        }
    }
    
    /// <summary>
    /// Update healthbar display based on current health
    /// </summary>
    public void UpdateDisplay()
    {
        if (boss == null || healthTiles == null) return;
        
        int currentHealth = boss.CurrentHealth;
        
        for (int i = 0; i < healthTiles.Length; i++)
        {
            if (healthTiles[i] != null)
            {
                // Fill tile if health point is active
                healthTiles[i].color = i < currentHealth ? filledColor : emptyColor;
            }
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
