using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Shared placement rules. Prefabs retain appearance and movement settings.</summary>
    [CreateAssetMenu(menuName = "DeepSky/World Content Profile")]
    public sealed class WorldContentProfile : ScriptableObject
    {
        [SerializeReference] private PopulationRule[] populations = Array.Empty<PopulationRule>();
        [Tooltip("Clear metres between mineral areas. Half is reserved at each chunk edge; areas must fit inside their chunk.")]
        [SerializeField, Min(0f)] private float mineralSeparation = 6f;

        public IReadOnlyList<PopulationRule> Populations => populations;
        public float MineralSeparation => Mathf.Max(0f, mineralSeparation);

        /// <summary>Rejects incomplete rules and duplicate stream identities before generation starts.</summary>
        /// <exception cref="InvalidOperationException">A rule or its prefab cannot safely populate a chunk.</exception>
        public void ValidateContent()
        {
            var streams = new HashSet<int>();
            foreach (PopulationRule rule in populations)
            {
                if (rule == null)
                {
                    throw new InvalidOperationException($"{name}: choose a type for every population rule.");
                }
                if (!streams.Add(rule.SeedStream))
                {
                    throw new InvalidOperationException($"{name}: duplicate seed stream {rule.SeedStream} at {rule.Label}.");
                }
                try
                {
                    rule.ValidateContent();
                }
                catch (InvalidOperationException error)
                {
                    throw new InvalidOperationException($"{name}, {rule.Label}: {error.Message}", error);
                }
            }
        }
    }

}
