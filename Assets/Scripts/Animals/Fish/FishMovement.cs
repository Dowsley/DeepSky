using DeepSky.Animals.Movement;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals.Fish
{
    /// <summary>Steers a fish within its school's shared home area.</summary>
    public sealed class FishMovement : SwimmingMovement
    {
        [SerializeField, Min(.1f)] private float neighbourSpacing = .65f;
        [SerializeField, Min(0f)] private float avoidanceWeight = .25f;

        private FishSchool school = null!;
        private System.Random random = null!;
        private Vector3 currentHeading = Vector3.forward;
        private Vector3 returnTarget = Vector3.zero;
        private bool returningToSchool = false;
        private AnimalThreatResponse threat = null!;

        protected override float ModelYawOffset => 180f;

        /// <summary>Caches the fish's escape response.</summary>
        private void Awake()
        {
            threat = GetComponent<AnimalThreatResponse>();
            Assert.IsNotNull(threat, nameof(threat));
        }

        /// <summary>Validates school membership and terrain initialization.</summary>
        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(school, nameof(school));
        }

        /// <summary>Combines roaming, nearby-fish avoidance and the school's escape alarm.</summary>
        private void Update()
        {
            Vector3 fromCenter = Vector3.ProjectOnPlane(transform.position - school.Center, Vector3.up);
            bool outside = fromCenter.sqrMagnitude > school.Radius * school.Radius;
            if (outside && !returningToSchool)
            {
                Vector3 offset = new Vector3((float)random.NextDouble() * 2f - 1f, 0f,
                    (float)random.NextDouble() * 2f - 1f) * school.Radius * .6f;
                returnTarget = school.Center + offset;
            }
            if (outside)
            {
                currentHeading = Vector3.ProjectOnPlane(returnTarget - transform.position, Vector3.up).normalized;
            }
            returningToSchool = outside;
            Vector3 direction = threat.IsFleeing ? threat.Heading : currentHeading;
            direction += school.Separation(transform.position, neighbourSpacing) * avoidanceWeight;
            Swim(direction, threat.SpeedMultiplier);
        }

        /// <summary>Assigns the shared school and independent initial steering before activation.</summary>
        /// <param name="spawn">World, school and randomized movement context.</param>
        public void JoinSchool(AnimalSpawn spawn)
        {
            school = spawn.School ?? throw new System.ArgumentException("A fish requires a school.", nameof(spawn));
            random = new System.Random(spawn.Seed);
            JoinWorld(spawn.World, spawn.Heading, spawn.Clearance);
            currentHeading = TravelHeading;
            returningToSchool = false;
        }
    }
}
