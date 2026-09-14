using System;

namespace DeepSky.World.Population
{
    /// <summary>Places individual prefab instances.</summary>
    [Serializable]
    public abstract class InstancePopulationRule : PopulationRule
    {
        /// <summary>Creates and initializes a candidate under its inactive owning chunk.</summary>
        /// <param name="spawn">Candidate placement and borrowed generation context.</param>
        public abstract void Spawn(PopulationSpawn spawn);
    }
}
