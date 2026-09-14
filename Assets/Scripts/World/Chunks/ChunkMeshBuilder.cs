using System.Threading;
using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.World.Chunks
{
    /// <summary>Numerical worker output. Unity mesh creation and collider cooking belong to WorldChunk.</summary>
    public sealed class ChunkMeshData
    {
        public Vector2Int Coordinate { get; }
        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public Color[] Colors { get; }
        public int[] Triangles { get; }

        /// <summary>Allocates the arrays filled by a chunk-generation worker.</summary>
        /// <param name="coordinate">Zero-based chunk indices identifying this output.</param>
        /// <param name="vertices">Nonnegative vertex count, also used for normals and colors.</param>
        /// <param name="triangles">Nonnegative index count, not the number of triangles.</param>
        internal ChunkMeshData(Vector2Int coordinate, int vertices, int triangles)
        {
            Coordinate = coordinate;
            Vertices = new Vector3[vertices];
            Normals = new Vector3[vertices];
            Colors = new Color[vertices];
            Triangles = new int[triangles];
        }
    }

    public static class ChunkMeshBuilder
    {
        /// <summary>Global lattice coordinates make neighboring edges and their halo-derived normals identical.</summary>
        /// <param name="world">Immutable generation snapshot safe to sample on the calling worker.</param>
        /// <param name="coordinate">Chunk indices within the world's valid grid.</param>
        /// <param name="cancellation">Cancellation checked before each vertex row.</param>
        /// <returns>Owned numerical arrays with chunk-local XZ, world Y, normals, material weights and indices.</returns>
        /// <exception cref="System.OperationCanceledException">Cancellation is requested during generation.</exception>
        public static ChunkMeshData Build(WorldData world, Vector2Int coordinate, CancellationToken cancellation)
        {
            int n = world.Subdivisions;
            int stride = n + 1;
            var result = new ChunkMeshData(coordinate, stride * stride, n * n * 6);
            Vector2 origin = world.ChunkOrigin(coordinate);
            for (int z = 0; z <= n; z++)
            {
                cancellation.ThrowIfCancellationRequested();
                for (int x = 0; x <= n; x++)
                {
                    int i = z * stride + x;
                    float wx = (coordinate.x * n + x) * world.Spacing - world.Size * 0.5f;
                    float wz = (coordinate.y * n + z) * world.Spacing - world.Size * 0.5f;
                    Vector3 normal = world.Normal(wx, wz);
                    float rock = world.RockWeight(normal);
                    result.Vertices[i] = new Vector3(wx - origin.x, world.Height(wx, wz), wz - origin.y);
                    result.Normals[i] = normal;
                    result.Colors[i] = new Color(1f - rock, rock, 0f, 1f);
                    if (x == n || z == n)
                    {
                        continue;
                    }

                    int t = (z * n + x) * 6;
                    result.Triangles[t] = i;
                    result.Triangles[t + 1] = i + stride;
                    result.Triangles[t + 2] = i + 1;
                    result.Triangles[t + 3] = i + 1;
                    result.Triangles[t + 4] = i + stride;
                    result.Triangles[t + 5] = i + stride + 1;
                }
            }

            return result;
        }
    }
}
