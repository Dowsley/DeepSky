using UnityEngine;

namespace DeepSky.Inventory.Dropping
{
    /// <summary>Discards an item without creating a world object.</summary>
    [CreateAssetMenu(menuName = "DeepSky/Items/Discard Behavior")]
    public sealed class DiscardItemBehavior : ItemDropBehavior
    {
        /// <summary>Accepts permanent disposal of the supplied stack.</summary>
        /// <param name="stack">Nonempty stack to discard.</param>
        /// <param name="dropper">Requesting scene service; no world placement is required.</param>
        /// <returns>True for a nonempty stack.</returns>
        public override bool TryDrop(ItemStack stack, ItemDropper dropper)
        {
            return !stack.IsEmpty;
        }
    }
}
