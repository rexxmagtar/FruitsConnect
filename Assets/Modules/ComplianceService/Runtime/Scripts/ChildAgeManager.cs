using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ComplianceService
{
    public class ChildAgeManager : MonoBehaviour
{
    private static ChildAgeManager _instance;
    public static ChildAgeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ChildAgeManager>(true);
                if (_instance != null)
                {
                    DontDestroyOnLoad(_instance.gameObject);
                }
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject childAgeDialogParent;
    [SerializeField] private TMP_Dropdown dayDropdown;
    [SerializeField] private TMP_Dropdown monthDropdown;
    [SerializeField] private TMP_Dropdown yearDropdown;
    [SerializeField] private Button confirmButton;

    [Header("Sound")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource _audioSource;

    private const string ChildAgeShownKey = "ChildAgeShown";
    private const string IsChildKey = "IsChild";
    private TaskCompletionSource<bool> _ageCompletionSource;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmAge);

        InitializeDropdowns();
        childAgeDialogParent.SetActive(false);
    }

    private void InitializeDropdowns()
    {
        // Initialize days (1-31)
        List<string> days = new List<string>();
        for (int i = 1; i <= 31; i++)
        {
            days.Add(i.ToString());
        }
        dayDropdown.ClearOptions();
        dayDropdown.AddOptions(days);

        // Initialize months
        List<string> months = new List<string>
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };
        monthDropdown.ClearOptions();
        monthDropdown.AddOptions(months);

        // Initialize years (current year - 100 to current year)
        List<string> years = new List<string>();
        int currentYear = DateTime.Now.Year;
        for (int i = currentYear; i >= currentYear - 100; i--)
        {
            years.Add(i.ToString());
        }
        yearDropdown.ClearOptions();
        yearDropdown.AddOptions(years);
    }

    public async Task<bool> CheckAndShowChildAgeDialogAsync()
    {
        if (HasChildAgeBeenShown())
        {
            return IsChild();
        }

        _ageCompletionSource = new TaskCompletionSource<bool>();

        if (IsUserInUSA())
        {
            ShowChildAgeDialog();
        }
        else
        {
            SetChildAgeShown();
            _ageCompletionSource.SetResult(false);
        }

        return await _ageCompletionSource.Task;
    }

    private void ShowChildAgeDialog()
    {
        if (childAgeDialogParent != null)
        {
            childAgeDialogParent.SetActive(true);
        }
    }

    private void OnConfirmAge()
    {
        PlayButtonClickSound();
        int day = int.Parse(dayDropdown.options[dayDropdown.value].text);
        int month = monthDropdown.value + 1; // Months are 0-based in dropdown
        int year = int.Parse(yearDropdown.options[yearDropdown.value].text);

        DateTime birthDate = new DateTime(year, month, day);
        DateTime today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        
        if (birthDate > today.AddYears(-age))
            age--;

        bool isChild = age < 13;
        SaveChildAge(isChild);
        // UpdateIronSourceChildStatus(isChild);
        HideChildAgeDialog();
        _ageCompletionSource?.SetResult(isChild);
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(buttonClickSound);
        }
    }

    private void HideChildAgeDialog()
    {
        if (childAgeDialogParent != null)
            childAgeDialogParent.SetActive(false);
    }

    private void SaveChildAge(bool isChild)
    {
        PlayerPrefs.SetInt(IsChildKey, isChild ? 1 : 0);
        SetChildAgeShown();
        PlayerPrefs.Save();
    }

    private void SetChildAgeShown()
    {
        PlayerPrefs.SetInt(ChildAgeShownKey, 1);
        PlayerPrefs.Save();
    }

    public bool HasChildAgeBeenShown()
    {
        return PlayerPrefs.GetInt(ChildAgeShownKey, 0) == 1;
    }

    public bool IsChild()
    {
        return PlayerPrefs.GetInt(IsChildKey, 0) == 1;
    }

    // private void UpdateIronSourceChildStatus(bool isChild)
    // {
    //     #if UNITY_ANDROID || UNITY_IOS
    //     IronSource.Agent.setMetaData("is_child_directed", isChild ? "true" : "false");
    //     #endif
    // }

    public bool IsUserInUSA()
    {
        #if UNITY_EDITOR
        return true;
        #endif
        
        TimeZoneInfo timeZone = TimeZoneInfo.Local;
        
        string[] usaTimeZones = new string[]
        {
            "America/New_York",
            "America/Chicago",
            "America/Denver",
            "America/Los_Angeles",
            "America/Anchorage",
            "America/Adak",
            "Pacific/Honolulu",
            "America/Phoenix",
            "America/Boise",
            "America/Detroit",
            "America/Indiana/Indianapolis",
            "America/Indiana/Knox",
            "America/Indiana/Marengo",
            "America/Indiana/Petersburg",
            "America/Indiana/Tell_City",
            "America/Indiana/Vevay",
            "America/Indiana/Vincennes",
            "America/Indiana/Winamac",
            "America/Kentucky/Louisville",
            "America/Kentucky/Monticello",
            "America/North_Dakota/Beulah",
            "America/North_Dakota/Center",
            "America/North_Dakota/New_Salem"
        };

        return Array.IndexOf(usaTimeZones, timeZone.Id) != -1;
    }
}
} 
