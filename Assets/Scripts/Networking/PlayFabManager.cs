using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace JungleVoodoo.Networking
{
    /// <summary>
    /// Thin, typed wrapper around the PlayFab Unity SDK.
    /// All PlayFab calls go through here — never call PlayFab SDK directly from
    /// game systems. This keeps the rest of the codebase decoupled from PlayFab.
    /// </summary>
    public class PlayFabManager
    {
        public string PlayFabId    { get; private set; }
        public bool   IsLoggedIn   => !string.IsNullOrEmpty(PlayFabId);

        private readonly string _titleId;

        public PlayFabManager(string titleId)
        {
            _titleId = titleId;
            PlayFabSettings.staticSettings.TitleId = titleId;
        }

        // ── Authentication ────────────────────────────────────────────────────

        public void LoginWithCustomId(
            string customId,
            bool createIfMissing,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            PlayFabClientAPI.LoginWithCustomID(
                new LoginWithCustomIDRequest
                {
                    CustomId         = customId,
                    CreateAccount    = createIfMissing,
                    InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                    {
                        GetPlayerProfile = true
                    }
                },
                result =>
                {
                    PlayFabId = result.PlayFabId;
                    onSuccess?.Invoke(result.PlayFabId);
                },
                error =>
                {
                    Debug.LogError($"[PlayFabManager] Login failed: {error.GenerateErrorReport()}");
                    onFailure?.Invoke(error.ErrorMessage);
                });
        }

        // ── Player Data ───────────────────────────────────────────────────────

        public void GetPlayerData(
            List<string> keys,
            Action<Dictionary<string, string>> onSuccess,
            Action<string> onFailure)
        {
            PlayFabClientAPI.GetUserData(
                new GetUserDataRequest { Keys = keys },
                result =>
                {
                    var data = new Dictionary<string, string>();
                    foreach (var kvp in result.Data)
                        data[kvp.Key] = kvp.Value.Value;
                    onSuccess?.Invoke(data);
                },
                error =>
                {
                    Debug.LogError($"[PlayFabManager] GetPlayerData failed: {error.GenerateErrorReport()}");
                    onFailure?.Invoke(error.ErrorMessage);
                });
        }

        public void SetPlayerData(
            Dictionary<string, string> data,
            Action onSuccess,
            Action<string> onFailure)
        {
            PlayFabClientAPI.UpdateUserData(
                new UpdateUserDataRequest { Data = data },
                _ => onSuccess?.Invoke(),
                error =>
                {
                    Debug.LogError($"[PlayFabManager] SetPlayerData failed: {error.GenerateErrorReport()}");
                    onFailure?.Invoke(error.ErrorMessage);
                });
        }

        // ── CloudScript ───────────────────────────────────────────────────────

        public void ExecuteCloudScript(
            string functionName,
            Dictionary<string, object> parameters,
            Action<string> onSuccess,
            Action<string> onFailure)
        {
            PlayFabClientAPI.ExecuteCloudScript(
                new ExecuteCloudScriptRequest
                {
                    FunctionName      = functionName,
                    FunctionParameter = parameters,
                    GeneratePlayStreamEvent = true
                },
                result =>
                {
                    if (result.Error != null)
                    {
                        Debug.LogError($"[PlayFabManager] CloudScript '{functionName}' error: {result.Error.Message}");
                        onFailure?.Invoke(result.Error.Message);
                        return;
                    }
                    var json = result.FunctionResult?.ToString() ?? "{}";
                    onSuccess?.Invoke(json);
                },
                error =>
                {
                    Debug.LogError($"[PlayFabManager] CloudScript call failed: {error.GenerateErrorReport()}");
                    onFailure?.Invoke(error.ErrorMessage);
                });
        }

        // ── Display Name ──────────────────────────────────────────────────────

        public void SetDisplayName(
            string displayName,
            Action onSuccess,
            Action<string> onFailure)
        {
            PlayFabClientAPI.UpdateUserTitleDisplayName(
                new UpdateUserTitleDisplayNameRequest { DisplayName = displayName },
                _ => onSuccess?.Invoke(),
                error =>
                {
                    Debug.LogError($"[PlayFabManager] SetDisplayName failed: {error.GenerateErrorReport()}");
                    onFailure?.Invoke(error.ErrorMessage);
                });
        }
    }
}
