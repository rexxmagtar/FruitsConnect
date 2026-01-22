using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class HitParticleUiButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI priceField;
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

            if (moneyPriceContainer != null)
                moneyPriceContainer.SetActive(!isUnlocked);

            if (priceField != null)
            {
                if (_data.price == 0)
                    priceField.text = "Free";
                else
                    priceField.text = _data.price.ToString();
            }

            bool isAffordable = HitParticlesManager.Instance.IsUnlocked(_data.id) || GameManager.Instance.GetCoins() >= _data.price;
            bool shouldBeGray = !_isAccessible || !isAffordable;

            foreach (var img in targetImages)
            {
                if (img != null)
                {
                    img.color = shouldBeGray ? Color.gray : Color.white;
                }
            }

            if (button != null)
            {
                button.interactable = _isAccessible && (isUnlocked || GameManager.Instance.GetCoins() >= _data.price);
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
