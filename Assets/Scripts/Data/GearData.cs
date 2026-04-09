using System;
using System.Collections.Generic;
using UnityEngine;

namespace JungleVoodoo.Data
{
    public enum GearSlot
    {
        Weapon,
        OffHand,
        Helmet,
        Chest,
        Legs,
        Boots,
        Ring,
        Amulet
    }

    public enum GearRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum GearAcquisitionSource
    {
        RaidDrop,
        EventReward
    }

    /// <summary>
    /// The type of bonus a gear piece or gem can provide.
    /// Flat bonuses are added before percent bonuses are applied.
    /// </summary>
    public enum GearBonusType
    {
        TroopAttackFlat,
        TroopAttackPercent,
        TroopDefenseFlat,
        TroopDefensePercent,
        TroopHPFlat,
        TroopHPPercent,
        MarchCapacity,
        ResourceProductionPercent,
        TrainingSpeedPercent,
        GatherSpeedPercent
    }

    [Serializable]
    public class GearBonus
    {
        public GearBonusType Type;
        public float         Value;
    }

    /// <summary>
    /// ScriptableObject defining a collectible gear piece archetype.
    /// One asset per gear piece type. Player ownership is tracked via GearInstance in PlayerProfile.
    /// Create via Assets > Create > JungleVoodoo > Gear Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGear", menuName = "JungleVoodoo/Gear Data")]
    public class GearData : ScriptableObject
    {
        [Header("Identity")]
        public string               GearId;
        public string               DisplayName;
        [TextArea(1, 3)]
        public string               Description;
        public GearSlot             Slot;
        public GearRarity           Rarity;
        public GearAcquisitionSource Source;
        public Sprite               Icon;

        [Header("Base Stat Bonuses")]
        public List<GearBonus>      BaseStatBonuses = new();

        /// <summary>
        /// Number of gem sockets on this piece.
        /// Common/Uncommon = 1, Rare = 2, Epic/Legendary = 3.
        /// </summary>
        public int SocketCount => Rarity switch
        {
            GearRarity.Common    => 1,
            GearRarity.Uncommon  => 1,
            GearRarity.Rare      => 2,
            GearRarity.Epic      => 3,
            GearRarity.Legendary => 3,
            _                    => 0
        };
    }

    /// <summary>
    /// ScriptableObject defining a gem type.
    /// One asset per gem archetype. Player ownership is tracked via GemInstance in PlayerProfile.
    /// Create via Assets > Create > JungleVoodoo > Gem Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGem", menuName = "JungleVoodoo/Gem Data")]
    public class GemData : ScriptableObject
    {
        [Header("Identity")]
        public string        GemId;
        public string        DisplayName;
        public Sprite        Icon;

        [Header("Bonus")]
        public GearBonusType BonusType;

        /// <summary>Bonus value per tier. Index 0 = Tier I, index 4 = Tier V.</summary>
        public float[]       BonusPerTier = new float[5];

        /// <summary>
        /// Resource cost to upgrade from tier N to N+1.
        /// Index 0 = I→II, index 1 = II→III, index 2 = III→IV, index 3 = IV→V.
        /// Uses existing ResourceCost (resourceId + amount) pattern.
        /// </summary>
        public List<ResourceCost>[] UpgradeCostPerTier = new List<ResourceCost>[4];

        public float GetBonus(int tier)
            => BonusPerTier[Mathf.Clamp(tier - 1, 0, 4)];

        /// <summary>Returns upgrade cost from currentTier to currentTier+1. Returns null at tier V.</summary>
        public List<ResourceCost> GetUpgradeCost(int currentTier)
        {
            if (currentTier >= 5) return null;
            return UpgradeCostPerTier[currentTier - 1];
        }
    }

    /// <summary>
    /// A player-owned instance of a gear piece. Serialized in PlayerProfile.GearInventory.
    /// </summary>
    [Serializable]
    public class GearInstance
    {
        public string       InstanceId;                              // Guid.NewGuid().ToString()
        public string       GearId;                                  // links to GearData.GearId
        public List<string> SocketedGemInstanceIds = new();          // up to GearData.SocketCount
    }

    /// <summary>
    /// A player-owned instance of a gem. Serialized in PlayerProfile.GemInventory.
    /// A gem that is socketed into a GearInstance is still listed here; its InstanceId
    /// appears in GearInstance.SocketedGemInstanceIds to indicate it is in use.
    /// </summary>
    [Serializable]
    public class GemInstance
    {
        public string InstanceId;   // Guid.NewGuid().ToString()
        public string GemId;        // links to GemData.GemId
        public int    Tier;         // 1–5
    }

    /// <summary>
    /// Aggregated stat bonuses from all equipped gear + socketed gems for a single hero.
    /// Passed to CombatSystem and mirrored in CloudScript/main.js for server-authoritative combat.
    ///
    /// Application order: flat bonuses first, then percent bonuses applied on top.
    /// </summary>
    [Serializable]
    public class HeroGearBonuses
    {
        public float TroopAttackFlat;
        public float TroopAttackPercent;
        public float TroopDefenseFlat;
        public float TroopDefensePercent;
        public float TroopHPFlat;
        public float TroopHPPercent;
        public int   MarchCapacity;
        public float ResourceProductionPercent;
        public float TrainingSpeedPercent;
        public float GatherSpeedPercent;
    }
}
