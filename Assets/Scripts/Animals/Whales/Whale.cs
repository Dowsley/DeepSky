using System;
using UnityEngine;

namespace DeepSky.Animals.Whales
{
    /// <summary>Represents a whale.</summary>
    public sealed class Whale : Animal
    {
        [SerializeField] private WhaleMovement movement = null!;

        /// <inheritdoc />
        public override void ValidateComposition()
        {
            base.ValidateComposition();
            if (!movement || movement.gameObject != gameObject || !GetComponent<AnimalThreatResponse>())
            {
                throw new InvalidOperationException($"{name}: assign this whale's movement and attach its threat response.");
            }
        }

        /// <inheritdoc />
        protected override void InitializeMovement(AnimalSpawn spawn)
        {
            movement.JoinWorld(spawn.World, spawn.Heading, spawn.Clearance);
        }
    }
}
