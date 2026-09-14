using System;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Defines a plant's batchable geometry.</summary>
    [DisallowMultipleComponent]
    public sealed class PlantBatchSource : MonoBehaviour
    {
        [SerializeField] private MeshFilter meshFilter = null!;
        [SerializeField] private MeshRenderer meshRenderer = null!;

        public Mesh Mesh => meshFilter.sharedMesh;
        public Material Material => meshRenderer.sharedMaterial;
        public Matrix4x4 LocalTransform => transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;

        /// <summary>Checks references and vertex channels required by the plant batching shader contract.</summary>
        /// <exception cref="InvalidOperationException">Geometry cannot be safely batched.</exception>
        public void ValidateComposition()
        {
            if (!meshFilter || !meshRenderer || !meshFilter.transform.IsChildOf(transform)
                || meshRenderer.gameObject != meshFilter.gameObject || !Mesh || !Material)
            {
                throw new InvalidOperationException($"{name}: assign a child mesh filter and its renderer and material.");
            }
            if (!Mesh.isReadable || Mesh.subMeshCount != 1 || meshRenderer.sharedMaterials.Length != 1)
            {
                throw new InvalidOperationException($"{name}: plant batching requires a readable mesh with one submesh and material.");
            }
            int count = Mesh.vertexCount;
            if (count == 0 || Mesh.normals.Length != count || Mesh.uv.Length != count || Mesh.colors.Length != count)
            {
                throw new InvalidOperationException($"{name}: plant batching requires normals, UVs and colors for every vertex.");
            }
        }
    }
}
