using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Inventory.UI
{
    /// <summary>Presents successful inventory additions.</summary>
    public sealed class PickupLogView : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory = null!;
        [SerializeField] private PickupLogEntry[] entries = Array.Empty<PickupLogEntry>();
        [SerializeField, Min(1f)] private float lineSpacing = 16f;

        private int nextEntry = 0;

        /// <summary>Validates inventory and the reusable message entries.</summary>
        private void Awake()
        {
            Assert.IsNotNull(inventory, nameof(inventory));
            Assert.IsTrue(entries.Length > 0, "Pickup log requires message entries.");
            foreach (PickupLogEntry entry in entries)
            {
                Assert.IsNotNull(entry, nameof(entries));
            }
        }

        /// <summary>Subscribes to accepted additions without observing inventory rearrangement.</summary>
        private void OnEnable()
        {
            inventory.Storage.ItemsAdded += ShowPickup;
        }

        /// <summary>Unsubscribes from storage notifications.</summary>
        private void OnDisable()
        {
            inventory.Storage.ItemsAdded -= ShowPickup;
        }

        /// <summary>Displays the accepted quantity, recycling the oldest entry when the log is full.</summary>
        /// <param name="item">Item added to storage.</param>
        /// <param name="quantity">Positive number accepted by storage.</param>
        private void ShowPickup(ItemDefinition item, int quantity)
        {
            foreach (PickupLogEntry entry in entries)
            {
                entry.Raise(lineSpacing);
            }
            entries[nextEntry].Show(item, quantity);
            nextEntry = (nextEntry + 1) % entries.Length;
        }
    }
}
