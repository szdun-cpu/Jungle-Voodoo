using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace JungleVoodoo.Core
{
    /// <summary>
    /// Loads scenes via Addressables with a loading screen overlay.
    /// All scene transitions should go through here — never call
    /// SceneManager.LoadScene directly from game code.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [SerializeField] private string _loadingScreenAddress = Constants.Addressables.LoadingScreen;

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading a scene. Ignoring request for '{sceneName}'.");
                return;
            }
            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            IsLoading = true;

            // Load the scene by Addressable key
            var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            while (!handle.IsDone)
            {
                float progress = handle.PercentComplete;
                OnLoadProgress(progress);
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                OnLoadComplete(handle.Result);
            }
            else
            {
                Debug.LogError($"[SceneLoader] Failed to load scene '{sceneName}': {handle.OperationException}");
            }

            IsLoading = false;
        }

        private void OnLoadProgress(float progress)
        {
            // TODO: update loading screen progress bar
        }

        private void OnLoadComplete(SceneInstance scene)
        {
            // TODO: hide loading screen, fire any scene-ready events
        }
    }
}
