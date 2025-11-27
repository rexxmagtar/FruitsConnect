using UnityEngine;

/// <summary>
/// Singleton class responsible for playing ambience sound during gameplay
/// </summary>
public class AmbienceSound : MonoBehaviour
{
    // Singleton pattern
    private static AmbienceSound _instance;
    public static AmbienceSound Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AmbienceSound>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AmbienceSound");
                    _instance = go.AddComponent<AmbienceSound>();
                }
            }
            return _instance;
        }
    }
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip ambienceClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private bool playOnAwake = false;
    
    private AudioSource audioSource;
    private AudioClip defaultAmbienceClip; // Store the default clip from inspector
    
    private void Awake()
    {
        // Ensure singleton instance
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            // Store the default clip before any overrides
            defaultAmbienceClip = ambienceClip;
            InitializeAudioSource();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// Initialize the AudioSource component
    /// </summary>
    private void InitializeAudioSource()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.clip = ambienceClip;
        audioSource.loop = true;
        audioSource.playOnAwake = playOnAwake;
        audioSource.volume = volume;
    }
    
    /// <summary>
    /// Play the ambience sound
    /// </summary>
    public void PlayAmbience()
    {
        if (audioSource != null && ambienceClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("[AmbienceSound] Started playing ambience music");
        }
        else if (ambienceClip == null)
        {
            Debug.LogWarning("[AmbienceSound] No ambience clip assigned!");
        }
    }
    
    /// <summary>
    /// Stop the ambience sound
    /// </summary>
    public void StopAmbience()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[AmbienceSound] Stopped ambience music");
        }
    }
    
    /// <summary>
    /// Set the volume of the ambience sound
    /// </summary>
    /// <param name="newVolume">Volume value between 0 and 1</param>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    /// <summary>
    /// Set a new ambience clip
    /// </summary>
    /// <param name="newClip">New audio clip to play</param>
    public void SetAmbienceClip(AudioClip newClip)
    {
        bool wasPlaying = audioSource != null && audioSource.isPlaying;
        
        if (wasPlaying)
        {
            StopAmbience();
        }
        
        ambienceClip = newClip;
        if (audioSource != null)
        {
            audioSource.clip = newClip;
        }
        
        if (wasPlaying)
        {
            PlayAmbience();
        }
    }
    
    /// <summary>
    /// Reset to default ambience clip
    /// </summary>
    public void ResetToDefaultClip()
    {
        SetAmbienceClip(defaultAmbienceClip);
    }
    
    /// <summary>
    /// Check if ambience is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }
}

