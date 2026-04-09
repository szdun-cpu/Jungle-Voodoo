using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JungleVoodoo.Core;
using JungleVoodoo.Map;
using JungleVoodoo.Systems;

namespace JungleVoodoo.UI
{
    /// <summary>
    /// Handles the world map overlay UI: tile selection info panel,
    /// march/raid buttons, and the search bar for finding player territories.
    /// Attach to the WorldMapPanel prefab root.
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        [Header("Selection Panel")]
        [SerializeField] private GameObject _selectionPanel;
        [SerializeField] private TMP_Text   _tileOwnerLabel;
        [SerializeField] private TMP_Text   _tileCoordLabel;
        [SerializeField] private Button     _ritualRaidButton;
        [SerializeField] private Button     _shadowGazeButton;
        [SerializeField] private Button     _gatherButton;

        [Header("March Status")]
        [SerializeField] private GameObject _marchStatusPanel;
        [SerializeField] private TMP_Text   _marchTimerLabel;

        private WorldMapManager _worldMapManager;
        private TroopSystem     _troopSystem;
        private CombatSystem    _combatSystem;

        private TileData _selectedTile;

        private void Awake()
        {
            _worldMapManager = ServiceLocator.Instance.Get<WorldMapManager>();
            _troopSystem     = ServiceLocator.Instance.Get<TroopSystem>();
            _combatSystem    = ServiceLocator.Instance.Get<CombatSystem>();

            _worldMapManager.OnTileSelected += HandleTileSelected;
            _combatSystem.OnCombatResolved  += HandleCombatResolved;
        }

        private void OnDestroy()
        {
            if (_worldMapManager != null)
                _worldMapManager.OnTileSelected -= HandleTileSelected;
            if (_combatSystem != null)
                _combatSystem.OnCombatResolved  -= HandleCombatResolved;
        }

        // ── Tile Selection ────────────────────────────────────────────────────

        private void HandleTileSelected(TileData tile)
        {
            _selectedTile = tile;
            _selectionPanel.SetActive(true);

            _tileCoordLabel.text = $"({tile.GridX}, {tile.GridY})";
            _tileOwnerLabel.text = string.IsNullOrEmpty(tile.OwnerId)
                ? "Unclaimed Territory"
                : $"Sacred Ground of {tile.OwnerDisplayName}";

            bool isEnemy       = !string.IsNullOrEmpty(tile.OwnerId);
            bool canGather     = tile.HasResources && !isEnemy;

            _ritualRaidButton.gameObject.SetActive(isEnemy);
            _shadowGazeButton.gameObject.SetActive(isEnemy);
            _gatherButton.gameObject.SetActive(canGather);
        }

        // ── Button Handlers ───────────────────────────────────────────────────

        public void OnRitualRaidClicked()
        {
            if (_selectedTile == null) return;
            // TODO: open troop selection popup, then call TroopSystem.StartMarch
            Debug.Log($"[WorldMapController] Ritual Raid on ({_selectedTile.GridX},{_selectedTile.GridY})");
        }

        public void OnShadowGazeClicked()
        {
            if (_selectedTile == null) return;
            _combatSystem.ShadowGaze(
                _selectedTile.GridX,
                _selectedTile.GridY,
                onResult:  json  => Debug.Log($"[Shadow Gaze] {json}"),
                onFailure: err   => Debug.LogError($"[Shadow Gaze] Failed: {err}"));
        }

        public void OnGatherClicked()
        {
            if (_selectedTile == null || !_selectedTile.HasResources) return;
            // TODO: open troop selection popup, then call TroopSystem.StartMarch (gathering type)
            Debug.Log($"[WorldMapController] Gathering at ({_selectedTile.GridX},{_selectedTile.GridY})");
        }

        public void OnCloseClicked()
        {
            _selectionPanel.SetActive(false);
            _selectedTile = null;
            UIManager.Instance.CloseTop();
        }

        // ── Combat Result ─────────────────────────────────────────────────────

        private void HandleCombatResolved(Systems.CombatResult result)
        {
            // TODO: show battle report popup
            Debug.Log($"[WorldMapController] Combat resolved. Attacker won: {result.AttackerWon}");
        }
    }
}
