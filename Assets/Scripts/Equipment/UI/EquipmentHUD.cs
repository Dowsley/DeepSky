using DeepSky.Player;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace DeepSky.Equipment.UI
{
    /// <summary>Displays tool selection and nearby collection prompts through an authored canvas prefab.</summary>
    public sealed class EquipmentHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEquipment equipment = null!;
        [SerializeField] private PlayerInputContext input = null!;
        [SerializeField] private GameObject aiming = null!;
        [SerializeField] private ToolbeltSlotView[] slots = Array.Empty<ToolbeltSlotView>();
        [SerializeField] private Text hint = null!;
        [SerializeField] private Text message = null!;

        /// <summary>Validates scene and prefab bindings.</summary>
        private void Awake()
        {
            Assert.IsNotNull(equipment, nameof(equipment));
            Assert.IsNotNull(input, nameof(input));
            Assert.IsNotNull(aiming, nameof(aiming));
            Assert.IsTrue(slots.Length >= equipment.Loadout.Count, "The toolbelt must fit the loadout.");
            for (int i = 0; i < slots.Length; i++)
            {
                Assert.IsNotNull(slots[i], nameof(slots));
                Sprite? icon = i < equipment.Loadout.Count ? equipment.Loadout[i].Icon : null;
                if (i < equipment.Loadout.Count)
                {
                    Assert.IsNotNull(icon, nameof(ToolDefinition.Icon));
                }
                slots[i].SetItem(icon);
            }
            Assert.IsNotNull(hint, nameof(hint));
            Assert.IsNotNull(message, nameof(message));
        }

        /// <summary>Updates selection and prompts after equipment has processed the frame.</summary>
        private void LateUpdate()
        {
            aiming.SetActive(!input.InventoryOpen);
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].SetSelected(i == equipment.SelectedIndex);
            }
            hint.text = equipment.Hint;
            message.text = equipment.Message;
        }
    }
}
