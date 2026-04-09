using System.Collections;
using UnityEngine;
using JungleVoodoo.Networking;
using JungleVoodoo.Systems;

namespace JungleVoodoo.Core
{
    public enum GameState { Loading, MainMenu, Base, WorldMap, Combat }

    /// <summary>
    /// Root entry point. Bootstraps all services into the ServiceLocator,
    /// drives the top-level game state machine, and persists across scenes.
    ///
    /// Place this on a GameObject in the Boot scene. It must be the first
    /// scene loaded (set via Project Settings > Build Settings).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("PlayFab")]
        [SerializeField] private string _playFabTitleId = "YOUR_PLAYFAB_TITLE_ID";

        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Loading;

        public static event System.Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Bootstrap();
        }

        private void Bootstrap()
        {
            // Networking layer
            var playFabManager   = new PlayFabManager(_playFabTitleId);
            var authService      = new AuthService(playFabManager);
            var playerDataService = new PlayerDataService(playFabManager);

            ServiceLocator.Instance.Register(playFabManager);
            ServiceLocator.Instance.Register(authService);
            ServiceLocator.Instance.Register(playerDataService);

            // Core systems
            var timerSystem    = new TimerSystem();
            var resourceSystem = new ResourceSystem(timerSystem);
            var buildingSystem = new BuildingSystem(timerSystem, resourceSystem);
            var troopSystem    = new TroopSystem(timerSystem, resourceSystem);
            var combatSystem   = new CombatSystem(playFabManager);

            ServiceLocator.Instance.Register(timerSystem);
            ServiceLocator.Instance.Register(resourceSystem);
            ServiceLocator.Instance.Register(buildingSystem);
            ServiceLocator.Instance.Register(troopSystem);
            ServiceLocator.Instance.Register(combatSystem);

            StartCoroutine(InitializeGame(authService, playerDataService, resourceSystem));
        }

        private IEnumerator InitializeGame(
            AuthService authService,
            PlayerDataService playerDataService,
            ResourceSystem resourceSystem)
        {
            SetState(GameState.Loading);

            // Login
            bool loginDone = false;
            bool loginSuccess = false;
            authService.Login(
                onSuccess: () => { loginSuccess = true;  loginDone = true; },
                onFailure: err  => { Debug.LogError($"[GameManager] Login failed: {err}"); loginDone = true; });

            yield return new WaitUntil(() => loginDone);

            if (!loginSuccess)
            {
                // Show error UI — for now just log
                Debug.LogError("[GameManager] Cannot proceed without authentication.");
                yield break;
            }

            // Load player data
            bool loadDone = false;
            playerDataService.LoadPlayerData(
                onSuccess: profile =>
                {
                    resourceSystem.Initialize(profile);
                    loadDone = true;
                },
                onFailure: err =>
                {
                    Debug.LogWarning($"[GameManager] Player data load failed ({err}), starting fresh.");
                    resourceSystem.InitializeDefaults();
                    loadDone = true;
                });

            yield return new WaitUntil(() => loadDone);

            SetState(GameState.MainMenu);
            SceneLoader.Instance.LoadScene(Constants.Scenes.MainMenu);
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
    }
}
