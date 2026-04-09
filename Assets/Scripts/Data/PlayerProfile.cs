using System;
using System.Collections.Generic;

namespace JungleVoodoo.Data
{
    public enum PlayerTitle
    {
        Apprentice,     // Level 1-5
        WitchDoctor,    // Level 6-15
        HighShaman,     // Level 16-25
        VoodooLord      // Level 26+
    }

    /// <summary>
    /// Serializable player state. Saved to / loaded from PlayFab Player Data as JSON.
    /// Never add MonoBehaviour to this class — it must round-trip cleanly through
    /// JSON serialization.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string PlayFabId;
        public string DisplayName;
        public int    Level;
        public long   TotalXp;
        public PlayerTitle Title;

        // Resources: key = ResourceType string (use Constants.Resources)
        public Dictionary<string, long> Resources = new();
        public Dictionary<string, long> ResourceStorageCaps = new();

        // Base
        public List<BuildingState>   Buildings   = new();
        public List<TroopCount>      Troops      = new();
        public List<ActiveMarch>     Marches     = new();
        public List<ActiveTimer>     Timers      = new();
        public List<ActiveHero>      Heroes      = new();

        // Social
        public string TribeId;
        public string TribeName;

        // Metadata
        public string LastSaveUtc;      // ISO 8601
        public string AccountCreatedUtc;

        public PlayerTitle ComputeTitle()
        {
            return Level switch
            {
                <= 5  => PlayerTitle.Apprentice,
                <= 15 => PlayerTitle.WitchDoctor,
                <= 25 => PlayerTitle.HighShaman,
                _     => PlayerTitle.VoodooLord
            };
        }

        public static PlayerProfile CreateNew(string playFabId, string displayName)
        {
            var now = DateTime.UtcNow.ToString("o");
            return new PlayerProfile
            {
                PlayFabId         = playFabId,
                DisplayName       = displayName,
                Level             = 1,
                TotalXp           = 0,
                Title             = PlayerTitle.Apprentice,
                Resources         = new Dictionary<string, long>
                {
                    [Core.Constants.Resources.SpiritEnergy] = 5000,
                    [Core.Constants.Resources.Bones]        = 2000,
                    [Core.Constants.Resources.DarkHerbs]    = 2000,
                    [Core.Constants.Resources.DarkEssence]  = 500,
                    [Core.Constants.Resources.VoodooTokens] = 100,
                },
                LastSaveUtc       = now,
                AccountCreatedUtc = now,
            };
        }
    }

    [Serializable]
    public class BuildingState
    {
        public string BuildingId;       // matches BuildingData.BuildingId
        public int    Level;
        public int    GridX;
        public int    GridY;
        public string ConstructionEndUtc;  // null if not under construction
    }

    [Serializable]
    public class TroopCount
    {
        public string TroopId;          // matches TroopData.TroopId
        public int    Available;
        public int    Wounded;
        public int    Training;
        public string TrainingEndUtc;
    }

    [Serializable]
    public class ActiveMarch
    {
        public string MarchId;
        public string TroopJson;        // serialized Dictionary<string, int>
        public int    TargetX;
        public int    TargetY;
        public string DepartureUtc;
        public string ArrivalUtc;
        public bool   IsReturning;
    }

    [Serializable]
    public class ActiveTimer
    {
        public string TimerId;
        public string Type;             // "construction" | "training" | "research" | "march"
        public string TargetId;
        public string EndUtc;
    }

    [Serializable]
    public class ActiveHero
    {
        public string HeroId;
        public int    Level;
        public long   Xp;
        public bool   IsOnMarch;
        public string AssignedMarchId;
    }
}
