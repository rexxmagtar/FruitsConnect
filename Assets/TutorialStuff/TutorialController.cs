using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JigsawSystem;
using UI;
using DataRepository;

namespace Managers
{
    public class TutorialController : MonoBehaviour
    {
        private static TutorialController _instance;
        public static TutorialController Instance => _instance;

        [Header("References")]
        [SerializeField] private Image darkOverlay;
        [SerializeField] private RectTransform lightRect;
        [SerializeField] private RectTransform fingerPointer;

        [Header("Tutorial Settings")]
        [SerializeField] private float fingerPointerOffset = 100f;
        [SerializeField] private float fingerPointerScaleTime = 0.5f;
        [SerializeField] private float fingerPointerScaleMultiplier = 0.8f;

        private bool isTutorialActive = false;
        private Material darkOverlayMaterial;
        private Tween fingerPointerTween;
        private TutorialRaycastFilter raycastFilter;

        private static readonly int RectXProperty = Shader.PropertyToID("_RectX");
        private static readonly int RectYProperty = Shader.PropertyToID("_RectY");
        private static readonly int RectWidthProperty = Shader.PropertyToID("_RectWidth");
        private static readonly int RectHeightProperty = Shader.PropertyToID("_RectHeight");
        private static readonly int ShapeTypeProperty = Shader.PropertyToID("_ShapeType");

        // Tutorial progress keys
        private const string PUZZLE_TUTORIAL_KEY = "Tutorial_Puzzle_Completed";
        private const string SKIN_TUTORIAL_KEY = "Tutorial_Skin_Completed";
        private const string UPGRADE_TUTORIAL_KEY = "Tutorial_Upgrade_Completed";
        private const string BASE_BUILDING_TUTORIAL_KEY = "Tutorial_BaseBuilding_Completed";

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (darkOverlay != null)
            {
                darkOverlayMaterial = darkOverlay.material;
                darkOverlay.gameObject.SetActive(false);

                // Add or get the filter component
                raycastFilter = darkOverlay.GetComponent<TutorialRaycastFilter>();
                if (raycastFilter == null)
                    raycastFilter = darkOverlay.gameObject.AddComponent<TutorialRaycastFilter>();
            }
            
            if (lightRect != null)
                lightRect.gameObject.SetActive(false);
                
            if (fingerPointer != null)
                fingerPointer.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isTutorialActive) return;

