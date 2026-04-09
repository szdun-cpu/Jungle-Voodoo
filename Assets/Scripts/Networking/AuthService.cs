using System;
using UnityEngine;
using JungleVoodoo.Core;

namespace JungleVoodoo.Networking
{
    /// <summary>
    /// Handles the full login flow:
    ///   1. First launch → anonymous login with a stable device ID
    ///   2. Returning player → reuse stored PlayFab ID
    ///   3. Optional: link email/password when the user opts in
    ///
    /// Never call PlayFabManager.LoginWithCustomId directly from game code;
    /// always go through AuthService.
    /// </summary>
    public class AuthService
    {
        private readonly PlayFabManager _playFab;

        public bool IsAuthenticated => _playFab.IsLoggedIn;
        public string PlayFabId     => _playFab.PlayFabId;

        public AuthService(PlayFabManager playFab)
        {
            _playFab = playFab;
        }

        /// <summary>
        /// Attempt to login. Uses a stable device-generated ID stored in PlayerPrefs.
        /// Creates a new account on first launch.
        /// </summary>
        public void Login(Action onSuccess, Action<string> onFailure)
        {
            var deviceId = GetOrCreateDeviceId();

            _playFab.LoginWithCustomId(
                customId: deviceId,
                createIfMissing: true,
                onSuccess: playFabId =>
                {
                    PlayerPrefs.SetString(Constants.Prefs.PlayFabId, playFabId);
                    PlayerPrefs.Save();
                    Debug.Log($"[AuthService] Logged in as {playFabId}");
                    onSuccess?.Invoke();
                },
                onFailure: err =>
                {
                    Debug.LogError($"[AuthService] Login failed: {err}");
                    onFailure?.Invoke(err);
                });
        }

        private string GetOrCreateDeviceId()
        {
            var stored = PlayerPrefs.GetString(Constants.Prefs.DeviceId, null);
            if (!string.IsNullOrEmpty(stored))
                return stored;

            // Generate a stable ID for this device
            var newId = SystemInfo.deviceUniqueIdentifier;
            if (newId == SystemInfo.unsupportedIdentifier)
                newId = Guid.NewGuid().ToString("N");

            PlayerPrefs.SetString(Constants.Prefs.DeviceId, newId);
            PlayerPrefs.Save();
            return newId;
        }

        /// <summary>
        /// Link an email and password to the anonymous account.
        /// Call this when the user registers an account in-game.
        /// </summary>
        public void LinkEmail(string email, string password, Action onSuccess, Action<string> onFailure)
        {
            // TODO: PlayFabClientAPI.AddUsernamePassword(...)
            // Implement when account-linking UI is built
            Debug.LogWarning("[AuthService] Email linking not yet implemented.");
            onFailure?.Invoke("not_implemented");
        }
    }
}
