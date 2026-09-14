using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.Building.Layout
{
    /// <summary>Defines a base's starting rooms and construction dimensions.</summary>
    [CreateAssetMenu(menuName = "DeepSky/Building/Building Plan")]
    public sealed class BuildingPlan : ScriptableObject
    {
        [Header("Scale")]
        [SerializeField, Min(1f)] private float cellSize = 1f;
        [SerializeField, Min(2.5f)] private float levelHeight = 3.5f;
        [SerializeField, Min(1)] private int horizontalRadius = 15;
        [SerializeField, Min(1)] private int maximumLevels = 6;

        [Header("Starting rooms")]
        [SerializeField] private Vector2Int footprint = new(5, 5);
        [SerializeField] private RectInt entryOpening = new(1, 1, 2, 2);
        [SerializeField] private Vector2Int entryLadder = new(1, 1);

        public float CellSize => cellSize;
        public float LevelHeight => levelHeight;
        public Vector2Int Footprint => footprint;

        /// <summary>Builds independent session state from the configured starter footprint.</summary>
        /// <returns>A connected room grid with a bottom opening and ladder.</returns>
        /// <exception cref="InvalidOperationException">The plan dimensions or entrance are invalid.</exception>
        public BuildingGrid CreateLayout()
        {
            ValidatePlan();
            var grid = new BuildingGrid(horizontalRadius, maximumLevels);
            for (int z = 0; z < footprint.y; z++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    var coordinate = new Vector3Int(x, 0, z);
                    grid.TryExpand(coordinate);
                    if (entryOpening.Contains(new Vector2Int(x, z)))
                    {
                        grid.TrySetFloor(coordinate, RoomFloor.Opening);
                    }
                }
            }
            var entrance = new Vector3Int(entryLadder.x, 0, entryLadder.y);
            bool hasLadder = false;
            for (int i = 0; i < 4; i++)
            {
                Vector3Int landing = entrance + RoomCell.Offset((RoomSide)i);
                if (grid.TryAddLadder(landing, (RoomSide)((i + 2) % 4)))
                {
                    hasLadder = true;
                    break;
                }
            }
            if (!hasLadder)
            {
                throw new InvalidOperationException("The entry ladder requires a neighboring solid landing.");
            }
            // Alternate solid service panels with glass along the long sides.
            foreach (Vector3Int coordinate in new List<Vector3Int>(grid.Coordinates))
            {
                if (coordinate.x % 2 == 0)
                {
                    grid.TryAddWindow(coordinate, RoomSide.North);
                    grid.TryAddWindow(coordinate, RoomSide.South);
                }
                if (coordinate.z % 2 == 0)
                {
                    grid.TryAddWindow(coordinate, RoomSide.East);
                    grid.TryAddWindow(coordinate, RoomSide.West);
                }
            }
            return grid;
        }

        /// <summary>Validates the footprint and reserves solid walking space around the entrance.</summary>
        /// <exception cref="InvalidOperationException">A dimension, bound or entrance is invalid.</exception>
        public void ValidatePlan()
        {
            if (cellSize < 1f || levelHeight < 2.5f || horizontalRadius < 1 || maximumLevels < 1
                || footprint.x < 3 || footprint.y < 3
                || footprint.x > horizontalRadius + 1 || footprint.y > horizontalRadius + 1)
            {
                throw new InvalidOperationException("The building plan requires a valid footprint and construction dimensions.");
            }
            if (entryOpening.width < 1 || entryOpening.height < 1
                || entryOpening.xMin < 1 || entryOpening.yMin < 1
                || entryOpening.xMax >= footprint.x || entryOpening.yMax >= footprint.y
                || !entryOpening.Contains(entryLadder))
            {
                throw new InvalidOperationException("The entry opening must contain its ladder and have a floor border.");
            }
        }
    }
}
