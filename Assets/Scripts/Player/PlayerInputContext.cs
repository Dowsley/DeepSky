using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeepSky.Player
{
    /// <summary>Owns cursor capture and inventory input mode so UI clicks cannot move or fire tools.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputContext : MonoBehaviour
    {
        private int captureFrame = -1;

        public event Action<bool>? InventoryChanged;
        public bool InventoryOpen { get; private set; } = false;

        public bool GameplayActive => Application.isFocused && !InventoryOpen
                                                            && Cursor.lockState == CursorLockMode.Locked && Time.frameCount > captureFrame;

        /// <summary>Starts with the mouse released to protect other applications when entering Play.</summary>
        private void Awake()
        {
            ReleaseCursor();
        }

        /// <summary>Handles mode changes before movement and tool input run.</summary>
        private void Update()
        {
            if (!Application.isFocused)
            {
                return;
            }
            Keyboard? keyboard = Keyboard.current;
            Mouse? mouse = Mouse.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                SetInventoryOpen(!InventoryOpen);
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetInventoryOpen(false, false);
                ReleaseCursor();
                return;
            }
            if (!InventoryOpen && Cursor.lockState != CursorLockMode.Locked
                && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                CaptureCursor();
            }
        }

        /// <summary>Releases the cursor when switching applications without closing the inventory.</summary>
        /// <param name="focused">Whether the game has OS focus.</param>
        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                ReleaseCursor();
            }
        }

        /// <summary>Ensures a disabled player cannot retain the mouse.</summary>
        private void OnDisable()
        {
            ReleaseCursor();
        }

        /// <summary>Opens or closes the inventory and announces the mode to UI observers.</summary>
        /// <param name="open">True to release the mouse and suppress gameplay input.</param>
        /// <param name="captureOnClose">Whether closing should capture the cursor when focused.</param>
        public void SetInventoryOpen(bool open, bool captureOnClose = true)
        {
            InventoryOpen = open;
            if (open || !captureOnClose || !Application.isFocused)
            {
                ReleaseCursor();
            }
            else
            {
                CaptureCursor();
            }
            InventoryChanged?.Invoke(open);
        }

        /// <summary>Captures the mouse and suppresses the click that entered gameplay for this frame.</summary>
        private void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            captureFrame = Time.frameCount;
        }

        /// <summary>Makes the OS cursor available without changing gameplay state.</summary>
        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
