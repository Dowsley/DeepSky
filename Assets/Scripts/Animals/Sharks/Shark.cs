using System;
using UnityEngine;

namespace DeepSky.Animals.Sharks
{
    /// <summary>Represents a shark.</summary>
    public sealed class Shark : Animal
    {
        [SerializeField] private SharkMovement movement = null!;

        /// <inheritdoc />
        public override void ValidateComposition()
        {
            base.ValidateComposition();
            if (!movement || movement.gameObject != gameObject)
            {
                throw new InvalidOperationException($"{name}: assign this shark's movement.");
            }
        }

        /// <inheritdoc />
        protected override void InitializeMovement(AnimalSpawn spawn)
        {
            movement.JoinPopulation(spawn);
        }
    }
}
