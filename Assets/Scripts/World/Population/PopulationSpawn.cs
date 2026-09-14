using DeepSky.World.Chunks;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Supplies placement and lifetime context for one population candidate.</summary>
    public readonly struct PopulationSpawn
    {
        public WorldManager Manager { get; }
        public WorldChunk Chunk { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Scale { get; }
        public int Seed { get; }
        public int Candidate { get; }
        public bool Preview { get; }

        /// <summary>Records a candidate without creating or owning Unity objects.</summary>
        /// <param name="manager">Owner supplying world data and scene dependencies.</param>
        /// <param name="chunk">Inactive destination owning any instantiated objects.</param>
        /// <param name="position">World position in metres.</param>
        /// <param name="rotation">World orientation.</param>
        /// <param name="scale">Positive multiplier of the prefab's local scale.</param>
        /// <param name="seed">Deterministic per-candidate steering seed.</param>
        /// <param name="candidate">Zero-based candidate index within the rule.</param>
        /// <param name="preview">Whether content is being generated for editor inspection.</param>
        public PopulationSpawn(WorldManager manager, WorldChunk chunk, Vector3 position, Quaternion rotation,
            float scale, int seed, int candidate, bool preview)
        {
            Manager = manager;
            Chunk = chunk;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Seed = seed;
            Candidate = candidate;
            Preview = preview;
        }

        /// <summary>Clones a typed prefab under the inactive chunk and applies its candidate placement.</summary>
        /// <typeparam name="T">Prefab root component type.</typeparam>
        /// <param name="prefab">Shared component on the prefab root, not modified.</param>
        /// <returns>A clone owned by the destination chunk.</returns>
        public T Instantiate<T>(T prefab) where T : Component
        {
            T instance = Object.Instantiate(prefab, Chunk.transform);
            Place(instance.transform, prefab.transform.localScale);
            return instance;
        }

        /// <summary>Applies a candidate's world pose and scaled prefab dimensions.</summary>
        /// <param name="instance">Transform parented to the destination chunk.</param>
        /// <param name="prefabScale">Unmodified prefab local scale.</param>
        public void Place(Transform instance, Vector3 prefabScale)
        {
            instance.SetPositionAndRotation(Position, Rotation);
            instance.localScale = prefabScale * Scale;
        }
    }
}
