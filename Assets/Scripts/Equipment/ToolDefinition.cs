using DeepSky.Equipment.Presentation;
using UnityEngine;

namespace DeepSky.Equipment
{
    public enum ToolKind
    {
        Knife,
        Harpoon,
        Drill
    }

    /// <summary>Inspector-authored tool identity, handling and reusable first-person presentation.</summary>
    [CreateAssetMenu(menuName = "DeepSky/Tool")]
    public sealed class ToolDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Tool";
        [SerializeField] private ToolKind kind = ToolKind.Knife;
        [SerializeField] private ToolView viewPrefab = null!;
        [SerializeField] private Sprite icon = null!;

        [Header("Handling")]
        [SerializeField, Min(.1f)] private float reach = 2.2f;
        [SerializeField, Min(.1f)] private float cooldown = .65f;
        [SerializeField, Min(0f)] private float damage = 30f;
        [Tooltip("Seconds from starting a knife swing to its contact query.")]
        [SerializeField, Min(0f)] private float contactDelay = .14f;

        [Header("Harpoon")]
        [SerializeField] private HarpoonProjectile? projectilePrefab;
        [SerializeField, Min(1f)] private float projectileSpeed = 24f;

        public string DisplayName => displayName;
        public ToolKind Kind => kind;
        public ToolView ViewPrefab => viewPrefab;
        public Sprite Icon => icon;
        public float Reach => reach;
        public float Cooldown => cooldown;
        public float Damage => damage;
        public float ContactDelay => Mathf.Min(contactDelay, cooldown);
        public HarpoonProjectile? ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;
    }
}
