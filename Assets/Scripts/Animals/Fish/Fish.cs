using System;
using UnityEngine;

namespace DeepSky.Animals.Fish
{
    /// <summary>Represents a schooling fish.</summary>
    public sealed class Fish : Animal
    {
        [SerializeField] private FishMovement movement = null!;

        private FishSchool? school;
        private AnimalThreatResponse threat = null!;

        public AnimalThreatResponse Threat => threat;

        /// <summary>Releases this fish's school alarm subscription.</summary>
        private void OnDestroy()
        {
            school?.Leave(this);
        }

        /// <inheritdoc />
        public override void ValidateComposition()
        {
            base.ValidateComposition();
            threat = GetComponent<AnimalThreatResponse>();
            if (!movement || movement.gameObject != gameObject || !threat)
            {
                throw new InvalidOperationException($"{name}: assign this fish's movement and attach its threat response.");
            }
        }

        /// <inheritdoc />
        protected override void InitializeMovement(AnimalSpawn spawn)
        {
            movement.JoinSchool(spawn);
            school = spawn.School ?? throw new InvalidOperationException("A fish requires a school.");
            school.Join(this);
        }
    }
}
