using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace DeepSky.Inventory.UI
{
    /// <summary>Presents a fading pickup message.</summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PickupLogEntry : MonoBehaviour
    {
        [SerializeField] private Text label = null!;
        [SerializeField, Min(.1f)] private float lifetime = 2.4f;
        [SerializeField, Min(0f)] private float downwardTravel = 12f;

        private RectTransform rect = null!;
        private CanvasGroup opacity = null!;
        private Vector2 origin = Vector2.zero;
        private float elapsed = 0f;
        private bool visible = false;

        /// <summary>Validates the text and caches the UI components.</summary>
        private void Awake()
        {
            Assert.IsNotNull(label, nameof(label));
            rect = GetComponent<RectTransform>();
            opacity = GetComponent<CanvasGroup>();
            Assert.IsNotNull(rect, nameof(rect));
            Assert.IsNotNull(opacity, nameof(opacity));
            Clear();
        }

        /// <summary>Drifts the message down and fades it using unscaled time.</summary>
        private void Update()
        {
            if (!visible)
            {
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / lifetime);
            rect.anchoredPosition = origin + Vector2.down * (downwardTravel * progress);
            opacity.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
            if (progress >= 1f)
            {
                Clear();
            }
        }

        /// <summary>Starts a pickup message at the bottom of the log.</summary>
        /// <param name="item">Item whose accepted addition is being displayed.</param>
        /// <param name="quantity">Positive number added to storage.</param>
        public void Show(ItemDefinition item, int quantity)
        {
            label.text = $"Picked up {quantity}x {item.DisplayName}";
            origin = Vector2.zero;
            rect.anchoredPosition = origin;
            elapsed = 0f;
            visible = true;
            opacity.alpha = 1f;
        }

        /// <summary>Makes room beneath a visible message without restarting its fade.</summary>
        /// <param name="distance">Upward spacing in canvas units.</param>
        public void Raise(float distance)
        {
            if (visible)
            {
                origin += Vector2.up * distance;
            }
        }

        /// <summary>Hides the message and cancels its animation.</summary>
        private void Clear()
        {
            visible = false;
            opacity.alpha = 0f;
            label.text = "";
        }
    }
}
