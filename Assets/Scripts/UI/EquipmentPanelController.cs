using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JungleVoodoo.Core;
using JungleVoodoo.Data;
using JungleVoodoo.Systems;

namespace JungleVoodoo.UI
{
    /// <summary>
    /// Controls the Equipment panel — the Witch Doctor hero's gear loadout screen.
    ///
    /// Shows 8 slot buttons (Weapon, Off-hand, Helmet, Chest, Legs, Boots, Ring, Amulet).
    /// Each slot displays the equipped gear icon, or an empty-slot placeholder.
    /// Tapping an occupied slot shows inline stats and an Unequip button.
    /// Tapping an empty slot opens the GearInventoryPanel filtered to that slot.
    ///
    /// Bind a hero before opening:
    ///   var ctrl = panel.GetComponent&lt;EquipmentPanelController&gt;();
    ///   ctrl.Bind(heroId);
    ///   UIManager.Instance.Open(Constants.Addressables.EquipmentPanel);
    /// </summary>
    public class EquipmentPanelController : MonoBehaviour
    {
        [Header("Hero Info")]
        [SerializeField] private Image      _heroPortrait;
        [SerializeField] private TextMeshProUGUI _heroNameLabel;
        [SerializeField] private TextMeshProUGUI _statsSummaryLabel;

        [Header("Gear Slots (assign in order: Weapon, OffHand, Helmet, Chest, Legs, Boots, Ring, Amulet)")]
        [SerializeField] private Button[]   _slotButtons   = new Button[8];
        [SerializeField] private Image[]    _slotIcons     = new Image[8];
        [SerializeField] private GameObject[] _emptySlotIndicators = new GameObject[8];

        [Header("Inline Detail View")]
        [SerializeField] private GameObject      _detailView;
        [SerializeField] private TextMeshProUGUI _detailNameLabel;
        [SerializeField] private TextMeshProUGUI _detailStatsLabel;
        [SerializeField] private Button          _unequipButton;

        [Header("Navigation")]
        [SerializeField] private Button _closeButton;

        private GearSystem  _gearSystem;
        private string      _heroId;
        private int         _selectedSlotIndex = -1;

        private void Awake()
        {
            _gearSystem = ServiceLocator.Instance.Get<GearSystem>();

            for (int i = 0; i < _slotButtons.Length; i++)
            {
                int slotIndex = i; // capture for lambda
                _slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIndex));
            }

            _unequipButton.onClick.AddListener(OnUnequipClicked);
            _closeButton.onClick.AddListener(() => UIManager.Instance.CloseTop());

