using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.Building.Layout
{
    /// <summary>Describes the rooms and ladder joining two levels.</summary>
    public readonly struct LevelConnection
    {
        public Vector3Int Lower { get; }
        public Vector3Int Opening { get; }
        public Vector3Int Landing { get; }
        public RoomSide Side { get; }
        public bool Downward { get; }
        public Vector3Int LowerLanding => Lower - RoomCell.Offset(Side);

        /// <summary>Defines a connection without modifying a layout.</summary>
        /// <param name="opening">Upper cell containing the floor opening.</param>
        /// <param name="side">Direction from the opening to the upper landing.</param>
        /// <param name="downward">Whether the connection includes a lower approach floor.</param>
        public LevelConnection(Vector3Int opening, RoomSide side, bool downward)
        {
            Opening = opening;
            Lower = opening + Vector3Int.down;
            Landing = opening + RoomCell.Offset(side);
            Side = side;
            Downward = downward;
        }

        /// <summary>Enumerates occupied rooms required by this connection.</summary>
        /// <returns>Grid coordinates, including a lower approach for downward construction.</returns>
        public IEnumerable<Vector3Int> Rooms()
        {
            yield return Lower;
            yield return Opening;
            yield return Landing;
            if (Downward)
            {
                yield return LowerLanding;
            }
        }
    }
}
