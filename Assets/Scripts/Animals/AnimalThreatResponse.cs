using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals
{
    /// <summary>Shared, temporary escape intent consumed by each species' terrain-aware swimming logic.</summary>
    public sealed class AnimalThreatResponse : MonoBehaviour
    {
        [Header("Escape")]
        [SerializeField, Min(.1f)] private float duration = 4f;
        [SerializeField, Min(1f)] private float speedMultiplier = 2f;

        private float remaining = 0f;
        private Vector3 away = Vector3.forward;
        private AnimalLife life = null!;

        public bool IsFleeing => remaining > 0f;
        public float SpeedMultiplier => IsFleeing ? speedMultiplier : 1f;
        public Vector3 Heading => away;

        /// <summary>Caches the life component that reports successful hits.</summary>
        private void Awake()
        {
            life = GetComponent<AnimalLife>();
            Assert.IsNotNull(life, nameof(life));
        }

        /// <summary>Listens for injuries while the animal is active.</summary>
        private void OnEnable()
        {
            life.Damaged += Threaten;
        }

        /// <summary>Releases the injury subscription when unloaded.</summary>
        private void OnDisable()
        {
            life.Damaged -= Threaten;
        }

        /// <summary>Expires escape intent in simulation time.</summary>
        private void Update()
        {
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
        }

        /// <summary>Starts an escape directly away from the source of a successful hit.</summary>
        /// <param name="source">Threat position in world metres.</param>
        public void Threaten(Vector3 source)
        {
            if (life.IsDead)
            {
                return;
            }
            away = Vector3.ProjectOnPlane(transform.position - source, Vector3.up).normalized;
            if (away.sqrMagnitude < .01f)
            {
                away = Vector3.forward;
            }
            remaining = duration;
        }
    }
}
