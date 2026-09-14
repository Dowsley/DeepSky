using System.Collections.Generic;
using DeepSky.Harvesting;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.World.Chunks
{
    /// <summary>Owns one chunk's collider, generated meshes and decoration.</summary>
    public sealed class WorldChunk : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshFilter terrainMesh = null!;
        [SerializeField] private MeshRenderer terrainRenderer = null!;
        [SerializeField] private MeshCollider terrainCollider = null!;
        [SerializeField] private MineableTerrain mining = null!;

        private readonly List<Mesh> ownedMeshes = new();
        private bool destructionRequested = false;

        public Vector2Int Coordinate { get; private set; } = Vector2Int.zero;
        public MineableTerrain Mining => mining;

        /// <summary>Releases generated meshes owned by this chunk, without destroying shared materials.</summary>
        private void OnDestroy()
        {
            ReleaseGeneratedMeshes();
        }

        /// <summary>Deactivates this chunk, releases its generated meshes and destroys its GameObject.</summary>
        /// <remarks>Safe before activation and on repeated calls. Shared assets are retained. Destruction is deferred in Play mode.</remarks>
        public void DestroyChunk()
        {
            if (destructionRequested)
            {
                return;
            }
            destructionRequested = true;
            gameObject.SetActive(false);
            ReleaseGeneratedMeshes();
            Release(gameObject);
        }

        /// <summary>Creates this chunk's terrain mesh and collider on the Unity main thread.</summary>
        /// <param name="data">Worker output with chunk-local XZ and world Y; copied into an owned mesh.</param>
        /// <param name="material">Shared terrain material; ownership remains with its authoring asset.</param>
        public void Build(ChunkMeshData data, Material material)
        {
            Assert.IsNotNull(terrainMesh, nameof(terrainMesh));
            Assert.IsNotNull(terrainRenderer, nameof(terrainRenderer));
            Assert.IsNotNull(terrainCollider, nameof(terrainCollider));
            Assert.IsNotNull(mining, nameof(mining));
            Coordinate = data.Coordinate;
            var mesh = new Mesh
            {
                name = $"Seabed {Coordinate}", hideFlags = HideFlags.DontSave,
                vertices = data.Vertices,
                normals = data.Normals,
                colors = data.Colors,
                triangles = data.Triangles
            };
            mesh.RecalculateBounds();
            ownedMeshes.Add(mesh);
            terrainMesh.sharedMesh = mesh;
            terrainRenderer.sharedMaterial = material;
            terrainCollider.sharedMesh = mesh;
            mining.Bind(mesh);
        }

        /// <summary>Attaches a static plant batch and takes ownership of its generated mesh.</summary>
        /// <param name="label">Name for the batch's child GameObject.</param>
        /// <param name="mesh">Chunk-local mesh to release when the chunk is destroyed.</param>
        /// <param name="material">Shared plant material; not owned by the chunk.</param>
        public void AddPlantBatch(string label, Mesh mesh, Material material)
        {
            ownedMeshes.Add(mesh);
            var node = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            node.transform.SetParent(transform, false);
            node.GetComponent<MeshFilter>().sharedMesh = mesh;
            node.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>Destroys an owned Unity object using the destruction mode appropriate to the editor state.</summary>
        /// <param name="value">Transient object to destroy, immediately outside Play mode or deferred during Play.</param>
        public static void Release(Object value)
        {
            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        /// <summary>Detaches and releases owned meshes, including inactive mineral surfaces, without relying on child callbacks.</summary>
        /// <remarks>Repeat-safe and valid during partial construction. Only recorded generated meshes are destroyed.</remarks>
        private void ReleaseGeneratedMeshes()
        {
            foreach (MineralDeposit deposit in GetComponentsInChildren<MineralDeposit>(true))
            {
                deposit.ReleaseGeneratedMesh();
            }
            if (terrainCollider && ownedMeshes.Contains(terrainCollider.sharedMesh))
            {
                terrainCollider.sharedMesh = null;
            }
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh? mesh = filter.sharedMesh;
                if (mesh != null && ownedMeshes.Contains(mesh))
                {
                    filter.sharedMesh = null;
                }
            }
            foreach (Mesh mesh in ownedMeshes)
            {
                if (mesh)
                {
                    Release(mesh);
                }
            }
            ownedMeshes.Clear();
        }
    }
}
