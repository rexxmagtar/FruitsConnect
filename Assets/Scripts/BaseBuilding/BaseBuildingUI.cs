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
    [SerializeField] private GameObject energyBalanceContainer;
    [SerializeField] private Image energySphereIcon;
    [SerializeField] private Sprite normalSphereSprite;
    [SerializeField] private Sprite graySphereSprite;
    [SerializeField] private RectTransform buildButtonCenter;
    
    [Header("World Space References")]
    [SerializeField] private Transform baseContainer;
    
    [Header("Cameras")]
    [SerializeField] private Camera baseViewCamera;
    
    [Header("Animation Settings")]
    [SerializeField] private GameObject energySpherePrefab;
    [SerializeField] private float sphereFlyDuration = 1f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private float arcSideOffset = 2f;
    [SerializeField] private float buildInterval = 0.2f;
    [SerializeField] private float scrollDuration = 0.5f;
    [SerializeField] private float scrollDistance = 10f;

    [Header("Sounds")]
    [SerializeField] private AudioClip scrollSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip sphereSpawnSound;
    [SerializeField] private AudioClip stageCompleteSound;
    [SerializeField] private AudioClip objectCompleteSound;

    private const int ENERGY_PER_SPHERE = 1;
    
    private int currentObjectIndex = 0;
    private bool isHoldingBuild = false;
    private float lastBuildTime = 0f;
    private List<BaseObject> instantiatedBaseObjects = new List<BaseObject>();
    private Queue<GameObject> spherePool = new Queue<GameObject>();
    private float currentSideOffset;
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
        InitializePool();
        InitializeBases();
        UpdateUI();
    }

    private void InitializePool()
    {
        for (int i = 0; i < 100; i++)
        {
            GameObject sphere = Instantiate(energySpherePrefab, transform);
            sphere.SetActive(false);
            spherePool.Enqueue(sphere);
        }
    }

    private GameObject GetSphereFromPool()
    {
        if (spherePool.Count > 0)
        {
            GameObject sphere = spherePool.Dequeue();
            sphere.SetActive(true);
            return sphere;
        }
        else
        {
            // Fallback if pool is empty
            GameObject sphere = Instantiate(energySpherePrefab, transform);
            return sphere;
        }
    }

    private void ReturnSphereToPool(GameObject sphere)
    {
        sphere.SetActive(false);
        sphere.transform.SetParent(transform);
        spherePool.Enqueue(sphere);
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
            
            // Provide camera reference for world-space UI billboarding
            baseObj.SetCamera(baseViewCamera);
            
            // Set initial visual state
            int objLevel = (i < currentObjectIndex) ? 10 : (i == currentObjectIndex ? (totalLevel % 10) : 0);
            int stagePrice = baseConfig.baseObjects[i].stagePrices[objLevel < 10 ? objLevel : 9];
            int currentStageProgress = (i == currentObjectIndex) ? SaveDataExtensions.GetBaseStageProgress() : 0;
            float progress = (float)currentStageProgress / stagePrice;
            
            baseObj.UpdateVisuals(objLevel, progress, true); // Use immediate=true for initialization
            
            if (i == currentObjectIndex && objLevel < 10)
            {
                var stage = baseObj.GetStage(objLevel);
                if (stage != null)
                {
                    stage.UpdatePriceText(stagePrice - currentStageProgress);
                }
            }
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

        int available = SaveDataExtensions.GetTotalEnergySpheres();
        if (available <= 0) return;

        // Spend at most ENERGY_PER_SPHERE at a time to ensure spheres spawn one by one
        int toDeduct = Mathf.Min(ENERGY_PER_SPHERE, available);
        toDeduct = Mathf.Min(toDeduct, totalPrice - currentProgress);
        
        if (SaveDataExtensions.RemoveEnergySpheres(toDeduct))
        {
            UpdateBalanceUI(); // Update balance text immediately as soon as we spend
            AnimateEnergySphere(objIndex, stageIndex, toDeduct, totalPrice);
        }
    }

    private void UpdateBalanceUI()
    {
        int totalEnergy = SaveDataExtensions.GetTotalEnergySpheres();
        energyBalanceText.text = totalEnergy.ToString();
        
        if (energySphereIcon != null)
        {
            energySphereIcon.sprite = totalEnergy > 0 ? normalSphereSprite : graySphereSprite;
        }

        buildButton.interactable = totalEnergy > 0;
    }

    private void AnimateEnergySphere(int objIndex, int stageIndex, int priceDeducted, int totalPrice)
    {
        int sphereCount = priceDeducted;
        List<GameObject> spheres = new List<GameObject>();
        List<Vector3> startPositions = new List<Vector3>();
        
        Vector3 baseStartPos = baseViewCamera.ScreenToWorldPoint(new Vector3(buildButtonCenter.position.x, buildButtonCenter.position.y, baseViewCamera.nearClipPlane + 2f));
        
        for (int i = 0; i < sphereCount; i++)
        {
            GameObject sphere = GetSphereFromPool();
            Vector3 spawnPos = baseStartPos;
            
            // Minimal jitter just for overlapping, but they will follow the same arc
            if (sphereCount > 1)
            {
                spawnPos += new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
            }
            
            sphere.transform.position = spawnPos;
            spheres.Add(sphere);
            startPositions.Add(spawnPos);
        }
        
        var targetObj = instantiatedBaseObjects[objIndex];
        var targetStage = targetObj.GetStage(stageIndex);
        
        if (sphereSpawnSound != null) audioSource.PlayOneShot(sphereSpawnSound);
        
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
            
            // Track moving target
            if (targetStage != null)
            {
                for (int i = 0; i < spheres.Count; i++)
                {
                    if (spheres[i] != null)
                    {
                        // Calculate base linear position
                        Vector3 linearPos = Vector3.Lerp(startPositions[i], targetStage.transform.position, x);
                        
                        // Calculate direction and perpendicular for side offset
                        Vector3 direction = (targetStage.transform.position - startPositions[i]).normalized;
                        Vector3 sideDir = Vector3.Cross(direction, Vector3.up).normalized;
                        
                        // Add arc height (up) and side offset (perpendicular) based on a sine curve
                        float curveFactor = Mathf.Sin(x * Mathf.PI);
                        float heightOffset = arcHeight * curveFactor;
                        float sideOffset = currentSideOffset * curveFactor;
                        
                        spheres[i].transform.position = linearPos + (Vector3.up * heightOffset) + (sideDir * sideOffset);
                    }
                }
            }
        }, 1f, sphereFlyDuration).SetEase(Ease.InQuad).OnComplete(() => {
            foreach (var sphere in spheres)
            {
                if (sphere != null) ReturnSphereToPool(sphere);
            }
            
            if (endProgress >= totalPrice)
            {
                OnStageBuildingProgressed(objIndex, stageIndex);
            }
        });
        
        targetStage.PlayBuildEffect();
        
        // Update visual progress on the 3D object
        targetObj.UpdateVisuals(stageIndex, endFill);
        targetStage.UpdatePriceText(totalPrice - endProgress);
    }

    private void OnStageBuildingProgressed(int objIndex, int stageIndex)
    {
        int totalLevel = SaveDataExtensions.GetBaseLevel();
        SaveDataExtensions.SetBaseLevel(totalLevel + 1);
        SaveDataExtensions.SetBaseStageProgress(0); // Reset progress for next stage
        
        instantiatedBaseObjects[objIndex].UpdateVisuals(stageIndex + 1, 0f);
        
        // Update price text for the next stage if it exists
        if (stageIndex + 1 < 10)
        {
            var nextStage = instantiatedBaseObjects[objIndex].GetStage(stageIndex + 1);
            if (nextStage != null)
            {
                int nextStagePrice = baseConfig.baseObjects[objIndex].stagePrices[stageIndex + 1];
                nextStage.UpdatePriceText(nextStagePrice);
            }
        }
        
        if (stageCompleteSound != null) audioSource.PlayOneShot(stageCompleteSound);
        
        if (stageIndex == 9) // Object completed
        {
            if (objectCompleteSound != null) audioSource.PlayOneShot(objectCompleteSound);
            
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
        
        UpdateBalanceUI();

        // Hide energy balance if level < 12
        if (energyBalanceContainer != null)
        {
            energyBalanceContainer.SetActive(SaveDataExtensions.GetCurrentLevelNumber() > 12);
        }
        
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
            buildButton.interactable = false; // All bases completed
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
        currentSideOffset = arcSideOffset;
        UpdateUI();
    }
}
