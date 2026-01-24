using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DataRepository;
using TMPro;

namespace UI
{
    public class HitParticlesSelectUI : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private List<Transform> stageContainers = new List<Transform>();
        [SerializeField] private List<GameObject> stageLockers = new List<GameObject>();
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI balanceText;

        [Header("Prefabs")]
        [SerializeField] private HitParticleUiButton buttonPrefab;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        public event System.Action OnClosed;

        private List<HitParticleUiButton> _buttons = new List<HitParticleUiButton>();

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => 
                {
                    gameObject.SetActive(false);
                    OnClosed?.Invoke();
                });
            }
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            ClearButtons();
            UpdateBalance();
            
            var allParticles = HitParticlesManager.Instance.GetAllParticles();
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            int currentLevel = saveData.CurrentLevel;

          

            foreach (var particle in allParticles)
            {
                int stageIndex = particle.stage;
                if (stageIndex >= 0 && stageIndex < stageContainers.Count)
                {
                    var container = stageContainers[stageIndex];
                    if (container != null)
                    {
                        var btn = Instantiate(buttonPrefab, container);
                        bool isAccessible = stageIndex == 0 || currentLevel >= stageIndex * 15;
                        btn.Initialize(particle, this, isAccessible);
                        _buttons.Add(btn);
                    }
                }
            }

              // Update stage lockers visibility
            // Stage 0: default, Stage 1: level 15, Stage 2: level 30...
            for (int i = 0; i < stageLockers.Count; i++)
            {
                bool isStageAccessible = i == 0 || currentLevel >= i * 15;
                if (stageLockers[i] != null){
                    stageLockers[i].SetActive(!isStageAccessible);
                    stageLockers[i].transform.SetAsLastSibling();
                }
            }
        }

        public void RefreshAllButtons()
        {
            UpdateBalance();
            foreach (var btn in _buttons)
            {
                btn.UpdateUI();
            }
        }

        private void UpdateBalance()
        {
            if (balanceText != null)
            {
                balanceText.text = GameManager.Instance.GetCoins().ToString();
            }
        }

        private void ClearButtons()
        {
            foreach (var btn in _buttons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _buttons.Clear();
        }
    }
}
