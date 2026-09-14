using System;

namespace DeepSky.Harvesting
{
    /// <summary>Stores death and remaining loot for one resource's lifetime.</summary>
    public sealed class ResourceState
    {
        public int Remaining { get; private set; } = 0;
        public bool IsDead { get; private set; } = false;

        /// <summary>Creates a resource with an uncollected yield.</summary>
        /// <param name="quantity">Positive number of collectible units.</param>
        public ResourceState(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }
            Remaining = quantity;
        }

        /// <summary>Subtracts units already accepted into inventory.</summary>
        /// <param name="quantity">Count between zero and Remaining inclusive.</param>
        public void Consume(int quantity)
        {
            if (quantity < 0 || quantity > Remaining)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }
            Remaining -= quantity;
        }

        /// <summary>Makes loot that requires a kill available for collection.</summary>
        public void MarkDead()
        {
            IsDead = true;
        }
    }
}
