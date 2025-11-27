using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

namespace ComplianceService
{
    /// <summary>
    /// Manages GDPR consent for users in Europe, handling the display of consent UI and saving user preferences.
    /// </summary>
    public class GDPRManager : MonoBehaviour
{
    private static GDPRManager _instance;
    public static GDPRManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GDPRManager>(true);
                if (_instance != null)
                {
                    DontDestroyOnLoad(_instance.gameObject);
                }
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject gdprDialogParent;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("Sound")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource _audioSource;

    private const string GDPRConsentKey = "GDPRConsent";
    private const string GDPRConsentShownKey = "GDPRConsentShown";
    private TaskCompletionSource<bool> _consentCompletionSource;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnAcceptConsent);
        if (declineButton != null)
            declineButton.onClick.AddListener(OnDeclineConsent);
    }

    private void Start()
    {
        if (gdprDialogParent != null)
            gdprDialogParent.SetActive(false);
    }

    /// <summary>
    /// Checks if the user is in Europe and shows the GDPR consent if necessary.
    /// </summary>
    public async Task<bool> CheckAndShowGDPRConsentAsync()
    {
        if (HasConsentBeenShown())
        {
            return HasUserConsented();
        }

        _consentCompletionSource = new TaskCompletionSource<bool>();

        if (IsUserInEurope())
        {
            ShowGDPRDialog();
        }
        else
        {
            SetConsentShown();
            _consentCompletionSource.SetResult(true);
        }

        return await _consentCompletionSource.Task;
    }

    /// <summary>
    /// Shows the GDPR consent dialog.
    /// </summary>
    private void ShowGDPRDialog()
    {
        if (gdprDialogParent != null)
        {
            gdprDialogParent.SetActive(true);
        }
        else
        {
            Debug.LogError("GDPR dialog parent is not assigned!");
        }
    }

    /// <summary>
    /// Handles the user accepting GDPR consent.
    /// </summary>
    private void OnAcceptConsent()
    {
        PlayButtonClickSound();
        SaveConsent(true);
        // UpdateIronSourceConsent(true);
        HideGDPRDialog();
        _consentCompletionSource?.SetResult(true);
    }

    /// <summary>
    /// Handles the user declining GDPR consent.
    /// </summary>
    private void OnDeclineConsent()
    {
        PlayButtonClickSound();
        SaveConsent(false);
        // UpdateIronSourceConsent(false);
        HideGDPRDialog();
        _consentCompletionSource?.SetResult(false);
    }

    /// <summary>
    /// Hides the GDPR consent dialog.
    /// </summary>
    private void HideGDPRDialog()
    {
        if (gdprDialogParent != null)
            gdprDialogParent.SetActive(false);
    }

    /// <summary>
    /// Saves the user's consent choice using ProgressSaveManager.
    /// </summary>
    private void SaveConsent(bool consented)
    {
        PlayerPrefs.SetInt(GDPRConsentKey, consented ? 1 : 0);
        SetConsentShown();
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Marks that the GDPR consent has been shown to the user.
    /// </summary>
    private void SetConsentShown()
    {
        PlayerPrefs.SetInt(GDPRConsentShownKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Checks if GDPR consent has already been shown to the user.
    /// </summary>
    public bool HasConsentBeenShown()
    {
        return PlayerPrefs.GetInt(GDPRConsentShownKey, 0) == 1;
    }

    /// <summary>
    /// Gets whether the user has given GDPR consent.
    /// </summary>
    public bool HasUserConsented()
    {
        return PlayerPrefs.GetInt(GDPRConsentKey, 0) == 1;
    }

    /// <summary>
    /// Updates IronSource SDK with the user's consent status.
    /// </summary>
    // private void UpdateIronSourceConsent(bool consented)
    // {
    //     #if UNITY_ANDROID || UNITY_IOS
    //     IronSource.Agent.setConsent(consented);
    //     #endif
    // }

    /// <summary>
    /// Checks if the user is in Europe based on their timezone and region.
    /// This is a more reliable approach than using system language.
    /// </summary>
    public bool IsUserInEurope()
    {
        #if UNITY_EDITOR
        return true;
        #endif
        
        // Get the system timezone
        TimeZoneInfo timeZone = TimeZoneInfo.Local;
        
        Debug.Log("Timezone: " + timeZone.Id);
        // List of European timezone IDs
        string[] europeanTimeZones = new string[]
        {
            "Europe/London",
            "Europe/Paris",
            "Europe/Berlin",
            "Europe/Moscow",
            "Europe/Madrid",
            "Europe/Rome",
            "Europe/Amsterdam",
            "Europe/Warsaw",
            "Europe/Lisbon",
            "Europe/Bucharest",
            "Europe/Kiev",
            "Europe/Budapest",
            "Europe/Prague",
            "Europe/Sofia",
            "Europe/Copenhagen",
            "Europe/Helsinki",
            "Europe/Oslo",
            "Europe/Stockholm",
            "Europe/Athens",
            "Europe/Istanbul",
            "Europe/Dublin",
            "Europe/Vienna",
            "Europe/Brussels",
            "Europe/Zurich",
            "Europe/Luxembourg",
            "Europe/Monaco",
            "Europe/Andorra",
            "Europe/San_Marino",
            "Europe/Vatican",
            "Europe/Malta",
            "Europe/Skopje",
            "Europe/Tirane",
            "Europe/Sarajevo",
            "Europe/Podgorica",
            "Europe/Belgrade",
            "Europe/Zagreb",
            "Europe/Ljubljana",
            "Europe/Bratislava",
            "Europe/Brussels",
            "Europe/Amsterdam",
            "Europe/Luxembourg",
            "Europe/Monaco",
            "Europe/Andorra",
            "Europe/San_Marino",
            "Europe/Vatican",
            "Europe/Malta"
        };

        // Check if the timezone is in Europe
        return Array.IndexOf(europeanTimeZones, timeZone.Id) != -1;
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(buttonClickSound);
        }
    }
}
} 