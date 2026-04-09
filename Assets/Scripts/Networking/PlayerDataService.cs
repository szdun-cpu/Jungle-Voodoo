using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using JungleVoodoo.Core;
using JungleVoodoo.Data;

namespace JungleVoodoo.Networking
{
    /// <summary>
    /// Serializes PlayerProfile to JSON and persists it in PlayFab Player Data.
    /// Server is always authoritative — on conflict, the server version wins.
    /// </summary>
    public class PlayerDataService
    {
        private readonly PlayFabManager _playFab;

        private static readonly List<string> AllKeys = new()
        {
            Constants.PlayFabKeys.PlayerProfile,
            Constants.PlayFabKeys.BuildingState,
            Constants.PlayFabKeys.TroopState,
            Constants.PlayFabKeys.ActiveTimers,
        };

        public PlayerDataService(PlayFabManager playFab)
        {
            _playFab = playFab;
        }

        // ── Load ──────────────────────────────────────────────────────────────

        public void LoadPlayerData(Action<PlayerProfile> onSuccess, Action<string> onFailure)
        {
            _playFab.GetPlayerData(
                keys: AllKeys,
                onSuccess: data =>
                {
                    try
                    {
                        PlayerProfile profile;

                        if (data.TryGetValue(Constants.PlayFabKeys.PlayerProfile, out var json)
                            && !string.IsNullOrEmpty(json))
                        {
                            profile = JsonConvert.DeserializeObject<PlayerProfile>(json);
                        }
                        else
                        {
                            // First-time player — create a new profile
                            profile = PlayerProfile.CreateNew(
                                _playFab.PlayFabId,
                                "Apprentice_" + _playFab.PlayFabId[..6]);
                        }

                        // Overlay sub-keys if they exist
                        if (data.TryGetValue(Constants.PlayFabKeys.BuildingState, out var buildingJson))
                            profile.Buildings = JsonConvert.DeserializeObject<List<BuildingState>>(buildingJson) ?? profile.Buildings;

                        if (data.TryGetValue(Constants.PlayFabKeys.TroopState, out var troopJson))
                            profile.Troops = JsonConvert.DeserializeObject<List<TroopCount>>(troopJson) ?? profile.Troops;

                        if (data.TryGetValue(Constants.PlayFabKeys.ActiveTimers, out var timerJson))
                            profile.Timers = JsonConvert.DeserializeObject<List<ActiveTimer>>(timerJson) ?? profile.Timers;

                        onSuccess?.Invoke(profile);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[PlayerDataService] Failed to deserialize player data: {ex.Message}");
                        onFailure?.Invoke("deserialization_error");
                    }
                },
                onFailure: onFailure);
        }

        // ── Save ──────────────────────────────────────────────────────────────

        public void SavePlayerData(PlayerProfile profile, Action onSuccess = null, Action<string> onFailure = null)
        {
            profile.LastSaveUtc = DateTime.UtcNow.ToString("o");
            profile.Title       = profile.ComputeTitle();

            var data = new Dictionary<string, string>
            {
                [Constants.PlayFabKeys.PlayerProfile] = JsonConvert.SerializeObject(profile),
                [Constants.PlayFabKeys.BuildingState] = JsonConvert.SerializeObject(profile.Buildings),
                [Constants.PlayFabKeys.TroopState]    = JsonConvert.SerializeObject(profile.Troops),
                [Constants.PlayFabKeys.ActiveTimers]  = JsonConvert.SerializeObject(profile.Timers),
            };

            _playFab.SetPlayerData(
                data: data,
                onSuccess: () =>
                {
                    PlayerPrefs.SetString(Constants.Prefs.LastSaveUtc, profile.LastSaveUtc);
                    PlayerPrefs.Save();
                    onSuccess?.Invoke();
                },
                onFailure: err =>
                {
                    Debug.LogError($"[PlayerDataService] Save failed: {err}");
                    onFailure?.Invoke(err);
                });
        }
    }
}
