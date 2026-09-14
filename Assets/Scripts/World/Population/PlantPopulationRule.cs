using System;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Places plants combined into chunk-owned batches.</summary>
    [Serializable]
    public sealed class PlantPopulationRule : PopulationRule
    {
        [SerializeField] private PlantBatchSource prefab = null!;

        public override Transform PrefabTransform => prefab.transform;
        public PlantBatchSource Source => prefab;

        /// <inheritdoc />
        public override void ValidateContent()
        {
            ValidateRoot(prefab).ValidateComposition();
        }
    }
}