            _detailView.SetActive(false);
        }

        private void OnEnable()
        {
            _gearSystem.OnGearEquipped   += HandleGearChanged;
            _gearSystem.OnGearUnequipped += HandleGearUnequipped;
            Refresh();
        }

        private void OnDisable()
        {
            _gearSystem.OnGearEquipped   -= HandleGearChanged;
            _gearSystem.OnGearUnequipped -= HandleGearUnequipped;
        }

        /// <summary>
        /// Call before opening this panel to bind it to a specific Witch Doctor hero.
        /// </summary>
        public void Bind(string heroId)
        {
            _heroId = heroId;
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            if (string.IsNullOrEmpty(_heroId)) return;

            RefreshSlots();
            RefreshStatsSummary();
            _detailView.SetActive(false);
            _selectedSlotIndex = -1;
        }

        private void RefreshSlots()
        {
            var profile = ServiceLocator.Instance.Get<Networking.PlayerDataService>().CachedProfile;
            var hero    = profile?.Heroes.Find(h => h.HeroId == _heroId);
            if (hero == null) return;

            var gearDataMap = GetGearDataMap();

            for (int i = 0; i < 8; i++)
            {
                string instanceId = hero.EquippedGearInstanceIds != null &&
                                    hero.EquippedGearInstanceIds.Length > i
                                    ? hero.EquippedGearInstanceIds[i]
                                    : null;

                bool hasGear = !string.IsNullOrEmpty(instanceId);

                _emptySlotIndicators[i].SetActive(!hasGear);

                if (hasGear)
                {
                    var gearInstance = profile.GearInventory.Find(g => g.InstanceId == instanceId);
                    if (gearInstance != null && gearDataMap.TryGetValue(gearInstance.GearId, out var gearData))
                        _slotIcons[i].sprite = gearData.Icon;
                }

                _slotIcons[i].gameObject.SetActive(hasGear);
            }
        }

        private void RefreshStatsSummary()
        {
            var bonuses = _gearSystem.GetHeroGearBonuses(_heroId);

            _statsSummaryLabel.text =
                $"ATK +{bonuses.TroopAttackFlat:F0} (+{bonuses.TroopAttackPercent:F1}%)  " +
                $"DEF +{bonuses.TroopDefenseFlat:F0} (+{bonuses.TroopDefensePercent:F1}%)  " +
                $"HP +{bonuses.TroopHPFlat:F0} (+{bonuses.TroopHPPercent:F1}%)  " +
                $"March +{bonuses.MarchCapacity}";
        }

        // ── Slot Interaction ──────────────────────────────────────────────────

        private void OnSlotClicked(int slotIndex)
        {
            var profile = ServiceLocator.Instance.Get<Networking.PlayerDataService>().CachedProfile;
            var hero    = profile?.Heroes.Find(h => h.HeroId == _heroId);
            if (hero == null) return;

            string instanceId = hero.EquippedGearInstanceIds != null &&
                                hero.EquippedGearInstanceIds.Length > slotIndex
                                ? hero.EquippedGearInstanceIds[slotIndex]
                                : null;

            if (string.IsNullOrEmpty(instanceId))
            {
                // Open inventory filtered to this slot type
                _selectedSlotIndex = slotIndex;
                OpenInventoryForSlot((GearSlot)slotIndex);
            }
            else
            {
                // Show detail view for the equipped piece
                _selectedSlotIndex = slotIndex;
                ShowDetailView(instanceId, profile);
            }
        }

        private void ShowDetailView(string gearInstanceId, Data.PlayerProfile profile)
        {
            var gearInstance = profile.GearInventory.Find(g => g.InstanceId == gearInstanceId);
            if (gearInstance == null) return;

            var gearDataMap = GetGearDataMap();
            if (!gearDataMap.TryGetValue(gearInstance.GearId, out var gearData)) return;

            _detailNameLabel.text = gearData.DisplayName;

            var sb = new System.Text.StringBuilder();
            foreach (var bonus in gearData.BaseStatBonuses)
                sb.AppendLine($"  {BonusLabel(bonus.Type)} +{bonus.Value}");

            sb.AppendLine($"Sockets: {gearInstance.SocketedGemInstanceIds.Count}/{gearData.SocketCount}");

            _detailStatsLabel.text = sb.ToString();
            _detailView.SetActive(true);
        }

        private void OnUnequipClicked()
        {
            if (_selectedSlotIndex < 0) return;
            _gearSystem.UnequipGear(_heroId, (GearSlot)_selectedSlotIndex);
            _detailView.SetActive(false);
            _selectedSlotIndex = -1;
        }

        private void OpenInventoryForSlot(GearSlot slot)
        {
            UIManager.Instance.Open(Constants.Addressables.GearInventoryPanel, go =>
            {
                var invCtrl = go.GetComponent<GearInventoryPanelController>();
                invCtrl?.Bind(_heroId, slot);
            });
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void HandleGearChanged(GearInstance _)  => Refresh();
        private void HandleGearUnequipped(GearSlot _)    => Refresh();

        // ── Helpers ───────────────────────────────────────────────────────────

        private System.Collections.Generic.Dictionary<string, GearData> GetGearDataMap()
        {
            var map   = new System.Collections.Generic.Dictionary<string, GearData>();
            var assets = Resources.LoadAll<GearData>("ScriptableObjects");
            foreach (var a in assets) map[a.GearId] = a;
            return map;
        }

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
    }
}
