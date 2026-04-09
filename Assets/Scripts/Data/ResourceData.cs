using UnityEngine;

namespace JungleVoodoo.Data
{
    public enum ResourceType
    {
        SpiritEnergy,   // Primary currency (like Cash in Mafia City)
        Bones,          // Military material
        DarkHerbs,      // Food / sustenance for troops
        DarkEssence,    // Premium material, gathered from the Cursed Wilds
        VoodooTokens    // Paid premium currency
    }

    /// <summary>
    /// ScriptableObject defining a resource type's display properties.
    /// One asset per ResourceType. Loaded via Addressables at runtime.
    /// Create instances via Assets > Create > JungleVoodoo > Resource Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewResource", menuName = "JungleVoodoo/Resource Data")]
    public class ResourceData : ScriptableObject
    {
        [Header("Identity")]
        public ResourceType Type;
        public string       DisplayName;
        [TextArea(1, 3)]
        public string       Description;
        public Sprite       Icon;
        public Color        UIColor = Color.white;

        [Header("Production")]
        /// <summary>
        /// Base production per hour from a single Level 1 production building.
        /// Actual rate = BaseProductionPerHour * buildingLevel * researchMultiplier.
        /// </summary>
        public float BaseProductionPerHour;

        [Header("Storage")]
        public bool         HasStorageCap = true;
        public int          BaseStorageCapacity;

        [Header("Gathering (Cursed Wilds)")]
        /// <summary>Whether this resource can be found on the world map tiles.</summary>
        public bool CanBeGathered;
        public float GatherRateMultiplier = 1f;
    }
}
