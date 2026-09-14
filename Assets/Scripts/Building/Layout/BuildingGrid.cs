using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.Building.Layout
{
    /// <summary>Owns room occupancy and structural edits.</summary>
    public sealed class BuildingGrid
    {
        private static readonly Vector3Int[] Neighbors =
        {
            Vector3Int.forward, Vector3Int.right, Vector3Int.back,
            Vector3Int.left, Vector3Int.up, Vector3Int.down
        };

        private readonly Dictionary<Vector3Int, RoomCell> cells = new();
        private readonly int radius;
        private readonly int levels;

        public event Action? Changed;
        public IEnumerable<Vector3Int> Coordinates => cells.Keys;
        public int Count => cells.Count;

        /// <summary>Creates an empty, bounded construction grid.</summary>
        /// <param name="radius">Positive maximum absolute horizontal cell coordinate.</param>
        /// <param name="levels">Positive level count, starting at level zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive.</exception>
        public BuildingGrid(int radius, int levels)
        {
            if (radius < 1 || levels < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Grid dimensions must be positive.");
            }
            this.radius = radius;
            this.levels = levels;
        }

        /// <summary>Reads a cell snapshot without exposing mutable layout state.</summary>
        /// <param name="position">Integer cell coordinates.</param>
        /// <param name="cell">The occupied cell, or the default value when absent.</param>
        /// <returns>Whether the position is occupied.</returns>
        public bool TryGet(Vector3Int position, out RoomCell cell)
        {
            return cells.TryGetValue(position, out cell);
        }

        /// <summary>Checks a prospective horizontal expansion without changing the layout.</summary>
        /// <param name="position">Requested cell coordinates.</param>
        /// <returns>Empty when valid, otherwise a short placement reason.</returns>
        public string CheckExpansion(Vector3Int position)
        {
            if (!InBounds(position))
            {
                return "Building limit";
            }
            if (cells.ContainsKey(position))
            {
                return "Already built";
            }
            if (cells.Count == 0)
            {
                return position.y == 0 ? "" : "Start at ground level";
            }
            for (int side = 0; side < 4; side++)
            {
                if (cells.ContainsKey(position + Neighbors[side]))
                {
                    return "";
                }
            }
            return "Attach to a room";
        }

        /// <summary>Adds a floor and room volume along an existing level.</summary>
        /// <param name="position">Requested cell coordinates.</param>
        /// <returns>True when the layout changed; invalid requests leave it untouched.</returns>
        public bool TryExpand(Vector3Int position)
        {
            if (CheckExpansion(position).Length > 0)
            {
                return false;
            }
            cells.Add(position, new RoomCell(RoomFloor.Solid));
            Changed?.Invoke();
            return true;
        }

        /// <summary>Plans a floor or ceiling connection, preferring the requested landing direction.</summary>
        /// <param name="origin">Existing room selected by the player.</param>
        /// <param name="upward">True for a ceiling connection, false for a floor connection.</param>
        /// <param name="preferred">Preferred direction from the opening to the landing.</param>
        /// <param name="connection">Validated connection, or default on failure.</param>
        /// <returns>Empty when valid, otherwise a short placement reason.</returns>
        public string CheckLevel(Vector3Int origin, bool upward, RoomSide preferred, out LevelConnection connection)
        {
            connection = default;
            if (!cells.ContainsKey(origin))
            {
                return "Aim at a room";
            }
            Vector3Int upper = upward ? origin + Vector3Int.up : origin;
            if (!InBounds(upper) || !InBounds(upper + Vector3Int.down))
            {
                return "Building limit";
            }
            if (cells.TryGetValue(upper, out RoomCell room) && room.Floor == RoomFloor.Ladder)
            {
                return "Already built";
            }
            for (int i = 0; i < 4; i++)
            {
                var side = (RoomSide)(((int)preferred + i) % 4);
                var candidate = new LevelConnection(upper, side, !upward);
                if (InBounds(candidate.Landing)
                    && (upward || InBounds(candidate.LowerLanding))
                    && (!cells.TryGetValue(candidate.Landing, out RoomCell landing) || landing.Floor == RoomFloor.Solid))
                {
                    connection = candidate;
                    return "";
                }
            }
            return "No room for a landing";
        }

        /// <summary>Creates a vertical connection and notifies observers once the layout is complete.</summary>
        /// <param name="origin">Existing selected room.</param>
        /// <param name="upward">True to connect above, false to connect below.</param>
        /// <param name="preferred">Preferred direction from the opening to its landing.</param>
        /// <returns>True when the connection was created.</returns>
        public bool TryAddLevel(Vector3Int origin, bool upward, RoomSide preferred)
        {
            if (CheckLevel(origin, upward, preferred, out LevelConnection connection).Length > 0)
            {
                return false;
            }
            foreach (Vector3Int room in connection.Rooms())
            {
                if (!cells.ContainsKey(room))
                {
                    cells.Add(room, new RoomCell(RoomFloor.Solid));
                }
            }
            cells.TryGetValue(connection.Opening, out RoomCell opening);
            cells[connection.Opening] = opening.WithLadder(connection.Side);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Finds an opening beside the selected landing, using facing before fallback directions.</summary>
        /// <param name="landing">Existing solid floor selected for ladder placement.</param>
        /// <param name="preferred">Preferred direction from the landing toward an opening.</param>
        /// <param name="opening">Adjacent opening, or default on failure.</param>
        /// <param name="side">Direction from the opening back to the landing.</param>
        /// <returns>Empty when a ladder fits, otherwise a placement reason.</returns>
        public string CheckLadder(Vector3Int landing, RoomSide preferred, out Vector3Int opening, out RoomSide side)
        {
            opening = default;
            side = default;
            if (!cells.TryGetValue(landing, out RoomCell floor) || floor.Floor != RoomFloor.Solid)
            {
                return "Aim at a floor beside an opening";
            }
            for (int i = 0; i < 4; i++)
            {
                var direction = (RoomSide)(((int)preferred + i) % 4);
                Vector3Int candidate = landing + RoomCell.Offset(direction);
                if (cells.TryGetValue(candidate, out RoomCell room) && room.Floor == RoomFloor.Opening)
                {
                    opening = candidate;
                    side = (RoomSide)(((int)direction + 2) % 4);
                    return "";
                }
            }
            return "Needs an adjacent opening";
        }

        /// <summary>Installs a ladder from a solid landing into a neighboring opening.</summary>
        /// <param name="landing">Selected solid floor.</param>
        /// <param name="preferred">Preferred direction from the landing to the opening.</param>
        /// <returns>True when a ladder was installed.</returns>
        public bool TryAddLadder(Vector3Int landing, RoomSide preferred)
        {
            if (CheckLadder(landing, preferred, out Vector3Int opening, out RoomSide side).Length > 0)
            {
                return false;
            }
            cells[opening] = cells[opening].WithLadder(side);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Checks a floor change without removing its room.</summary>
        /// <param name="position">Occupied cell coordinates.</param>
        /// <param name="floor">Solid floor or empty opening. Ladders use their placement operation.</param>
        /// <returns>Empty when valid, otherwise a short placement reason.</returns>
        public string CheckFloorChange(Vector3Int position, RoomFloor floor)
        {
            if (!Enum.IsDefined(typeof(RoomFloor), floor) || floor == RoomFloor.Ladder)
            {
                return "Invalid floor type";
            }
            if (!cells.TryGetValue(position, out RoomCell cell))
            {
                return "Aim at a room";
            }
            if (cell.Floor == floor)
            {
                return "Already built";
            }
            return "";
        }

        /// <summary>Changes a floor without removing the surrounding room or its air volume.</summary>
        /// <param name="position">Occupied cell coordinates.</param>
        /// <param name="floor">Solid floor or empty opening.</param>
        /// <returns>True when the floor changed.</returns>
        public bool TrySetFloor(Vector3Int position, RoomFloor floor)
        {
            if (CheckFloorChange(position, floor).Length > 0)
            {
                return false;
            }
            cells[position] = cells[position].WithFloor(floor);
            RemoveInvalidLadders();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Applies glass to an exposed horizontal wall.</summary>
        /// <param name="position">Occupied cell coordinates.</param>
        /// <param name="side">Direction of the selected exterior wall.</param>
        /// <returns>True when the wall changed; interior boundaries are not editable walls.</returns>
        public bool TryAddWindow(Vector3Int position, RoomSide side)
        {
            Vector3Int neighbor = position + RoomCell.Offset(side);
            if (!cells.TryGetValue(position, out RoomCell cell) || cells.ContainsKey(neighbor) || cell.HasWindow(side))
            {
                return false;
            }
            cells[position] = cell.WithWindow(side);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Checks whether a room exists for removal.</summary>
        /// <param name="position">Cell to remove, including its room volume.</param>
        /// <returns>Empty when valid, otherwise a short placement reason.</returns>
        public string CheckRemoval(Vector3Int position)
        {
            if (!cells.ContainsKey(position))
            {
                return "Aim at a room";
            }
            return "";
        }

        /// <summary>Removes a room and notifies observers after validating the remaining structure.</summary>
        /// <param name="position">Occupied cell to remove.</param>
        /// <returns>True when the room was removed, including the final room of a base.</returns>
        public bool TryRemove(Vector3Int position)
        {
            if (CheckRemoval(position).Length > 0)
            {
                return false;
            }
            cells.Remove(position);
            RemoveInvalidLadders();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Checks the grid's finite authoring bounds.</summary>
        /// <param name="position">Integer room coordinates.</param>
        /// <returns>Whether the horizontal coordinates and level are permitted.</returns>
        private bool InBounds(Vector3Int position)
        {
            return position.x >= -radius && position.x <= radius && position.z >= -radius && position.z <= radius
                && position.y >= 0 && position.y < levels;
        }

        /// <summary>Removes ladders whose upper landing has been removed or opened.</summary>
        private void RemoveInvalidLadders()
        {
            foreach (Vector3Int position in new List<Vector3Int>(cells.Keys))
            {
                RoomCell room = cells[position];
                if (room.Floor == RoomFloor.Ladder
                    && (!cells.TryGetValue(position + RoomCell.Offset(room.LadderSide), out RoomCell landing)
                        || landing.Floor != RoomFloor.Solid))
                {
                    cells[position] = room.WithFloor(RoomFloor.Opening);
                }
            }
        }
    }
}
