using System;
using DeepSky.Player;
using DeepSky.Inventory.Dropping;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace DeepSky.Inventory.UI
{
    /// <summary>Owns inventory UI presentation and drag state; the storage model performs all item mutations.</summary>
    public sealed class InventoryView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory inventory = null!;
        [SerializeField] private PlayerInputContext input = null!;
        [SerializeField] private ItemDropper dropper = null!;
        [SerializeField] private GameObject panel = null!;
        [SerializeField] private RectTransform window = null!;
        [SerializeField] private Transform slotContainer = null!;
        [SerializeField] private InventorySlotView slotPrefab = null!;
        [SerializeField] private Image draggedIcon = null!;
        [SerializeField] private Text description = null!;
        [SerializeField] private Text capacity = null!;

        private InventorySlotView[] slots = Array.Empty<InventorySlotView>();
        private int draggedSlot = -1;

        /// <summary>Validates authored UI references and instantiates one reusable prefab per inventory slot.</summary>
        private void Awake()
        {
            Assert.IsNotNull(inventory, nameof(inventory));
            Assert.IsNotNull(input, nameof(input));
            Assert.IsNotNull(dropper, nameof(dropper));
            Assert.IsNotNull(panel, nameof(panel));
            Assert.IsNotNull(window, nameof(window));
            Assert.IsNotNull(slotContainer, nameof(slotContainer));
            Assert.IsNotNull(slotPrefab, nameof(slotPrefab));
            Assert.IsNotNull(draggedIcon, nameof(draggedIcon));
            Assert.IsNotNull(description, nameof(description));
            Assert.IsNotNull(capacity, nameof(capacity));
            slots = new InventorySlotView[inventory.Storage.Capacity];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Instantiate(slotPrefab, slotContainer);
                slots[i].Bind(this, i);
            }
            draggedIcon.raycastTarget = false;
        }

        /// <summary>Subscribes to item and input changes and immediately synchronizes the display.</summary>
        private void OnEnable()
        {
            inventory.Storage.Changed += Refresh;
            input.InventoryChanged += SetVisible;
            Refresh();
            SetVisible(input.InventoryOpen);
        }

        /// <summary>Unsubscribes and cancels drags without touching storage.</summary>
        private void OnDisable()
        {
            inventory.Storage.Changed -= Refresh;
            input.InventoryChanged -= SetVisible;
            CancelDrag();
        }

        /// <summary>Begins displaying an occupied slot under the mouse without extracting its contents.</summary>
        /// <param name="index">Slot index in the bound storage.</param>
        /// <param name="screenPosition">Mouse position in screen pixels.</param>
        public void BeginDrag(int index, Vector2 screenPosition)
        {
            ItemStack stack = inventory.Storage.Read(index);
            if (stack.IsEmpty || !input.InventoryOpen)
            {
                return;
            }
            draggedSlot = index;
            draggedIcon.sprite = stack.Item ? stack.Item.Icon : null;
            draggedIcon.gameObject.SetActive(true);
            MoveDrag(screenPosition);
        }

        /// <summary>Positions the drag image in its screen-space overlay canvas.</summary>
        /// <param name="screenPosition">Pointer position in screen pixels.</param>
        public void MoveDrag(Vector2 screenPosition)
        {
            if (draggedSlot >= 0)
            {
                draggedIcon.transform.position = screenPosition;
            }
        }

        /// <summary>Commits a drag only when a destination slot accepts it.</summary>
        /// <param name="destination">Slot index in the bound storage.</param>
        public void DropOn(int destination)
        {
            if (draggedSlot >= 0 && input.InventoryOpen)
            {
                inventory.Storage.Move(draggedSlot, destination);
            }
            CancelDrag();
            Describe(destination);
        }

        /// <summary>Hides the drag image; source items remain in storage unless a drop already succeeded.</summary>
        public void CancelDrag()
        {
            draggedSlot = -1;
            draggedIcon.gameObject.SetActive(false);
        }

        /// <summary>Drops outside the panel and cancels releases over its unused interior.</summary>
        /// <param name="screenPosition">Release position in screen pixels.</param>
        /// <param name="eventCamera">Event camera, or null for the screen-space overlay canvas.</param>
        public void EndDrag(Vector2 screenPosition, Camera? eventCamera)
        {
            if (draggedSlot >= 0 && input.InventoryOpen
                && !RectTransformUtility.RectangleContainsScreenPoint(window, screenPosition, eventCamera))
            {
                dropper.TryDrop(draggedSlot);
            }
            CancelDrag();
            description.text = "";
        }

        /// <summary>Displays the hovered item's name, quantity and description.</summary>
        /// <param name="index">Slot index in the bound storage.</param>
        public void Describe(int index)
        {
            ItemStack stack = inventory.Storage.Read(index);
            description.text = stack.Item && !stack.IsEmpty
                ? $"{stack.Item.DisplayName.ToUpperInvariant()}  x{stack.Quantity}\n{stack.Item.Description}"
                : "";
        }

        /// <summary>Updates slot snapshots and the occupied-slot count after storage changes.</summary>
        private void Refresh()
        {
            int used = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                ItemStack stack = inventory.Storage.Read(i);
                slots[i].Display(stack);
                if (!stack.IsEmpty)
                {
                    used++;
                }
            }
            capacity.text = $"{used:00}/{slots.Length:00}";
        }

        /// <summary>Synchronizes panel visibility with input mode and clears stale drag/hover presentation.</summary>
        /// <param name="visible">Whether the inventory is open.</param>
        private void SetVisible(bool visible)
        {
            CancelDrag();
            panel.SetActive(visible);
            description.text = "";
        }
    }
}
