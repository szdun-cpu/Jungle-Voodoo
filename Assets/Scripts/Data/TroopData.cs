using System;
using System.Collections.Generic;
using UnityEngine;
using JungleVoodoo.Data;

namespace JungleVoodoo.Data
{
    /// <summary>
    /// The five primary combat troop types that form a pentagonal
    /// rock-paper-scissors advantage cycle:
    ///
    ///   WitchDoctor → beats → Harpy
    ///   Harpy       → beats → Gorilla
    ///   Gorilla     → beats → Zombie
    ///   Zombie      → beats → Exorcist
    ///   Exorcist    → beats → WitchDoctor
    ///
    /// "Beats" means the attacker deals bonus damage (TypeAdvantage.StrongMultiplier)
    /// and the defender's effective HP is reduced.
    ///
    /// Scout and Siege are utility types outside the cycle.
    /// </summary>
    public enum TroopType
    {
        WitchDoctor,    // Strong vs Harpy,     weak vs Exorcist
        Exorcist,       // Strong vs WitchDoctor, weak vs Zombie
        Gorilla,        // Strong vs Zombie,     weak vs Harpy
        Zombie,         // Strong vs Exorcist,   weak vs Gorilla
        Harpy,          // Strong vs Gorilla,    weak vs WitchDoctor

        // Utility (outside the advantage cycle)
        Scout,          // Shadow Wraith — reconnaissance only
        Siege           // Voodoo Doll   — structure damage bonus
    }

    /// <summary>
    /// Lookup table for the pentagonal type-advantage system.
    /// Use GetMultiplier() in CombatSystem (and mirror the same table in CloudScript).
    /// </summary>
    public static class TypeAdvantage
    {
        /// <summary>Attack power multiplier when the attacker has type advantage.</summary>
        public const float StrongMultiplier = 1.5f;

        /// <summary>Attack power multiplier when the attacker has type disadvantage.</summary>
        public const float WeakMultiplier   = 0.67f; // ≈ 1 / 1.5

        /// <summary>
        /// Returns the effective attack multiplier for <paramref name="attacker"/>
        /// fighting <paramref name="defender"/>.
        /// </summary>
        public static float GetMultiplier(TroopType attacker, TroopType defender)
        {
            if (Beats(attacker, defender)) return StrongMultiplier;
            if (Beats(defender, attacker)) return WeakMultiplier;
            return 1f;
        }

        /// <summary>Returns true if <paramref name="a"/> has type advantage over <paramref name="b"/>.</summary>
        public static bool Beats(TroopType a, TroopType b)
        {
            return (a, b) switch
            {
                (TroopType.WitchDoctor, TroopType.Harpy)       => true,
                (TroopType.Harpy,       TroopType.Gorilla)     => true,
                (TroopType.Gorilla,     TroopType.Zombie)      => true,
                (TroopType.Zombie,      TroopType.Exorcist)    => true,
                (TroopType.Exorcist,    TroopType.WitchDoctor) => true,
                _                                              => false
            };
        }
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
        public TroopType  Type;
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
