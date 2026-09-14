using DeepSky.Animals;
using DeepSky.Harvesting;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Equipment
{
    /// <summary>Sweeps each harpoon flight segment to prevent tunneling through moving wildlife or terrain.</summary>
    public sealed class HarpoonProjectile : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField, Min(.001f)] private float radius = .035f;
        [SerializeField, Min(0f)] private float sinkAcceleration = .7f;
        [SerializeField, Min(.05f)] private float lodgedLifetime = .6f;
        [SerializeField] private HarvestImpact animalImpact = null!;

        private Vector3 velocity = Vector3.zero;
        private Vector3 source = Vector3.zero;
        private float remainingRange = 0f;
        private float damage = 0f;
        private LayerMask collisionMask = 0;
        private bool launched = false;

        /// <summary>Validates the authored hit effect before flight can begin.</summary>
        private void Awake()
        {
            Assert.IsNotNull(animalImpact, nameof(animalImpact));
        }

        /// <summary>Integrates flight and accepts only the first obstruction along a swept segment.</summary>
        private void Update()
        {
            if (!launched || Time.deltaTime <= 0f)
            {
                return;
            }
            Vector3 step = velocity * Time.deltaTime;
            float distance = Mathf.Min(step.magnitude, remainingRange);
            if (Physics.SphereCast(transform.position, radius, step.normalized, out RaycastHit hit,
                    distance, collisionMask, QueryTriggerInteraction.Collide))
            {
                transform.position = hit.point;
                AnimalLife animal = hit.collider.GetComponentInParent<AnimalLife>();
                if (animal && animal.Hit(damage, source))
                {
                    Instantiate(animalImpact, hit.point, Quaternion.LookRotation(hit.normal), transform.parent);
                }
                launched = false;
                Destroy(gameObject, lodgedLifetime);
                return;
            }
            transform.position += step.normalized * distance;
            remainingRange -= distance;
            velocity += Vector3.down * (sinkAcceleration * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(velocity);
            if (remainingRange <= 0f)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Initializes an instantiated harpoon before its first update.</summary>
        /// <param name="direction">Nonzero world-space travel direction.</param>
        /// <param name="speed">Positive metres per second.</param>
        /// <param name="range">Positive maximum flight distance in metres.</param>
        /// <param name="hitDamage">Positive damage on wildlife contact.</param>
        /// <param name="attacker">Attacker world position, used for escape direction.</param>
        /// <param name="mask">Layers that stop flight, excluding the player and held-tool geometry.</param>
        public void Launch(Vector3 direction, float speed, float range, float hitDamage, Vector3 attacker, LayerMask mask)
        {
            velocity = direction.normalized * speed;
            remainingRange = range;
            damage = hitDamage;
            source = attacker;
            collisionMask = mask;
            launched = true;
        }
    }
}
