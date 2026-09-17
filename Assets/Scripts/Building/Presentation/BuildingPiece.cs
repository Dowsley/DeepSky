using DeepSky.Building.Layout;
using UnityEngine;

namespace DeepSky.Building.Presentation
{
    public enum BuildingSurface { Floor, Wall, Ceiling, Ladder, Support, Water }

    /// <summary>Connects a selectable shell piece to its owning room.</summary>
    public sealed class BuildingPiece : MonoBehaviour
    {
        [SerializeField] private BuildingSurface surface = BuildingSurface.Floor;
        [SerializeField] private Transform? lamp;

        public Habitat Owner { get; private set; } = null!;
        public Vector3Int Cell { get; private set; } = Vector3Int.zero;
        public RoomSide Side { get; private set; } = RoomSide.North;
        public BuildingSurface Surface => surface;
        public Transform? Lamp => lamp;

        /// <summary>Associates a spawned piece with its layout cell.</summary>
        /// <param name="owner">Habitat owning the piece's lifetime.</param>
        /// <param name="cell">Cell coordinates in that habitat.</param>
        /// <param name="side">Horizontal direction, used only for walls.</param>
        public void Bind(Habitat owner, Vector3Int cell, RoomSide side = RoomSide.North)
        {
            Owner = owner;
            Cell = cell;
            Side = side;
        }
    }
}
