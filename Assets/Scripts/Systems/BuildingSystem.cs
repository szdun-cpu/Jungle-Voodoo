using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JungleVoodoo.Core;
using JungleVoodoo.Data;

namespace JungleVoodoo.Systems
{
    /// <summary>
    /// Manages the player's base: building placement, upgrade initiation,
    /// and applying completed-construction bonuses to other systems.
    /// </summary>
    public class BuildingSystem
    {
        private readonly List<BuildingState> _buildings = new();
        private readonly TimerSystem         _timerSystem;
        private readonly ResourceSystem      _resourceSystem;

        // Loaded from Addressables at runtime; populated by BuildingSystemLoader MonoBehaviour
        private readonly Dictionary<string, BuildingData> _buildingDefinitions = new();

        public event Action<BuildingState>  OnBuildingUpgradeStarted;
        public event Action<BuildingState>  OnBuildingUpgradeCompleted;
        public event Action<BuildingState>  OnBuildingPlaced;

        public BuildingSystem(TimerSystem timerSystem, ResourceSystem resourceSystem)
        {
            _timerSystem    = timerSystem;
            _resourceSystem = resourceSystem;

            _timerSystem.OnTimerCompleted += HandleTimerCompleted;
        }

        // ── Initialization ────────────────────────────────────────────────────

        public void LoadFromProfile(List<BuildingState> buildings)
        {
            _buildings.Clear();
            _buildings.AddRange(buildings);
            RecalculateProductionRates();
        }

        public void RegisterDefinition(BuildingData data)
        {
            _buildingDefinitions[data.BuildingId] = data;
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public BuildingState GetBuilding(string buildingId) =>
            _buildings.FirstOrDefault(b => b.BuildingId == buildingId);

        public IReadOnlyList<BuildingState> GetAllBuildings() => _buildings;

        public bool IsUnderConstruction(BuildingState building) =>
            building.ConstructionEndUtc != null;

        // ── Actions ───────────────────────────────────────────────────────────

        /// <summary>Place a new building at grid position. Returns false if blocked.</summary>
        public bool PlaceBuilding(string buildingId, int gridX, int gridY)
        {
            if (_buildings.Any(b => b.GridX == gridX && b.GridY == gridY))
            {
                Debug.LogWarning($"[BuildingSystem] Grid ({gridX},{gridY}) is occupied.");
                return false;
            }

            if (!_buildingDefinitions.TryGetValue(buildingId, out var def))
            {
                Debug.LogError($"[BuildingSystem] Unknown buildingId: {buildingId}");
                return false;
            }

            var levelData = def.GetLevelData(1);
            var cost = BuildCostDictionary(levelData.UpgradeCost);

            if (!_resourceSystem.Spend(cost))
            {
                Debug.LogWarning("[BuildingSystem] Cannot afford to place building.");
                return false;
            }

            var timerId = Guid.NewGuid().ToString("N");
            var endUtc  = DateTime.UtcNow.AddSeconds(levelData.BuildTimeSeconds);

            var state = new BuildingState
            {
                BuildingId           = buildingId,
                Level                = 0,           // 0 = placed but not yet built (level 1)
                GridX                = gridX,
                GridY                = gridY,
                ConstructionEndUtc   = endUtc.ToString("o")
            };

            _buildings.Add(state);
            _timerSystem.AddTimer(timerId, TimerType.Construction, buildingId, endUtc);
            OnBuildingPlaced?.Invoke(state);
            return true;
        }

        /// <summary>Begin upgrading an existing building to the next level.</summary>
        public bool UpgradeBuilding(string buildingId)
        {
            var state = GetBuilding(buildingId);
            if (state == null)
            {
                Debug.LogWarning($"[BuildingSystem] Building not found: {buildingId}");
                return false;
            }
            if (IsUnderConstruction(state))
            {
                Debug.LogWarning($"[BuildingSystem] {buildingId} is already under construction.");
                return false;
            }
            if (!_buildingDefinitions.TryGetValue(buildingId, out var def))
                return false;

            int nextLevel  = state.Level + 1;
            if (nextLevel > def.MaxLevel)
            {
                Debug.LogWarning($"[BuildingSystem] {buildingId} is already max level.");
                return false;
            }

            var levelData = def.GetLevelData(nextLevel);
            var cost = BuildCostDictionary(levelData.UpgradeCost);

            if (!_resourceSystem.Spend(cost))
                return false;

            var timerId = Guid.NewGuid().ToString("N");
            var endUtc  = DateTime.UtcNow.AddSeconds(levelData.BuildTimeSeconds);
            state.ConstructionEndUtc = endUtc.ToString("o");

            _timerSystem.AddTimer(timerId, TimerType.Construction, buildingId, endUtc);
            OnBuildingUpgradeStarted?.Invoke(state);
            return true;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void HandleTimerCompleted(GameTimer timer)
        {
            if (timer.Type != TimerType.Construction) return;

            var state = GetBuilding(timer.TargetId);
            if (state == null) return;

            state.Level++;
            state.ConstructionEndUtc = null;

            RecalculateProductionRates();
            OnBuildingUpgradeCompleted?.Invoke(state);
        }

        private void RecalculateProductionRates()
        {
            // Reset rates, then sum contributions from all buildings
            _resourceSystem.SetProductionRate(Core.Constants.Resources.SpiritEnergy, 0);
            _resourceSystem.SetProductionRate(Core.Constants.Resources.Bones, 0);
            _resourceSystem.SetProductionRate(Core.Constants.Resources.DarkHerbs, 0);
            _resourceSystem.SetProductionRate(Core.Constants.Resources.DarkEssence, 0);

            foreach (var building in _buildings)
            {
                if (building.Level == 0 || !_buildingDefinitions.TryGetValue(building.BuildingId, out var def))
                    continue;

                var ld = def.GetLevelData(building.Level);
                if (!string.IsNullOrEmpty(ld.ProducedResourceId) && ld.ProductionPerHour > 0)
                {
                    // Accumulate — we'd normally track per-resource, simplified here
                    _resourceSystem.SetProductionRate(ld.ProducedResourceId, ld.ProductionPerHour);
                }
            }
        }

        private static Dictionary<string, long> BuildCostDictionary(List<ResourceCost> costs)
        {
            var dict = new Dictionary<string, long>();
            foreach (var c in costs)
                dict[c.ResourceId] = c.Amount;
            return dict;
        }

        public List<BuildingState> Serialize() => new(_buildings);
    }
}
