using UnityEngine;

namespace DeepSky.Inventory
{
    /// <summary>Owns the player's inventory for the current Play session.</summary>
    [DefaultExecutionOrder(-300)]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField, Min(1)] private int slotCount = 24;

        public InventoryStorage Storage { get; private set; } = null!;

        /// <summary>Creates empty storage before UI and harvesting components initialize.</summary>
        private void Awake()
        {
            Storage = new InventoryStorage(Mathf.Max(1, slotCount));
        }
    }
}
