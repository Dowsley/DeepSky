using DeepSky.Harvesting;
using DeepSky.World.Generation;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Inventory.Dropping
{
    /// <summary>Represents a collectible stack resting or sinking toward the seabed.</summary>
    public sealed class DroppedItem : MonoBehaviour, ICollectible
    {
        [SerializeField, Min(.01f)] private float sinkingSpeed = .75f;
        [SerializeField, Min(0f)] private float groundClearance = .12f;

        private ItemDefinition item = null!;
        private WorldData world = null!;
        private int remaining = 0;

        public bool CanCollect => remaining > 0;

        /// <summary>Validates spawn-time initialization.</summary>
        private void Start()
        {
            Assert.IsNotNull(item, nameof(item));
            Assert.IsNotNull(world, nameof(world));
        }

        /// <summary>Settles without depending on streamed terrain colliders or billboard animation.</summary>
        private void Update()
        {
            Vector3 position = transform.position;
            if (world.TryGetHeight(position, out float height))
            {
                position.y = Mathf.MoveTowards(position.y, height + groundClearance, sinkingSpeed * Time.deltaTime);
                transform.position = position;
            }
        }

        /// <summary>Binds the complete dropped stack before its first frame.</summary>
        /// <param name="stack">Nonempty item snapshot, with positive quantity.</param>
        /// <param name="terrain">Session terrain sampler, valid while the owning dropper retains this object.</param>
        public void Initialize(ItemStack stack, WorldData terrain)
        {
            Assert.IsFalse(stack.IsEmpty, "A dropped item requires a nonempty stack.");
            Assert.IsNotNull(stack.Item, nameof(stack.Item));
            item = stack.Item!;
            remaining = stack.Quantity;
            world = terrain;
        }

        /// <summary>Transfers accepted units and removes the world object only when exhausted.</summary>
        /// <param name="inventory">Receiving storage, which may reject some or all units.</param>
        /// <param name="maximum">Positive maximum units to collect.</param>
        /// <returns>Number stored, with rejected units retained in this object.</returns>
        public int Collect(InventoryStorage inventory, int maximum)
        {
            int accepted = inventory.Add(item, Mathf.Min(remaining, Mathf.Max(0, maximum)));
            remaining -= accepted;
            if (remaining == 0)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            return accepted;
        }
    }
}
