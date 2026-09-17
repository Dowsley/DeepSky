using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Samples deterministic population candidates on the seabed.</summary>
    internal static class PopulationSampling
    {
        /// <summary>Derives a repeatable random seed for one rule in one chunk.</summary>
        /// <param name="world">Active terrain snapshot.</param>
        /// <param name="coordinate">Chunk indices.</param>
        /// <param name="rule">Rule with a stable seed stream.</param>
        /// <returns>A seed independent of population order.</returns>
        internal static int Seed(WorldData world, Vector2Int coordinate, PopulationRule rule)
        {
            int seed = unchecked(world.Seed + rule.SeedStream * 73856093);
            return (int)(CoordinateRandom.Value01(seed, coordinate.x, coordinate.y, 0) * int.MaxValue);
        }

        /// <summary>Rounds the expected population using deterministic fractional acceptance.</summary>
        /// <param name="world">Snapshot supplying chunk area in square metres.</param>
        /// <param name="rule">Candidate density settings.</param>
        /// <param name="seed">Chunk and rule seed.</param>
        /// <returns>Nonnegative candidate count before terrain filtering.</returns>
        internal static int Count(WorldData world, PopulationRule rule, int seed)
        {
            float expected = rule.Density * world.ChunkSize * world.ChunkSize;
            int count = Mathf.FloorToInt(expected);
            return count + (CoordinateRandom.Value01(seed, 0, 0, 1) < expected - count ? 1 : 0);
        }

        /// <summary>Samples a candidate without changing the world or random state.</summary>
        /// <param name="world">Terrain snapshot.</param>
        /// <param name="origin">Chunk origin in world XZ metres.</param>
        /// <param name="rule">Surface and placement settings.</param>
        /// <param name="seed">Chunk and rule seed.</param>
        /// <param name="index">Zero-based candidate index.</param>
        /// <param name="inset">Minimum distance from the chunk boundary in metres.</param>
        /// <param name="position">Sampled world position, including seabed clearance.</param>
        /// <param name="normal">Sampled terrain normal.</param>
        /// <returns>Whether the candidate fits the chunk and passes surface filters.</returns>
        internal static bool TryPosition(WorldData world, Vector2 origin, PopulationRule rule,
            int seed, int index, float inset, out Vector3 position, out Vector3 normal)
        {
            position = Vector3.zero;
            normal = Vector3.up;
            if (inset * 2f > world.ChunkSize)
            {
                return false;
            }
            float x = origin.x + Mathf.Lerp(inset, world.ChunkSize - inset, CoordinateRandom.Value01(seed, index, 0, 2));
            float z = origin.y + Mathf.Lerp(inset, world.ChunkSize - inset, CoordinateRandom.Value01(seed, index, 0, 3));
            normal = world.Normal(x, z);
            float rock = world.RockWeight(normal);
            if ((new Vector2(x, z) - world.Spawn).sqrMagnitude < world.ClearingRadius * world.ClearingRadius
                || Vector3.Angle(Vector3.up, normal) > rule.MaximumSlope
                || rock < rule.RockWeightRange.x || rock > rule.RockWeightRange.y
                || !AcceptsRegion(world, rule, x, z)
                || (rule.FollowVegetationPatches && CoordinateRandom.Value01(seed, index, 0, 4) > world.VegetationPatch(x, z)))
            {
                return false;
            }
            if (!world.TryGetHeight(new Vector3(x, 0f, z), out float height))
            {
                return false;
            }
            position = new Vector3(x, height + rule.SeabedClearance, z);
            return true;
        }

        /// <summary>Filters species through a shared world-space pattern without chunk seams.</summary>
        /// <param name="world">Snapshot supplying the world seed.</param>
        /// <param name="rule">Optional region identity, interval in metres and accepted zero-to-one range.</param>
        /// <param name="x">Candidate world X in metres.</param>
        /// <param name="z">Candidate world Z in metres.</param>
        /// <returns>True when regional filtering is disabled or the candidate is inside its range.</returns>
        private static bool AcceptsRegion(WorldData world, PopulationRule rule, float x, float z)
        {
            if (rule.RegionStream == 0)
            {
                return true;
            }
            float offsetX = CoordinateRandom.Value01(world.Seed, rule.RegionStream, 0, 81) * 1000f;
            float offsetZ = CoordinateRandom.Value01(world.Seed, rule.RegionStream, 0, 82) * 1000f;
            float value = Mathf.Clamp01(Mathf.PerlinNoise(x / rule.RegionSize + offsetX, z / rule.RegionSize + offsetZ));
            return value >= rule.RegionRange.x && value <= rule.RegionRange.y;
        }

        /// <summary>Samples the uniform multiplier for a candidate's prefab scale.</summary>
        /// <param name="rule">Scale range.</param>
        /// <param name="seed">Chunk and rule seed.</param>
        /// <param name="index">Zero-based candidate index.</param>
        /// <returns>Uniform scale multiplier.</returns>
        internal static float Scale(PopulationRule rule, int seed, int index)
        {
            return Mathf.Lerp(rule.ScaleMultiplier.x, rule.ScaleMultiplier.y, CoordinateRandom.Value01(seed, index, 0, 5));
        }
    }
}
