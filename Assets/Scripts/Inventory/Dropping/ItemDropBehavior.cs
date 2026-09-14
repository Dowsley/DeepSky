using UnityEngine;

namespace DeepSky.Inventory.Dropping
{
    /// <summary>Defines an item's response to being dropped.</summary>
    public abstract class ItemDropBehavior : ScriptableObject
    {
        /// <summary>Accepts a drop without modifying its source inventory.</summary>
        /// <param name="stack">Nonempty snapshot whose contents remain in storage until acceptance.</param>
        /// <param name="dropper">Scene service supplying placement and world ownership.</param>
        /// <returns>True if the caller should remove the stack; false to retain it unchanged.</returns>
        public abstract bool TryDrop(ItemStack stack, ItemDropper dropper);
    }
}
