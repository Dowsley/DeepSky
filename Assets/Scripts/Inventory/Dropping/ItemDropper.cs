using DeepSky.World;
using DeepSky.World.Generation;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Inventory.Dropping
{
    /// <summary>Coordinates inventory drops and owns their world instances.</summary>
    public sealed class ItemDropper : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory = null!;
        [SerializeField] private Camera view = null!;
        [SerializeField] private WorldManager world = null!;
        [SerializeField] private Transform playerRoot = null!;

        [Header("Placement")]
        [SerializeField, Min(.1f)] private float distance = 1.2f;
        [SerializeField, Min(.01f)] private float clearance = .2f;
        [SerializeField] private LayerMask obstructionLayers = ~0;

        private WorldData? activeWorld;

        /// <summary>Validates the scene's inventory, view and world bindings.</summary>
        private void Awake()
        {
            Assert.IsNotNull(inventory, nameof(inventory));
            Assert.IsNotNull(view, nameof(view));
            Assert.IsNotNull(world, nameof(world));
            Assert.IsNotNull(playerRoot, nameof(playerRoot));
        }

        /// <summary>Clears session drops when the procedural world is regenerated.</summary>
        private void Update()
        {
            SynchronizeWorld();
        }

        /// <summary>Dispatches a stack's drop behavior and removes it only after acceptance.</summary>
        /// <param name="slot">Source index within the player's inventory capacity.</param>
        /// <returns>True if the stack left storage, false for an empty slot or rejected drop.</returns>
        public bool TryDrop(int slot)
        {
            ItemStack stack = inventory.Storage.Read(slot);
            ItemDefinition? item = stack.Item;
            if (stack.IsEmpty || !item || !item.DropBehavior)
            {
                return false;
            }
            if (!item.DropBehavior.TryDrop(stack, this))
            {
                return false;
            }
            inventory.Storage.Remove(slot);
            return true;
        }

        /// <summary>Places one model for a complete stack without changing inventory contents.</summary>
        /// <param name="prefab">Collectible prefab with geometry contained within the placement clearance.</param>
        /// <param name="stack">Nonempty item and quantity to bind before its first update.</param>
        /// <returns>True after spawning; false when terrain or nearby collision makes placement unsafe.</returns>
        public bool TrySpawn(DroppedItem prefab, ItemStack stack)
        {
            SynchronizeWorld();
            if (stack.IsEmpty || !world.IsReady)
            {
                return false;
            }
            Ray ray = new Ray(view.transform.position, view.transform.forward);
            float travel = distance;
            foreach (RaycastHit hit in Physics.SphereCastAll(ray, clearance, distance, obstructionLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(playerRoot))
                {
                    travel = Mathf.Min(travel, hit.distance - .05f);
                }
            }
            if (travel < clearance * 2f)
            {
                return false;
            }
            Vector3 position = ray.GetPoint(travel);
            if (!world.Data.TryGetHeight(position, out float floor) || position.y < floor + clearance)
            {
                return false;
            }
            foreach (Collider obstruction in Physics.OverlapSphere(position, clearance, obstructionLayers,
                         QueryTriggerInteraction.Ignore))
            {
                if (!obstruction.transform.IsChildOf(playerRoot))
                {
                    return false;
                }
            }
            DroppedItem instance = Instantiate(prefab, position, Quaternion.Euler(0f, view.transform.eulerAngles.y, 0f), transform);
            instance.Initialize(stack, world.Data);
            return true;
        }

        /// <summary>Keeps dropped objects associated with their generating world's lifetime.</summary>
        private void SynchronizeWorld()
        {
            if (ReferenceEquals(activeWorld, world.Data))
            {
                return;
            }
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            activeWorld = world.Data;
        }
    }
}
