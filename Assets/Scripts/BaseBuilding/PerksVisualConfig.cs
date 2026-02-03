using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PerksVisualConfig", menuName = "Fruit Connect/Base Building/Perks Visual Config")]
public class PerksVisualConfig : ScriptableObject
{
    [System.Serializable]
    public class PerkVisualInfo
    {
        public PerkType type;
        public Sprite icon;
        public string displayName;
        public string displayNameFormat = "+{0}%";
        public string descriptionFormat = "{0}";
    }

    public List<PerkVisualInfo> perkVisuals;

    public PerkVisualInfo GetPerkVisual(PerkType type)
    {
        return perkVisuals.Find(v => v.type == type);
    }
}
