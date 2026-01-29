using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AdsServices;

namespace UI
{
    public class HitParticleUiButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI priceField;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private TextMeshProUGUI displayNameField;
        [SerializeField] private TextMeshProUGUI damagePowerField;
        [SerializeField] private TextMeshProUGUI connectionSpeedField;
        [SerializeField] private Image effectImage;
        [SerializeField] private GameObject moneyPriceContainer;
        [SerializeField] private GameObject adPriceContainer;
        [SerializeField] private Image adIcon;
        [SerializeField] private Image loadingImage;
        [SerializeField] private Image blackPreviewImage;
        [SerializeField] private System.Collections.Generic.List<Image> targetImages = new System.Collections.Generic.List<Image>();
        [SerializeField] private Button button;
        [SerializeField] private Image selectionBorder;

        private HitParticlesData _data;
        private HitParticlesSelectUI _parentUI;
        private bool _isAccessible;
        private bool _isWatchingAd = false;
        private float _rotationSpeed = 180f;

        public void Initialize(HitParticlesData data, HitParticlesSelectUI parentUI, bool isAccessible)
        {
            _data = data;
            _parentUI = parentUI;
            _isAccessible = isAccessible;

            if (effectImage != null)
                effectImage.sprite = data.effectSprite;

            if (blackPreviewImage != null)
            {
                blackPreviewImage.gameObject.SetActive(!_isAccessible);
                if (!_isAccessible)
                {
                    blackPreviewImage.sprite = data.effectSprite;
                }
            }

            if (displayNameField != null)
                displayNameField.text = data.displayName;

            UpdateUI();
            
            _lastAdReadyState = AdsManager.Instance.IsRewardedAdReady();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        public void UpdateUI()
        {
            bool isUnlocked = HitParticlesManager.Instance.IsUnlocked(_data.id);
            bool isSelected = HitParticlesManager.Instance.GetCurrentParticle()?.id == _data.id;
            
            bool isAdPrice = _data.price == "ad";
            int priceValue = 0;
            int.TryParse(_data.price, out priceValue);
            bool isAffordable = GameManager.Instance.GetCoins() >= priceValue;
            bool isAdReady = AdsManager.Instance.IsRewardedAdReady();

            if (damagePowerField != null)
                damagePowerField.text =  _data.damagePowerValue.ToString();
            
            if (connectionSpeedField != null)
                connectionSpeedField.text =  _data.connectionSpeedValue.ToString();

            // Handle Price Containers
            if (moneyPriceContainer != null)
                moneyPriceContainer.SetActive(!isUnlocked && !isAdPrice);
            
            if (adPriceContainer != null)
                adPriceContainer.SetActive(!isUnlocked && isAdPrice);

            // Handle Ad Icon and Loading Image
            if (isAdPrice && !isUnlocked)
            {
                if (adIcon != null) adIcon.gameObject.SetActive(isAdReady);
                if (loadingImage != null) loadingImage.gameObject.SetActive(!isAdReady);
            }

            Color targetColor = Color.white;

            if (isUnlocked)
            {
                if (priceField != null) priceField.gameObject.SetActive(false);
                if (statusField != null)
                {
                    statusField.gameObject.SetActive(true);
                    if (isSelected)
                    {
                        targetColor = Color.green;
                        statusField.text = "Selected";
                    }
                    else
                    {
                        targetColor = Color.blue;
                        statusField.text = "Select";
                    }
                }
            }
            else
            {
                // Locked
                if (statusField != null) statusField.gameObject.SetActive(false);
                if (priceField != null)
                {
                    priceField.gameObject.SetActive(!isAdPrice);
                    if (_data.price == "0" || _data.price == "Free")
                        priceField.text = "Free";
                    else
                        priceField.text = _data.price;
                }

                if (isAdPrice)
                {
                    targetColor = isAdReady ? Color.blue : Color.gray;
                }
                else if (!_isAccessible || !isAffordable)
                {
                    targetColor = Color.gray;
                }
                else
                {
                    targetColor = Color.blue;
                }
            }

            foreach (var img in targetImages)
            {
                if (img != null)
                {
                    img.color = targetColor;
                }
            }

            if (button != null)
            {
                bool shouldButtonBeActive = _isAccessible || isUnlocked;
                button.gameObject.SetActive(shouldButtonBeActive);

                if (shouldButtonBeActive)
                {
                    // Allow clicking if it's unlocked OR (accessible and affordable) OR (accessible and ad ready)
                    if (isUnlocked)
                    {
                        button.interactable = true;
                    }
                    else if (_isAccessible)
                    {
                        if (isAdPrice)
                            button.interactable = isAdReady;
                        else
                            button.interactable = isAffordable;
                    }
                    else
                    {
                        button.interactable = false;
                    }
                }
            }

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(isSelected);
            }
        }

        private bool _lastAdReadyState = false;

        private void Update()
        {
            if (_data == null) return;

            bool isUnlocked = HitParticlesManager.Instance.IsUnlocked(_data.id);
            bool isAdPrice = _data.price == "ad";

            if (!isUnlocked && isAdPrice)
            {
                bool isAdReady = AdsManager.Instance.IsRewardedAdReady();
                
                // Animate loading image if it's active
                if (loadingImage != null && loadingImage.gameObject.activeSelf)
                {
                    loadingImage.rectTransform.Rotate(Vector3.forward, -_rotationSpeed * Time.deltaTime);
                }

                // Refresh UI state if ad readiness changes
                if (isAdReady != _lastAdReadyState && !_isWatchingAd)
                {
                    _lastAdReadyState = isAdReady;
                    UpdateUI();
                }
            }
        }

        private async void OnButtonClicked()
        {
            if (!_isAccessible) return;

            if (HitParticlesManager.Instance.IsUnlocked(_data.id))
            {
                HitParticlesManager.Instance.SelectParticle(_data.id);
                _parentUI.RefreshAllButtons();
                _parentUI.PlaySelectSound();
            }
            else
            {
                if (_data.price == "ad")
                {
                    if (_isWatchingAd) return;
                    _isWatchingAd = true;

                    bool success = await AdsManager.Instance.ShowRewardedAdAsync();
                    
                    if (success)
                    {
                        HitParticlesManager.Instance.UnlockParticleDirectly(_data.id);
                        HitParticlesManager.Instance.SelectParticle(_data.id);
                        _parentUI.RefreshAllButtons();
                        _parentUI.PlayPurchaseSound();
                        
                        if (_parentUI != null && effectImage != null)
                        {
                            _parentUI.TriggerPurchaseEffect(effectImage.rectTransform);
                        }
                    }
                    
                    _isWatchingAd = false;
                    UpdateUI();
                }
                else
                {
                    if (HitParticlesManager.Instance.UnlockParticle(_data))
                    {
                        HitParticlesManager.Instance.SelectParticle(_data.id);
                        _parentUI.RefreshAllButtons();
                        _parentUI.PlayPurchaseSound();
                        
                        // Trigger purchase effect
                        if (_parentUI != null && effectImage != null)
                        {
                            _parentUI.TriggerPurchaseEffect(effectImage.rectTransform);
                        }
                    }
                }
            }
        }
    }
}
