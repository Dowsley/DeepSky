using UnityEngine;

namespace DeepSky.Inventory.Dropping
{
    /// <summary>Creates a collectible world representation of an item.</summary>
    [CreateAssetMenu(menuName = "DeepSky/Items/World Drop Behavior")]
    public sealed class WorldItemBehavior : ItemDropBehavior
    {
        [SerializeField] private DroppedItem prefab = null!;

        /// <summary>Requests a collectible at the dropper's unobstructed placement point.</summary>
        /// <param name="stack">Nonempty stack to represent with one world object.</param>
        /// <param name="dropper">Scene service owning the spawned object.</param>
        /// <returns>True if the complete stack was represented; false when placement is blocked.</returns>
        public override bool TryDrop(ItemStack stack, ItemDropper dropper)
        {
            return prefab && dropper.TrySpawn(prefab, stack);
        }
    }
}
