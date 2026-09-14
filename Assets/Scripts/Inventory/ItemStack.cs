namespace DeepSky.Inventory
{
    /// <summary>Immutable slot snapshot. A default value represents an empty slot.</summary>
    public readonly struct ItemStack
    {
        public ItemDefinition? Item { get; }
        public int Quantity { get; }
        public bool IsEmpty => !Item || Quantity <= 0;

        /// <summary>Creates a nonempty snapshot; callers must respect the definition's stack limit.</summary>
        /// <param name="item">Live item asset identifying this stack.</param>
        /// <param name="quantity">Positive count not exceeding the item's stack limit.</param>
        public ItemStack(ItemDefinition item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }
}
