using System;
using Unity.Mathematics;
using UnityEngine;

namespace DeepSky.World.Generation
{
    /// <summary>
    /// Immutable seeded height field. Safe to sample on a worker: owns values, not Unity objects.
    /// The finite world is centered on XZ = 0; Y = 0 is sea level. Shelf indices are shallow to deep.
    /// </summary>
    public sealed class WorldData
    {
        private readonly Vector3 depths;
        private readonly BasinLayout layout;
        private readonly TerrainRelief relief;
        private readonly Vector2 rockSlope;
        private readonly float spawnOutcropRadius;
        private readonly float spawnOutcropFade;

        public int Seed { get; }
        public int ChunksPerSide { get; }
        public float ChunkSize { get; }
        public int Subdivisions { get; }
        public float Size => ChunksPerSide * ChunkSize;
        public float Spacing => ChunkSize / Subdivisions;
        public float MaximumDepth { get; }
        public Vector2 Spawn { get; }
        public float ClearingRadius { get; }

        /// <summary>Captures generation inputs and selects a shallow spawn satisfying the basin constraints.</summary>
        /// <param name="seed">Seed shared by all coordinate-based generation decisions.</param>
        /// <param name="chunksPerSide">Positive number of chunks along each world axis.</param>
        /// <param name="chunkSize">Width of one chunk in metres, greater than zero.</param>
        /// <param name="subdivisions">Positive number of terrain quads per chunk axis.</param>
        /// <param name="depths">Positive shelf depths in metres, ordered shallow to deep.</param>
        /// <param name="rockSlope">Slope angles in degrees for the start and end of the rock blend.</param>
        /// <param name="preferredSpawn">Preferred XZ position as signed fractions of the world width.</param>
        /// <param name="clearingRadius">Radius in metres excluded from vegetation placement around spawn.</param>
        /// <param name="spawnMaximumDepth">Maximum seabed depth at spawn in metres, including dunes.</param>
        /// <param name="spawnBasinRadius">Positive radius in metres of the required shallow basin.</param>
        /// <param name="spawnBasinTolerance">Maximum basin-height deviation in metres around a candidate.</param>
        /// <param name="spawnMaximumSlope">Maximum local floor slope at spawn, in degrees.</param>
        /// <param name="spawnOutcropRadius">Radius in metres around spawn with suppressed outcrops.</param>
        /// <param name="spawnOutcropFade">Distance in metres over which outcrops reach full height.</param>
        /// <param name="maximumDepth">Reference depth in metres for mapping and player recovery.</param>
        /// <param name="layout">Immutable basin layout sampled by this world.</param>
        /// <param name="relief">Immutable dune and outcrop sampler.</param>
        /// <exception cref="InvalidOperationException">No candidate satisfies the spawn constraints.</exception>
        internal WorldData(int seed, int chunksPerSide, float chunkSize, int subdivisions,
            Vector3 depths, Vector2 rockSlope, Vector2 preferredSpawn, float clearingRadius, float spawnMaximumDepth,
            float spawnBasinRadius, float spawnBasinTolerance, float spawnMaximumSlope, float spawnOutcropRadius, float spawnOutcropFade,
            float maximumDepth, BasinLayout layout, TerrainRelief relief)
        {
            Seed = seed;
            ChunksPerSide = chunksPerSide;
            ChunkSize = chunkSize;
            Subdivisions = subdivisions;
            this.depths = depths;
            this.layout = layout;
            this.relief = relief;
            this.rockSlope = rockSlope;
            this.spawnOutcropRadius = spawnOutcropRadius;
            this.spawnOutcropFade = spawnOutcropFade;
            MaximumDepth = maximumDepth;
            ClearingRadius = clearingRadius;
            Spawn = FindSpawn(preferredSpawn * Size, spawnMaximumDepth, spawnBasinRadius, spawnBasinTolerance, spawnMaximumSlope);
        }

        /// <summary>Checks the finite world domain, excluding its outer edges.</summary>
        /// <param name="position">World XZ coordinates in metres.</param>
        /// <returns>Whether both coordinates lie strictly inside the world square.</returns>
        public bool Contains(Vector2 position)
        {
            return Math.Abs(position.x) < Size * 0.5f && Math.Abs(position.y) < Size * 0.5f;
        }

