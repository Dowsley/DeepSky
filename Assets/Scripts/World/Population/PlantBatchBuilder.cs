using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepSky.World.Population
{
    /// <summary>Builds combined plant meshes.</summary>
    internal static class PlantBatchBuilder
    {
        /// <summary>Combines plant instances into one chunk-local mesh on the Unity main thread.</summary>
        /// <param name="source">Readable mesh with normals, UVs and colors for every vertex; not modified.</param>
        /// <param name="sourceTransform">Transform from mesh-local coordinates to prefab-root coordinates.</param>
        /// <param name="placements">Transforms from prefab-root coordinates to chunk-local coordinates; not modified.</param>
        /// <param name="label">Name assigned to the generated mesh.</param>
        /// <returns>A transient mesh whose ownership must be transferred to the chunk or explicitly released.</returns>
        internal static Mesh Build(Mesh source, Matrix4x4 sourceTransform, IReadOnlyList<Matrix4x4> placements, string label)
        {
            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector2[] sourceUv = source.uv;
            Color[] sourceColors = source.colors;
            int[] sourceTriangles = source.triangles;
            int count = sourceVertices.Length * placements.Count;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var colors = new Color[count];
            var uv = new Vector2[count];
            var swayData = new Vector2[count];
            var triangles = new int[sourceTriangles.Length * placements.Count];
            for (int p = 0; p < placements.Count; p++)
            {
                Matrix4x4 matrix = placements[p] * sourceTransform;
                Matrix4x4 normalMatrix = matrix.inverse.transpose;
                // Alpha encodes normalized root-to-tip height plus direction, not metres.
                // UV2.x preserves the direction offset so coherent sway can recover height.
                float directionOffset = matrix.rotation.eulerAngles.y * Mathf.Deg2Rad / 1000f;
                for (int v = 0; v < sourceVertices.Length; v++)
                {
                    int target = p * sourceVertices.Length + v;
                    vertices[target] = matrix.MultiplyPoint3x4(sourceVertices[v]);
                    normals[target] = normalMatrix.MultiplyVector(sourceNormals[v]).normalized;
                    uv[target] = sourceUv[v];
                    swayData[target] = new Vector2(directionOffset, 0f);
                    Color color = sourceColors[v];
                    color.a += directionOffset;
                    colors[target] = color;
                }

                for (int t = 0; t < sourceTriangles.Length; t++)
                {
                    triangles[p * sourceTriangles.Length + t] = p * sourceVertices.Length + sourceTriangles[t];
                }
            }

            var mesh = new Mesh
            {
                name = label, hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32,
                vertices = vertices,
                normals = normals,
                colors = colors,
                uv = uv,
                uv2 = swayData,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            Bounds bounds = mesh.bounds;
            bounds.Expand(4f);
            mesh.bounds = bounds;
            return mesh;
        }
    }
}
