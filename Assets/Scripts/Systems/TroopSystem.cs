using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JungleVoodoo.Core;
using JungleVoodoo.Data;

namespace JungleVoodoo.Systems
{
    /// <summary>
    /// Manages troop counts, training queues, marches, and healing.
    /// Combat resolution is delegated to CombatSystem (CloudScript).
    /// </summary>
    public class TroopSystem
    {
        private readonly List<TroopCount>  _troops  = new();
        private readonly List<ActiveMarch> _marches = new();
        private readonly TimerSystem       _timerSystem;
        private readonly ResourceSystem    _resourceSystem;

        private readonly Dictionary<string, TroopData> _troopDefinitions = new();

        public event Action<TroopCount>  OnTrainingComplete;
        public event Action<ActiveMarch> OnMarchDeparted;
        public event Action<ActiveMarch> OnMarchArrived;

        public TroopSystem(TimerSystem timerSystem, ResourceSystem resourceSystem)
        {
            _timerSystem    = timerSystem;
            _resourceSystem = resourceSystem;

            _timerSystem.OnTimerCompleted += HandleTimerCompleted;
        }

        // ── Init ──────────────────────────────────────────────────────────────

        public void LoadFromProfile(List<TroopCount> troops, List<ActiveMarch> marches)
        {
            _troops.Clear();
            _troops.AddRange(troops);
            _marches.Clear();
            _marches.AddRange(marches);
        }

        public void RegisterDefinition(TroopData data)
        {
            _troopDefinitions[data.TroopId] = data;
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public int GetAvailable(string troopId) =>
            _troops.FirstOrDefault(t => t.TroopId == troopId)?.Available ?? 0;

        public IReadOnlyList<TroopCount>  GetAllTroops()  => _troops;
        public IReadOnlyList<ActiveMarch> GetAllMarches() => _marches;

        // ── Training ─────────────────────────────────────────────────────────

        /// <summary>Start training <paramref name="count"/> troops of the given type.</summary>
        public bool TrainTroops(string troopId, int count)
        {
            if (!_troopDefinitions.TryGetValue(troopId, out var def))
            {
                Debug.LogError($"[TroopSystem] Unknown troopId: {troopId}");
                return false;
            }

            var cost = new Dictionary<string, long>();
            foreach (var c in def.TrainingCost)
                cost[c.ResourceId] = (long)c.Amount * count;

            if (!_resourceSystem.Spend(cost)) return false;

            var timerId = Guid.NewGuid().ToString("N");
            var endUtc  = DateTime.UtcNow.AddSeconds(def.TrainingTimeSeconds * count);

            var entry = GetOrCreateEntry(troopId);
            entry.Training    += count;
            entry.TrainingEndUtc = endUtc.ToString("o");

            _timerSystem.AddTimer(timerId, TimerType.Training, troopId, endUtc);
            return true;
        }

        // ── Marching ──────────────────────────────────────────────────────────

        /// <summary>
        /// Send troops on a march. <paramref name="composition"/> is troopId → count.
        /// Returns the new march, or null if validation fails.
        /// </summary>
        public ActiveMarch StartMarch(
            Dictionary<string, int> composition,
            int targetX,
            int targetY,
            float speedOverride = 0)
        {
            if (_marches.Count >= Constants.Balance.MaxMarchSlots)
            {
                Debug.LogWarning("[TroopSystem] Max march slots reached.");
                return null;
            }

            foreach (var kvp in composition)
            {
                if (GetAvailable(kvp.Key) < kvp.Value)
                {
                    Debug.LogWarning($"[TroopSystem] Not enough {kvp.Key} available.");
                    return null;
                }
            }

            // Deduct troops
            foreach (var kvp in composition)
                GetOrCreateEntry(kvp.Key).Available -= kvp.Value;

            // Calculate travel time (simplified: flat 1 hour for now — real impl uses tile distance + speed)
            var travelSeconds = 3600f;
            var arrivalUtc    = DateTime.UtcNow.AddSeconds(travelSeconds);
            var timerId       = Guid.NewGuid().ToString("N");

            var march = new ActiveMarch
            {
                MarchId      = timerId,
                TroopJson    = Newtonsoft.Json.JsonConvert.SerializeObject(composition),
                TargetX      = targetX,
                TargetY      = targetY,
                DepartureUtc = DateTime.UtcNow.ToString("o"),
                ArrivalUtc   = arrivalUtc.ToString("o"),
                IsReturning  = false
            };

            _marches.Add(march);
            _timerSystem.AddTimer(timerId, TimerType.March, timerId, arrivalUtc);
            OnMarchDeparted?.Invoke(march);
            return march;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void HandleTimerCompleted(GameTimer timer)
        {
            if (timer.Type == TimerType.Training)
            {
                var entry = GetOrCreateEntry(timer.TargetId);
                entry.Available     += entry.Training;
                entry.Training       = 0;
                entry.TrainingEndUtc = null;
                OnTrainingComplete?.Invoke(entry);
            }
            else if (timer.Type == TimerType.March)
            {
                var march = _marches.FirstOrDefault(m => m.MarchId == timer.TimerId);
                if (march != null)
                {
                    OnMarchArrived?.Invoke(march);
                    // Actual combat resolution / return march is handled by CombatSystem
                }
            }
        }

        private TroopCount GetOrCreateEntry(string troopId)
        {
            var entry = _troops.FirstOrDefault(t => t.TroopId == troopId);
            if (entry == null)
            {
                entry = new TroopCount { TroopId = troopId };
                _troops.Add(entry);
            }
            return entry;
        }

        public void ReturnTroops(string marchId, Dictionary<string, int> survivors)
        {
            var march = _marches.FirstOrDefault(m => m.MarchId == marchId);
            if (march == null) return;

            foreach (var kvp in survivors)
                GetOrCreateEntry(kvp.Key).Available += kvp.Value;

            _marches.Remove(march);
        }

        public List<TroopCount>  SerializeTroops()  => new(_troops);
        public List<ActiveMarch> SerializeMarches() => new(_marches);
    }
}
