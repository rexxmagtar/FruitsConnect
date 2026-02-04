using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using AdsServices;
using WindowManager;

public class BaseBuildingUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private BaseConfig baseConfig;
    [SerializeField] private PerksVisualConfig perksVisualConfig;
    
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI nextLevelText;
    [SerializeField] private TextMeshProUGUI energyBalanceText;
    [SerializeField] private TextMeshProUGUI baseNameDisplayText;
    [SerializeField] private GameObject energyBalanceContainer;
    [SerializeField] private Image energySphereIcon;
    [SerializeField] private Sprite normalSphereSprite;
    [SerializeField] private Sprite graySphereSprite;
    [SerializeField] private RectTransform buildButtonCenter;

    [Header("Perk UI")]
    [SerializeField] private GameObject nextPerkContainer;
    [SerializeField] private Image nextPerkIcon;
    [SerializeField] private Image nextPerkIconSecondary;
    [SerializeField] private TextMeshProUGUI nextPerkText;
    [SerializeField] private float perkPulseScale = 1.05f;
    [SerializeField] private float perkPulseDuration = 0.8f;
    [SerializeField] private float perkPulsePause = 2f;
    
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
    [SerializeField] private float cameraOrbitSpeed = 20f;
    [SerializeField] private float sphereSpawnDistanceFromCamera = 2f;

    [Header("Sounds")]
    [SerializeField] private AudioClip scrollSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip sphereSpawnSound;
    [SerializeField] private AudioClip stageCompleteSound;
    [SerializeField] private AudioClip objectCompleteSound;

    [Header("Particles")]
    [SerializeField] private ParticleSystem stageCompleteParticles;

    [Header("No Energy Popup")]
    [SerializeField] private SimplePopupUI noEnergyPopup;
    [SerializeField] private RectTransform noEnergyInfoContainer;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeNoEnergyButton;
    [SerializeField] private float popupScaleAnimationDuration = 0.3f;
    
    [Header("Watch Ad Button")]
    [SerializeField] private GameObject watchAdLoadingContainer;
    [SerializeField] private GameObject watchAdClickContainer;
    [SerializeField] private RectTransform watchAdLoadingIcon;
    [SerializeField] private float watchAdRotationSpeed = 180f;

    [Header("Reward Popup")]
    [SerializeField] private SimplePopupUI rewardPopup;
    [SerializeField] private RectTransform rewardInfoContainer;
    [SerializeField] private TextMeshProUGUI rewardAmountText;
    [SerializeField] private RectTransform rewardPopupParent;
    [SerializeField] private int energyRewardAmount = 5;

    [Header("Confetti Settings")]
    [SerializeField] private int confettiCount = 50;
    [SerializeField] private int confettiBurstCount = 3;
    [SerializeField] private float confettiSpawnDuration = 2f;
    [SerializeField] private float confettiAnimationDuration = 3f;
    [SerializeField] private Vector2 confettiMinSize = new Vector2(10f, 10f);
    [SerializeField] private Vector2 confettiMaxSize = new Vector2(20f, 20f);
    [SerializeField] private float confettiSpawnZoneSize = 50f;
    [SerializeField] private Color[] confettiColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };

    [Header("Success Sound")]
    [SerializeField] private AudioClip successSound;

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
    private float currentOrbitAngle;
    private Vector3 cameraInitialOffset;
    private Quaternion cameraInitialRotation;
    private Sequence perkPulseSequence;
    private bool isShowingAd = false;
    private SimplePopupUI noEnergyPopupUI;
    private SimplePopupUI rewardPopupUI;

    public event System.Action OnClosed;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        backButton.onClick.AddListener(OnBackButtonClick);
        leftArrowButton.onClick.AddListener(() => ScrollBases(-1));
        rightArrowButton.onClick.AddListener(() => ScrollBases(1));
        
        // Setup no energy popup buttons
        if (watchAdButton != null)
        {
            watchAdButton.onClick.AddListener(OnWatchAdButtonClick);
        }
        
        // Setup SimplePopupUI components
        noEnergyPopupUI = noEnergyPopup != null ? noEnergyPopup : null;
        if (noEnergyPopupUI == null && noEnergyPopup != null)
        {
            noEnergyPopupUI = noEnergyPopup.GetComponent<SimplePopupUI>();
        }
        if (noEnergyPopupUI != null)
        {
            if (closeNoEnergyButton != null)
            {
                noEnergyPopupUI.SetCloseButton(closeNoEnergyButton);
            }
            noEnergyPopupUI.Close();
        }
        
        rewardPopupUI = rewardPopup != null ? rewardPopup : null;
        if (rewardPopupUI == null && rewardPopup != null)
        {
            rewardPopupUI = rewardPopup.GetComponent<SimplePopupUI>();
        }
        if (rewardPopupUI != null)
        {
            rewardPopupUI.Close();
        }
        
        // Initialize watch ad button state
        UpdateWatchAdButtonState();
        
        // Setup hold functionality for build button using AnimatedButton events
        AnimatedButton animatedBuildButton = buildButton.GetComponent<AnimatedButton>();
        if (animatedBuildButton != null)
        {
            animatedBuildButton.OnPointerDownEvent += () => { isHoldingBuild = true; };
            animatedBuildButton.OnPointerUpEvent += () => { isHoldingBuild = false; };
        }

        mainCamera = Camera.main;
        
        if (baseContainer != null)
        {
            initialContainerPosition = baseContainer.position;
        }

        if (baseViewCamera != null)
        {
            // Capture initial state relative to the container center
            cameraInitialOffset = baseViewCamera.transform.position - initialContainerPosition;
            cameraInitialRotation = baseViewCamera.transform.rotation;
            currentOrbitAngle = 0f;
        }
    }

    private void Start()
    {
        InitializePool();
        InitializeBases();
        UpdateUI();
    }

    private void OnDisable()
    {
        StopPerkPulse();
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
        currentObjectIndex = 0;
        
        for (int i = 0; i < baseConfig.baseObjects.Count; i++)
        {
            var info = baseConfig.baseObjects[i];
            var objGo = Instantiate(info.prefab, baseContainer);
            // Position relative to the container. Note: Objects are moved by container, 
            // but the camera orbits around their absolute world position.
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

        UpdateCameraRotation();
        
        // Rotate loading icon if visible
        if (watchAdLoadingContainer != null && watchAdLoadingContainer.activeSelf)
        {
            if (watchAdLoadingIcon != null)
            {
                watchAdLoadingIcon.Rotate(Vector3.forward, -watchAdRotationSpeed * Time.deltaTime);
            }
        }
        
        // Periodically check ad readiness if popup is visible and not showing ad
        if (noEnergyPopupUI != null && noEnergyPopupUI.IsVisible() && !isShowingAd)
        {
            UpdateWatchAdButtonState();
        }
    }

    private void UpdateCameraRotation()
    {
        if (baseViewCamera == null || !baseViewCamera.gameObject.activeInHierarchy || baseContainer == null) return;

        // Apply rotation
        currentOrbitAngle += cameraOrbitSpeed * Time.deltaTime;
        
        // The viewing area center is always initialContainerPosition
        Vector3 viewingCenter = initialContainerPosition;
        
        // Rotate the camera's position around the Y axis of the viewing center
        Quaternion orbitRotation = Quaternion.Euler(0, currentOrbitAngle, 0);
        Vector3 rotatedOffset = orbitRotation * cameraInitialOffset;
        
        baseViewCamera.transform.position = viewingCenter + rotatedOffset;
        
        // Rotate the camera's orientation by the same amount to keep it relative to the center
        baseViewCamera.transform.rotation = orbitRotation * cameraInitialRotation;
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
        if (available <= 0)
        {
            // Show no energy popup if not already showing
            if (noEnergyPopupUI != null && !noEnergyPopupUI.IsVisible() && !isShowingAd)
            {
                ShowNoEnergyPopup();
            }
            return;
        }

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

        // Don't disable button interactability - we need events to fire for hold detection
        // The TryBuild() method will handle checking if we can build
    }

    private void AnimateEnergySphere(int objIndex, int stageIndex, int priceDeducted, int totalPrice)
    {
        int sphereCount = priceDeducted;
        List<GameObject> spheres = new List<GameObject>();
        List<Vector3> startPositions = new List<Vector3>();
        
        Vector3 baseStartPos = baseViewCamera.ScreenToWorldPoint(new Vector3(buildButtonCenter.position.x, buildButtonCenter.position.y, baseViewCamera.nearClipPlane + sphereSpawnDistanceFromCamera));
        
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
                        // Calculate base linear position (straight line from start to end)
                        Vector3 linearPos = Vector3.Lerp(startPositions[i], targetStage.transform.position, x);
                        
                        // Only apply curve near the end (last 20% of the path)
                        // Use a curve that starts at 0 and increases near the end
                        float curveIntensity = 0f;
                        if (x > 0.8f)
                        {
                            // Normalize x from 0.8-1.0 to 0-1.0 for the curve calculation
                            float normalizedX = (x - 0.8f) / 0.2f;
                            // Create a smooth curve that peaks near the end
                            curveIntensity = Mathf.Sin(normalizedX * Mathf.PI * 0.5f);
                        }
                        
                        // Add slight arc height only near the end
                        float heightOffset = arcHeight * curveIntensity * 0.3f; // Reduced height for subtle curve
                        
                        spheres[i].transform.position = linearPos + (Vector3.up * 0);
                        
                        // Make sphere always look at camera
                        if (baseViewCamera != null)
                        {
                            spheres[i].transform.LookAt(baseViewCamera.transform);
                        }
                    }
                }
            }
        }, 1f, sphereFlyDuration).SetEase(Ease.InQuad).OnComplete(() => {
            foreach (var sphere in spheres)
            {
                if (sphere != null) ReturnSphereToPool(sphere);
            }
            
            // Update visual progress on the 3D object only after sphere reaches building
            targetObj.UpdateVisuals(stageIndex, endFill);
            targetStage.UpdatePriceText(totalPrice - endProgress);
            
            if (endProgress >= totalPrice)
            {
                OnStageBuildingProgressed(objIndex, stageIndex);
            }
        });
        
        targetStage.PlayBuildEffect();
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
        if (stageCompleteParticles != null) stageCompleteParticles.Play();
        
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
            // Don't disable button interactability - we need events to fire for hold detection
            // The TryBuild() method will handle checking if we can build
        }
        
        leftArrowButton.interactable = currentObjectIndex > 0;
        rightArrowButton.interactable = currentObjectIndex < instantiatedBaseObjects.Count - 1;

        // Update base name display text
        if (baseNameDisplayText != null && baseConfig != null && currentObjectIndex >= 0 && currentObjectIndex < baseConfig.baseObjects.Count)
        {
            baseNameDisplayText.text = baseConfig.baseObjects[currentObjectIndex].displayName;
        }

        // Update next perk info
        if (nextPerkContainer != null)
        {
            var nextPerk = PerksManager.Instance.GetNextPerkInfo();
            if (nextPerk != null)
            {
                bool wasActive = nextPerkContainer.activeSelf;
                nextPerkContainer.SetActive(true);
                var visual = perksVisualConfig != null ? perksVisualConfig.GetPerkVisual(nextPerk.type) : null;
                
                if (visual != null)
                {
                    if (nextPerkIcon != null) nextPerkIcon.sprite = visual.icon;
                    if (nextPerkIconSecondary != null) nextPerkIconSecondary.sprite = visual.icon;
                }

                if (nextPerkText != null)
                {
                    string sign = nextPerk.value >= 0 ? "+" : "";
                    string unit = "%";
                    string displayName = visual != null ? visual.displayName : nextPerk.type.ToString();
                    nextPerkText.text = $"{displayName} {sign}{nextPerk.value}{unit}";
                }

                if (!wasActive)
                {
                    StartPerkPulse();
                }
            }
            else
            {
                nextPerkContainer.SetActive(false);
                StopPerkPulse();
            }
        }
    }

    private void StartPerkPulse()
    {
        StopPerkPulse();
        if (nextPerkContainer == null) return;

        perkPulseSequence = DOTween.Sequence();
        perkPulseSequence.Append(nextPerkContainer.transform.DOScale(perkPulseScale, perkPulseDuration).SetEase(Ease.InOutQuad));
        perkPulseSequence.Append(nextPerkContainer.transform.DOScale(1f, perkPulseDuration).SetEase(Ease.InOutQuad));
        perkPulseSequence.Append(nextPerkContainer.transform.DOScale(perkPulseScale, perkPulseDuration).SetEase(Ease.InOutQuad));
        perkPulseSequence.Append(nextPerkContainer.transform.DOScale(1f, perkPulseDuration).SetEase(Ease.InOutQuad));
        perkPulseSequence.AppendInterval(perkPulsePause);
        perkPulseSequence.SetLoops(-1);
    }

    private void StopPerkPulse()
    {
        if (perkPulseSequence != null)
        {
            perkPulseSequence.Kill();
            perkPulseSequence = null;
        }
        if (nextPerkContainer != null)
        {
            nextPerkContainer.transform.localScale = Vector3.one;
        }
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
        
        // Restore level visibility
        if (GameController.Instance != null)
        {
            GameController.Instance.SetLevelVisibility(true);
        }
        
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
        
        // Hide level prefab while in building UI
        if (GameController.Instance != null)
        {
            GameController.Instance.SetLevelVisibility(false);
        }
        
        // Ensure camera is positioned correctly immediately
        if (baseViewCamera != null)
        {
            UpdateCameraRotation();
        }
    }

    private void ShowNoEnergyPopup()
    {
        if (noEnergyPopupUI == null) return;
        
        // Scale animation
        RectTransform popupRect = noEnergyPopupUI.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            popupRect.localScale = Vector3.zero;
            popupRect.DOScale(Vector3.one, popupScaleAnimationDuration).SetEase(Ease.OutBack);
        }
        
        // Show popup using SimplePopupUI
        noEnergyPopupUI.Show();
        
        // Update watch ad button state when popup is shown
        UpdateWatchAdButtonState();
        
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
    }
    
    /// <summary>
    /// Update watch ad button state based on ad readiness
    /// </summary>
    private void UpdateWatchAdButtonState()
    {
        if (AdsManager.Instance == null) return;
        
        bool isReady = AdsManager.Instance.IsRewardedAdReady();
        
        if (watchAdLoadingContainer != null) 
            watchAdLoadingContainer.SetActive(!isReady);
        if (watchAdClickContainer != null) 
            watchAdClickContainer.SetActive(isReady);
        
        // Update button interactability
        if (watchAdButton != null)
        {
            watchAdButton.interactable = isReady;
        }
    }

    private void HideNoEnergyPopup()
    {
        if (noEnergyPopupUI == null) return;
        
        RectTransform popupRect = noEnergyPopupUI != null ? noEnergyPopupUI.GetComponent<RectTransform>() : null;
        if (popupRect != null)
        {
            popupRect.DOScale(Vector3.zero, popupScaleAnimationDuration).SetEase(Ease.InBack)
                .OnComplete(() => {
                    noEnergyPopupUI.Close();
                });
        }
        else
        {
            noEnergyPopupUI.Close();
        }
    }

    private async void OnWatchAdButtonClick()
    {
        if (isShowingAd || AdsManager.Instance == null) return;
        
        // Check if ad is ready before proceeding
        if (!AdsManager.Instance.IsRewardedAdReady()) return;
        
        isShowingAd = true;
        if (watchAdButton != null) watchAdButton.interactable = false;
        
        if (buttonClickSound != null) audioSource.PlayOneShot(buttonClickSound);
        
        bool success = await AdsManager.Instance.ShowRewardedAdAsync();
        
        if (success)
        {
            // Hide no energy popup
            HideNoEnergyPopup();
            
            // Add energy spheres reward
            GameManager.Instance.AddEnergySpheres(energyRewardAmount);
            
            // Play success sound
            if (successSound != null) audioSource.PlayOneShot(successSound);
            
            // Show reward popup with confetti
            ShowRewardPopup();
        }
        else
        {
            // Ad failed, re-enable button and update state
            isShowingAd = false;
            UpdateWatchAdButtonState();
        }
    }

    private void ShowRewardPopup()
    {
        if (rewardPopupUI == null) return;
        
        // Update reward amount text
        if (rewardAmountText != null)
        {
            rewardAmountText.text = $"+{energyRewardAmount}";
        }
        
        // Scale animation
        RectTransform popupRect = rewardPopupUI.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            popupRect.localScale = Vector3.zero;
            popupRect.DOScale(Vector3.one, popupScaleAnimationDuration).SetEase(Ease.OutBack);
        }
        
        // Show popup using SimplePopupUI
        rewardPopupUI.Show();
        
        // Ad flow finished successfully; allow another attempt after closing
        isShowingAd = false;
        if (watchAdButton != null) watchAdButton.interactable = true;
        
        // Spawn confetti
        StartCoroutine(SpawnConfettiParticlesRoutine());
        
        // Update balance UI
        UpdateBalanceUI();
    }

    private void HideRewardPopup()
    {
        if (rewardPopupUI == null) return;
        
        RectTransform popupRect = rewardPopupUI != null ? rewardPopupUI.GetComponent<RectTransform>() : null;
        if (popupRect != null)
        {
            popupRect.DOScale(Vector3.zero, popupScaleAnimationDuration).SetEase(Ease.InBack)
                .OnComplete(() => {
                    rewardPopupUI.Close();
                    isShowingAd = false;
                    if (watchAdButton != null) watchAdButton.interactable = true;
                });
        }
        else
        {
            rewardPopupUI.Close();
            isShowingAd = false;
            if (watchAdButton != null) watchAdButton.interactable = true;
        }
    }

    private IEnumerator SpawnConfettiParticlesRoutine()
    {
        RectTransform parent = rewardPopupParent != null ? rewardPopupParent : (rewardPopupUI != null ? rewardPopupUI.GetComponent<RectTransform>() : null);
        if (parent == null) parent = (RectTransform)transform;
        
        int totalBursts = confettiCount / confettiBurstCount;
        if (totalBursts <= 0) totalBursts = 1;
        float interval = confettiSpawnDuration / totalBursts;
        
        for (int i = 0; i < totalBursts; i++)
        {
            // Spawn multiple particles from both sides in each burst
            for (int j = 0; j < confettiBurstCount; j++)
            {
                SpawnSingleConfetti(parent, true);  // Left side
                SpawnSingleConfetti(parent, false); // Right side
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnSingleConfetti(RectTransform parent, bool isLeft)
    {
        GameObject confettiObj = new GameObject("Confetti");
        confettiObj.transform.SetParent(parent, false);
        
        RectTransform rect = confettiObj.AddComponent<RectTransform>();
        Image img = confettiObj.AddComponent<Image>();
        
        // Random color
        if (confettiColors != null && confettiColors.Length > 0)
        {
            img.color = confettiColors[Random.Range(0, confettiColors.Length)];
        }
        else
        {
            img.color = new Color(Random.value, Random.value, Random.value, 1f);
        }
        
        // Random size
        float sizeX = Random.Range(confettiMinSize.x, confettiMaxSize.x);
        float sizeY = Random.Range(confettiMinSize.y, confettiMaxSize.y);
        rect.sizeDelta = new Vector2(sizeX, sizeY);
        
        LayoutElement le = confettiObj.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        img.raycastTarget = false;
        
        // Use Anchors to ensure it stays in the corners regardless of screen size
        if (isLeft)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
        }
        else
        {
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
        }

        // Randomize spawn within anchor zone
        float zoneOffset = confettiSpawnZoneSize;
        float startX = isLeft ? Random.Range(0, zoneOffset) : Random.Range(-zoneOffset, 0);
        float startY = Random.Range(0, zoneOffset);
        rect.anchoredPosition = new Vector2(startX, startY);
        
        // Random initial rotation
        rect.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
        
        float duration = Random.Range(confettiAnimationDuration * 0.8f, confettiAnimationDuration * 1.2f);
        
        // Peak and End positions relative to the anchor
        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        
        // Horizontal distance: how far across the screen to travel
        float horizontalTravel = isLeft ? Random.Range(parentWidth * 0.3f, parentWidth * 0.8f) : Random.Range(-parentWidth * 0.8f, -parentWidth * 0.3f);
        
        // Vertical peaks
        float peakY = Random.Range(parentHeight * 0.4f, parentHeight * 0.9f);
        float endY = -100f; // Below the anchor (off screen)

        // Sequence for the trajectory
        Sequence trajectory = DOTween.Sequence();
        
        // Horizontal movement: burst towards center
        trajectory.Join(rect.DOAnchorPosX(startX + horizontalTravel, duration).SetEase(Ease.OutQuad));
        
        // Vertical movement: burst UP, then fall DOWN
        Sequence verticalSeq = DOTween.Sequence();
        verticalSeq.Append(rect.DOAnchorPosY(peakY, duration * 0.4f).SetEase(Ease.OutQuad));
        verticalSeq.Append(rect.DOAnchorPosY(endY, duration * 0.6f).SetEase(Ease.InQuad));
        trajectory.Join(verticalSeq);
        
        // Rotation and flipping
        rect.DORotate(new Vector3(0, 0, Random.Range(-720f, 720f)), duration, RotateMode.FastBeyond360);
        rect.DOScaleX(0, duration / 4).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        
        // Fade and Cleanup
        CanvasGroup cg = confettiObj.AddComponent<CanvasGroup>();
        cg.DOFade(0, 0.5f).SetDelay(duration - 0.5f).OnComplete(() => {
            if (confettiObj != null) Destroy(confettiObj);
        });
    }
}
