using DeepSky.Building.Layout;
using DeepSky.Building.Presentation;
using UnityEngine;

namespace DeepSky.Building.Construction
{
    /// <summary>Identifies a structural grid region independently of its mesh.</summary>
    public readonly struct ConstructionTarget
    {
        public Vector3Int Cell { get; }
        public BuildingSurface Surface { get; }
        public RoomSide Side { get; }
        public float Distance { get; }

        /// <summary>Records a selection along an aim ray.</summary>
        /// <param name="cell">Occupied room owning the selected region.</param>
        /// <param name="surface">Floor, ceiling, wall or ladder.</param>
        /// <param name="side">Outward wall direction; unused for horizontal surfaces.</param>
        /// <param name="distance">Distance along the world-space ray in metres.</param>
        public ConstructionTarget(Vector3Int cell, BuildingSurface surface, RoomSide side, float distance)
        {
            Cell = cell;
            Surface = surface;
            Side = side;
            Distance = distance;
        }
    }
}
