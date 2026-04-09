using System;
using System.Collections.Generic;
using UnityEngine;
using JungleVoodoo.Core;
using JungleVoodoo.Data;

namespace JungleVoodoo.Systems
{
    /// <summary>
    /// Owns the player's current resource amounts.
    /// Applies production ticks aligned to elapsed real time (handles offline gains).
    /// Fires ResourceChanged so UI can update reactively.
    /// </summary>
    public class ResourceSystem
    {
        // key = Constants.Resources.*
        private readonly Dictionary<string, long> _amounts    = new();
        private readonly Dictionary<string, long> _caps       = new();
        private readonly Dictionary<string, float> _ratesPerHour = new();

        private DateTime _lastTickUtc;
        private readonly TimerSystem _timerSystem;

        public event Action<string, long> OnResourceChanged; // resourceId, newAmount

        public ResourceSystem(TimerSystem timerSystem)
        {
            _timerSystem = timerSystem;
        }

        // ── Initialization ────────────────────────────────────────────────────

        public void Initialize(PlayerProfile profile)
        {
            _amounts.Clear();
            _caps.Clear();

            foreach (var kvp in profile.Resources)
                _amounts[kvp.Key] = kvp.Value;

            foreach (var kvp in profile.ResourceStorageCaps)
                _caps[kvp.Key] = kvp.Value;

            // Apply offline production
            if (DateTime.TryParse(profile.LastSaveUtc, null,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var lastSave))
            {
                var elapsed = DateTime.UtcNow - lastSave;
                var cappedHours = (float)Math.Min(elapsed.TotalHours, Constants.Balance.OfflineProductionCap);
                ApplyProduction(cappedHours);
            }

            _lastTickUtc = DateTime.UtcNow;
        }

        public void InitializeDefaults()
        {
            _amounts[Constants.Resources.SpiritEnergy] = 5000;
            _amounts[Constants.Resources.Bones]        = 2000;
            _amounts[Constants.Resources.DarkHerbs]    = 2000;
            _amounts[Constants.Resources.DarkEssence]  = 500;
            _amounts[Constants.Resources.VoodooTokens] = 100;

            _caps[Constants.Resources.SpiritEnergy] = 200_000;
            _caps[Constants.Resources.Bones]        = 200_000;
            _caps[Constants.Resources.DarkHerbs]    = 200_000;
            _caps[Constants.Resources.DarkEssence]  = 50_000;
            _caps[Constants.Resources.VoodooTokens] = long.MaxValue;

            _lastTickUtc = DateTime.UtcNow;
        }

        // ── Production ────────────────────────────────────────────────────────

        public void SetProductionRate(string resourceId, float amountPerHour)
        {
            _ratesPerHour[resourceId] = amountPerHour;
        }

        /// <summary>Call every frame. Internally throttled to Constants.Balance.ResourceTickSeconds.</summary>
        public void Tick()
        {
            var now     = DateTime.UtcNow;
            var elapsed = (now - _lastTickUtc).TotalSeconds;

            if (elapsed < Constants.Balance.ResourceTickSeconds) return;

            _lastTickUtc = now;
            ApplyProduction((float)elapsed / 3600f);
        }

        private void ApplyProduction(float hours)
        {
            foreach (var kvp in _ratesPerHour)
            {
                if (kvp.Value <= 0) continue;
                long gain = (long)(kvp.Value * hours);
                AddResource(kvp.Key, gain);
            }
        }

        // ── CRUD ──────────────────────────────────────────────────────────────

        public long GetAmount(string resourceId)
        {
            return _amounts.TryGetValue(resourceId, out var v) ? v : 0;
        }

        public long GetCap(string resourceId)
        {
            return _caps.TryGetValue(resourceId, out var v) ? v : long.MaxValue;
        }

        public bool CanAfford(Dictionary<string, long> cost)
        {
            foreach (var kvp in cost)
            {
                if (GetAmount(kvp.Key) < kvp.Value)
                    return false;
            }
            return true;
        }

        /// <summary>Deducts cost. Returns false (no-op) if funds insufficient.</summary>
        public bool Spend(Dictionary<string, long> cost)
        {
            if (!CanAfford(cost)) return false;
            foreach (var kvp in cost)
                SetAmount(kvp.Key, GetAmount(kvp.Key) - kvp.Value);
            return true;
        }

        public void AddResource(string resourceId, long amount)
        {
            var cap = GetCap(resourceId);
            var newVal = Math.Min(GetAmount(resourceId) + amount, cap);
            SetAmount(resourceId, newVal);
        }

        private void SetAmount(string resourceId, long value)
        {
            _amounts[resourceId] = value;
            OnResourceChanged?.Invoke(resourceId, value);
        }

        public Dictionary<string, long> Serialize() => new(_amounts);
    }
}
