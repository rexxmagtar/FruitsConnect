using UnityEngine;
using UnityEngine.Audio;


public class AudioSettings : MonoBehaviour
{
    // Singleton pattern
    private static AudioSettings _instance;
    public static AudioSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioSettings>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioSettings");
                    _instance = go.AddComponent<AudioSettings>();
                }
            }
            return _instance;
        }
    }
    
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    
    [Header("Mixer Parameters")]
    [SerializeField] private string ambienceVolumeParameter = "AmbienceVolume";
    [SerializeField] private string effectsVolumeParameter = "EffectsVolume";
    
    // Volume settings (on/off)
    private bool isMusicEnabled = true;
    private bool isEffectsEnabled = true;
    
    // Audio values
    private const float VOLUME_ON = 0;    // 0 dB (full volume)
    private const float VOLUME_OFF = -80f; // -80 dB (muted)
    
    // Events
    public static event System.Action<bool> OnMusicToggled;
    public static event System.Action<bool> OnEffectsToggled;
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
    }

    public void Initialize()
    {
        LoadSettings();
    }
    
    /// <summary>
    /// Load audio settings from PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        // Load from PlayerPrefs (1 = enabled, 0 = disabled, default to 1 if not set)
        isMusicEnabled = PlayerPrefs.GetInt("AudioSettings_MusicEnabled", 1) == 1;
        isEffectsEnabled = PlayerPrefs.GetInt("AudioSettings_EffectsEnabled", 1) == 1;
        
        // Apply settings to audio mixer
        ApplySettings();
        
        Debug.Log($"[AudioSettings] Settings loaded - Music: {isMusicEnabled}, Effects: {isEffectsEnabled}");
    }
    
    /// <summary>
    /// Apply current settings to audio mixer
    /// </summary>
    private void ApplySettings()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[AudioSettings] Audio mixer not assigned!");
            return;
        }
        
        // Set ambience volume
        audioMixer.SetFloat(ambienceVolumeParameter, isMusicEnabled ? VOLUME_ON : VOLUME_OFF);
        
        // Set effects volume
        audioMixer.SetFloat(effectsVolumeParameter, isEffectsEnabled ? VOLUME_ON : VOLUME_OFF);
    }
    
    /// <summary>
    /// Toggle music on/off
    /// </summary>
    public void ToggleMusic()
    {
        isMusicEnabled = !isMusicEnabled;
        
        // Apply to mixer
        if (audioMixer != null)
        {
            audioMixer.SetFloat(ambienceVolumeParameter, isMusicEnabled ? VOLUME_ON : VOLUME_OFF);
        }
        
        // Save to PlayerPrefs
        PlayerPrefs.SetInt("AudioSettings_MusicEnabled", isMusicEnabled ? 1 : 0);
        PlayerPrefs.Save();
        
        // Invoke event
        OnMusicToggled?.Invoke(isMusicEnabled);
        
        Debug.Log($"[AudioSettings] Music toggled: {isMusicEnabled}");
    }
    
    /// <summary>
    /// Toggle effects on/off
    /// </summary>
    public void ToggleEffects()
    {
        isEffectsEnabled = !isEffectsEnabled;
        
        // Apply to mixer
        if (audioMixer != null)
        {
            audioMixer.SetFloat(effectsVolumeParameter, isEffectsEnabled ? VOLUME_ON : VOLUME_OFF);
        }
        
        // Save to PlayerPrefs
        PlayerPrefs.SetInt("AudioSettings_EffectsEnabled", isEffectsEnabled ? 1 : 0);
        PlayerPrefs.Save();
        
        // Invoke event
        OnEffectsToggled?.Invoke(isEffectsEnabled);
        
        Debug.Log($"[AudioSettings] Effects toggled: {isEffectsEnabled}");
    }
    
    /// <summary>
    /// Set music enabled state
    /// </summary>
    public void SetMusicEnabled(bool enabled)
    {
        if (isMusicEnabled == enabled) return;
        ToggleMusic();
    }
    
    /// <summary>
    /// Set effects enabled state
    /// </summary>
    public void SetEffectsEnabled(bool enabled)
    {
        if (isEffectsEnabled == enabled) return;
        ToggleEffects();
    }
    
    /// <summary>
    /// Get music enabled state
    /// </summary>
    public bool IsMusicEnabled()
    {
        return isMusicEnabled;
    }
    
    /// <summary>
    /// Get effects enabled state
    /// </summary>
    public bool IsEffectsEnabled()
    {
        return isEffectsEnabled;
    }
}

