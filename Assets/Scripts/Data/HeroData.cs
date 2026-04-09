using System;
using System.Collections.Generic;
using UnityEngine;
using JungleVoodoo.Data;

namespace JungleVoodoo.Data
{
    public enum WitchDoctorSpecialization
    {
        WarDoctor,      // Buffs troop attack/defense
        CurseDoctor,    // Debuffs enemy troops in combat
        SpiritDoctor,   // Boosts resource production
        RitualDoctor,   // Reduces construction/research time
        ShadowDoctor    // Boosts gathering and scouting
    }

    /// <summary>
    /// ScriptableObject defining a Witch Doctor hero's static data.
    /// One asset per hero archetype. Active hero state is in PlayerProfile.ActiveHero.
    /// Create instances via Assets > Create > JungleVoodoo > Hero Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHero", menuName = "JungleVoodoo/Hero Data")]
    public class HeroData : ScriptableObject
    {
        [Header("Identity")]
        public string                     HeroId;
        public string                     DisplayName;
        [TextArea(2, 4)]
        public string                     Lore;
        public WitchDoctorSpecialization  Specialization;
        public Sprite                     Portrait;
        public GameObject                 Prefab;

        [Header("Passive Bonus (active while hero is in base)")]
        public string  PassiveBonusDescription;
        public float   PassiveBonusValue;         // context-dependent, e.g. 0.10 = 10% boost

        [Header("Level-Up Stats")]
        public List<HeroLevelData> Levels = new();

        [Header("Unlock")]
        public bool                IsStarterHero;
        public List<ResourceCost>  UnlockCost = new();

        public HeroLevelData GetLevelData(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
            return Levels[index];
        }
    }

    [Serializable]
    public class HeroLevelData
    {
        public long  XpRequired;          // cumulative XP to reach this level
        public float AttackMultiplier;    // troops led by this hero
        public float DefenseMultiplier;
        public float SpecialBonusValue;   // specialization-specific bonus
        public int   MarchCapacityBonus;  // extra troops this hero can lead
    }

    [Serializable]
    public class ResourceCost
    {
        public string ResourceId;
        public int    Amount;
    }
}
