using System;
using System.Collections.Generic;
using UnityEngine;
using JungleVoodoo.Data;

namespace JungleVoodoo.Systems
{
    /// <summary>
    /// Manages the collectible gear system for Witch Doctor heroes.
    ///
    /// Responsibilities:
    ///   - Adding gear pieces and gems to the player's inventory (from raid drops / events)
    ///   - Equipping / unequipping gear on a hero (8 slots: Weapon, OffHand, Helmet, Chest,
    ///     Legs, Boots, Ring, Amulet)
    ///   - Socketing gems into gear pieces and upgrading gem tiers (I–V)
    ///   - Calculating combined HeroGearBonuses for CombatSystem and UI
    ///
    /// NOTE FOR CLOUDSCRIPT:
    ///   When CloudScript/main.js is created, the ResolveCombat function must mirror
    ///   GetHeroGearBonuses() to apply gear bonuses server-side. Order of application:
    ///     1. Sum all flat bonuses (TroopAttackFlat, TroopDefenseFlat, TroopHPFlat, MarchCapacity)
    ///     2. Sum all percent bonuses (TroopAttackPercent, TroopDefensePercent, TroopHPPercent)
    ///     3. Effective stat = (base + flat) * (1 + percent / 100)
    ///
    /// Register in GameManager.Bootstrap() after ResourceSystem.
    /// Initialize by calling Initialize(profile) after player data loads.
    /// </summary>
    public class GearSystem
    {
        private readonly ResourceSystem _resourceSystem;
        private PlayerProfile _profile;

        // GearData and GemData assets loaded at initialization (gearId / gemId → asset)
        private readonly Dictionary<string, GearData> _gearDataMap = new();
        private readonly Dictionary<string, GemData>  _gemDataMap  = new();

        public event Action<GearInstance> OnGearAdded;
        public event Action<GearInstance> OnGearEquipped;
        public event Action<GearSlot>     OnGearUnequipped;
        public event Action<GemInstance>  OnGemAdded;
        public event Action<GemInstance>  OnGemUpgraded;
        public event Action<GearInstance> OnGemSocketed;

        public GearSystem(ResourceSystem resourceSystem)
        {
            _resourceSystem = resourceSystem;
        }

        /// <summary>
        /// Call after player data loads. Caches all GearData and GemData ScriptableObjects
        /// so lookups are O(1) at runtime.
        /// </summary>
        public void Initialize(PlayerProfile profile)
        {
            _profile = profile;

            // Load all GearData assets
            var gearAssets = Resources.LoadAll<GearData>("ScriptableObjects");
            foreach (var asset in gearAssets)
                _gearDataMap[asset.GearId] = asset;

            var gemAssets = Resources.LoadAll<GemData>("ScriptableObjects");
            foreach (var asset in gemAssets)
                _gemDataMap[asset.GemId] = asset;
        }

        // ── Inventory ─────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new GearInstance from the given archetype ID and adds it to inventory.
        /// Called by HandleRaidLoot and event reward handlers.
        /// </summary>
        public GearInstance AddGearToInventory(string gearId)
        {
            var instance = new GearInstance
            {
                InstanceId = Guid.NewGuid().ToString(),
                GearId     = gearId,
            };
            _profile.GearInventory.Add(instance);
            OnGearAdded?.Invoke(instance);
            return instance;
        }

        /// <summary>
        /// Creates a new GemInstance at Tier I and adds it to inventory.
        /// Called by HandleRaidLoot and event reward handlers.
        /// </summary>
        public GemInstance AddGemToInventory(string gemId)
        {
            var instance = new GemInstance
            {
                InstanceId = Guid.NewGuid().ToString(),
                GemId      = gemId,
                Tier       = 1,
            };
            _profile.GemInventory.Add(instance);
            OnGemAdded?.Invoke(instance);
            return instance;
        }

        // ── Equipping ─────────────────────────────────────────────────────────

