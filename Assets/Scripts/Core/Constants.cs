namespace JungleVoodoo.Core
{
    /// <summary>
    /// All magic strings in one place. Never use raw string literals for keys
    /// outside of this file.
    /// </summary>
    public static class Constants
    {
        public static class Scenes
        {
            public const string Boot      = "Boot";
            public const string MainMenu  = "MainMenu";
            public const string Base      = "Base";
            public const string WorldMap  = "WorldMap";
        }

        /// <summary>PlayFab Player Data keys.</summary>
        public static class PlayFabKeys
        {
            public const string PlayerProfile  = "PlayerProfile";
            public const string BuildingState  = "BuildingState";
            public const string TroopState     = "TroopState";
            public const string ActiveTimers   = "ActiveTimers";
            public const string AllianceId     = "AllianceId";
        }

        /// <summary>PlayFab CloudScript function names.</summary>
        public static class CloudScript
        {
            public const string ResolveCombat   = "ResolveCombat";
            public const string ValidateMarch   = "ValidateMarch";
            public const string ClaimResources  = "ClaimResources";
            public const string JoinTribe       = "JoinTribe";
            public const string LeaveTribe      = "LeaveTribe";
        }

        /// <summary>Resource type string IDs — must match ResourceData asset names.</summary>
        public static class Resources
        {
            public const string SpiritEnergy = "SpiritEnergy";
            public const string Bones        = "Bones";
            public const string DarkHerbs    = "DarkHerbs";
            public const string DarkEssence  = "DarkEssence";
            public const string VoodooTokens = "VoodooTokens";
        }

        /// <summary>Building type IDs — must match BuildingData asset names.</summary>
        public static class Buildings
        {
            public const string GreatHut       = "great_hut";
            public const string ZombiePit      = "zombie_pit";
            public const string WitchsCauldron = "witchs_cauldron";
            public const string BoneForge      = "bone_forge";
            public const string SpiritWell     = "spirit_well";
            public const string SkullTotem     = "skull_totem";
            public const string ShamansLodge   = "shamans_lodge";
            public const string CursedFarm     = "cursed_farm";
            public const string VoodooAltar    = "voodoo_altar";
        }

        /// <summary>
        /// Troop type IDs — must match TroopData asset names.
        ///
        /// Pentagonal type-advantage cycle (see TypeAdvantage in TroopData.cs):
        ///   WitchDoctor → beats → Harpy → beats → Gorilla
        ///   → beats → Zombie → beats → Exorcist → beats → WitchDoctor
        /// </summary>
        public static class Troops
        {
            // ── Witch Doctor (strong vs Harpy, weak vs Exorcist) ─────────────
            public const string WitchDoctorT1 = "witch_doctor_t1";  // Apprentice Shaman
            public const string WitchDoctorT2 = "witch_doctor_t2";  // Cursed Shaman
            public const string WitchDoctorT3 = "witch_doctor_t3";  // Dark Shaman
            public const string WitchDoctorT4 = "witch_doctor_t4";  // High Witch Doctor

            // ── Exorcist (strong vs WitchDoctor, weak vs Zombie) ─────────────
            public const string ExorcistT1    = "exorcist_t1";      // Bone Priest
            public const string ExorcistT2    = "exorcist_t2";      // Spirit Breaker
            public const string ExorcistT3    = "exorcist_t3";      // Soul Warden
            public const string ExorcistT4    = "exorcist_t4";      // Void Exorcist

            // ── Gorilla (strong vs Zombie, weak vs Harpy) ────────────────────
            public const string GorillaT1     = "gorilla_t1";       // Jungle Brute
            public const string GorillaT2     = "gorilla_t2";       // Cursed Ape
            public const string GorillaT3     = "gorilla_t3";       // Voodoo Gorilla
            public const string GorillaT4     = "gorilla_t4";       // Ancient Silverback

            // ── Zombie (strong vs Exorcist, weak vs Gorilla) ─────────────────
            public const string ZombieT1      = "zombie_t1";        // Zombie Shambler
            public const string ZombieT2      = "zombie_t2";        // Plague Zombie
            public const string ZombieT3      = "zombie_t3";        // Swamp Revenant
            public const string ZombieT4      = "zombie_t4";        // Ancient Dead

            // ── Harpy (strong vs Gorilla, weak vs WitchDoctor) ───────────────
            public const string HarpyT1       = "harpy_t1";         // Jungle Harpy
            public const string HarpyT2       = "harpy_t2";         // Cursed Harpy
            public const string HarpyT3       = "harpy_t3";         // Storm Harpy
            public const string HarpyT4       = "harpy_t4";         // Death Harpy

            // ── Utility (outside the advantage cycle) ────────────────────────
            public const string ShadowWraith  = "shadow_wraith";    // Scout
            public const string VoodooDoll    = "voodoo_doll";      // Siege
        }

        /// <summary>Addressable asset keys for UI panels and prefabs.</summary>
        public static class Addressables
        {
            public const string LoadingScreen        = "UI/LoadingScreen";
            public const string MainMenuPanel        = "UI/MainMenuPanel";
            public const string BaseHUD              = "UI/BaseHUD";
            public const string BuildingPanel        = "UI/BuildingPanel";
            public const string WorldMapPanel        = "UI/WorldMapPanel";
            public const string EquipmentPanel       = "UI/EquipmentPanel";
            public const string GearInventoryPanel   = "UI/GearInventoryPanel";
            public const string BuildingPrefabPrefix = "Prefabs/Buildings/";
            public const string TroopPrefabPrefix    = "Prefabs/Troops/";
            public const string GearPrefabPrefix     = "Prefabs/Gear/";
            public const string GemPrefabPrefix      = "Prefabs/Gems/";
        }

        /// <summary>Gear piece and gem asset ID prefixes. Individual IDs live in GearData assets.</summary>
        public static class Gear
        {
            // Rarity display colors (hex) for UI borders
            public const string ColorCommon    = "#9E9E9E";
            public const string ColorUncommon  = "#4CAF50";
            public const string ColorRare      = "#2196F3";
            public const string ColorEpic      = "#9C27B0";
            public const string ColorLegendary = "#FF9800";
        }

        /// <summary>PlayerPrefs keys for local persistence.</summary>
        public static class Prefs
        {
            public const string DeviceId    = "jv_device_id";
            public const string AuthToken   = "jv_auth_token";
            public const string PlayFabId   = "jv_playfab_id";
            public const string LastSaveUtc = "jv_last_save_utc";
        }

        /// <summary>Game balance constants.</summary>
        public static class Balance
        {
            public const int    ResourceTickSeconds     = 5;
            public const int    MaxMarchSlots           = 5;
            public const int    MaxBuildingQueueSlots   = 2;
            public const int    MaxTrainingQueueSlots   = 3;
            public const float  OfflineProductionCap    = 8f; // hours
        }
    }
}
