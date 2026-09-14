using UnityEngine;
using DeepSky.Inventory.Dropping;

namespace DeepSky.Inventory
{
    /// <summary>Shared item identity and presentation. Inventory stacks reference this asset, not copied definitions.</summary>
    [CreateAssetMenu(menuName = "DeepSky/Inventory Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Item";
        [SerializeField, TextArea] private string description = "";
        [SerializeField] private Sprite icon = null!;

        [Header("Storage")]
        [SerializeField, Min(1)] private int stackLimit = 20;

        [Header("Dropping")]
        [SerializeField] private ItemDropBehavior dropBehavior = null!;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int StackLimit => Mathf.Max(1, stackLimit);
        public ItemDropBehavior DropBehavior => dropBehavior;
    }
}
