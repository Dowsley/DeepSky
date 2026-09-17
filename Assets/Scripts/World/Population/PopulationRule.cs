using System;
using UnityEngine;

namespace DeepSky.World.Population
{
    [Serializable]
    public abstract class PopulationRule
    {
        [Header("Identity")]
        [SerializeField] private string label = "Population";
        [Tooltip("Keep this ID unique and stable. Reordering rules will not change their placements.")]
        [SerializeField] private int seedStream = 1;

        [Header("Distribution")]
        [Tooltip("Candidates per square metre before slope, patch and clearing rejection.")]
        [SerializeField, Min(0f)] private float density = 0.01f;
        [SerializeField, Range(0f, 90f)] private float maximumSlope = 30f;
        [SerializeField] private bool followVegetationPatches = true;
        [Tooltip("Zero disables species regions. Matching nonzero IDs share a continuous spatial pattern.")]
        [SerializeField, Min(0)] private int regionStream = 0;
        [Tooltip("World metres per species-region noise interval. Match this across rules sharing a region.")]
        [SerializeField, Min(1f)] private float regionSize = 40f;
        [Tooltip("Accepted part of the shared region pattern, from zero to one. Overlapping ranges allow mixed edges.")]
        [SerializeField] private Vector2 regionRange = new Vector2(0f, 1f);
        [SerializeField] private Vector2 scaleMultiplier = Vector2.one;
        [Tooltip("Metres above the sampled seabed. Match the animal prefab's desired clearance.")]
        [SerializeField, Min(0f)] private float seabedClearance = 0f;

        [Header("Surface attachment")]
        [Tooltip("Accepted terrain rock blend: zero is sand, one is exposed rock.")]
        [SerializeField] private Vector2 rockWeightRange = new Vector2(0f, 1f);
        [SerializeField] private bool alignToSurface = false;

        public string Label => label;
        public int SeedStream => seedStream;
        public abstract Transform PrefabTransform { get; }
        public float Density => Mathf.Max(0f, density);
        public float MaximumSlope => maximumSlope;
        public bool FollowVegetationPatches => followVegetationPatches;
        public int RegionStream => regionStream;
        public float RegionSize => Mathf.Max(1f, regionSize);
        public Vector2 RegionRange => regionRange;
        public Vector2 ScaleMultiplier => scaleMultiplier;
        public float SeabedClearance => seabedClearance;
        public Vector2 RockWeightRange => rockWeightRange;
        public bool AlignToSurface => alignToSurface;

        /// <summary>Checks the rule's prefab before candidate generation.</summary>
        /// <exception cref="InvalidOperationException">The prefab reference or composition is invalid.</exception>
        public abstract void ValidateContent();

        /// <summary>Rejects missing prefab references and components attached below the prefab root.</summary>
        /// <typeparam name="T">Component expected on the prefab root.</typeparam>
        /// <param name="root">Referenced prefab component, or null when unassigned.</param>
        /// <returns>The validated root component.</returns>
        /// <exception cref="InvalidOperationException">The reference does not identify a prefab root.</exception>
        protected static T ValidateRoot<T>(T? root) where T : Component
        {
            if (!root || root.transform.parent)
            {
                throw new InvalidOperationException("Assign a prefab root, not a child object.");
            }
            return root;
        }
    }
}
