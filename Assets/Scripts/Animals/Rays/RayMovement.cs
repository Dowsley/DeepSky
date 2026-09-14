using DeepSky.Animals.Movement;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals.Rays
{
    /// <summary>Glides a ray along a passing group's travel direction.</summary>
    public sealed class RayMovement : SwimmingMovement
    {
        private const float WingWaveLength = 4.5f;
        private const float WingDisplacementAmplitude = 2f;
        private static readonly int Phase = Shader.PropertyToID("_Phase");

        [Header("Ray cruising")]
        [Tooltip("Effective cruising speed in metres per second, after the reference movement filter.")]
        [SerializeField] private Vector2 cruiseSpeedRange = new(1.2857143f, 1.5f);

        [Header("Appearance")]
        [SerializeField] private float phase = 0f;

        private AnimalThreatResponse threat = null!;

        protected override float ModelYawOffset => 0f;

        /// <summary>Caches the ray's escape response.</summary>
        private void Awake()
        {
            threat = GetComponent<AnimalThreatResponse>();
            Assert.IsNotNull(threat, nameof(threat));
            Assert.IsTrue(cruiseSpeedRange.x > 0f && cruiseSpeedRange.y >= cruiseSpeedRange.x);
        }

        /// <summary>Applies wing animation and deformation bounds.</summary>
        private void OnEnable()
        {
            ApplyPhase();
        }

        /// <summary>Follows the seabed while passing through the active population.</summary>
        private void Update()
        {
            Swim(threat.IsFleeing ? threat.Heading : TravelHeading, threat.SpeedMultiplier);
        }

        /// <summary>Initializes this ray with an independently sampled cruising speed.</summary>
        /// <param name="spawn">Terrain, heading, clearance and per-animal random seed.</param>
        public void JoinPopulation(AnimalSpawn spawn)
        {
            var random = new System.Random(spawn.Seed);
            float speed = Mathf.Lerp(cruiseSpeedRange.x, cruiseSpeedRange.y, (float)random.NextDouble());
            JoinWorld(spawn.World, spawn.Heading, spawn.Clearance, speed);
        }

        /// <summary>Sets per-renderer wing phase and expands culling bounds to contain shader-deformed wings.</summary>
        private void ApplyPhase()
        {
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(properties);
                properties.SetFloat(Phase, phase + Mathf.Repeat(transform.position.sqrMagnitude, Mathf.PI * 2f));
                renderer.SetPropertyBlock(properties);
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh)
                {
                    continue;
                }
                Bounds bounds = filter.sharedMesh.bounds;
                float extent = Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x)) / WingWaveLength;
                // These bounds must match the wing deformation in Ray.shader.
                float displacement = WingDisplacementAmplitude * extent * extent;
                bounds.Expand(new Vector3(0f, 2f * displacement, 0f));
                renderer.localBounds = bounds;
            }
        }

    }
}
