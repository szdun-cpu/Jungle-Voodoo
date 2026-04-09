using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JungleVoodoo.Core;
using JungleVoodoo.Data;
using JungleVoodoo.Systems;

namespace JungleVoodoo.UI
{
    /// <summary>
    /// Shown when the player taps a building or opens the build menu.
    /// Displays current level, upgrade cost, timer countdown, and
    /// an Upgrade button that deducts resources and starts construction.
    /// Attach to the BuildingPanel prefab root.
    /// </summary>
    public class BuildingPanelController : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] private TMP_Text  _buildingNameLabel;
        [SerializeField] private TMP_Text  _levelLabel;
        [SerializeField] private TMP_Text  _descriptionLabel;
        [SerializeField] private Image     _buildingIcon;

        [Header("Upgrade")]
        [SerializeField] private Button    _upgradeButton;
        [SerializeField] private TMP_Text  _upgradeCostLabel;
        [SerializeField] private TMP_Text  _upgradeTimeLabel;
        [SerializeField] private GameObject _constructionProgress;
        [SerializeField] private TMP_Text  _timerLabel;

        private BuildingState  _currentState;
        private BuildingSystem _buildingSystem;
        private TimerSystem    _timerSystem;

        private void Awake()
        {
            _buildingSystem = ServiceLocator.Instance.Get<BuildingSystem>();
            _timerSystem    = ServiceLocator.Instance.Get<TimerSystem>();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Call this before opening the panel to bind it to a building.</summary>
        public void Bind(BuildingState building)
        {
            _currentState = building;
            Refresh();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_currentState == null) return;

            bool isConstructing = _buildingSystem.IsUnderConstruction(_currentState);

            _buildingNameLabel.text = _currentState.BuildingId; // TODO: resolve display name via BuildingData
            _levelLabel.text        = $"Level {_currentState.Level}";

            _constructionProgress.SetActive(isConstructing);
            _upgradeButton.gameObject.SetActive(!isConstructing);
        }

        private void Update()
        {
            if (_currentState == null || !_buildingSystem.IsUnderConstruction(_currentState)) return;

            var remaining = _timerSystem.GetRemainingSeconds(_currentState.BuildingId);
            if (_timerLabel != null)
                _timerLabel.text = Utilities.Extensions.ToTimeString(remaining);
        }

        // ── Button Handlers (wire in Inspector) ───────────────────────────────

        public void OnUpgradeClicked()
        {
            if (_currentState == null) return;
            _buildingSystem.UpgradeBuilding(_currentState.BuildingId);
            Refresh();
        }

        public void OnCloseClicked()
        {
            UIManager.Instance.CloseTop();
        }
    }
}
