using DeepSky.Inventory;
using DeepSky.World.Generation;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Harvesting
{
    /// <summary>Transfers finite loot into storage while retaining unaccepted units at the source.</summary>
    public sealed class HarvestableResource : MonoBehaviour, ICollectible
    {
        [Header("Yield")]
        [SerializeField] private string displayName = "Resource";
        [SerializeField] private ItemDefinition item = null!;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private bool requiresKill = false;

        public string DisplayName => displayName;
        public ItemDefinition Item => item;
        public int InitialQuantity => Mathf.Max(1, quantity);
        public ResourceState State { get; private set; } = null!;
        public WorldData World { get; private set; } = null!;
        public bool CanCollect => State.Remaining > 0 && (!requiresKill || State.IsDead);

        /// <summary>Checks that authoring and the spawning pipeline supplied required dependencies.</summary>
        private void Start()
        {
            Assert.IsNotNull(item, nameof(item));
            Assert.IsNotNull(State, nameof(State));
            Assert.IsNotNull(World, nameof(World));
        }

        /// <summary>Binds loot state and terrain before this resource becomes active.</summary>
        /// <param name="state">Loot state belonging to this resource's lifetime.</param>
        /// <param name="world">Terrain sampler usable independently of loaded colliders.</param>
        public void Bind(ResourceState state, WorldData world)
        {
            State = state;
            World = world;
            if (state.Remaining == 0)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Collects only units that fit. An exhausted source is deactivated, not regenerated.</summary>
        /// <param name="inventory">Destination storage.</param>
        /// <param name="maximum">Positive maximum number of units for this interaction.</param>
        /// <returns>Actually transferred count; zero for a full inventory or unavailable source.</returns>
        public int Collect(InventoryStorage inventory, int maximum)
        {
            if (!CanCollect || maximum <= 0)
            {
                return 0;
            }
            int accepted = inventory.Add(item, Mathf.Min(maximum, State.Remaining));
            State.Consume(accepted);
            if (State.Remaining == 0)
            {
                gameObject.SetActive(false);
            }
            return accepted;
        }
    }
}