        /// <summary>
        /// Equips a gear piece from inventory onto the hero's correct slot.
        /// If another piece occupies that slot it is moved back to inventory (unequipped).
        /// Returns false if the hero or gear instance is not found.
        /// </summary>
        public bool EquipGear(string heroId, string gearInstanceId)
        {
            var hero = FindHero(heroId);
            if (hero == null)
            {
                Debug.LogWarning($"[GearSystem] Hero not found: {heroId}");
                return false;
            }

            var gearInstance = FindGearInstance(gearInstanceId);
            if (gearInstance == null)
            {
                Debug.LogWarning($"[GearSystem] GearInstance not found: {gearInstanceId}");
                return false;
            }

            if (!_gearDataMap.TryGetValue(gearInstance.GearId, out var gearData))
            {
                Debug.LogWarning($"[GearSystem] GearData asset not loaded for: {gearInstance.GearId}");
                return false;
            }

            int slotIndex = (int)gearData.Slot;

            // Ensure array is initialised (handles profiles created before this system existed)
            if (hero.EquippedGearInstanceIds == null || hero.EquippedGearInstanceIds.Length != 8)
                hero.EquippedGearInstanceIds = new string[8];

            // Unequip whatever is currently in this slot
            string currentId = hero.EquippedGearInstanceIds[slotIndex];
            if (!string.IsNullOrEmpty(currentId))
            {
                // The piece stays in GearInventory; it was never removed — just mark slot empty
                hero.EquippedGearInstanceIds[slotIndex] = null;
                OnGearUnequipped?.Invoke(gearData.Slot);
            }

            hero.EquippedGearInstanceIds[slotIndex] = gearInstanceId;
            OnGearEquipped?.Invoke(gearInstance);
            return true;
        }

        /// <summary>Removes gear from a hero's slot. The piece remains in GearInventory.</summary>
        public void UnequipGear(string heroId, GearSlot slot)
        {
            var hero = FindHero(heroId);
            if (hero == null) return;

            if (hero.EquippedGearInstanceIds == null || hero.EquippedGearInstanceIds.Length != 8)
                return;

            int slotIndex = (int)slot;
            hero.EquippedGearInstanceIds[slotIndex] = null;
            OnGearUnequipped?.Invoke(slot);
        }

        // ── Gem Socketing ─────────────────────────────────────────────────────

        /// <summary>
        /// Sockets a gem into a gear piece.
        /// Fails if the piece is already full or the gem is already socketed elsewhere.
        /// Returns false on validation failure.
        /// </summary>
        public bool SocketGem(string gearInstanceId, string gemInstanceId)
        {
            var gearInstance = FindGearInstance(gearInstanceId);
            if (gearInstance == null)
            {
                Debug.LogWarning($"[GearSystem] GearInstance not found: {gearInstanceId}");
                return false;
            }

            var gemInstance = FindGemInstance(gemInstanceId);
            if (gemInstance == null)
            {
                Debug.LogWarning($"[GearSystem] GemInstance not found: {gemInstanceId}");
                return false;
            }

            if (!_gearDataMap.TryGetValue(gearInstance.GearId, out var gearData))
            {
                Debug.LogWarning($"[GearSystem] GearData asset not loaded for: {gearInstance.GearId}");
                return false;
            }

            if (gearInstance.SocketedGemInstanceIds.Count >= gearData.SocketCount)
            {
                Debug.LogWarning($"[GearSystem] Gear piece {gearInstanceId} has no free sockets.");
                return false;
            }

            // Prevent socketing the same gem instance into two different pieces
            if (IsGemSocketed(gemInstanceId))
            {
                Debug.LogWarning($"[GearSystem] Gem {gemInstanceId} is already socketed in another piece.");
                return false;
            }

            gearInstance.SocketedGemInstanceIds.Add(gemInstanceId);
            OnGemSocketed?.Invoke(gearInstance);
            return true;
        }

        /// <summary>Removes a gem from a gear piece. The gem remains in GemInventory.</summary>
        public void UnsocketGem(string gearInstanceId, string gemInstanceId)
        {
            var gearInstance = FindGearInstance(gearInstanceId);
            if (gearInstance == null) return;

            gearInstance.SocketedGemInstanceIds.Remove(gemInstanceId);
            OnGemSocketed?.Invoke(gearInstance);
        }

        // ── Gem Upgrading ─────────────────────────────────────────────────────

        /// <summary>
        /// Upgrades a gem by one tier (max tier V). Spends resources via ResourceSystem.
        /// Returns false if the gem is already at max tier or the player cannot afford the cost.
        /// </summary>
        public bool UpgradeGem(string gemInstanceId)
        {
            var gemInstance = FindGemInstance(gemInstanceId);
            if (gemInstance == null)
            {
                Debug.LogWarning($"[GearSystem] GemInstance not found: {gemInstanceId}");
                return false;
            }

            if (gemInstance.Tier >= 5)
            {
                Debug.LogWarning($"[GearSystem] Gem {gemInstanceId} is already at max tier.");
                return false;
            }

            if (!_gemDataMap.TryGetValue(gemInstance.GemId, out var gemData))
            {
                Debug.LogWarning($"[GearSystem] GemData asset not loaded for: {gemInstance.GemId}");
                return false;
            }

            var cost = gemData.GetUpgradeCost(gemInstance.Tier);
            if (cost == null || cost.Count == 0)
            {
                Debug.LogWarning($"[GearSystem] No upgrade cost defined for gem {gemInstance.GemId} tier {gemInstance.Tier}.");
                return false;
            }

            var costDict = new Dictionary<string, long>();
            foreach (var c in cost)
                costDict[c.ResourceId] = c.Amount;

            if (!_resourceSystem.CanAfford(costDict))
                return false;

            _resourceSystem.Spend(costDict);
            gemInstance.Tier++;
            OnGemUpgraded?.Invoke(gemInstance);
            return true;
        }

