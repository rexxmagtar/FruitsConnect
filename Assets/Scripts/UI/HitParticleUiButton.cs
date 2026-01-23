using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class HitParticleUiButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI priceField;
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private Image effectImage;
        [SerializeField] private GameObject moneyPriceContainer;
        [SerializeField] private System.Collections.Generic.List<Image> targetImages = new System.Collections.Generic.List<Image>();
        [SerializeField] private Button button;
        [SerializeField] private Image selectionBorder;

        private HitParticlesData _data;
        private HitParticlesSelectUI _parentUI;
        private bool _isAccessible;

        public void Initialize(HitParticlesData data, HitParticlesSelectUI parentUI, bool isAccessible)
        {
            _data = data;
            _parentUI = parentUI;
            _isAccessible = isAccessible;

            if (effectImage != null)
                effectImage.sprite = data.effectSprite;

            UpdateUI();

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
            bool isAffordable = GameManager.Instance.GetCoins() >= _data.price;

            // Handle Money Icon (moneyPriceContainer)
            if (moneyPriceContainer != null)
                moneyPriceContainer.SetActive(!isUnlocked);

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
                    priceField.gameObject.SetActive(true);
                    if (_data.price == 0)
                        priceField.text = "Free";
                    else
                        priceField.text = _data.price.ToString();
                }

                if (!_isAccessible || !isAffordable)
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
                // Allow clicking if it's unlocked OR (accessible and affordable)
                button.interactable = isUnlocked || (_isAccessible && isAffordable);
            }

            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(isSelected);
            }
        }

        private void OnButtonClicked()
        {
            if (!_isAccessible) return;

            if (HitParticlesManager.Instance.IsUnlocked(_data.id))
            {
                HitParticlesManager.Instance.SelectParticle(_data.id);
                _parentUI.RefreshAllButtons();
            }
            else
            {
                if (HitParticlesManager.Instance.UnlockParticle(_data))
                {
                    HitParticlesManager.Instance.SelectParticle(_data.id);
                    _parentUI.RefreshAllButtons();
                }
            }
        }
    }
}
