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
    /// Controls the Gear Inventory panel — a scrollable grid of all gear pieces the player owns.
    ///
    /// Can be opened standalone (browse all gear) or from EquipmentPanelController with a slot
    /// filter (only show pieces that fit the selected slot).
    ///
    /// Each grid cell shows:
    ///   - A rarity-colored border
    ///   - The gear icon
    ///   - Filled/empty socket dot indicators
    ///
    /// Selecting a cell shows a detail view with stats, socket slots, and an Equip button.
    ///
    /// Bind before opening via UIManager.Open callback:
    ///   invCtrl.Bind(heroId, GearSlot.Helmet);   // slot-filtered
    ///   invCtrl.Bind(heroId);                     // show all
    /// </summary>
    public class GearInventoryPanelController : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Transform       _gridContainer;
        [SerializeField] private GameObject      _gearCellPrefab;    // GearCellUI prefab

        [Header("Detail View")]
        [SerializeField] private GameObject      _detailView;
        [SerializeField] private Image           _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailNameLabel;
        [SerializeField] private TextMeshProUGUI _detailRarityLabel;
        [SerializeField] private TextMeshProUGUI _detailStatsLabel;
        [SerializeField] private Transform       _socketContainer;   // parent for socket slot UI
        [SerializeField] private GameObject      _socketSlotPrefab;  // SocketSlotUI prefab
        [SerializeField] private Button          _equipButton;
        [SerializeField] private Button          _closeDetailButton;

        [Header("Gem Sub-View")]
        [SerializeField] private GameObject      _gemPickerView;
        [SerializeField] private Transform       _gemPickerContainer;
        [SerializeField] private GameObject      _gemCellPrefab;     // GemCellUI prefab
        [SerializeField] private Button          _closeGemPickerButton;

        [Header("Navigation")]
        [SerializeField] private Button          _closeButton;

        // Rarity border colors matching Constants.Gear
        private static readonly Color ColorCommon    = HexToColor(Constants.Gear.ColorCommon);
        private static readonly Color ColorUncommon  = HexToColor(Constants.Gear.ColorUncommon);
        private static readonly Color ColorRare      = HexToColor(Constants.Gear.ColorRare);
        private static readonly Color ColorEpic      = HexToColor(Constants.Gear.ColorEpic);
        private static readonly Color ColorLegendary = HexToColor(Constants.Gear.ColorLegendary);

        private GearSystem  _gearSystem;
        private string      _heroId;
        private GearSlot?   _slotFilter;

        private GearInstance _selectedGearInstance;
        private int          _selectedSocketIndex = -1;

        private Dictionary<string, GearData> _gearDataMap = new();
        private Dictionary<string, GemData>  _gemDataMap  = new();
        private List<GearInstance>           _displayedGear = new();

        private void Awake()
        {
            _gearSystem = ServiceLocator.Instance.Get<GearSystem>();

            _equipButton.onClick.AddListener(OnEquipClicked);
            _closeDetailButton.onClick.AddListener(() => _detailView.SetActive(false));
            _closeGemPickerButton.onClick.AddListener(() => _gemPickerView.SetActive(false));
            _closeButton.onClick.AddListener(() => UIManager.Instance.CloseTop());

            _detailView.SetActive(false);
            _gemPickerView.SetActive(false);

            LoadAssets();
        }

        private void OnEnable()
        {
            _gearSystem.OnGemSocketed  += HandleGemSocketed;
            _gearSystem.OnGearEquipped += HandleGearEquipped;
            RebuildGrid();
        }

        private void OnDisable()
        {
            _gearSystem.OnGemSocketed  -= HandleGemSocketed;
            _gearSystem.OnGearEquipped -= HandleGearEquipped;
        }

        /// <summary>
        /// Bind this panel to a hero before opening.
        /// slotFilter restricts which gear is shown; null shows all gear.
        /// </summary>
        public void Bind(string heroId, GearSlot? slotFilter = null)
        {
            _heroId     = heroId;
            _slotFilter = slotFilter;
        }

        // ── Grid Building ─────────────────────────────────────────────────────

        private void RebuildGrid()
        {
            foreach (Transform child in _gridContainer)
                Destroy(child.gameObject);

            _displayedGear.Clear();

            var profile = ServiceLocator.Instance.Get<Networking.PlayerDataService>().CachedProfile;
            if (profile == null) return;

            foreach (var gearInstance in profile.GearInventory)
            {
                if (!_gearDataMap.TryGetValue(gearInstance.GearId, out var gearData)) continue;
                if (_slotFilter.HasValue && gearData.Slot != _slotFilter.Value) continue;

                _displayedGear.Add(gearInstance);
                CreateGearCell(gearInstance, gearData);
            }
        }

        private void CreateGearCell(GearInstance instance, GearData data)
        {
            var cell        = Instantiate(_gearCellPrefab, _gridContainer);
            var borderImage = cell.transform.Find("Border")?.GetComponent<Image>();
            var iconImage   = cell.transform.Find("Icon")?.GetComponent<Image>();
            var socketDots  = cell.transform.Find("Sockets");

            if (borderImage != null) borderImage.color = RarityColor(data.Rarity);
            if (iconImage   != null) iconImage.sprite  = data.Icon;

            // Socket dots: fill up to SocketCount; mark occupied ones differently
            if (socketDots != null)
            {
                for (int i = 0; i < socketDots.childCount; i++)
                {
                    var dot  = socketDots.GetChild(i);
                    bool has = i < data.SocketCount;
                    bool used = has && i < instance.SocketedGemInstanceIds.Count;
                    dot.gameObject.SetActive(has);

                    var dotImage = dot.GetComponent<Image>();
                    if (dotImage != null)
                        dotImage.color = used ? Color.yellow : Color.grey;
                }
            }

            cell.GetComponent<Button>()?.onClick.AddListener(() => ShowDetailView(instance));
        }

        // ── Detail View ───────────────────────────────────────────────────────

        private void ShowDetailView(GearInstance instance)
        {
            _selectedGearInstance = instance;

            if (!_gearDataMap.TryGetValue(instance.GearId, out var gearData)) return;

            _detailIcon.sprite         = gearData.Icon;
            _detailNameLabel.text      = gearData.DisplayName;
            _detailRarityLabel.text    = gearData.Rarity.ToString();
            _detailRarityLabel.color   = RarityColor(gearData.Rarity);

            var sb = new System.Text.StringBuilder();
            foreach (var bonus in gearData.BaseStatBonuses)
                sb.AppendLine($"{BonusLabel(bonus.Type)}  +{bonus.Value}");
            _detailStatsLabel.text = sb.ToString();

            BuildSocketUI(instance, gearData);

            _equipButton.gameObject.SetActive(!string.IsNullOrEmpty(_heroId) && _slotFilter.HasValue);
            _detailView.SetActive(true);
        }

        private void BuildSocketUI(GearInstance instance, GearData data)
        {
            foreach (Transform child in _socketContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < data.SocketCount; i++)
            {
                int socketIndex = i;
                var slot = Instantiate(_socketSlotPrefab, _socketContainer);

                bool isOccupied = i < instance.SocketedGemInstanceIds.Count;
                var  label      = slot.GetComponentInChildren<TextMeshProUGUI>();

                if (isOccupied)
                {
                    string gemInstanceId = instance.SocketedGemInstanceIds[i];
                    var    gemInstance   = GetGemInstance(gemInstanceId);
                    if (gemInstance != null && _gemDataMap.TryGetValue(gemInstance.GemId, out var gemData))
                    {
                        if (label != null)
                            label.text = $"{gemData.DisplayName} (T{gemInstance.Tier})";

                        // Unsocket button
                        var unsocketBtn = slot.transform.Find("UnsocketButton")?.GetComponent<Button>();
                        if (unsocketBtn != null)
                            unsocketBtn.onClick.AddListener(() =>
                                _gearSystem.UnsocketGem(instance.InstanceId, gemInstanceId));
                    }
                }
                else
                {
                    if (label != null) label.text = "Empty";

                    // Socket gem button
                    var socketBtn = slot.transform.Find("SocketButton")?.GetComponent<Button>();
                    if (socketBtn != null)
                        socketBtn.onClick.AddListener(() =>
                        {
                            _selectedSocketIndex = socketIndex;
                            OpenGemPicker();
                        });
                }
            }
        }

        // ── Gem Picker ────────────────────────────────────────────────────────

        private void OpenGemPicker()
        {
            foreach (Transform child in _gemPickerContainer)
                Destroy(child.gameObject);

            var profile = ServiceLocator.Instance.Get<Networking.PlayerDataService>().CachedProfile;
            if (profile == null) return;

            foreach (var gemInstance in profile.GemInventory)
            {
                if (!_gemDataMap.TryGetValue(gemInstance.GemId, out var gemData)) continue;

                var cell  = Instantiate(_gemCellPrefab, _gemPickerContainer);
                var label = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{gemData.DisplayName} T{gemInstance.Tier}\n+{gemData.GetBonus(gemInstance.Tier):F1}";

                string capturedId = gemInstance.InstanceId;
                cell.GetComponent<Button>()?.onClick.AddListener(() => OnGemSelected(capturedId));
            }

            _gemPickerView.SetActive(true);
        }

        private void OnGemSelected(string gemInstanceId)
        {
            if (_selectedGearInstance == null) return;

            _gearSystem.SocketGem(_selectedGearInstance.InstanceId, gemInstanceId);
            _gemPickerView.SetActive(false);
            ShowDetailView(_selectedGearInstance); // refresh socket UI
        }

        // ── Equip ─────────────────────────────────────────────────────────────

        private void OnEquipClicked()
        {
            if (_selectedGearInstance == null || string.IsNullOrEmpty(_heroId)) return;

            _gearSystem.EquipGear(_heroId, _selectedGearInstance.InstanceId);
            UIManager.Instance.CloseTop();
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void HandleGemSocketed(GearInstance _)  => RebuildGrid();
        private void HandleGearEquipped(GearInstance _) => RebuildGrid();

        // ── Helpers ───────────────────────────────────────────────────────────

        private void LoadAssets()
        {
            var gearAssets = Resources.LoadAll<GearData>("ScriptableObjects");
            foreach (var a in gearAssets) _gearDataMap[a.GearId] = a;

            var gemAssets = Resources.LoadAll<GemData>("ScriptableObjects");
            foreach (var a in gemAssets)  _gemDataMap[a.GemId]  = a;
        }

        private GemInstance GetGemInstance(string instanceId)
        {
            var profile = ServiceLocator.Instance.Get<Networking.PlayerDataService>().CachedProfile;
            return profile?.GemInventory.Find(g => g.InstanceId == instanceId);
        }

        private static Color RarityColor(GearRarity rarity) => rarity switch
        {
            GearRarity.Common    => ColorCommon,
            GearRarity.Uncommon  => ColorUncommon,
            GearRarity.Rare      => ColorRare,
            GearRarity.Epic      => ColorEpic,
            GearRarity.Legendary => ColorLegendary,
            _                    => Color.white
        };

        private static string BonusLabel(GearBonusType type) => type switch
        {
            GearBonusType.TroopAttackFlat           => "Troop ATK",
            GearBonusType.TroopAttackPercent        => "Troop ATK%",
            GearBonusType.TroopDefenseFlat          => "Troop DEF",
            GearBonusType.TroopDefensePercent       => "Troop DEF%",
            GearBonusType.TroopHPFlat               => "Troop HP",
            GearBonusType.TroopHPPercent            => "Troop HP%",
            GearBonusType.MarchCapacity             => "March Cap",
            GearBonusType.ResourceProductionPercent => "Production%",
            GearBonusType.TrainingSpeedPercent      => "Training Spd%",
            GearBonusType.GatherSpeedPercent        => "Gather Spd%",
            _                                       => type.ToString()
        };

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }
}
