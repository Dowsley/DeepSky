using System.Collections.Generic;
using DeepSky.World.Chunks;
using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>
    /// Incremental main-thread placement. Candidate randomness depends only on seed, chunk, rule ID and index.
    /// </summary>
    internal static class ChunkPopulation
    {
        /// <summary>Places deterministic content incrementally on the Unity main thread.</summary>
        /// <param name="manager">Active world supplying generation data, depth profiles and the observer camera.</param>
        /// <param name="chunk">Inactive destination chunk at its world origin; owns generated meshes and children.</param>
        /// <param name="preview">Whether content is being created for editor preview.</param>
        /// <returns>An enumerator yielding zero for rejected candidates and one for placements or completed batches. Dispose when canceled.</returns>
        internal static IEnumerator<int> Populate(WorldManager manager, WorldChunk chunk, bool preview)
        {
            WorldData world = manager.Data;
            Vector2 origin = world.ChunkOrigin(chunk.Coordinate);
            WorldContentProfile profile = manager.ContentAt(origin + Vector2.one * (world.ChunkSize * 0.5f));
            var minerals = new MineralPlacement(world, chunk.Coordinate, profile);
            foreach (PopulationRule rule in profile.Populations)
            {
                var placements = new List<Matrix4x4>();
                MineralPopulationRule? mineral = rule as MineralPopulationRule;
                int chunkSeed = PopulationSampling.Seed(world, chunk.Coordinate, rule);
                int count = PopulationSampling.Count(world, rule, chunkSeed);

                for (int i = 0; i < count; i++)
                {
                    float scale = PopulationSampling.Scale(rule, chunkSeed, i);
                    float inset = mineral != null ? mineral.BoundingRadius * scale + profile.MineralSeparation * .5f : 0f;
                    if ((mineral != null && !minerals.Contains(rule, i))
                        || !PopulationSampling.TryPosition(world, origin, rule, chunkSeed, i, inset,
                            out Vector3 position, out Vector3 normal))
                    {
                        yield return 0;
                        continue;
                    }

                    float angle = CoordinateRandom.Value01(chunkSeed, i, 0, 6) * 360f;
                    Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                    if (rule.AlignToSurface)
                    {
                        rotation = Quaternion.FromToRotation(Vector3.up, normal) * rotation;
                    }
                    if (rule is PlantPopulationRule)
                    {
                        placements.Add(Matrix4x4.TRS(position - chunk.transform.position, rotation,
                            rule.PrefabTransform.localScale * scale));
                    }
                    else
                    {
                        ((InstancePopulationRule)rule).Spawn(new PopulationSpawn(manager, chunk,
                            position, rotation, scale, chunkSeed + i, i, preview));
                    }

                    yield return 1;
                }

                if (placements.Count > 0)
                {
                    PlantBatchSource source = ((PlantPopulationRule)rule).Source;
                    Mesh mesh = PlantBatchBuilder.Build(source.Mesh, source.LocalTransform, placements, rule.Label);
                    chunk.AddPlantBatch(rule.Label, mesh, source.Material);
                    yield return 1;
                }
            }
        }
    }
}
