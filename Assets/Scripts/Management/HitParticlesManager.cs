using UnityEngine;
using System.Collections.Generic;
using DataRepository;
using System.Linq;

public class HitParticlesManager : MonoBehaviour
{
    private static HitParticlesManager _instance;
    public static HitParticlesManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<HitParticlesManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("HitParticlesManager");
                    _instance = go.AddComponent<HitParticlesManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private List<HitParticlesData> allParticles = new List<HitParticlesData>();
    
    private HitParticlesData _currentParticle;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Initialize()
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        
        // Set default if none selected
        if (string.IsNullOrEmpty(saveData.SelectedHitParticleId))
        {
            // Find default (stage 0, price 0)
            var defaultParticle = allParticles.FirstOrDefault(p => p.stage == 0 && p.price == 0);
            if (defaultParticle != null)
            {
                saveData.SelectedHitParticleId = defaultParticle.id;
                if (!saveData.UnlockedHitParticleIds.Contains(defaultParticle.id))
                {
                    saveData.UnlockedHitParticleIds.Add(defaultParticle.id);
                }
                ProgressSaveManager<SaveData>.Instance.SaveGameData();
            }
        }

        UpdateCurrentParticle();
    }

    private void UpdateCurrentParticle()
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        _currentParticle = allParticles.FirstOrDefault(p => p.id == saveData.SelectedHitParticleId);
        
        // Fallback to first available if not found
        if (_currentParticle == null && allParticles.Count > 0)
        {
            _currentParticle = allParticles[0];
        }
    }

    public List<HitParticlesData> GetAllParticles() => allParticles;

    public bool IsUnlocked(string id)
    {
        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        return saveData.UnlockedHitParticleIds.Contains(id);
    }

    public bool CanSelect(HitParticlesData data)
    {
        return IsUnlocked(data.id);
    }

    public void SelectParticle(string id)
    {
        if (!IsUnlocked(id)) return;

        var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
        saveData.SelectedHitParticleId = id;
        ProgressSaveManager<SaveData>.Instance.SaveGameData();
        UpdateCurrentParticle();
    }

    public bool UnlockParticle(HitParticlesData data)
    {
        if (IsUnlocked(data.id)) return true;

        int currentCoins = GameManager.Instance.GetCoins();
        if (currentCoins >= data.price)
        {
            // Deduct coins
            GameManager.Instance.AddCoins(-data.price);
            
            var saveData = ProgressSaveManager<SaveData>.Instance.GetGameData();
            saveData.UnlockedHitParticleIds.Add(data.id);
            ProgressSaveManager<SaveData>.Instance.SaveGameData();
            return true;
        }

        return false;
    }

    public void SpawnHitParticle(Vector3 position)
    {
        if (_currentParticle == null || _currentParticle.particlePrefab == null) return;

        GameObject effect = Instantiate(_currentParticle.particlePrefab, position, Quaternion.identity);
        
        // Most hit particles in this project seem to have HitEffectPrefab or similar for auto-destruction
        var hitEffect = effect.GetComponent<HitEffectPrefab>();
        if (hitEffect != null)
        {
            hitEffect.Play();
        }
        else
        {
            // Fallback to simple destruction after 1 second if no component found
            Destroy(effect, 1f);
        }
    }

    public HitParticlesData GetCurrentParticle() => _currentParticle;
}
