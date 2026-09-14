using System;
using System.Collections.Generic;
using DeepSky.Animals;
using DeepSky.Equipment.Presentation;
using DeepSky.Harvesting;
using DeepSky.Inventory;
using DeepSky.Player;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace DeepSky.Equipment
{
    /// <summary>Routes equipped tool input to weapon hits, drill contact and corpse collection.</summary>
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera view = null!;
        [SerializeField] private PlayerInputContext input = null!;
        [SerializeField] private PlayerInventory inventory = null!;
        [SerializeField] private Transform toolMount = null!;
        [SerializeField] private Transform effectsRoot = null!;
        [SerializeField] private ToolDefinition[] tools = Array.Empty<ToolDefinition>();
        [SerializeField] private HarvestImpact animalImpact = null!;
        [SerializeField] private HarvestImpact mineralImpact = null!;
        [SerializeField] private HarvestImpact sandImpact = null!;

        [Header("Interaction")]
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField, Min(.1f)] private float collectReach = 3f;
        [SerializeField, Min(.001f)] private float knifeRadius = .12f;
        [SerializeField, Min(.05f)] private float drillEffectInterval = .18f;

        private ToolView[] views = Array.Empty<ToolView>();
        private KnifeView knifeView = null!;
        private HarpoonView harpoonView = null!;
        private DrillView drillView = null!;
        private float cooldown = 0f;
        private float knifeDelay = -1f;
        private float effectTimer = 0f;
        private float messageTime = 0f;
        private string message = "";

        public int SelectedIndex { get; private set; } = 0;

        public IReadOnlyList<ToolDefinition> Loadout => tools;
        public string Hint { get; private set; } = "";
        public string Message => messageTime > 0f ? message : "";

        /// <summary>Validates the loadout and instantiates its reusable held-tool prefabs.</summary>
        private void Awake()
        {
            Assert.IsNotNull(view, nameof(view));
            Assert.IsNotNull(input, nameof(input));
            Assert.IsNotNull(inventory, nameof(inventory));
            Assert.IsNotNull(toolMount, nameof(toolMount));
            Assert.IsNotNull(effectsRoot, nameof(effectsRoot));
            Assert.IsNotNull(animalImpact, nameof(animalImpact));
            Assert.IsNotNull(mineralImpact, nameof(mineralImpact));
            Assert.IsNotNull(sandImpact, nameof(sandImpact));
            Assert.IsTrue(tools.Length == 3, "The field loadout requires knife, harpoon and drill in that order.");
            views = new ToolView[tools.Length];
            for (int i = 0; i < tools.Length; i++)
            {
                Assert.IsNotNull(tools[i].ViewPrefab, nameof(ToolDefinition.ViewPrefab));
                views[i] = Instantiate(tools[i].ViewPrefab, toolMount);
                views[i].gameObject.SetActive(i == SelectedIndex);
            }
            Assert.IsTrue(views[0] is KnifeView, "Knife loadout requires a KnifeView prefab.");
            Assert.IsTrue(views[1] is HarpoonView, "Harpoon loadout requires a HarpoonView prefab.");
            Assert.IsTrue(views[2] is DrillView, "Drill loadout requires a DrillView prefab.");
            knifeView = (KnifeView)views[0];
            harpoonView = (HarpoonView)views[1];
            drillView = (DrillView)views[2];
        }

        /// <summary>Suppresses tool use during UI interaction and advances cooldowns and contact queries.</summary>
        private void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime);
            effectTimer = Mathf.Max(0f, effectTimer - Time.deltaTime);
            messageTime = Mathf.Max(0f, messageTime - Time.unscaledDeltaTime);
            Hint = "";
            if (!input.GameplayActive)
            {
                knifeDelay = -1f;
                drillView.SetDrilling(false);
                return;
            }
            ReadSelection();
            ToolDefinition tool = tools[SelectedIndex];
            Ray ray = new Ray(view.transform.position, view.transform.forward);
            float reach = Mathf.Max(collectReach, tool.Reach);
            bool found = Physics.Raycast(ray, out RaycastHit hit, reach, targetLayers, QueryTriggerInteraction.Collide);
            if (found)
            {
                DescribeTarget(hit);
            }
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && found && hit.distance <= collectReach)
            {
                TryCollect(hit);
            }
            Mouse? mouse = Mouse.current;
            bool held = mouse != null && mouse.leftButton.isPressed;
            drillView.SetDrilling(tool.Kind == ToolKind.Drill && held);
            if (tool.Kind == ToolKind.Drill)
            {
                if (held && found && hit.distance <= tool.Reach)
                {
                    Drill(hit);
                }
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && cooldown <= 0f)
            {
                cooldown = tool.Cooldown;
                if (tool.Kind == ToolKind.Knife)
                {
                    knifeView.PlaySwing(tool.Cooldown);
                    knifeDelay = tool.ContactDelay;
                }
                else
                {
                    harpoonView.PlayShot(tool.Cooldown);
                    FireHarpoon(ray, tool);
                }
            }

            if (!(knifeDelay >= 0f))
            {
                return;
            }
            knifeDelay -= Time.deltaTime;
            if (!(knifeDelay <= 0f))
            {
                return;
            }
            Strike(ray, tool);
            knifeDelay = -1f;
        }

        /// <summary>Stops sustained motor audio and pending attacks when disabled.</summary>
        private void OnDisable()
        {
            knifeDelay = -1f;
            if (drillView)
            {
                drillView.SetDrilling(false);
            }
        }

        /// <summary>Changes the held tool without allowing switching to bypass attack cooldowns.</summary>
        /// <param name="index">Zero-based loadout index.</param>
        public void Equip(int index)
        {
            if (index < 0 || index >= tools.Length || SelectedIndex == index)
            {
                return;
            }
            views[SelectedIndex].gameObject.SetActive(false);
            SelectedIndex = index;
            knifeDelay = -1f;
            views[SelectedIndex].gameObject.SetActive(true);
        }

        /// <summary>Reads the three fixed loadout keys while gameplay owns input.</summary>
        private void ReadSelection()
        {
            Keyboard? keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Equip(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Equip(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                Equip(2);
            }
        }

        /// <summary>Offers a collection prompt for reachable corpses and dropped items.</summary>
        /// <param name="hit">Nearest aim-ray collision.</param>
        private void DescribeTarget(RaycastHit hit)
        {
            ICollectible collectible = hit.collider.GetComponentInParent<ICollectible>();
            if (collectible != null && collectible.CanCollect && hit.distance <= collectReach)
            {
                Hint = "Press E to collect";
            }
        }

        /// <summary>Transfers nearby loot while retaining units that do not fit.</summary>
        /// <param name="hit">Unobstructed hit within collection reach.</param>
        private void TryCollect(RaycastHit hit)
        {
            ICollectible collectible = hit.collider.GetComponentInParent<ICollectible>();
            if (collectible == null || !collectible.CanCollect)
            {
                return;
            }
            int received = collectible.Collect(inventory.Storage, int.MaxValue);
            if (received == 0)
            {
                Notify("Inventory full");
            }
        }

        /// <summary>Applies a close-range knife hit at the swing contact time, respecting terrain occlusion.</summary>
        /// <param name="ray">Player-camera aim ray at contact time.</param>
        /// <param name="tool">Knife damage and reach settings.</param>
        private void Strike(Ray ray, ToolDefinition tool)
        {
            if (!Physics.SphereCast(ray, knifeRadius, out RaycastHit hit, tool.Reach,
                    targetLayers, QueryTriggerInteraction.Collide))
            {
                return;
            }
            AnimalLife animal = hit.collider.GetComponentInParent<AnimalLife>();
            if (animal && animal.Hit(tool.Damage, ray.origin))
            {
                SpawnImpact(animalImpact, hit);
            }
        }

        /// <summary>Launches a physical harpoon along the aim ray, starting in front of the player collision volume.</summary>
        /// <param name="ray">Camera aim ray.</param>
        /// <param name="tool">Projectile, range, speed and damage settings.</param>
        private void FireHarpoon(Ray ray, ToolDefinition tool)
        {
            HarpoonProjectile? prefab = tool.ProjectilePrefab;
            if (!prefab)
            {
                return;
            }
            Vector3 origin = harpoonView.Muzzle.position;
            if (Physics.Linecast(ray.origin, origin, targetLayers, QueryTriggerInteraction.Collide))
            {
                Notify("MUZZLE BLOCKED - STEP BACK");
                return;
            }
            Vector3 aim = Physics.Raycast(ray, out RaycastHit hit, tool.Reach, targetLayers, QueryTriggerInteraction.Collide)
                ? hit.point : ray.GetPoint(tool.Reach);
            Vector3 direction = (aim - origin).normalized;
            HarpoonProjectile projectile = Instantiate(prefab, origin, Quaternion.LookRotation(direction), effectsRoot);
            projectile.Launch(direction, tool.ProjectileSpeed, tool.Reach, tool.Damage, ray.origin, targetLayers);
        }

        /// <summary>Advances surface extraction and emits contact debris at a bounded rate.</summary>
        /// <param name="hit">Nearest aim-ray hit within drill reach.</param>
        private void Drill(RaycastHit hit)
        {
            MineableTerrain terrain = hit.collider.GetComponentInParent<MineableTerrain>();
            if (!terrain)
            {
                return;
            }
            int received = terrain.Drill(hit, inventory.Storage, Time.deltaTime);
            if (effectTimer <= 0f)
            {
                SpawnImpact(terrain.IsStone(hit) ? mineralImpact : sandImpact, hit);
                effectTimer = drillEffectInterval;
            }
            if (received < 0)
            {
                Notify("Inventory full");
            }
        }

        /// <summary>Places an authored transient effect at a surface contact.</summary>
        /// <param name="prefab">Reusable effect asset.</param>
        /// <param name="hit">World contact point and normal.</param>
        private void SpawnImpact(HarvestImpact prefab, RaycastHit hit)
        {
            Instantiate(prefab, hit.point, Quaternion.LookRotation(hit.normal), effectsRoot);
        }

        /// <summary>Displays a short action result without blocking gameplay.</summary>
        /// <param name="text">Human-readable feedback.</param>
        private void Notify(string text)
        {
            message = text;
            messageTime = 2f;
        }
    }
}
