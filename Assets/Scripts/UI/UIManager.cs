using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace JungleVoodoo.UI
{
    /// <summary>
    /// Manages a stack of UI panels using Addressables.
    /// Open a panel with UIManager.Instance.Open("UI/BuildingPanel").
    /// Close the top panel with UIManager.Instance.CloseTop().
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private readonly Stack<GameObject>       _panelStack = new();
        private readonly Dictionary<string, GameObject> _cache  = new();

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

        // ── Public API ────────────────────────────────────────────────────────

        public void Open(string addressableKey, Action<GameObject> onOpened = null)
        {
            if (_cache.TryGetValue(addressableKey, out var cached))
            {
                PushPanel(cached);
                onOpened?.Invoke(cached);
                return;
            }

            Addressables.InstantiateAsync(addressableKey).Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[UIManager] Failed to load panel: {addressableKey}");
                    return;
                }
                var panel = handle.Result;
                panel.transform.SetParent(transform, worldPositionStays: false);
                _cache[addressableKey] = panel;
                PushPanel(panel);
                onOpened?.Invoke(panel);
            };
        }

        public void CloseTop()
        {
            if (_panelStack.Count == 0) return;
            var panel = _panelStack.Pop();
            panel.SetActive(false);
        }

        public void CloseAll()
        {
            while (_panelStack.Count > 0)
                CloseTop();
        }

        public bool IsOpen(string addressableKey) =>
            _cache.TryGetValue(addressableKey, out var p) && p.activeSelf;

        // ── Internal ──────────────────────────────────────────────────────────

        private void PushPanel(GameObject panel)
        {
            // Deactivate current top without removing it from the stack
            if (_panelStack.Count > 0)
                _panelStack.Peek().SetActive(false);

            panel.SetActive(true);
            _panelStack.Push(panel);
        }

        private void Update()
        {
            // Android back button / iOS swipe-back gesture
            if (Input.GetKeyDown(KeyCode.Escape) && _panelStack.Count > 0)
                CloseTop();
        }
    }
}
