using System;
using DeepSky.Harvesting;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals
{
    /// <summary>Handles wildlife health and corpse settling, separate from living locomotion and loot storage.</summary>
    public sealed class AnimalLife : MonoBehaviour
    {
        private static readonly int AnimationWeight = Shader.PropertyToID("_AnimationWeight");

        [Header("References")]
        [SerializeField] private HarvestableResource resource = null!;
        [SerializeField] private Behaviour[] livingBehaviours = Array.Empty<Behaviour>();
        [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();

        [Header("Health")]
        [SerializeField, Min(1f)] private float maximumHealth = 50f;

        [Header("Corpse")]
        [SerializeField, Min(0f)] private float sinkSpeed = .65f;
        [SerializeField, Min(0f)] private float seabedClearance = .15f;
        [SerializeField, Range(-180f, 180f)] private float restingRoll = 90f;
        [SerializeField, Min(0f)] private float rollSpeed = 55f;

        private float health = 0f;
        private float deadSeconds = 0f;
        private MaterialPropertyBlock properties = null!;

        public event Action<Vector3>? Damaged;
        public float DeadSeconds => deadSeconds;
        public bool IsDead => resource.State.IsDead;
        public float HealthFraction => IsDead ? 0f : health / maximumHealth;
        public HarvestableResource Resource => resource;

        /// <summary>Initializes health and validates prefab references before damage is possible.</summary>
        private void Awake()
        {
            Assert.IsNotNull(resource, nameof(resource));
            Assert.IsTrue(livingBehaviours.Length > 0, "An animal requires its swimming behaviour.");
            Assert.IsTrue(renderers.Length > 0, "An animal requires renderers.");
            health = maximumHealth;
            properties = new MaterialPropertyBlock();
        }

        /// <summary>Disables living behaviours when initialized with an already dead resource.</summary>
        private void Start()
        {
            if (IsDead)
            {
                StopSwimming();
            }
        }

        /// <summary>Settles a corpse toward the sampled terrain and advances its lifetime.</summary>
        private void LateUpdate()
        {
            if (!IsDead)
            {
                return;
            }
            Vector3 position = transform.position;
            deadSeconds += Time.deltaTime;
            if (resource.World.TryGetHeight(position, out float floor))
            {
                position.y = Mathf.MoveTowards(position.y, floor + seabedClearance, sinkSpeed * Time.deltaTime);
            }
            Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, restingRoll);
            transform.SetPositionAndRotation(position,
                Quaternion.RotateTowards(transform.rotation, rotation, rollSpeed * Time.deltaTime));
        }

        /// <summary>Applies weapon damage, handles death and notifies species reactions.</summary>
        /// <param name="damage">Positive health units to remove.</param>
        /// <param name="source">Attacker position in world metres.</param>
        /// <returns>True when a living animal accepted damage.</returns>
        public bool Hit(float damage, Vector3 source)
        {
            if (IsDead || damage <= 0f)
            {
                return false;
            }
            health = Mathf.Max(0f, health - damage);
            if (health <= 0f)
            {
                resource.State.MarkDead();
                StopSwimming();
            }
            Damaged?.Invoke(source);
            return true;
        }

        /// <summary>Disables living movement and shader animation without destroying the collectible body.</summary>
        private void StopSwimming()
        {
            foreach (Behaviour behaviour in livingBehaviours)
            {
                if (behaviour)
                {
                    behaviour.enabled = false;
                }
            }
            foreach (Renderer renderer in renderers)
            {
                renderer.GetPropertyBlock(properties);
                properties.SetFloat(AnimationWeight, 0f);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
