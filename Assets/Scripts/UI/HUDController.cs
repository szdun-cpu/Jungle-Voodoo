using UnityEngine;
using TMPro;
using JungleVoodoo.Core;
using JungleVoodoo.Systems;

namespace JungleVoodoo.UI
{
    /// <summary>
    /// Drives the persistent base HUD: resource counters, player level/title,
    /// and shortcut buttons (World Map, Alliance, etc.).
    /// Attach to the BaseHUD prefab root.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Resource Labels")]
        [SerializeField] private TMP_Text _spiritEnergyLabel;
        [SerializeField] private TMP_Text _bonesLabel;
        [SerializeField] private TMP_Text _darkHerbsLabel;
        [SerializeField] private TMP_Text _darkEssenceLabel;
        [SerializeField] private TMP_Text _voodooTokensLabel;

        [Header("Player Info")]
        [SerializeField] private TMP_Text _playerNameLabel;
        [SerializeField] private TMP_Text _playerTitleLabel;
        [SerializeField] private TMP_Text _playerLevelLabel;

        private ResourceSystem _resourceSystem;

        private void Start()
        {
            _resourceSystem = ServiceLocator.Instance.Get<ResourceSystem>();
            _resourceSystem.OnResourceChanged += HandleResourceChanged;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_resourceSystem != null)
                _resourceSystem.OnResourceChanged -= HandleResourceChanged;
        }

        private void HandleResourceChanged(string resourceId, long newAmount)
        {
            switch (resourceId)
            {
                case Constants.Resources.SpiritEnergy:
                    UpdateLabel(_spiritEnergyLabel, newAmount);
                    break;
                case Constants.Resources.Bones:
                    UpdateLabel(_bonesLabel, newAmount);
                    break;
                case Constants.Resources.DarkHerbs:
                    UpdateLabel(_darkHerbsLabel, newAmount);
                    break;
                case Constants.Resources.DarkEssence:
                    UpdateLabel(_darkEssenceLabel, newAmount);
                    break;
                case Constants.Resources.VoodooTokens:
                    UpdateLabel(_voodooTokensLabel, newAmount);
                    break;
            }
        }

        private void RefreshAll()
        {
            UpdateLabel(_spiritEnergyLabel, _resourceSystem.GetAmount(Constants.Resources.SpiritEnergy));
            UpdateLabel(_bonesLabel,        _resourceSystem.GetAmount(Constants.Resources.Bones));
            UpdateLabel(_darkHerbsLabel,    _resourceSystem.GetAmount(Constants.Resources.DarkHerbs));
            UpdateLabel(_darkEssenceLabel,  _resourceSystem.GetAmount(Constants.Resources.DarkEssence));
            UpdateLabel(_voodooTokensLabel, _resourceSystem.GetAmount(Constants.Resources.VoodooTokens));
        }

        private static void UpdateLabel(TMP_Text label, long value)
        {
            if (label == null) return;
            label.text = Utilities.Extensions.ToResourceString(value);
        }

        // ── Button Handlers (wire up in Inspector) ────────────────────────────

        public void OnWorldMapButtonClicked()
        {
            GameManager.Instance.SetState(GameState.WorldMap);
            UIManager.Instance.Open(Constants.Addressables.WorldMapPanel);
        }

        public void OnBuildingButtonClicked()
        {
            UIManager.Instance.Open(Constants.Addressables.BuildingPanel);
        }
    }
}
