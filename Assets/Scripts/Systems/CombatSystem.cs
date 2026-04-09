using System;
using System.Collections.Generic;
using UnityEngine;
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
    }

    [Serializable]
    public class CombatResult
    {
        public bool                     AttackerWon;
        public Dictionary<string, int>  AttackerSurvivors;
        public Dictionary<string, int>  DefenderSurvivors;
        public Dictionary<string, long> LootGained;
        public string                   BattleReportId;
    }

    /// <summary>
    /// Delegates combat resolution to PlayFab CloudScript (server-authoritative).
    /// Applies results locally and notifies TroopSystem of survivors.
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

        /// <summary>
        /// Called when a march arrives at its target tile.
        /// Sends request to CloudScript and fires OnCombatResolved with the result.
        /// </summary>
        public void ResolveCombat(CombatRequest request)
        {
            var payload = new Dictionary<string, object>
            {
                ["marchId"]           = request.MarchId,
                ["attackerPlayFabId"] = request.AttackerPlayFabId,
                ["defenderPlayFabId"] = request.DefenderPlayFabId,
                ["targetX"]           = request.TargetX,
                ["targetY"]           = request.TargetY,
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
