using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.Harvesting
{
    /// <summary>Fits transparent deposit geometry to the existing terrain triangles.</summary>
    internal static class SurfaceDepositMesh
    {
        /// <summary>Copies intersecting triangles without changing their shape or terrain ownership.</summary>
        /// <param name="terrain">Readable source mesh with rock weights in vertex green.</param>
        /// <param name="terrainTransform">Transform placing the source mesh in the world.</param>
        /// <param name="deposit">Destination mesh transform, with yaw and uniform patch scale.</param>
        /// <param name="extents">Positive XYZ half-extents of the deposit in local metres.</param>
        /// <param name="onStone">Whether coverage belongs to stone instead of sand.</param>
        /// <returns>An owned mesh with footprint UVs, normalized projection depth in UV2.y and substrate coverage in vertex alpha.</returns>
        internal static Mesh Build(Mesh terrain, Transform terrainTransform, Transform deposit, Vector3 extents, bool onStone)
        {
            Vector3[] source = terrain.vertices;
            Vector3[] sourceNormals = terrain.normals;
            Color[] sourceColors = terrain.colors;
            int[] indices = terrain.triangles;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var projectionDepth = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            Matrix4x4 matrix = deposit.worldToLocalMatrix * terrainTransform.localToWorldMatrix;
            Matrix4x4 normalMatrix = matrix.inverse.transpose;
            for (int t = 0; t < indices.Length; t += 3)
            {
                Vector3 a = matrix.MultiplyPoint3x4(source[indices[t]]);
                Vector3 b = matrix.MultiplyPoint3x4(source[indices[t + 1]]);
                Vector3 c = matrix.MultiplyPoint3x4(source[indices[t + 2]]);
                if (Mathf.Min(a.x, b.x, c.x) > extents.x || Mathf.Max(a.x, b.x, c.x) < -extents.x
                    || Mathf.Min(a.z, b.z, c.z) > extents.z || Mathf.Max(a.z, b.z, c.z) < -extents.z)
                {
                    continue;
                }
                if (Mathf.Min(a.y, b.y, c.y) > extents.y || Mathf.Max(a.y, b.y, c.y) < -extents.y)
                {
                    continue;
                }
                for (int corner = 0; corner < 3; corner++)
                {
                    int index = indices[t + corner];
                    Vector3 point = matrix.MultiplyPoint3x4(source[index]);
                    triangles.Add(vertices.Count);
                    vertices.Add(point);
                    normals.Add(normalMatrix.MultiplyVector(sourceNormals[index]).normalized);
                    uv.Add(new Vector2(point.x / extents.x, point.z / extents.z) * .5f + Vector2.one * .5f);
                    projectionDepth.Add(new Vector2(0f, point.y / extents.y));
                    float rock = sourceColors[index].g;
                    colors.Add(new Color(1f, 1f, 1f, onStone ? rock : 1f - rock));
                }
            }
            var mesh = new Mesh { name = "Surface mineral patch", hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, projectionDepth);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
