using System;
using System.Collections.Generic;
using UnityEngine;
using SchoolFish = DeepSky.Animals.Fish.Fish;

namespace DeepSky.Animals.Population
{
    /// <summary>Configures the wildlife population surrounding the player.</summary>
    [CreateAssetMenu(menuName = "DeepSky/Wildlife Profile")]
    public sealed class WildlifeProfile : ScriptableObject
    {
        [Header("Population lifetime")]
        [SerializeField, Min(1f)] private float checkInterval = 3f;
        [Tooltip("New groups enter beyond the visible water range, in horizontal metres.")]
        [SerializeField, Min(60f)] private float spawnRadius = 72f;
        [SerializeField, Min(75f)] private float despawnRadius = 96f;
        [SerializeField, Min(1f)] private float corpseLifetime = 120f;
        [Tooltip("Horizontal distance range in metres for passing animals present when the world starts.")]
        [SerializeField] private Vector2 initialPassingRadius = new(20f, 38f);

        [Header("Fish schools")]
        [SerializeField] private SchoolFish[] fish = Array.Empty<SchoolFish>();
        [SerializeField, Min(1)] private int schoolSize = 20;
        [SerializeField, Min(1)] private int maximumSchools = 12;
        [SerializeField, Min(1f)] private float schoolRadius = 10f;
        [SerializeField, Min(1f)] private float schoolSeparation = 40f;
        [SerializeField] private Vector2 fishClearance = new(3f, 5.5f);
        [SerializeField] private Vector2 fishScale = new(.9f, 1.1f);

        [Header("Passing animals")]
        [SerializeField] private PassingPopulation[] passing = Array.Empty<PassingPopulation>();

        public float CheckInterval => checkInterval;
        public float SpawnRadius => spawnRadius;
        public float DespawnRadius => despawnRadius;
        public float CorpseLifetime => corpseLifetime;
        public Vector2 InitialPassingRadius => initialPassingRadius;
        public IReadOnlyList<SchoolFish> Fish => fish;
        public int SchoolSize => schoolSize;
        public int MaximumSchools => maximumSchools;
        public float SchoolRadius => schoolRadius;
        public float SchoolSeparation => schoolSeparation;
        public Vector2 FishClearance => fishClearance;
        public Vector2 FishScale => fishScale;
        public IReadOnlyList<PassingPopulation> Passing => passing;

        /// <summary>Validates population limits and every referenced animal composition.</summary>
        /// <exception cref="InvalidOperationException">A required reference or range is invalid.</exception>
        public void Validate()
        {
            if (fish.Length == 0 || checkInterval <= 0f || spawnRadius - schoolRadius < 55f
                || despawnRadius <= spawnRadius + schoolRadius || corpseLifetime <= 0f
                || initialPassingRadius.x <= 0f || initialPassingRadius.y < initialPassingRadius.x
                || initialPassingRadius.y > spawnRadius
                || schoolSize <= 0 || maximumSchools <= 0 || schoolRadius <= 0f || schoolSeparation < schoolRadius * 2f
                || fishClearance.x <= 0f || fishClearance.y < fishClearance.x
                || fishScale.x <= 0f || fishScale.y < fishScale.x)
            {
                throw new InvalidOperationException($"{name}: invalid wildlife distances, school settings or fish references.");
            }
            foreach (SchoolFish prefab in fish)
            {
                if (!prefab || prefab.transform.parent)
                {
                    throw new InvalidOperationException($"{name}: assign a root fish prefab to each school variant.");
                }
                prefab.ValidateComposition();
            }
            foreach (PassingPopulation population in passing)
            {
                if (population == null)
                {
                    throw new InvalidOperationException($"{name}: a passing population is missing.");
                }
                population.Validate();
                float outerRadius = spawnRadius + population.GroupSpread;
                if (outerRadius >= despawnRadius)
                {
                    throw new InvalidOperationException($"{name}: passing groups must fit inside the despawn radius.");
                }
            }
        }
    }
}
