using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BaseConfig", menuName = "Fruit Connect/Base Building/Base Config")]
public class BaseConfig : ScriptableObject
{
    [System.Serializable]
    public class BaseObjectInfo
    {
        public string displayName;
        public GameObject prefab;
        public List<int> stagePrices; // Price for each of the 10 stages
        public List<PerkInfo> stagePerks; // Perk for each of the 10 stages
    }

    public List<BaseObjectInfo> baseObjects;

    public BaseObjectInfo GetBaseObjectInfo(int objectIndex)
    {
        if (objectIndex >= 0 && objectIndex < baseObjects.Count)
        {
            return baseObjects[objectIndex];
        }
        return null;
    }
}
