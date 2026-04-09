using System;
using System.Collections.Generic;
using UnityEngine;
using JungleVoodoo.Data;

namespace JungleVoodoo.Data
{
    public enum TroopClass
    {
        Infantry,   // Zombie Shambler, Cursed Warrior, Swamp Revenant
        Ranged,     // Bone Thrower, Hex Archer
        Caster,     // Voodoo Witch, Death Witch
        Cavalry,    // Spirit Beast
        Siege,      // Voodoo Doll
        Scout       // Shadow Wraith
    }

    public enum TroopTier { T1 = 1, T2 = 2, T3 = 3, T4 = 4, Special = 5 }

    /// <summary>
    /// ScriptableObject defining a troop type's static data.
    /// Create instances via Assets > Create > JungleVoodoo > Troop Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTroop", menuName = "JungleVoodoo/Troop Data")]
    public class TroopData : ScriptableObject
    {
        [Header("Identity")]
        public string     TroopId;
        public string     DisplayName;
        [TextArea(2, 3)]
        public string     Description;
        public TroopClass Class;
        public TroopTier  Tier;
        public Sprite     Icon;
        public GameObject Prefab;

        [Header("Training")]
        public List<ResourceCost> TrainingCost   = new();
        public int                TrainingTimeSeconds;
        public string             RequiredBuildingId;
        public int                RequiredBuildingLevel;

        [Header("Combat Stats")]
        public int   Attack;
        public int   Defense;
        public int   HP;
        public int   Load;           // resources carried per troop
        public float Speed;          // march speed (tiles per hour)

        [Header("Capacities")]
        public int   FoodUpkeep;     // Dark Herbs consumed per hour per troop
        public int   EncampmentSlots; // how many hospital/camp slots this troop uses when wounded

        [Header("Special Abilities")]
        [TextArea(2, 4)]
        public string AbilityDescription;
        public float  AbilityValue;
    }
}
