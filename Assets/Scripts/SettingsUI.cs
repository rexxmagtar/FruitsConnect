using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Settings Buttons")]
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private Button effectsToggleButton;
    [SerializeField] private Button closeButton;
    
    [Header("Toggle Icons")]
    [SerializeField] private Image musicToggleIcon;
    [SerializeField] private Image effectsToggleIcon;
    
    [Header("Icon Sprites")]
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite effectsOnSprite;
    [SerializeField] private Sprite effectsOffSprite;
    
    private void Awake()
    {
        // Subscribe to button events
        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.AddListener(OnMusicToggleClicked);
        }
        
        if (effectsToggleButton != null)
        {
            effectsToggleButton.onClick.AddListener(OnEffectsToggleClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }
        
        // Subscribe to audio settings events
        AudioSettings.OnMusicToggled += OnMusicToggled;
        AudioSettings.OnEffectsToggled += OnEffectsToggled;
    }

    private void OnEnable()
    {
        UpdateMusicIcon(AudioSettings.Instance.IsMusicEnabled());
        UpdateEffectsIcon(AudioSettings.Instance.IsEffectsEnabled());
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from button events
        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.RemoveListener(OnMusicToggleClicked);
        }
        
        if (effectsToggleButton != null)
        {
            effectsToggleButton.onClick.RemoveListener(OnEffectsToggleClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }
        
        // Unsubscribe from audio settings events
        AudioSettings.OnMusicToggled -= OnMusicToggled;
        AudioSettings.OnEffectsToggled -= OnEffectsToggled;
    }
    
    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Handle music toggle button click
    /// </summary>
    private void OnMusicToggleClicked()
    {
        AudioSettings.Instance.ToggleMusic();
    }
    
    /// <summary>
    /// Handle effects toggle button click
    /// </summary>
    private void OnEffectsToggleClicked()
    {
        AudioSettings.Instance.ToggleEffects();
    }
    
    /// <summary>
    /// Handle music toggle event
    /// </summary>
    private void OnMusicToggled(bool isEnabled)
    {
        UpdateMusicIcon(isEnabled);
    }
    
    /// <summary>
    /// Handle effects toggle event
    /// </summary>
    private void OnEffectsToggled(bool isEnabled)
    {
        UpdateEffectsIcon(isEnabled);
    }
    
    /// <summary>
    /// Update music icon based on state
    /// </summary>
    private void UpdateMusicIcon(bool isEnabled)
    {
        if (musicToggleIcon != null)
        {
            musicToggleIcon.sprite = isEnabled ? musicOnSprite : musicOffSprite;
        }
    }
    
    /// <summary>
    /// Update effects icon based on state
    /// </summary>
    private void UpdateEffectsIcon(bool isEnabled)
    {
        if (effectsToggleIcon != null)
        {
            effectsToggleIcon.sprite = isEnabled ? effectsOnSprite : effectsOffSprite;
        }
    }
}

