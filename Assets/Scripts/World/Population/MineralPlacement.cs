using System.Collections.Generic;
using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Reserves separated mineral areas within one chunk.</summary>
    internal sealed class MineralPlacement
    {
        private readonly HashSet<(int stream, int index)> accepted = new();

        /// <summary>Chooses nonoverlapping candidates in seeded priority order.</summary>
        /// <param name="world">Terrain snapshot used for all placement checks.</param>
        /// <param name="coordinate">Destination chunk indices.</param>
        /// <param name="profile">Mineral rules and minimum edge separation in metres.</param>
        internal MineralPlacement(WorldData world, Vector2Int coordinate, WorldContentProfile profile)
        {
            var candidates = new List<(int stream, int index, Vector2 position, float radius, float priority)>();
            Vector2 origin = world.ChunkOrigin(coordinate);
            foreach (PopulationRule rule in profile.Populations)
            {
                if (rule is not MineralPopulationRule mineral)
                {
                    continue;
                }
                int seed = PopulationSampling.Seed(world, coordinate, rule);
                int count = PopulationSampling.Count(world, rule, seed);
                for (int i = 0; i < count; i++)
                {
                    float radius = mineral.BoundingRadius * PopulationSampling.Scale(rule, seed, i);
                    float inset = radius + profile.MineralSeparation * .5f;
                    if (PopulationSampling.TryPosition(world, origin, rule, seed, i, inset, out Vector3 point, out _))
                    {
                        candidates.Add((rule.SeedStream, i, new Vector2(point.x, point.z), radius,
                            CoordinateRandom.Value01(seed, i, 0, 7)));
                    }
                }
            }
            candidates.Sort((a, b) =>
            {
                int priority = a.priority.CompareTo(b.priority);
                if (priority != 0)
                {
                    return priority;
                }
                int stream = a.stream.CompareTo(b.stream);
                return stream != 0 ? stream : a.index.CompareTo(b.index);
            });
            var reserved = new List<(Vector2 position, float radius)>();
            foreach (var candidate in candidates)
            {
                bool blocked = false;
                foreach (var area in reserved)
                {
                    float separation = candidate.radius + area.radius + profile.MineralSeparation;
                    if ((candidate.position - area.position).sqrMagnitude < separation * separation)
                    {
                        blocked = true;
                        break;
                    }
                }
                if (!blocked)
                {
                    reserved.Add((candidate.position, candidate.radius));
                    accepted.Add((candidate.stream, candidate.index));
                }
            }
        }

        /// <summary>Checks whether a mineral candidate owns a reserved area.</summary>
        /// <param name="rule">Rule with a unique seed stream in the profile.</param>
        /// <param name="index">Zero-based candidate index.</param>
        /// <returns>Whether the candidate can be instantiated.</returns>
        internal bool Contains(PopulationRule rule, int index)
        {
            return accepted.Contains((rule.SeedStream, index));
        }
    }
}
