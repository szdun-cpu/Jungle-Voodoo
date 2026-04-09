using System;
using UnityEngine;

namespace JungleVoodoo.Map
{
    public enum TileType
    {
        Jungle,         // Standard terrain
        Swamp,          // Slower movement
        CursedGrove,    // Dark Essence deposit
        AncientRuins,   // PvE event location
        PlayerBase,     // Another player's Sacred Ground
        Wilderness,     // Gatherable resources (Dark Herbs, Bones)
        Alliance        // Tribe-controlled territory
    }

    /// <summary>
    /// Lightweight data model for a single tile on the Cursed Wilds map.
    /// Populated from PlayFab queries; not a MonoBehaviour.
    /// </summary>
    [Serializable]
    public class TileData
    {
        public int      GridX;
        public int      GridY;
        public TileType Type;

        // Owner info (null = unclaimed)
        public string   OwnerId;
        public string   OwnerDisplayName;
        public string   OwnerTribeId;
        public int      OwnerPower;         // for display in selection panel

        // Resources
        public bool     HasResources;
        public string   ResourceId;
        public long     ResourceAmount;
        public int      ResourceTier;       // 1-5, affects gather rate

        // Flags
        public bool     IsAllied;           // owned by own tribe
        public bool     IsOwnTerritory;

        public Vector2Int GridPosition => new(GridX, GridY);

        public float DistanceTo(TileData other)
        {
            return Vector2Int.Distance(GridPosition, other.GridPosition);
        }
    }
}
