using DeepSky.Animals.Fish;
using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.Animals
{
    /// <summary>Supplies an animal's runtime spawning context.</summary>
    public readonly struct AnimalSpawn
    {
        public WorldData World { get; }
        public Camera Observer { get; }
        public Vector3 Heading { get; }
        public float Clearance { get; }
        public int Seed { get; }
        public FishSchool? School { get; }

        /// <summary>Captures dependencies and movement intent before activation.</summary>
        /// <param name="world">Shared numerical terrain sampler.</param>
        /// <param name="observer">Player camera used by animal perception.</param>
        /// <param name="heading">Initial world-space travel direction.</param>
        /// <param name="clearance">Desired metres above the seabed.</param>
        /// <param name="seed">Seed for this instance's movement variation.</param>
        /// <param name="school">Shared school for fish, or null for passing animals.</param>
        public AnimalSpawn(WorldData world, Camera observer, Vector3 heading, float clearance, int seed, FishSchool? school = null)
        {
            World = world;
            Observer = observer;
            Heading = heading;
            Clearance = clearance;
            Seed = seed;
            School = school;
        }
    }
}
