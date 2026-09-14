using DeepSky.Building.Construction;
using DeepSky.Player;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace DeepSky.Building.Presentation
{
    /// <summary>Presents construction selection.</summary>
    public sealed class BuildingHUD : MonoBehaviour
    {
        [SerializeField] private ConstructionController construction = null!;
        [SerializeField] private PlayerInputContext input = null!;
        [SerializeField] private GameObject palette = null!;
        [SerializeField] private Text[] choices = System.Array.Empty<Text>();
        [SerializeField] private Text hint = null!;

        /// <summary>Validates scene references and palette size.</summary>
        private void Awake()
        {
            Assert.IsNotNull(construction, nameof(construction));
            Assert.IsNotNull(input, nameof(input));
            Assert.IsNotNull(palette, nameof(palette));
            Assert.IsNotNull(hint, nameof(hint));
            Assert.IsTrue(choices.Length == 7, "Construction palette needs seven choices.");
        }

        /// <summary>Updates the palette and concise interaction prompt.</summary>
        private void LateUpdate()
        {
            bool active = construction.Active;
            palette.SetActive(active);
            hint.text = input.InventoryOpen ? "" : active
                ? construction.Reason.Length > 0 ? construction.Reason : "Click to build     B to finish"
                : "";
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i].color = i == (int)construction.Action ? new Color(1f,.79f,.35f,1f) : new Color(.7f,.78f,.78f,1f);
            }
        }
    }
}
