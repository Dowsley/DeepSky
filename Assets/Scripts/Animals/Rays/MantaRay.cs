using System;
using UnityEngine;

namespace DeepSky.Animals.Rays
{
    /// <summary>Represents a manta ray.</summary>
    public sealed class MantaRay : Animal
    {
        [SerializeField] private RayMovement movement = null!;

        /// <inheritdoc />
        public override void ValidateComposition()
        {
            base.ValidateComposition();
            if (!movement || movement.gameObject != gameObject || !GetComponent<AnimalThreatResponse>())
            {
                throw new InvalidOperationException($"{name}: assign this ray's movement and attach its threat response.");
            }
        }

        /// <inheritdoc />
        protected override void InitializeMovement(AnimalSpawn spawn)
        {
            movement.JoinPopulation(spawn);
        }
    }
}