        /// <summary>Maps a world position to its containing chunk without clamping to the world.</summary>
        /// <param name="position">World XZ coordinates in metres.</param>
        /// <returns>Zero-based chunk indices, potentially outside the valid chunk range.</returns>
        public Vector2Int ChunkAt(Vector2 position)
        {
            return new Vector2Int((int)Math.Floor((position.x + Size * 0.5f) / ChunkSize),
                (int)Math.Floor((position.y + Size * 0.5f) / ChunkSize));
        }

        /// <summary>Locates the minimum-XZ corner of a chunk.</summary>
        /// <param name="coordinate">Zero-based chunk indices; not bounds-checked.</param>
        /// <returns>The chunk origin in world XZ metres.</returns>
        public Vector2 ChunkOrigin(Vector2Int coordinate)
        {
            return new Vector2(coordinate.x * ChunkSize - Size * 0.5f,
                coordinate.y * ChunkSize - Size * 0.5f);
        }

        /// <summary>Continuous generation formula, also sampled outside the boundary for normal halos.</summary>
        /// <param name="x">World X coordinate in metres.</param>
        /// <param name="z">World Z coordinate in metres.</param>
        /// <returns>Seabed world Y in metres, including dunes and spawn-weighted outcrops.</returns>
        public float Height(float x, float z)
        {
            float dx = x - Spawn.x;
            float dz = z - Spawn.y;
            float distance = (float)Math.Sqrt(dx * dx + dz * dz);
            float outcropWeight = math.smoothstep(spawnOutcropRadius, spawnOutcropRadius + spawnOutcropFade, distance);
            return BasinFloor(x, z) + relief.OutcropHeight(x, z) * outcropWeight;
        }

        /// <summary>Samples the basin floor without outcrops.</summary>
        /// <param name="x">World X coordinate in metres.</param>
        /// <param name="z">World Z coordinate in metres.</param>
        /// <returns>World Y in metres with dune relief added to the basin layout.</returns>
        private float BasinFloor(float x, float z)
        {
            return layout.Height(x, z) + relief.DuneHeight(x, z);
        }

        /// <summary>Searches a regular grid for the nearest eligible shallow-basin spawn.</summary>
        /// <param name="preferred">Preferred world XZ coordinates in metres.</param>
        /// <param name="maximumDepth">Maximum actual seabed depth in metres below sea level.</param>
        /// <param name="radius">Positive basin radius in metres; also determines the search grid spacing.</param>
        /// <param name="tolerance">Allowed basin-height deviation in metres.</param>
        /// <param name="maximumSlopeDegrees">Maximum local dune-floor slope in degrees.</param>
        /// <returns>The eligible candidate closest to the preferred position.</returns>
        /// <exception cref="InvalidOperationException">No eligible candidate exists on the search grid.</exception>
        private Vector2 FindSpawn(Vector2 preferred, float maximumDepth, float radius, float tolerance, float maximumSlopeDegrees)
        {
            float searchStep = radius * 0.25f;
            float maximumSlope = (float)Math.Tan(maximumSlopeDegrees * Math.PI / 180.0);
            int limit = (int)Math.Ceiling(Size / searchStep);
            Vector2 best = Vector2.zero;
            float nearest = float.PositiveInfinity;
            for (int z = 0; z <= limit; z++)
            {
                for (int x = 0; x <= limit; x++)
                {
                    var point = new Vector2(x * searchStep - Size * 0.5f, z * searchStep - Size * 0.5f);
                    float distance = (point - preferred).sqrMagnitude;
                    if (distance >= nearest || -layout.Height(point.x, point.y) > (depths.x + depths.y) * 0.5f + tolerance
                        || -BasinFloor(point.x, point.y) > maximumDepth
                        || layout.Variation(point, radius) > tolerance)
                    {
                        continue;
                    }

                    float dx = BasinFloor(point.x - Spacing, point.y) - BasinFloor(point.x + Spacing, point.y);
                    float dz = BasinFloor(point.x, point.y - Spacing) - BasinFloor(point.x, point.y + Spacing);
                    float slope = (float)Math.Sqrt(dx * dx + dz * dz) / (2f * Spacing);
                    if (slope > maximumSlope)
                    {
                        continue;
                    }

                    best = point;
                    nearest = distance;
                }
            }

            return float.IsPositiveInfinity(nearest)
                ? throw new InvalidOperationException("No basin satisfies the spawn depth, flatness and slope constraints. Adjust the maximum spawn depth, basin radius/tolerance, shallow region, or seed.")
                : best;
        }

