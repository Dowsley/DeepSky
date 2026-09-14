using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace DeepSky.Equipment.UI
{
    /// <summary>Presents a toolbelt slot.</summary>
    public sealed class ToolbeltSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon = null!;
        [SerializeField] private GameObject selection = null!;

        /// <summary>Validates prefab references.</summary>
        private void Awake()
        {
            Assert.IsNotNull(icon, nameof(icon));
            Assert.IsNotNull(selection, nameof(selection));
        }

        /// <summary>Displays an item icon or an empty slot.</summary>
        /// <param name="sprite">Item artwork, or null for an empty slot.</param>
        public void SetItem(Sprite? sprite)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        /// <summary>Updates the slot's selection border.</summary>
        /// <param name="selected">Whether this slot contains the equipped tool.</param>
        public void SetSelected(bool selected)
        {
            selection.SetActive(selected);
        }
    }
}
