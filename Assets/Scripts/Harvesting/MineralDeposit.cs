using System;
using DeepSky.Inventory;
using DeepSky.World.Chunks;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Harvesting
{
    /// <summary>Represents an inexhaustible mineral patch on an existing surface.</summary>
    public sealed class MineralDeposit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ItemDefinition item = null!;
        [SerializeField] private MeshFilter surfaceMesh = null!;
        [SerializeField] private MeshRenderer surfaceRenderer = null!;

        [Header("Surface")]
        [SerializeField, Min(.1f)] private float radius = 3f;
        [Tooltip("Deposit-local XYZ radius multipliers, independent of texture repeat size.")]
        [SerializeField] private Vector3 stretch = Vector3.one;
        [SerializeField] private bool onStone = true;

        [Header("Extraction")]
        [SerializeField, Min(.1f)] private float secondsPerUnit = 1.1f;

        private readonly DrillContact contact = new();
        private Mesh? ownedMesh;

        public ItemDefinition Item => item;
        /// <summary>Conservative world-space radius about the prefab root, including the surface transform.</summary>
        public float BoundingRadius
        {
            get
            {
                Vector3 scale = surfaceMesh.transform.lossyScale;
                Vector3 extents = Vector3.Scale(Extents, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                return Mathf.Max(extents.x, extents.y, extents.z)
                    + Vector3.Distance(transform.position, surfaceMesh.transform.position);
            }
        }
        private Vector3 Extents => stretch * radius;

        /// <summary>Validates the prefab's surface and yield references.</summary>
        private void Awake()
        {
            Assert.IsNotNull(item, nameof(item));
            Assert.IsNotNull(surfaceMesh, nameof(surfaceMesh));
            Assert.IsNotNull(surfaceRenderer, nameof(surfaceRenderer));
        }

        /// <summary>Keeps the deposit's local half-extents positive.</summary>
        private void OnValidate()
        {
            radius = Mathf.Max(.1f, radius);
            stretch = Vector3.Max(Vector3.one * .1f, stretch);
        }

        /// <summary>Releases the fitted mesh without changing the shared material.</summary>
        private void OnDestroy()
        {
            ReleaseGeneratedMesh();
        }

        /// <summary>Detaches and releases this deposit's fitted mesh without destroying shared assets or the GameObject.</summary>
        /// <remarks>Safe before activation and on repeated calls. Mesh destruction is deferred in Play mode.</remarks>
        public void ReleaseGeneratedMesh()
        {
            if (ownedMesh)
            {
                if (surfaceMesh && surfaceMesh.sharedMesh == ownedMesh)
                {
                    surfaceMesh.sharedMesh = null;
                }
                WorldChunk.Release(ownedMesh);
            }
            ownedMesh = null;
        }

        /// <summary>Checks references needed to fit and mine the deposit before chunk activation.</summary>
        /// <exception cref="InvalidOperationException">The deposit's prefab composition is incomplete.</exception>
        public void ValidateComposition()
        {
            if (!item || !surfaceMesh || !surfaceRenderer || !surfaceMesh.transform.IsChildOf(transform)
                || surfaceRenderer.gameObject != surfaceMesh.gameObject || !surfaceRenderer.sharedMaterial)
            {
                throw new InvalidOperationException($"{name}: assign the mineral item, surface mesh, renderer and material.");
            }
        }

        /// <summary>Fits this patch to its chunk's triangles before activation.</summary>
        /// <param name="terrain">Chunk-owned readable mesh with rock weights in vertex green.</param>
        /// <param name="terrainTransform">World placement of the terrain mesh.</param>
        public void Conform(Mesh terrain, Transform terrainTransform)
        {
            ReleaseGeneratedMesh();
            ownedMesh = SurfaceDepositMesh.Build(terrain, terrainTransform, surfaceMesh.transform, Extents, onStone);
            surfaceMesh.sharedMesh = ownedMesh;
        }

        /// <summary>Scores a contact inside the deposit area independently of decorative texture gaps.</summary>
        /// <param name="point">Contact point in world metres.</param>
        /// <param name="rockWeight">Interpolated terrain rock fraction from zero to one.</param>
        /// <returns>Center proximity from zero to one, zero outside the deposit or on the wrong substrate.</returns>
        public float Coverage(Vector3 point, float rockWeight)
        {
            Vector3 local = surfaceMesh.transform.InverseTransformPoint(point);
            if (onStone ? rockWeight < .5f : rockWeight >= .5f)
            {
                return 0f;
            }
            Vector3 extents = Extents;
            Vector3 normalized = new(local.x / extents.x, local.y / extents.y, local.z / extents.z);
            return Mathf.Clamp01(1f - normalized.magnitude);
        }

        /// <summary>Advances contact time and yields at most one unit without depleting the patch.</summary>
        /// <param name="inventory">Receiving inventory, which can reject a unit when full.</param>
        /// <param name="seconds">Nonnegative drill contact duration in simulation seconds.</param>
        /// <param name="point">Contact position in world metres.</param>
        /// <returns>One when a unit was collected, zero while drilling, or minus one when inventory is full.</returns>
        public int Drill(InventoryStorage inventory, float seconds, Vector3 point)
        {
            if (!contact.Advance(point, seconds, secondsPerUnit))
            {
                return 0;
            }
            int collected = inventory.Add(item, 1);
            if (collected > 0)
            {
                contact.Reset();
            }
            return collected > 0 ? collected : -1;
        }
    }
}
