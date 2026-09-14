using DeepSky.Inventory;

namespace DeepSky.Harvesting
{
    /// <summary>Provides finite items for nearby collection.</summary>
    public interface ICollectible
    {
        public bool CanCollect { get; }

        /// <summary>Transfers only units accepted by the destination.</summary>
        /// <param name="inventory">Receiving storage.</param>
        /// <param name="maximum">Positive maximum units for this interaction.</param>
        /// <returns>Transferred count; unavailable or rejected units remain at the source.</returns>
        public int Collect(InventoryStorage inventory, int maximum);
    }
}
