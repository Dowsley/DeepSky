using DeepSky.Animals.Movement;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals.Whales
{
    /// <summary>Steers a passing whale through open water.</summary>
    public sealed class WhaleMovement : SwimmingMovement
    {
        private AnimalThreatResponse threat = null!;

        protected override float ModelYawOffset => 0f;
        protected override bool MaintainCruiseClearance => true;

        /// <summary>Caches the whale's escape response.</summary>
        private void Awake()
        {
            threat = GetComponent<AnimalThreatResponse>();
            Assert.IsNotNull(threat, nameof(threat));
        }

        /// <summary>Maintains a passing course, with temporary escape after injury.</summary>
        private void Update()
        {
            Swim(threat.IsFleeing ? threat.Heading : TravelHeading, threat.SpeedMultiplier, false);
        }
    }
}
