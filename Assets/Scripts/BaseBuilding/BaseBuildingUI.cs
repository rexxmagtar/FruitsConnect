using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BaseBuildingUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private BaseConfig baseConfig;
    
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI nextLevelText;
    [SerializeField] private TextMeshProUGUI energyBalanceText;
    [SerializeField] private RectTransform buildButtonCenter;
    
    [Header("World Space References")]
    [SerializeField] private Transform baseContainer;
    
    [Header("Cameras")]
    [SerializeField] private Camera baseViewCamera;
    
    [Header("Animation Settings")]
    [SerializeField] private GameObject energySpherePrefab;
    [SerializeField] private float sphereFlyDuration = 1f;
    [SerializeField] private float buildInterval = 0.2f;
    [SerializeField] private float scrollDuration = 0.5f;
    [SerializeField] private float scrollDistance = 10f;

    [Header("Sounds")]
    [SerializeField] private AudioClip scrollSound;
    [SerializeField] private AudioClip buttonClickSound;

    private const int ENERGY_PER_SPHERE = 10;
    
    private int currentObjectIndex = 0;
    private bool isHoldingBuild = false;
    private float lastBuildTime = 0f;
    private List<BaseObject> instantiatedBaseObjects = new List<BaseObject>();
    private AudioSource audioSource;
    private Camera mainCamera;
    private Vector3 initialContainerPosition;

    public event System.Action OnClosed;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        backButton.onClick.AddListener(OnBackButtonClick);
        leftArrowButton.onClick.AddListener(() => ScrollBases(-1));
        rightArrowButton.onClick.AddListener(() => ScrollBases(1));
        
        // Setup hold functionality for build button
        EventTrigger trigger = buildButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = buildButton.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener((data) => { isHoldingBuild = true; });
        trigger.triggers.Add(downEntry);

        EventTrigger.Entry upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener((data) => { isHoldingBuild = false; });
        trigger.triggers.Add(upEntry);

        mainCamera = Camera.main;
        
        if (baseContainer != null)
        {
            initialContainerPosition = baseContainer.position;
        }
    }

    private void Start()
    {
        InitializeBases();
        UpdateUI();
    }

    private void InitializeBases()
    {
        int totalLevel = SaveDataExtensions.GetBaseLevel();
        currentObjectIndex = totalLevel / 10;
        
        for (int i = 0; i < baseConfig.baseObjects.Count; i++)
        {
            var info = baseConfig.baseObjects[i];
            var objGo = Instantiate(info.prefab, baseContainer);
            // Position relative to the container
            objGo.transform.localPosition = new Vector3(i * scrollDistance, 0, 0);
            var baseObj = objGo.GetComponent<BaseObject>();
            instantiatedBaseObjects.Add(baseObj);
            
            // Set initial visual state
            int objLevel = (i < currentObjectIndex) ? 10 : (i == currentObjectIndex ? (totalLevel % 10) : 0);
            float progress = (i == currentObjectIndex) ? (float)SaveDataExtensions.GetBaseStageProgress() / baseConfig.baseObjects[i].stagePrices[totalLevel % 10] : 0f;
            baseObj.UpdateVisuals(objLevel, progress, true); // Use immediate=true for initialization
        }
        
        // Position container so current object is at center relative to its initial position
        if (baseContainer != null)
        {
            baseContainer.position = initialContainerPosition + new Vector3(-currentObjectIndex * scrollDistance, 0, 0);
        }
    }

    private void Update()
    {
        if (isHoldingBuild && Time.time - lastBuildTime > buildInterval)
        {
            TryBuild();
            lastBuildTime = Time.time;
        }
    }

    private void TryBuild()
    {
        int totalLevel = SaveDataExtensions.GetBaseLevel();
        int objIndex = totalLevel / 10;
        int stageIndex = totalLevel % 10;
        
        if (objIndex >= baseConfig.baseObjects.Count) return;
        
        var info = baseConfig.baseObjects[objIndex];
        int totalPrice = info.stagePrices[stageIndex];
        int currentProgress = SaveDataExtensions.GetBaseStageProgress();
        
        if (currentProgress >= totalPrice) return;

        int toDeduct = Mathf.Min(ENERGY_PER_SPHERE, totalPrice - currentProgress);
        
        if (SaveDataExtensions.RemoveEnergySpheres(toDeduct))
        {
            AnimateEnergySphere(objIndex, stageIndex, toDeduct, totalPrice);
        }
    }

    private void AnimateEnergySphere(int objIndex, int stageIndex, int priceDeducted, int totalPrice)
    {
        GameObject sphere = Instantiate(energySpherePrefab);
        
        Vector3 startPos = baseViewCamera.ScreenToWorldPoint(new Vector3(buildButtonCenter.position.x, buildButtonCenter.position.y, baseViewCamera.nearClipPlane + 2f));
        sphere.transform.position = startPos;
        
        var targetObj = instantiatedBaseObjects[objIndex];
        var targetStage = targetObj.GetStage(stageIndex);
        Vector3 targetPos = targetStage.transform.position;
        
        int startProgress = SaveDataExtensions.GetBaseStageProgress();
        int endProgress = startProgress + priceDeducted;
        SaveDataExtensions.SetBaseStageProgress(endProgress);

        float startFill = (float)startProgress / totalPrice;
        float endFill = (float)endProgress / totalPrice;

        float elapsed = 0f;
        DOTween.To(() => elapsed, x => {
            elapsed = x;
            // Smoothly interpolate slider during flight
            progressSlider.value = Mathf.Lerp(startFill, endFill, x);
        }, 1f, sphereFlyDuration).SetEase(Ease.InQuad);

        sphere.transform.DOMove(targetPos, sphereFlyDuration).SetEase(Ease.InQuad).OnComplete(() => {
            Destroy(sphere);
            if (endProgress >= totalPrice)
            {
                OnStageBuildingProgressed(objIndex, stageIndex);
            }
        });
        
        targetStage.PlayBuildEffect();
        
        // Update visual progress on the 3D object
        targetObj.UpdateVisuals(stageIndex, endFill);
        
        int currentBalance = SaveDataExtensions.GetTotalEnergySpheres() + priceDeducted;
        DOTween.To(() => currentBalance, x => {
            energyBalanceText.text = x.ToString();
        }, SaveDataExtensions.GetTotalEnergySpheres(), sphereFlyDuration);
    }

    private void OnStageBuildingProgressed(int objIndex, int stageIndex)
    {
        int totalLevel = SaveDataExtensions.GetBaseLevel();
        SaveDataExtensions.SetBaseLevel(totalLevel + 1);
        SaveDataExtensions.SetBaseStageProgress(0); // Reset progress for next stage
        
        instantiatedBaseObjects[objIndex].UpdateVisuals(stageIndex + 1, 0f);
        
        if (stageIndex == 9) // Object completed
        {
            if (objIndex + 1 < instantiatedBaseObjects.Count)
            {
                ScrollBases(1, true);
            }
        }
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        int totalLevel = SaveDataExtensions.GetBaseLevel();
        currentLevelText.text = totalLevel.ToString();
        nextLevelText.text = (totalLevel + 1).ToString();
        energyBalanceText.text = SaveDataExtensions.GetTotalEnergySpheres().ToString();
        
        int objIndex = totalLevel / 10;
        int stageIndex = totalLevel % 10;
        
        if (objIndex < baseConfig.baseObjects.Count)
        {
            int totalPrice = baseConfig.baseObjects[objIndex].stagePrices[stageIndex];
            int currentProgress = SaveDataExtensions.GetBaseStageProgress();
            progressSlider.value = (float)currentProgress / totalPrice;
        }
        else
        {
            progressSlider.value = 1f;
        }
        
        leftArrowButton.interactable = currentObjectIndex > 0;
        rightArrowButton.interactable = currentObjectIndex < instantiatedBaseObjects.Count - 1;
    }

    private void ScrollBases(int direction, bool autoUnlock = false)
    {
        int nextIndex = currentObjectIndex + direction;
        if (nextIndex < 0 || nextIndex >= instantiatedBaseObjects.Count) return;
        
        currentObjectIndex = nextIndex;
        
        if (baseContainer != null)
        {
            Vector3 targetPosition = initialContainerPosition + new Vector3(-currentObjectIndex * scrollDistance, 0, 0);
            baseContainer.DOMove(targetPosition, scrollDuration).SetEase(Ease.InOutQuad);
        }
        
        if (scrollSound != null) audioSource.PlayOneShot(scrollSound);
        
        if (autoUnlock)
        {
            // Handle shader reveal here if needed
            // For now just update interactability
        }
        
        UpdateUI();
    }

    private void OnBackButtonClick()
    {
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
        
        // Switch cameras back
        baseViewCamera.gameObject.SetActive(false);
        if (mainCamera != null) mainCamera.gameObject.SetActive(true);
        
        // Hide this UI
        gameObject.SetActive(false);
        
        OnClosed?.Invoke();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
        baseViewCamera.gameObject.SetActive(true);
        UpdateUI();
    }
}
