using System;
using UnityEngine;

namespace DeepSky.Building.Layout
{
    public enum RoomFloor
    {
        Solid,
        Opening,
        Ladder
    }

    public enum RoomSide
    {
        North,
        East,
        South,
        West
    }

    /// <summary>Stores the editable surfaces of an occupied room cell.</summary>
    public readonly struct RoomCell
    {
        private readonly int windows;

        public RoomFloor Floor { get; }
        public RoomSide LadderSide { get; }

        /// <summary>Creates a cell snapshot.</summary>
        /// <param name="floor">Floor surface or vertical opening.</param>
        /// <param name="windows">Four-bit window mask, indexed by RoomSide.</param>
        /// <param name="ladderSide">Direction from the opening to its landing.</param>
        internal RoomCell(RoomFloor floor, int windows = 0, RoomSide ladderSide = RoomSide.North)
        {
            Floor = floor;
            this.windows = windows;
            LadderSide = ladderSide;
        }

        /// <summary>Reads a wall's selected appearance, including walls concealed by adjacent cells.</summary>
        /// <param name="side">Horizontal wall direction.</param>
        /// <returns>True when the wall is configured as a window.</returns>
        public bool HasWindow(RoomSide side)
        {
            return (windows & (1 << (int)side)) != 0;
        }

        /// <summary>Copies the cell with a different floor.</summary>
        /// <param name="floor">Requested floor surface.</param>
        /// <returns>A snapshot preserving wall selections.</returns>
        internal RoomCell WithFloor(RoomFloor floor)
        {
            return new RoomCell(floor, windows, LadderSide);
        }

        /// <summary>Copies the opening with a ladder facing its landing.</summary>
        /// <param name="side">Direction from this opening to a solid neighboring floor.</param>
        /// <returns>A ladder opening preserving window selections.</returns>
        internal RoomCell WithLadder(RoomSide side)
        {
            return new RoomCell(RoomFloor.Ladder, windows, side);
        }

        /// <summary>Copies the cell with glass on one wall.</summary>
        /// <param name="side">Horizontal wall direction.</param>
        /// <returns>A snapshot preserving the floor and other walls.</returns>
        internal RoomCell WithWindow(RoomSide side)
        {
            return new RoomCell(Floor, windows | (1 << (int)side), LadderSide);
        }

        /// <summary>Converts a wall direction into a grid step.</summary>
        /// <param name="side">One of the four horizontal directions.</param>
        /// <returns>Unit offset to the neighboring room cell.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The direction is invalid.</exception>
        public static Vector3Int Offset(RoomSide side)
        {
            return side switch
            {
                RoomSide.North => Vector3Int.forward,
                RoomSide.East => Vector3Int.right,
                RoomSide.South => Vector3Int.back,
                RoomSide.West => Vector3Int.left,
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };
        }
    }
}
