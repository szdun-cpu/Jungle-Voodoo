using System;
using System.Collections.Generic;
using UnityEngine;
using JungleVoodoo.Data;
using JungleVoodoo.Networking;

namespace JungleVoodoo.Systems
{
    [Serializable]
    public class CombatRequest
    {
        public string MarchId;
        public string AttackerPlayFabId;
        public string DefenderPlayFabId;
        public int    TargetX;
        public int    TargetY;

        /// <summary>
        /// Troop type breakdown of the attacking march: troopId → count.
        /// Sent to CloudScript so server-side combat can apply type multipliers.
        /// </summary>
        public Dictionary<string, int> AttackerComposition;

        /// <summary>
        /// Aggregated gear bonuses from the hero leading this march.
        /// Null if no hero is assigned. Sent to CloudScript for server-authoritative application.
        /// Populate via GearSystem.GetHeroGearBonuses(heroId) before calling ResolveCombat.
        /// </summary>
        public HeroGearBonuses AttackerGearBonuses;
    }

    [Serializable]
    public class CombatResult
    {
        public bool                     AttackerWon;
        public Dictionary<string, int>  AttackerSurvivors;
        public Dictionary<string, int>  DefenderSurvivors;
        public Dictionary<string, long> LootGained;
        public string                   BattleReportId;

        /// <summary>
        /// Gear archetype IDs that dropped from this raid.
        /// Client calls GearSystem.HandleRaidLoot(DroppedGearIds, DroppedGemIds) on receipt.
        /// </summary>
        public List<string> DroppedGearIds;

        /// <summary>Gem archetype IDs that dropped from this raid.</summary>
        public List<string> DroppedGemIds;
    }

    /// <summary>
    /// Delegates combat resolution to PlayFab CloudScript (server-authoritative).
    /// Applies results locally and notifies TroopSystem of survivors.
    ///
    /// Type advantage system — pentagonal cycle (mirrors TypeAdvantage in TroopData.cs
    /// and MUST be duplicated in CloudScript/main.js for server-side resolution):
    ///
    ///   WitchDoctor beats Harpy      (1.5×)
    ///   Harpy       beats Gorilla    (1.5×)
    ///   Gorilla     beats Zombie     (1.5×)
    ///   Zombie      beats Exorcist   (1.5×)
    ///   Exorcist    beats WitchDoctor (1.5×)
    ///
    /// Effective attack = base attack × TypeAdvantage.GetMultiplier(attackerType, defenderType)
    /// </summary>
    public class CombatSystem
    {
        private readonly PlayFabManager _playFab;

        public event Action<CombatResult> OnCombatResolved;
        public event Action<string>        OnCombatFailed;

        public CombatSystem(PlayFabManager playFab)
        {
            _playFab = playFab;
        }

        // ── Type Advantage (client-side preview only) ─────────────────────────
        // Server always wins — use this for UI hints, not for authoritative results.

        /// <summary>
        /// Returns the attack multiplier for a single-type matchup.
        /// Useful for showing "strong/weak" indicators in the UI before sending a march.
        /// </summary>
        public static float GetTypeMultiplier(TroopType attacker, TroopType defender)
            => TypeAdvantage.GetMultiplier(attacker, defender);

        /// <summary>
        /// Given a mixed-composition attack force and a mixed-composition defense force,
        /// returns a rough effective-power ratio for pre-march UI feedback.
        /// troopTypeMap: troopId → TroopType (populated from loaded TroopData assets).
        /// </summary>
        public static float EstimateMatchupScore(
            Dictionary<string, int>      attackerComposition,
            Dictionary<string, int>      defenderComposition,
            Dictionary<string, TroopType> troopTypeMap)
        {
            float attackPower  = 0;
            float defensePower = 0;

            foreach (var (atkId, atkCount) in attackerComposition)
            {
                if (!troopTypeMap.TryGetValue(atkId, out var atkType)) continue;
                float typeScore = 0;

                foreach (var (defId, defCount) in defenderComposition)
                {
                    if (!troopTypeMap.TryGetValue(defId, out var defType)) continue;
                    typeScore += TypeAdvantage.GetMultiplier(atkType, defType) * defCount;
                }

                attackPower += typeScore * atkCount;
            }

            foreach (var (defId, defCount) in defenderComposition)
            {
                if (!troopTypeMap.TryGetValue(defId, out var defType)) continue;
                float typeScore = 0;

                foreach (var (atkId, atkCount) in attackerComposition)
                {
                    if (!troopTypeMap.TryGetValue(atkId, out var atkType)) continue;
                    typeScore += TypeAdvantage.GetMultiplier(defType, atkType) * atkCount;
                }

                defensePower += typeScore * defCount;
            }

            return defensePower > 0 ? attackPower / defensePower : float.MaxValue;
        }

        // ── Server Resolution ─────────────────────────────────────────────────

        /// <summary>
        /// Called when a march arrives at its target tile.
        /// Sends request to CloudScript and fires OnCombatResolved with the result.
        /// </summary>
        public void ResolveCombat(CombatRequest request)
        {
            var payload = new Dictionary<string, object>
            {
                ["marchId"]              = request.MarchId,
                ["attackerPlayFabId"]    = request.AttackerPlayFabId,
                ["defenderPlayFabId"]    = request.DefenderPlayFabId,
                ["targetX"]              = request.TargetX,
                ["targetY"]              = request.TargetY,
                ["attackerComposition"]  = request.AttackerComposition,
                ["attackerGearBonuses"]  = request.AttackerGearBonuses,
            };

            _playFab.ExecuteCloudScript(
                Core.Constants.CloudScript.ResolveCombat,
                payload,
                onSuccess: resultJson =>
                {
                    try
                    {
                        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<CombatResult>(resultJson);
                        OnCombatResolved?.Invoke(result);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CombatSystem] Failed to parse combat result: {ex.Message}");
                        OnCombatFailed?.Invoke("parse_error");
                    }
                },
                onFailure: err =>
                {
                    Debug.LogError($"[CombatSystem] CloudScript error: {err}");
                    OnCombatFailed?.Invoke(err);
                });
        }

        /// <summary>Scout a tile without committing troops to combat.</summary>
        public void ShadowGaze(int targetX, int targetY, Action<string> onResult, Action<string> onFailure)
        {
            var payload = new Dictionary<string, object>
            {
                ["targetX"] = targetX,
                ["targetY"] = targetY,
            };

            _playFab.ExecuteCloudScript(
                Core.Constants.CloudScript.ValidateMarch,
                payload,
                onSuccess: onResult,
                onFailure: onFailure);
        }
    }
}
