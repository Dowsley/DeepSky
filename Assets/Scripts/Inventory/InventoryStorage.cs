using System;

namespace DeepSky.Inventory
{
    /// <summary>Fixed-capacity session inventory. Only successful mutations notify observers.</summary>
    public sealed class InventoryStorage
    {
        private readonly ItemStack[] slots;

        public event Action? Changed;
        /// <summary>Reports the item and accepted quantity after a successful addition, excluding slot moves.</summary>
        public event Action<ItemDefinition, int>? ItemsAdded;
        public int Capacity => slots.Length;

        /// <summary>Creates empty storage without allocating item definitions.</summary>
        /// <param name="capacity">Positive number of slots.</param>
        public InventoryStorage(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            slots = new ItemStack[capacity];
        }

        /// <summary>Reads a slot without exposing mutable storage.</summary>
        /// <param name="index">Slot index in [0, Capacity).</param>
        /// <returns>Current item/count, or an empty snapshot.</returns>
        public ItemStack Read(int index)
        {
            return slots[index];
        }

        /// <summary>Fills matching stacks before empty slots and reports accepted additions. Overflow remains with the caller.</summary>
        /// <param name="item">Live item definition.</param>
        /// <param name="quantity">Nonnegative requested count.</param>
        /// <returns>Number actually stored, between zero and quantity inclusive.</returns>
        public int Add(ItemDefinition item, int quantity)
        {
            if (!item)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (quantity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }
            int remaining = quantity;
            for (int pass = 0; pass < 2 && remaining > 0; pass++)
            {
                for (int i = 0; i < slots.Length && remaining > 0; i++)
                {
                    ItemStack slot = slots[i];
                    bool eligible = pass == 0 ? !slot.IsEmpty && slot.Item == item : slot.IsEmpty;
                    if (!eligible)
                    {
                        continue;
                    }
                    int count = Math.Min(remaining, Math.Max(0, item.StackLimit - slot.Quantity));
                    slots[i] = new ItemStack(item, slot.Quantity + count);
                    remaining -= count;
                }
            }
            int accepted = quantity - remaining;
            if (accepted > 0)
            {
                Changed?.Invoke();
                ItemsAdded?.Invoke(item, accepted);
            }
            return accepted;
        }

        /// <summary>Removes a slot's contents after the receiving operation has accepted the stack.</summary>
        /// <param name="index">Slot index in [0, Capacity).</param>
        /// <returns>The removed stack, or an empty snapshot if the slot was empty.</returns>
        public ItemStack Remove(int index)
        {
            ItemStack stack = slots[index];
            if (!stack.IsEmpty)
            {
                slots[index] = default;
                Changed?.Invoke();
            }
            return stack;
        }

        /// <summary>Moves into an empty slot, merges compatible stacks, or swaps different items without losing excess.</summary>
        /// <param name="source">Occupied source slot index in [0, Capacity).</param>
        /// <param name="destination">Destination slot index in [0, Capacity).</param>
        /// <returns>True when contents changed; false for a self-drop, empty source or full matching destination.</returns>
        public bool Move(int source, int destination)
        {
            ItemStack from = slots[source];
            ItemStack to = slots[destination];
            if (source == destination || from.IsEmpty)
            {
                return false;
            }
            ItemDefinition? item = from.Item;
            if (!item)
            {
                return false;
            }
            if (!to.IsEmpty && to.Item == item)
            {
                int moved = Math.Min(from.Quantity, item.StackLimit - to.Quantity);
                if (moved <= 0)
                {
                    return false;
                }
                slots[destination] = new ItemStack(item, to.Quantity + moved);
                slots[source] = moved == from.Quantity ? default : new ItemStack(item, from.Quantity - moved);
            }
            else
            {
                slots[destination] = from;
                slots[source] = to;
            }
            Changed?.Invoke();
            return true;
        }
    }
}
