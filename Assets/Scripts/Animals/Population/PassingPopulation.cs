using System;
using UnityEngine;

namespace DeepSky.Animals.Population
{
    /// <summary>Configures groups travelling through the player's surroundings.</summary>
    [Serializable]
    public sealed class PassingPopulation
    {
        [SerializeField] private Animal prefab = null!;
        [SerializeField, Range(0f, 1f)] private float chancePerCheck = .08f;
        [SerializeField, Min(1)] private int groupSize = 5;
        [SerializeField, Min(1)] private int maximumAlive = 10;
        [Tooltip("Living animals seeded at startup and replenished at the outer boundary.")]
        [SerializeField, Min(0)] private int minimumAlive = 0;
        [Tooltip("Independent extra X/Z distances in metres, weighted by the shared travel direction.")]
        [SerializeField, Min(0f)] private float groupSpread = 21f;
        [Tooltip("Maximum heading offset in degrees from a course toward the player from their forward sector.")]
        [SerializeField, Range(0f, 180f)] private float headingVariation = 45f;
        [SerializeField] private Vector2 clearance = new(17.5f, 32.5f);
        [SerializeField] private Vector2 scale = new(.9f, 1.1f);

        public Animal Prefab => prefab;
        public float ChancePerCheck => chancePerCheck;
        public int GroupSize => groupSize;
        public int MaximumAlive => maximumAlive;
        public int MinimumAlive => minimumAlive;
        public float GroupSpread => groupSpread;
        public float HeadingVariation => headingVariation;
        public Vector2 Clearance => clearance;
        public Vector2 Scale => scale;

        /// <summary>Rejects incomplete references and invalid spawning ranges.</summary>
        /// <exception cref="InvalidOperationException">The population cannot spawn safely.</exception>
        public void Validate()
        {
            if (!prefab || prefab is Fish.Fish || prefab.transform.parent || groupSize < 1 || maximumAlive < groupSize
                || minimumAlive < 0 || minimumAlive > maximumAlive || minimumAlive % groupSize != 0
                || chancePerCheck < 0f || chancePerCheck > 1f || groupSpread < 0f
                || headingVariation < 0f || headingVariation > 180f
                || clearance.x <= 0f || clearance.y < clearance.x || scale.x <= 0f || scale.y < scale.x)
            {
                throw new InvalidOperationException("A passing population requires a root animal prefab and valid counts, probability and ranges.");
            }
            prefab.ValidateComposition();
        }
    }
}
