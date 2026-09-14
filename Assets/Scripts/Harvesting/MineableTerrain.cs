using System.Collections.Generic;
using DeepSky.Inventory;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Harvesting
{
    /// <summary>Resolves mineral deposits and ordinary terrain extraction.</summary>
    public sealed class MineableTerrain : MonoBehaviour
    {
        [Header("Yield")]
        [SerializeField] private ItemDefinition sand = null!;
        [SerializeField] private ItemDefinition stone = null!;
        [SerializeField, Min(.1f)] private float sandSecondsPerUnit = .8f;
        [SerializeField, Min(.1f)] private float stoneSecondsPerUnit = 1.1f;

        private readonly List<MineralDeposit> deposits = new();
        private readonly DrillContact contact = new();
        private Mesh terrainMesh = null!;
        private Color[] colors = System.Array.Empty<Color>();
        private int[] triangles = System.Array.Empty<int>();
        private ItemDefinition? contactedItem;

        /// <summary>Validates the ordinary terrain yields.</summary>
        private void Awake()
        {
            Assert.IsNotNull(sand, nameof(sand));
            Assert.IsNotNull(stone, nameof(stone));
        }

        /// <summary>Receives the collider mesh before chunk activation.</summary>
        /// <param name="mesh">Chunk-owned terrain mesh with material weights in vertex colors.</param>
        public void Bind(Mesh mesh)
        {
            terrainMesh = mesh;
            colors = mesh.colors;
            triangles = mesh.triangles;
        }

        /// <summary>Fits and registers a deposit on this terrain.</summary>
        /// <param name="deposit">Chunk-owned deposit with its item and surface settings assigned.</param>
        public void AddDeposit(MineralDeposit deposit)
        {
            deposit.Conform(terrainMesh, transform);
            deposits.Add(deposit);
        }

        /// <summary>Finds the surface resource using the collider's interpolated material weights.</summary>
        /// <param name="hit">Hit on this chunk's unmodified terrain mesh.</param>
        /// <param name="deposit">Active ore source, or null for ordinary terrain.</param>
        /// <returns>The item yielded at the contact point.</returns>
        public ItemDefinition Resolve(RaycastHit hit, out MineralDeposit? deposit)
        {
            float rock = RockWeight(hit);
            deposit = null;
            float strongest = 0f;
            foreach (MineralDeposit candidate in deposits)
            {
                if (!candidate || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }
                float coverage = candidate.Coverage(hit.point, rock);
                if (coverage > strongest)
                {
                    strongest = coverage;
                    deposit = candidate;
                }
            }
            return deposit ? deposit.Item : rock >= .5f ? stone : sand;
        }

        /// <summary>Classifies the substrate independently of any mineral deposit covering it.</summary>
        /// <param name="hit">Hit on this chunk's unmodified terrain mesh.</param>
        /// <returns>True for stone; false for sand, using the same material threshold as extraction.</returns>
        public bool IsStone(RaycastHit hit)
        {
            return RockWeight(hit) >= .5f;
        }

        /// <summary>Extracts at most one unit from an ore patch or the underlying sand or stone.</summary>
        /// <param name="hit">Terrain hit within the caller's drill reach.</param>
        /// <param name="inventory">Receiving inventory; rejected units are not consumed.</param>
        /// <param name="seconds">Nonnegative simulation seconds of drill contact.</param>
        /// <returns>One for collection, zero while drilling, or minus one for full storage.</returns>
        public int Drill(RaycastHit hit, InventoryStorage inventory, float seconds)
        {
            ItemDefinition item = Resolve(hit, out MineralDeposit? deposit);
            if (item != contactedItem || deposit)
            {
                contact.Reset();
                contactedItem = item;
            }
            if (deposit)
            {
                return deposit.Drill(inventory, seconds, hit.point);
            }
            if (!contact.Advance(hit.point, seconds, item == sand ? sandSecondsPerUnit : stoneSecondsPerUnit))
            {
                return 0;
            }
            int accepted = inventory.Add(item, 1);
            if (accepted > 0)
            {
                contact.Reset();
            }
            return accepted > 0 ? accepted : -1;
        }

        /// <summary>Interpolates the terrain's stone contribution at a collider hit.</summary>
        /// <param name="hit">Hit on this chunk's bound terrain mesh, including its triangle and barycentric coordinates.</param>
        /// <returns>Stone weight in the zero-to-one range.</returns>
        private float RockWeight(RaycastHit hit)
        {
            int index = hit.triangleIndex * 3;
            Assert.IsTrue(index >= 0 && index + 2 < triangles.Length, "Mining requires a terrain mesh hit.");
            Vector3 barycentric = hit.barycentricCoordinate;
            return colors[triangles[index]].g * barycentric.x
                + colors[triangles[index + 1]].g * barycentric.y
                + colors[triangles[index + 2]].g * barycentric.z;
        }
    }
}
