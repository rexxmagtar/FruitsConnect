using UnityEngine;
using System.Collections.Generic;

public class PerksManager : MonoBehaviour
{
    private static PerksManager _instance;
    public static PerksManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("PerksManager");
                _instance = go.AddComponent<PerksManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [SerializeField] private BaseConfig baseConfig;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (baseConfig == null)
        {
            baseConfig = Resources.Load<BaseConfig>("BaseConfig");
            if (baseConfig == null)
            {
                // Try to find it in the project if Resources load fails (though in runtime it should be in Resources or assigned)
                // For now let's assume it's assigned or loaded.
            }
        }
    }

    public void SetConfig(BaseConfig config)
    {
        baseConfig = config;
    }

    public float GetTotalBonus(PerkType type)
    {
        if (baseConfig == null) return 0f;

        int totalLevel = SaveDataExtensions.GetBaseLevel();
        float totalBonus = 0f;

        // Iterate through all objects and their stages up to totalLevel
        int levelsProcessed = 0;
        for (int i = 0; i < baseConfig.baseObjects.Count; i++)
        {
            var info = baseConfig.baseObjects[i];
            if (info.stagePerks == null) continue;

            for (int j = 0; j < info.stagePerks.Count; j++)
            {
                if (levelsProcessed < totalLevel)
                {
                    if (info.stagePerks[j].type == type)
                    {
                        totalBonus += info.stagePerks[j].value;
                    }
                    levelsProcessed++;
                }
                else
                {
                    break;
                }
            }

            if (levelsProcessed >= totalLevel) break;
        }

        return totalBonus;
    }

    public PerkInfo GetNextPerkInfo()
    {
        if (baseConfig == null) return null;

        int totalLevel = SaveDataExtensions.GetBaseLevel();
        int objIndex = totalLevel / 10;
        int stageIndex = totalLevel % 10;

        if (objIndex < baseConfig.baseObjects.Count)
        {
            var info = baseConfig.baseObjects[objIndex];
            if (info.stagePerks != null && stageIndex < info.stagePerks.Count)
            {
                return info.stagePerks[stageIndex];
            }
        }

        return null;
    }
}
