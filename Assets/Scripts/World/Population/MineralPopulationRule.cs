using System;
using DeepSky.Harvesting;
using UnityEngine;

namespace DeepSky.World.Population
{
    /// <summary>Places surface mineral deposits.</summary>
    [Serializable]
    public sealed class MineralPopulationRule : InstancePopulationRule
    {
        [SerializeField] private MineralDeposit prefab = null!;

        public override Transform PrefabTransform => prefab.transform;
        public float BoundingRadius => prefab.BoundingRadius;

        /// <inheritdoc />
        public override void ValidateContent()
        {
            ValidateRoot(prefab).ValidateComposition();
        }

        /// <inheritdoc />
        public override void Spawn(PopulationSpawn spawn)
        {
            MineralDeposit deposit = spawn.Instantiate(prefab);
            spawn.Chunk.Mining.AddDeposit(deposit);
        }
    }
}