            CheckForTutorialTriggers();
        }

        private void CheckForTutorialTriggers()
        {
            MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
            if (mainMenu == null || !mainMenu.gameObject.activeInHierarchy || !mainMenu.IsVisible()) return;

            int currentLevelIndex = 0;
            LevelsManager levelsManager = LevelsManager.Instance;
            if (levelsManager != null)
            {
                currentLevelIndex = levelsManager.GetCurrentLevelNumber() - 1;
            }

            // Puzzle Tutorial: Level 6 completed (index >= 6)
            if (currentLevelIndex >= 6 && PlayerPrefs.GetInt(PUZZLE_TUTORIAL_KEY, 0) == 0)
            {
                StartCoroutine(PuzzleTutorialRoutine(mainMenu));
            }
            // Skin Tutorial: Level 10 completed (index >= 10)
            else if (currentLevelIndex >= 10 && PlayerPrefs.GetInt(SKIN_TUTORIAL_KEY, 0) == 0)
            {
                StartCoroutine(SkinTutorialRoutine(mainMenu));
            }
            // Upgrade Tutorial: Level 3 completed (index >= 3)
            else if (currentLevelIndex >= 3 && PlayerPrefs.GetInt(UPGRADE_TUTORIAL_KEY, 0) == 0)
            {
                StartCoroutine(UpgradeTutorialRoutine(mainMenu));
            }
            // Base Building Tutorial: Level 15 completed (index >= 15)
            else if (currentLevelIndex >= 15 && PlayerPrefs.GetInt(BASE_BUILDING_TUTORIAL_KEY, 0) == 0)
            {
                StartCoroutine(BaseBuildingTutorialRoutine(mainMenu));
            }
        }

        private IEnumerator PuzzleTutorialRoutine(MainMenuUI mainMenu)
        {
            isTutorialActive = true;
            darkOverlay.gameObject.SetActive(true);

            // Step 1: Highlight Puzzle Button
            Button jigsawButton = GetPrivateField<Button>(mainMenu, "jigsawButton");
            if (jigsawButton != null)
            {
                RectTransform jigsawRect = jigsawButton.GetComponent<RectTransform>();
                UpdateLightZone(jigsawRect);
                ShowFingerPointer(jigsawRect);

                // Wait for jigsaw select screen to open
                GameObject jigsawSelectScreen = GetPrivateField<JigsawPuzzleSelectScreen>(mainMenu, "jigsawSelectScreen").gameObject;
                while (!jigsawSelectScreen.activeInHierarchy)
                {
                    yield return null;
                }

                // Step 2: Highlight a puzzle with pieces
                HideFingerPointer();
                lightRect.gameObject.SetActive(false);
                
                // Wait for buttons to be refreshed
                yield return new WaitForSeconds(0.2f);
                
                JigsawPuzzleSelectScreen selectScreen = jigsawSelectScreen.GetComponent<JigsawPuzzleSelectScreen>();
                List<PuzzleButtonUi> activeButtons = GetPrivateField<List<PuzzleButtonUi>>(selectScreen, "activeButtons");
                
                PuzzleButtonUi targetButton = null;
                foreach (var btn in activeButtons)
                {
                    JigsawPuzzleData data = GetPrivateField<JigsawPuzzleData>(btn, "data");
                    if (data != null && JigsawPuzzleManager.Instance.GetCollectedPieceCount(data.puzzleId) > 0)
                    {
                        targetButton = btn;
                        break;
                    }
                }

                if (targetButton != null)
                {
                    RectTransform btnRect = targetButton.GetComponent<RectTransform>();
                    UpdateLightZone(btnRect);
                    ShowFingerPointer(btnRect);

                    // Wait for user to click (solve UI opens or select screen closes)
                    GameObject solveUI = GetPrivateField<PuzzleSolveUI>(selectScreen, "solveUI").gameObject;
                    while (!solveUI.activeInHierarchy && jigsawSelectScreen.activeInHierarchy)
                    {
                        yield return null;
                    }
                }
            }

            CompleteTutorial(PUZZLE_TUTORIAL_KEY);
        }

        private IEnumerator SkinTutorialRoutine(MainMenuUI mainMenu)
        {
            isTutorialActive = true;
            darkOverlay.gameObject.SetActive(true);

            // Step 1: Highlight Skin Select Button
            Button skinButton = GetPrivateField<Button>(mainMenu, "skinSelectButton");
            if (skinButton != null)
            {
                RectTransform skinRect = skinButton.GetComponent<RectTransform>();
                UpdateLightZone(skinRect);
                ShowFingerPointer(skinRect);

                // Wait for skin select UI to open
                GameObject skinSelectUI = GetPrivateField<HitParticlesSelectUI>(mainMenu, "hitParticlesSelectUI").gameObject;
                while (!skinSelectUI.activeInHierarchy)
                {
                    yield return null;
                }
            }

            CompleteTutorial(SKIN_TUTORIAL_KEY);
        }

        private IEnumerator UpgradeTutorialRoutine(MainMenuUI mainMenu)
        {
            isTutorialActive = true;
            darkOverlay.gameObject.SetActive(true);

            // Step 1: Hint Damage Upgrade Buy Button
            ProgressPurchaseContainer damageContainer = GetPrivateField<ProgressPurchaseContainer>(mainMenu, "monsterDamageContainer");
            if (damageContainer != null)
            {
                Button buyButton = GetPrivateField<Button>(damageContainer, "purchaseButton");
                if (buyButton != null)
                {
                    RectTransform buyRect = buyButton.GetComponent<RectTransform>();
                    UpdateLightZone(buyRect);
                    ShowFingerPointer(buyRect);

                    bool damageUpgraded = false;
                    System.Action<UpgradableParam, bool> onUpgrade = (param, isLevelComp) =>
                    {
                        if (param is MonsterDamage) damageUpgraded = true;
                    };
                    PlayerProgressController.OnUpgradePurchased += onUpgrade;

                    while (!damageUpgraded)
                    {
                        // Update hole position in case of UI layout changes
                        UpdateLightZone(buyRect);
                        yield return null;
                    }
                    PlayerProgressController.OnUpgradePurchased -= onUpgrade;
                }
            }

            // Step 2: Hint Delivery Speed Upgrade Buy Button
            HideFingerPointer();
            lightRect.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);

            ProgressPurchaseContainer speedContainer = GetPrivateField<ProgressPurchaseContainer>(mainMenu, "connectionSpeedContainer");
            if (speedContainer != null)
            {
                Button buyButton = GetPrivateField<Button>(speedContainer, "purchaseButton");
                if (buyButton != null)
                {
                    RectTransform buyRect = buyButton.GetComponent<RectTransform>();
                    UpdateLightZone(buyRect);
                    ShowFingerPointer(buyRect);

                    bool speedUpgraded = false;
                    System.Action<UpgradableParam, bool> onUpgrade = (param, isLevelComp) =>
                    {
                        if (param is ConnectionSpeed) speedUpgraded = true;
                    };
                    PlayerProgressController.OnUpgradePurchased += onUpgrade;

                    while (!speedUpgraded)
                    {
                        // Update hole position in case of UI layout changes
                        UpdateLightZone(buyRect);
                        yield return null;
                    }
                    PlayerProgressController.OnUpgradePurchased -= onUpgrade;
                }
            }

            CompleteTutorial(UPGRADE_TUTORIAL_KEY);
        }

        private IEnumerator BaseBuildingTutorialRoutine(MainMenuUI mainMenu)
        {
            isTutorialActive = true;
            darkOverlay.gameObject.SetActive(true);

            // Step 1: Highlight Base Building Button
            Button baseBuildingButton = GetPrivateField<Button>(mainMenu, "baseBuildingButton");
            if (baseBuildingButton != null)
            {
                RectTransform baseRect = baseBuildingButton.GetComponent<RectTransform>();
                UpdateLightZone(baseRect);
                ShowFingerPointer(baseRect);

                // Wait for Base Building UI to open
                BaseBuildingUI baseBuildingUI = GetPrivateField<BaseBuildingUI>(mainMenu, "baseBuildingUI");
                while (baseBuildingUI != null && !baseBuildingUI.gameObject.activeInHierarchy)
                {
                    yield return null;
                }

                if (baseBuildingUI != null)
                {
                    // Step 2: Highlight Build Button
                    HideFingerPointer();
                    lightRect.gameObject.SetActive(false);
                    yield return new WaitForSeconds(0.2f);

                    Button buildButton = GetPrivateField<Button>(baseBuildingUI, "buildButton");
                    if (buildButton != null)
                    {
                        RectTransform buildRect = buildButton.GetComponent<RectTransform>();
                        UpdateLightZone(buildRect);
                        ShowFingerPointer(buildRect);

                        // Wait for user to start building
                        bool startedBuilding = false;
                        while (!startedBuilding && baseBuildingUI.gameObject.activeInHierarchy)
                        {
                            UpdateLightZone(buildRect);
                            if (GetPrivateField<bool>(baseBuildingUI, "isHoldingBuild"))
                            {
                                startedBuilding = true;
                            }
                            yield return null;
                        }
                    }
                }
            }

            CompleteTutorial(BASE_BUILDING_TUTORIAL_KEY);
        }

        private void UpdateLightZone(RectTransform targetRect)
        {
            if (lightRect == null || darkOverlayMaterial == null) return;

            lightRect.gameObject.SetActive(true);
            
            Vector2 size = targetRect.rect.size;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, targetRect.position);
            
            // Adjust for pivot if necessary (assuming center pivot for simplification, 
            // but RectTransformUtility.WorldToScreenPoint usually gives pivot position)
            // We want the bottom-left for the shader
            Vector2 shaderPos = screenPoint - (size * targetRect.pivot);

            lightRect.sizeDelta = size;
            lightRect.position = screenPoint;
            
            darkOverlayMaterial.SetFloat(RectXProperty, shaderPos.x);
            darkOverlayMaterial.SetFloat(RectYProperty, shaderPos.y);
            darkOverlayMaterial.SetFloat(RectWidthProperty, size.x);
            darkOverlayMaterial.SetFloat(RectHeightProperty, size.y);
            darkOverlayMaterial.SetFloat(ShapeTypeProperty, 1); // Rectangle

            // Update the raycast filter so clicks pass through this exact area
            if (raycastFilter != null)
            {
                raycastFilter.SetHole(new Rect(shaderPos, size), true);
            }
        }

        private void ShowFingerPointer(RectTransform target)
        {
            if (fingerPointer == null) return;

            fingerPointer.gameObject.SetActive(true);
            fingerPointer.position = target.position + new Vector3(0, fingerPointerOffset, 0);

            fingerPointerTween?.Kill();
            fingerPointer.localScale = Vector3.one;
            fingerPointerTween = fingerPointer.DOScale(Vector3.one * fingerPointerScaleMultiplier, fingerPointerScaleTime)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void HideFingerPointer()
        {
            if (fingerPointer != null)
                fingerPointer.gameObject.SetActive(false);
            
            fingerPointerTween?.Kill();
        }

        private void CompleteTutorial(string key)
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            isTutorialActive = false;
            if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);
            if (lightRect != null) lightRect.gameObject.SetActive(false);
            HideFingerPointer();
        }

        // Helper to access private fields from other classes without modifying them
        private T GetPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return (T)field.GetValue(instance);
            }
            return default;
        }

        public bool IsTutorialActive => isTutorialActive;

        [ContextMenu("Reset Tutorials")]
        public void ResetTutorials()
        {
            PlayerPrefs.DeleteKey(PUZZLE_TUTORIAL_KEY);
            PlayerPrefs.DeleteKey(SKIN_TUTORIAL_KEY);
            PlayerPrefs.DeleteKey(UPGRADE_TUTORIAL_KEY);
            PlayerPrefs.DeleteKey(BASE_BUILDING_TUTORIAL_KEY);
            PlayerPrefs.Save();
            Debug.Log("Tutorials reset");
        }
    }

    /// <summary>
    /// Helper component to allow clicks to pass through the tutorial hole
    /// </summary>
    public class TutorialRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private Rect _activeHole;
        private bool _isRectangle;

        public void SetHole(Rect hole, bool isRectangle)
        {
            _activeHole = hole;
            _isRectangle = isRectangle;
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // If the point is INSIDE the hole, we return FALSE so the click passes THROUGH
            // If the point is OUTSIDE the hole, we return TRUE so the dark overlay BLOCKS it

            if (_isRectangle)
            {
                return !_activeHole.Contains(sp);
            }
            else
            {
                // Ellipse check
                Vector2 center = _activeHole.center;
                Vector2 radius = _activeHole.size * 0.5f;
                if (radius.x <= 0 || radius.y <= 0) return true;

                Vector2 normalized = new Vector2((sp.x - center.x) / radius.x, (sp.y - center.y) / radius.y);
                return normalized.sqrMagnitude > 1f;
            }
        }
    }
}
