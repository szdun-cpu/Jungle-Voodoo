using System;
using System.Collections.Generic;
using UnityEngine;

namespace JungleVoodoo.Data
{
    public enum BuildingCategory
    {
        Headquarters,   // Great Hut
        Military,       // Zombie Pit, Shamans Lodge
        Production,     // Spirit Well, Bone Forge, Cursed Farm
        Research,       // Witch's Cauldron
        Defense,        // Skull Totem
        Special         // Voodoo Altar
    }

    [Serializable]
    public class ResourceCost
    {
        public string ResourceId;
        public int    Amount;
    }

    /// <summary>
    /// ScriptableObject defining a building type's static data.
    /// Create instances via Assets > Create > JungleVoodoo > Building Data.
    /// Runtime state (level, position, timer) is stored in BuildingState.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "JungleVoodoo/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string          BuildingId;
        public string          DisplayName;
        [TextArea(2, 4)]
        public string          Description;
        public BuildingCategory Category;
        public Sprite          Icon;
        public GameObject      Prefab;

        [Header("Progression")]
        public int MaxLevel = 25;

        [Header("Per-Level Stats")]
        /// <summary>
        /// Index 0 = Level 1. Each entry describes the stats AT that level.
        /// </summary>
        public List<BuildingLevelData> Levels = new();

        public BuildingLevelData GetLevelData(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
            return Levels[index];
        }
    }

    [Serializable]
    public class BuildingLevelData
    {
        [Header("Construction")]
        public List<ResourceCost> UpgradeCost        = new();
        public int                BuildTimeSeconds;

        [Header("Production (if applicable)")]
        public string             ProducedResourceId;
        public float              ProductionPerHour;
        public int                StorageCapacity;

        [Header("Military (if applicable)")]
        public int                TroopQueueSlots;
        public float              TrainingSpeedBonus; // multiplier, 1.0 = no bonus

        [Header("Combat (if applicable)")]
        public int                DefenseRating;
        public float              TroopAttackBonus;   // multiplier for troops trained here
        public float              TroopDefenseBonus;
    }
}
