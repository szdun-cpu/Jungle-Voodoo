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

        /// <summary>Troop type IDs — must match TroopData asset names.</summary>
        public static class Troops
        {
            public const string ZombieShambler = "zombie_shambler";
            public const string BoneThrower    = "bone_thrower";
            public const string VoodooWitch    = "voodoo_witch";
            public const string CursedWarrior  = "cursed_warrior";
            public const string SwampRevenant  = "swamp_revenant";
            public const string HexArcher      = "hex_archer";
            public const string SpiritBeast    = "spirit_beast";
            public const string DeathWitch     = "death_witch";
            public const string VoodooDoll     = "voodoo_doll";
            public const string ShadowWraith   = "shadow_wraith";
        }

        /// <summary>Addressable asset keys for UI panels and prefabs.</summary>
        public static class Addressables
        {
            public const string LoadingScreen        = "UI/LoadingScreen";
            public const string MainMenuPanel        = "UI/MainMenuPanel";
            public const string BaseHUD              = "UI/BaseHUD";
            public const string BuildingPanel        = "UI/BuildingPanel";
            public const string WorldMapPanel        = "UI/WorldMapPanel";
            public const string BuildingPrefabPrefix = "Prefabs/Buildings/";
            public const string TroopPrefabPrefix    = "Prefabs/Troops/";
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
