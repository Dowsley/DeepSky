using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepSky.Inventory.UI
{
    /// <summary>Displays one storage slot and forwards pointer gestures to its owning inventory view.</summary>
    public sealed class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Image icon = null!;
        [SerializeField] private Text quantity = null!;
        [SerializeField] private Image border = null!;

        [Header("Colors")]
        [SerializeField] private Color idleColor = new(.23f, .38f, .39f, 1f);
        [SerializeField] private Color hoverColor = new(.85f, .71f, .39f, 1f);

        private InventoryView owner = null!;
        private int index = 0;

        /// <summary>Validates prefab bindings before the slot is used.</summary>
        private void Awake()
        {
            Assert.IsNotNull(icon, nameof(icon));
            Assert.IsNotNull(quantity, nameof(quantity));
            Assert.IsNotNull(border, nameof(border));
        }

        /// <summary>Binds this instantiated slot to its current storage location.</summary>
        /// <param name="view">Owning UI view, which outlives its slot children.</param>
        /// <param name="slotIndex">Index within the view's inventory capacity.</param>
        public void Bind(InventoryView view, int slotIndex)
        {
            owner = view;
            index = slotIndex;
        }

        /// <summary>Displays a snapshot without retaining mutable inventory contents.</summary>
        /// <param name="stack">Current slot contents, including empty snapshots.</param>
        public void Display(ItemStack stack)
        {
            icon.sprite = stack.Item ? stack.Item.Icon : null;
            icon.enabled = !stack.IsEmpty;
            quantity.text = stack.IsEmpty ? "" : stack.Quantity.ToString();
            border.color = idleColor;
        }

        /// <summary>Starts a left-button item drag without removing anything from storage.</summary>
        /// <param name="eventData">Pointer event identifying the held button and screen position.</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                owner.BeginDrag(index, eventData.position);
            }
        }

        /// <summary>Moves the UI drag image with the pointer.</summary>
        /// <param name="eventData">Current screen-space pointer position.</param>
        public void OnDrag(PointerEventData eventData)
        {
            owner.MoveDrag(eventData.position);
        }

        /// <summary>Completes releases outside a destination slot through the inventory view.</summary>
        /// <param name="eventData">Completed drag event.</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            owner.EndDrag(eventData.position, eventData.pressEventCamera);
        }

        /// <summary>Requests a move, swap or merge into this slot.</summary>
        /// <param name="eventData">Drop event from the UI event system.</param>
        public void OnDrop(PointerEventData eventData)
        {
            owner.DropOn(index);
        }

        /// <summary>Highlights this slot and displays its item description.</summary>
        /// <param name="eventData">Pointer-enter event.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            border.color = hoverColor;
            owner.Describe(index);
        }

        /// <summary>Clears this slot's pointer highlight.</summary>
        /// <param name="eventData">Pointer-exit event.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            border.color = idleColor;
        }
    }
}