        /// <summary>
        /// Samples the same upward-facing triangles as the chunk mesh, including unloaded chunks.
        /// Returns false outside the finite domain, with height zero. Does not require a collider.
        /// </summary>
        /// <param name="position">World position in metres; its Y coordinate is ignored.</param>
        /// <param name="height">Triangle-interpolated world Y in metres, or zero when outside the domain.</param>
        /// <returns>Whether the position lies inside the terrain domain.</returns>
        public bool TryGetHeight(Vector3 position, out float height)
        {
            height = 0f;
            if (!Contains(new Vector2(position.x, position.z)))
            {
                return false;
            }

            float gx = (position.x + Size * 0.5f) / Spacing;
            float gz = (position.z + Size * 0.5f) / Spacing;
            int ix = (int)Math.Floor(gx);
            int iz = (int)Math.Floor(gz);
            float u = gx - ix;
            float v = gz - iz;
            float x = ix * Spacing - Size * 0.5f;
            float z = iz * Spacing - Size * 0.5f;
            float a = Height(x, z);
            float b = Height(x + Spacing, z);
            float c = Height(x, z + Spacing);
            float d = Height(x + Spacing, z + Spacing);
            height = u + v <= 1f ? a + (b - a) * u + (c - a) * v
                : d + (c - d) * (1f - u) + (b - d) * (1f - v);
            return true;
        }

        /// <summary>Estimates the upward terrain normal using central differences at mesh spacing.</summary>
        /// <param name="x">World X coordinate in metres.</param>
        /// <param name="z">World Z coordinate in metres.</param>
        /// <returns>A unit normal, including when samples extend beyond the world boundary.</returns>
        public Vector3 Normal(float x, float z)
        {
            float dx = Height(x - Spacing, z) - Height(x + Spacing, z);
            float dz = Height(x, z - Spacing) - Height(x, z + Spacing);
            float y = 2f * Spacing;
            float length = (float)Math.Sqrt(dx * dx + y * y + dz * dz);
            return new Vector3(dx / length, y / length, dz / length);
        }

        /// <summary>Converts terrain slope to the authored sand-to-rock blend.</summary>
        /// <param name="normal">Unit terrain normal in world space.</param>
        /// <returns>A weight from zero (sand) to one (rock).</returns>
        public float RockWeight(Vector3 normal)
        {
            float angle = (float)(Math.Acos(Math.Min(1f, Math.Max(-1f, normal.y))) * 180.0 / Math.PI);
            return math.smoothstep(rockSlope.x, rockSlope.y, angle);
        }

        /// <summary>Classifies terrain using the midpoints between authored shelf depths.</summary>
        /// <param name="height">Terrain world Y in metres, with sea level at zero.</param>
        /// <returns>Zero for shallow, one for middle, or two for deep terrain.</returns>
        public int Stage(float height)
        {
            float depth = -height;
            return depth < (depths.x + depths.y) * 0.5f ? 0 : depth < (depths.y + depths.z) * 0.5f ? 1 : 2;
        }

        /// <summary>Samples the seeded vegetation-density mask.</summary>
        /// <param name="x">World X coordinate in metres.</param>
        /// <param name="z">World Z coordinate in metres.</param>
        /// <returns>A patch weight between zero and one.</returns>
        public float VegetationPatch(float x, float z)
        {
            return math.smoothstep(-0.65f, 0.35f, Noise(x / 28f, z / 28f, 41));
        }

        /// <summary>Interpolates coordinate-hashed lattice values with cubic smoothstep weights.</summary>
        /// <param name="x">X coordinate in noise-grid units.</param>
        /// <param name="z">Z coordinate in noise-grid units.</param>
        /// <param name="stream">Discriminator separating independent seeded patterns.</param>
        /// <returns>Signed value noise in the range [-1, 1).</returns>
        private float Noise(float x, float z, int stream)
        {
            int ix = (int)Math.Floor(x);
            int iz = (int)Math.Floor(z);
            float u = math.smoothstep(0f, 1f, x - ix);
            float v = math.smoothstep(0f, 1f, z - iz);
            return Mathf.LerpUnclamped(
                Mathf.LerpUnclamped(CoordinateRandom.Value01(Seed, ix, iz, stream), CoordinateRandom.Value01(Seed, ix + 1, iz, stream), u),
                Mathf.LerpUnclamped(CoordinateRandom.Value01(Seed, ix, iz + 1, stream), CoordinateRandom.Value01(Seed, ix + 1, iz + 1, stream), u), v) * 2f - 1f;
        }
    }
}
