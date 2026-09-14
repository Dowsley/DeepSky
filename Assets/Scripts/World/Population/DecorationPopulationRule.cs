using System;
using DeepSky.Animals;
using DeepSky.Harvesting;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Places passive scenery without gameplay initialization.</summary>
    [Serializable]
    public sealed class DecorationPopulationRule : InstancePopulationRule
    {
        [SerializeField] private GameObject prefab = null!;

        public override Transform PrefabTransform => prefab.transform;

        /// <inheritdoc />
        public override void ValidateContent()
        {
            Transform root = ValidateRoot(prefab ? prefab.transform : null);
            if (root.GetComponentInChildren<Animal>(true) || root.GetComponentInChildren<HarvestableResource>(true)
                || root.GetComponentInChildren<MineralDeposit>(true))
            {
                throw new InvalidOperationException("Gameplay content requires an animal or mineral rule.");
            }
        }

        /// <inheritdoc />
        public override void Spawn(PopulationSpawn spawn)
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab, spawn.Chunk.transform);
            spawn.Place(instance.transform, prefab.transform.localScale);
        }
    }
}
