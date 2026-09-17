using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.Animals.Movement
{
    /// <summary>Caches conservative terrain-cell heights shared by nearby swimming animals.</summary>
    internal sealed class SwimmingTerrain
    {
        private static readonly ConditionalWeakTable<WorldData, SwimmingTerrain> Worlds = new();
        private readonly Dictionary<Vector2Int, float> cells = new();
        private readonly WorldData world;

        /// <summary>Retains one immutable terrain snapshot for clearance queries.</summary>
        /// <param name="terrain">World whose generated cells are sampled.</param>
        private SwimmingTerrain(WorldData terrain)
        {
            world = terrain;
        }

        /// <summary>Shares cached heights without extending the world's lifetime.</summary>
        /// <param name="world">Immutable terrain snapshot.</param>
        /// <returns>The snapshot's swimming clearance cache.</returns>
        internal static SwimmingTerrain For(WorldData world)
        {
            return Worlds.GetValue(world, terrain => new SwimmingTerrain(terrain));
        }

        /// <summary>Finds the highest vertex of the containing terrain cell.</summary>
        /// <param name="point">World position; Y is ignored.</param>
        /// <returns>Conservative terrain height, or positive infinity outside the world.</returns>
        internal float Height(Vector3 point)
        {
            if (!world.Contains(new Vector2(point.x, point.z)))
            {
                return float.PositiveInfinity;
            }
            float half = world.Size * .5f;
            float spacing = world.Spacing;
            Vector2Int cell = new(Mathf.FloorToInt((point.x + half) / spacing), Mathf.FloorToInt((point.z + half) / spacing));
            if (cells.TryGetValue(cell, out float height))
            {
                return height;
            }
            float x = cell.x * spacing - half;
            float z = cell.y * spacing - half;
            height = Mathf.Max(world.Height(x, z), world.Height(x + spacing, z),
                world.Height(x, z + spacing), world.Height(x + spacing, z + spacing));
            if (cells.Count >= 65536)
            {
                cells.Clear();
            }
            cells.Add(cell, height);
            return height;
        }
    }
}