        // ── Stat Aggregation ──────────────────────────────────────────────────

        /// <summary>
        /// Aggregates all bonuses from equipped gear and socketed gems for a given hero.
        /// Called by CombatSystem before building a CombatRequest.
        /// Returns a zeroed HeroGearBonuses if the hero has no gear.
        /// </summary>
        public HeroGearBonuses GetHeroGearBonuses(string heroId)
        {
            var bonuses = new HeroGearBonuses();
            var hero    = FindHero(heroId);

            if (hero?.EquippedGearInstanceIds == null) return bonuses;

            foreach (var instanceId in hero.EquippedGearInstanceIds)
            {
                if (string.IsNullOrEmpty(instanceId)) continue;

                var gear = FindGearInstance(instanceId);
                if (gear == null) continue;

                if (!_gearDataMap.TryGetValue(gear.GearId, out var gearData)) continue;

                // Apply base gear bonuses
                foreach (var b in gearData.BaseStatBonuses)
                    ApplyBonus(bonuses, b.Type, b.Value);

                // Apply socketed gem bonuses
                foreach (var gemId in gear.SocketedGemInstanceIds)
                {
                    var gemInstance = FindGemInstance(gemId);
                    if (gemInstance == null) continue;

                    if (!_gemDataMap.TryGetValue(gemInstance.GemId, out var gemData)) continue;

                    float gemValue = gemData.GetBonus(gemInstance.Tier);
                    ApplyBonus(bonuses, gemData.BonusType, gemValue);
                }
            }

            return bonuses;
        }

        // ── Loot Handling ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates GearInstance and GemInstance entries for loot dropped by a Ritual Raid.
        /// Called from the CombatSystem.OnCombatResolved handler in GameManager.
        /// gearIds and gemIds are archetype IDs from GearData/GemData assets.
        /// </summary>
        public void HandleRaidLoot(List<string> gearIds, List<string> gemIds)
        {
            if (gearIds != null)
                foreach (var id in gearIds)
                    AddGearToInventory(id);

            if (gemIds != null)
                foreach (var id in gemIds)
                    AddGemToInventory(id);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private GearInstance FindGearInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return _profile.GearInventory.Find(g => g.InstanceId == instanceId);
        }

        private GemInstance FindGemInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            return _profile.GemInventory.Find(g => g.InstanceId == instanceId);
        }

        private ActiveHero FindHero(string heroId)
            => _profile.Heroes.Find(h => h.HeroId == heroId);

        private bool IsGemSocketed(string gemInstanceId)
        {
            foreach (var gear in _profile.GearInventory)
                if (gear.SocketedGemInstanceIds.Contains(gemInstanceId))
                    return true;
            return false;
        }

        private static void ApplyBonus(HeroGearBonuses bonuses, GearBonusType type, float value)
        {
            switch (type)
            {
                case GearBonusType.TroopAttackFlat:           bonuses.TroopAttackFlat           += value; break;
                case GearBonusType.TroopAttackPercent:        bonuses.TroopAttackPercent         += value; break;
                case GearBonusType.TroopDefenseFlat:          bonuses.TroopDefenseFlat           += value; break;
                case GearBonusType.TroopDefensePercent:       bonuses.TroopDefensePercent        += value; break;
                case GearBonusType.TroopHPFlat:               bonuses.TroopHPFlat                += value; break;
                case GearBonusType.TroopHPPercent:            bonuses.TroopHPPercent             += value; break;
                case GearBonusType.MarchCapacity:             bonuses.MarchCapacity              += (int)value; break;
                case GearBonusType.ResourceProductionPercent: bonuses.ResourceProductionPercent  += value; break;
                case GearBonusType.TrainingSpeedPercent:      bonuses.TrainingSpeedPercent       += value; break;
                case GearBonusType.GatherSpeedPercent:        bonuses.GatherSpeedPercent         += value; break;
            }
        }
    }
}
