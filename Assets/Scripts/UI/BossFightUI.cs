using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// UI overlay for boss fight - alert screen and fight UI
/// </summary>
public class BossFightUI : MonoBehaviour
{
    [Header("Boss Alert Screen")]
    [SerializeField] private GameObject bossAlertPanel;
    [SerializeField] private TextMeshProUGUI bossAlertText;
    [SerializeField] private Image bossAlertBackground;
    [SerializeField] private float alertFadeInDuration = 0.5f;
    [SerializeField] private float alertFadeOutDuration = 0.3f;
    
    [Header("Fight UI")]
    [SerializeField] private GameObject fightUIPanel;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private BossHealthBar bossHealthBar;
    
    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float alertScaleAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve alertScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    private bool isAlertVisible = false;
    private bool isFightUIVisible = false;
    
    private void Awake()
    {
        // Get canvas group if not assigned
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Initialize visibility
        if (bossAlertPanel != null)
        {
            bossAlertPanel.SetActive(false);
        }
        if (fightUIPanel != null)
        {
            fightUIPanel.SetActive(false);
        }
    }
    
    private void Start()
    {
        // Hide initially
        Hide();
    }
    
    /// <summary>
    /// Show boss alert screen
    /// </summary>
    public void ShowBossAlert()
    {
        if (isAlertVisible) return;
        
        // Ensure canvas group is visible
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        if (bossAlertPanel != null)
        {
            bossAlertPanel.SetActive(true);
            StartCoroutine(ShowAlertAnimation());
        }
    }
    
    /// <summary>
    /// Hide boss alert screen
    /// </summary>
    public void HideBossAlert()
    {
        if (!isAlertVisible) return;
        
        StartCoroutine(HideAlertAnimation());
    }
    
    /// <summary>
    /// Show fight UI
    /// </summary>
    public void ShowFightUI()
    {
        if (isFightUIVisible) return;
        
        // Ensure canvas group is visible
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        if (fightUIPanel != null)
        {
            fightUIPanel.SetActive(true);
            isFightUIVisible = true;
        }
        
        // Setup boss health bar
        BossFightManager manager = BossFightManager.Instance;
        if (manager != null && manager.CurrentBoss != null)
        {
            Boss boss = manager.CurrentBoss;
            if (bossHealthBar != null)
            {
                // Try to get health bar from boss
                BossHealthBar bossHealthBarComponent = boss.GetComponentInChildren<BossHealthBar>();
                if (bossHealthBarComponent != null)
                {
                    bossHealthBar = bossHealthBarComponent;
                }
            }
        }
    }
    
    /// <summary>
    /// Hide fight UI
    /// </summary>
    public void HideFightUI()
    {
        if (!isFightUIVisible) return;
        
        if (fightUIPanel != null)
        {
            fightUIPanel.SetActive(false);
            isFightUIVisible = false;
        }
    }
    
    /// <summary>
    /// Update timer display
    /// </summary>
    public void UpdateTimer(float timeRemaining, float timeLimit)
    {
        if (timerSlider != null && timeLimit > 0f)
        {
            // Timer slider goes from 1 to 0 as time runs out
            float normalizedTime = timeRemaining / timeLimit;
            timerSlider.value = Mathf.Clamp01(normalizedTime);
        }
        
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeRemaining);
            timerText.text = $"{seconds}s";
        }
    }
    
    /// <summary>
    /// Show alert animation
    /// </summary>
    private IEnumerator ShowAlertAnimation()
    {
        isAlertVisible = true;
        
        // Setup initial state
        if (bossAlertPanel != null)
        {
            RectTransform rectTransform = bossAlertPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.zero;
            }
        }
        
        if (bossAlertBackground != null)
        {
            Color bgColor = bossAlertBackground.color;
            bgColor.a = 0f;
            bossAlertBackground.color = bgColor;
        }
        
        if (bossAlertText != null)
        {
            Color textColor = bossAlertText.color;
            textColor.a = 0f;
            bossAlertText.color = textColor;
        }
        
        // Fade in and scale up
        float elapsedTime = 0f;
        while (elapsedTime < alertFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / alertFadeInDuration;
            float curveValue = alertScaleCurve.Evaluate(t);
            
            if (bossAlertPanel != null)
            {
                RectTransform rectTransform = bossAlertPanel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, curveValue);
                }
            }
            
            if (bossAlertBackground != null)
            {
                Color bgColor = bossAlertBackground.color;
                bgColor.a = Mathf.Lerp(0f, 1f, t);
                bossAlertBackground.color = bgColor;
            }
            
            if (bossAlertText != null)
            {
                Color textColor = bossAlertText.color;
                textColor.a = Mathf.Lerp(0f, 1f, t);
                bossAlertText.color = textColor;
            }
            
            yield return null;
        }
        
        // Ensure final state
        if (bossAlertPanel != null)
        {
            RectTransform rectTransform = bossAlertPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }
        
        if (bossAlertBackground != null)
        {
            Color bgColor = bossAlertBackground.color;
            bgColor.a = 1f;
            bossAlertBackground.color = bgColor;
        }
        
        if (bossAlertText != null)
        {
            Color textColor = bossAlertText.color;
            textColor.a = 1f;
            bossAlertText.color = textColor;
        }
    }
    
    /// <summary>
    /// Hide alert animation
    /// </summary>
    private IEnumerator HideAlertAnimation()
    {
        // Fade out
        float elapsedTime = 0f;
        float startAlpha = 1f;
        
        if (bossAlertBackground != null)
        {
            startAlpha = bossAlertBackground.color.a;
        }
        
        while (elapsedTime < alertFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / alertFadeOutDuration;
            float alpha = Mathf.Lerp(startAlpha, 0f, t);
            
            if (bossAlertBackground != null)
            {
                Color bgColor = bossAlertBackground.color;
                bgColor.a = alpha;
                bossAlertBackground.color = bgColor;
            }
            
            if (bossAlertText != null)
            {
                Color textColor = bossAlertText.color;
                textColor.a = alpha;
                bossAlertText.color = textColor;
            }
            
            if (bossAlertPanel != null)
            {
                RectTransform rectTransform = bossAlertPanel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                }
            }
            
            yield return null;
        }
        
        // Ensure final state
        if (bossAlertBackground != null)
        {
            Color bgColor = bossAlertBackground.color;
            bgColor.a = 0f;
            bossAlertBackground.color = bgColor;
        }
        
        if (bossAlertText != null)
        {
            Color textColor = bossAlertText.color;
            textColor.a = 0f;
            bossAlertText.color = textColor;
        }
        
        if (bossAlertPanel != null)
        {
            bossAlertPanel.SetActive(false);
        }
        
        isAlertVisible = false;
    }
    
    /// <summary>
    /// Show the UI
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
    
    /// <summary>
    /// Hide the UI
    /// </summary>
    public void Hide()
    {
        HideBossAlert();
        HideFightUI();
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
