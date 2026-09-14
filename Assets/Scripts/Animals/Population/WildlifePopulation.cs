using System.Collections.Generic;
using DeepSky.Animals.Fish;
using DeepSky.World;
using DeepSky.World.Generation;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals.Population
{
    /// <summary>Owns temporary wildlife around the player, independently of terrain chunks.</summary>
    public sealed class WildlifePopulation : MonoBehaviour
    {
        [SerializeField] private WorldManager world = null!;
        [SerializeField] private WildlifeProfile profile = null!;
        [SerializeField] private Camera observer = null!;

        private readonly List<FishSchool> schools = new();
        private readonly List<(Animal Animal, PassingPopulation? Population)> animals = new();
        private readonly List<Transform> passes = new();
        private WorldData? terrain;
        private Transform? root;
        private System.Random random = new();
        private float countdown = 0f;

        /// <summary>Validates the saved spawning configuration.</summary>
        private void Awake()
        {
            Assert.IsNotNull(world, nameof(world));
            Assert.IsNotNull(profile, nameof(profile));
            Assert.IsNotNull(observer, nameof(observer));
            profile.Validate();
        }

        /// <summary>Maintains transient wildlife and periodically adds schools and passing groups.</summary>
        private void Update()
        {
            if (!world.HasRuntimeWorld)
            {
                Clear();
                return;
            }
            if (!ReferenceEquals(terrain, world.Data))
            {
                Clear();
                if (!world.IsReady)
                {
                    return;
                }
                Begin();
            }
            RemoveExpired();
            if (!world.IsReady)
            {
                return;
            }
            countdown -= Time.deltaTime;
            if (countdown > 0f)
            {
                return;
            }
            countdown = profile.CheckInterval;
            PopulateRing();
            foreach (PassingPopulation population in profile.Passing)
            {
                if (CountLiving(population) < population.MinimumAlive || random.NextDouble() < population.ChancePerCheck)
                {
                    SpawnPass(population, profile.SpawnRadius);
                }
            }
        }

        /// <summary>Destroys the population when its owning component is disabled.</summary>
        private void OnDisable()
        {
            Clear();
        }

        /// <summary>Creates schools and baseline passing groups around a collision-ready player.</summary>
        private void Begin()
        {
            terrain = world.Data;
            random = new System.Random();
            root = new GameObject("Active wildlife").transform;
            root.SetParent(transform, false);
            root.gameObject.hideFlags = HideFlags.DontSave;
            countdown = profile.CheckInterval;
            Vector3 center = observer.transform.position;
            float spacing = profile.SchoolSeparation;
            for (float z = -profile.SpawnRadius; z <= profile.SpawnRadius; z += spacing)
            {
                for (float x = -profile.SpawnRadius; x <= profile.SpawnRadius; x += spacing)
                {
                    Vector3 offset = new Vector3(x, 0f, z);
                    if (offset.sqrMagnitude <= profile.SpawnRadius * profile.SpawnRadius)
                    {
                        TrySchool(center + offset);
                    }
                }
            }
            foreach (PassingPopulation population in profile.Passing)
            {
                for (int i = 0; i < population.MinimumAlive; i += population.GroupSize)
                {
                    SpawnPass(population, Sample(profile.InitialPassingRadius));
                }
            }
        }

        /// <summary>Samples fresh school centres around the outer population boundary.</summary>
        private void PopulateRing()
        {
            float phase = (float)random.NextDouble() * Mathf.PI * 2f;
            for (int i = 0; i < 32; i++)
            {
                float angle = phase + i * Mathf.PI / 16f;
                TrySchool(observer.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * profile.SpawnRadius);
            }
        }

        /// <summary>Creates a single-species school when its area is available.</summary>
        /// <param name="center">Candidate centre in world metres; Y is derived from terrain.</param>
        private void TrySchool(Vector3 center)
        {
            if (schools.Count >= profile.MaximumSchools || terrain == null)
            {
                return;
            }
            foreach (FishSchool school in schools)
            {
                if (HorizontalDistanceSquared(center, school.Center) < profile.SchoolSeparation * profile.SchoolSeparation)
                {
                    return;
                }
            }
            if (!terrain.TryGetHeight(center, out float floor))
            {
                return;
            }
            center.y = floor;
            var prefab = profile.Fish[random.Next(profile.Fish.Count)];
            Transform group = CreateGroup(prefab.name + " school");
            group.position = center;
            var created = new FishSchool(group, center, profile.SchoolRadius);
            schools.Add(created);
            for (int i = 0; i < profile.SchoolSize; i++)
            {
                Vector3 offset = RandomDirection() * Mathf.Sqrt((float)random.NextDouble()) * profile.SchoolRadius;
                Spawn(prefab, center + offset, RandomDirection(), Sample(profile.FishClearance), Sample(profile.FishScale), group, created, null);
            }
            if (group.childCount == 0)
            {
                schools.Remove(created);
                Destroy(group.gameObject);
                return;
            }
            group.gameObject.SetActive(true);
        }

        /// <summary>Counts living members of one passing population.</summary>
        /// <param name="population">Shared species and group settings.</param>
        /// <returns>The number of active living members registered to these settings.</returns>
        private int CountLiving(PassingPopulation population)
        {
            int count = 0;
            foreach (var entry in animals)
            {
                if (ReferenceEquals(entry.Population, population) && entry.Animal
                    && entry.Animal.gameObject.activeSelf && !entry.Animal.Life.IsDead)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Creates a bounded group travelling across the observer's surroundings.</summary>
        /// <param name="population">Shared species and group settings.</param>
        /// <param name="radius">Horizontal distance from the observer in metres.</param>
        private void SpawnPass(PassingPopulation population, float radius)
        {
            if (CountLiving(population) + population.GroupSize > population.MaximumAlive)
            {
                return;
            }
            float yaw = observer.transform.eulerAngles.y + Sample(new Vector2(-population.HeadingVariation, population.HeadingVariation));
            Vector3 outward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 center = observer.transform.position + outward * radius;
            Vector3 direction = -outward;
            Transform group = CreateGroup(population.Prefab.name + " pass");
            group.position = center;
            passes.Add(group);
            for (int i = 0; i < population.GroupSize; i++)
            {
                Vector3 position = center + new Vector3(outward.x * (float)random.NextDouble(), 0f,
                    outward.z * (float)random.NextDouble()) * population.GroupSpread;
                Spawn(population.Prefab, position, direction, Sample(population.Clearance), Sample(population.Scale), group, null, population);
            }
            group.gameObject.SetActive(true);
        }

        /// <summary>Initializes an animal under an inactive group before any callbacks can run.</summary>
        /// <param name="prefab">Validated root animal prefab.</param>
        /// <param name="position">Candidate XZ world position.</param>
        /// <param name="heading">Initial world-space swimming direction.</param>
        /// <param name="clearance">Desired metres above terrain.</param>
        /// <param name="scale">Positive multiplier on the prefab root scale.</param>
        /// <param name="parent">Inactive transient group.</param>
        /// <param name="school">Fish school, or null for passing animals.</param>
        /// <param name="population">Passing settings used for population accounting, or null for fish.</param>
        private void Spawn(Animal prefab, Vector3 position, Vector3 heading, float clearance, float scale,
            Transform parent, FishSchool? school, PassingPopulation? population)
        {
            if (terrain == null || !terrain.TryGetHeight(position, out float floor) || floor + clearance >= -2f)
            {
                return;
            }
            position.y = floor + clearance;
            Animal animal = Instantiate(prefab, position, Quaternion.identity, parent);
            animal.transform.localScale = prefab.transform.localScale * scale;
            animal.Initialize(new AnimalSpawn(terrain, observer, heading, clearance, random.Next(), school));
            animals.Add((animal, population));
        }

        /// <summary>Removes collected bodies, expired corpses and distant animals and group centres.</summary>
        private void RemoveExpired()
        {
            Vector3 position = observer.transform.position;
            float rangeSquared = profile.DespawnRadius * profile.DespawnRadius;
            for (int i = animals.Count - 1; i >= 0; i--)
            {
                Animal animal = animals[i].Animal;
                if (!animal || !animal.gameObject.activeSelf || animal.Life.DeadSeconds >= profile.CorpseLifetime
                    || HorizontalDistanceSquared(animal.transform.position, position) > rangeSquared)
                {
                    if (animal)
                    {
                        animal.gameObject.SetActive(false);
                        Destroy(animal.gameObject);
                    }
                    animals.RemoveAt(i);
                }
            }
            for (int i = schools.Count - 1; i >= 0; i--)
            {
                FishSchool school = schools[i];
                if (HorizontalDistanceSquared(school.Center, position) > rangeSquared && school.Root.childCount == 0)
                {
                    Destroy(school.Root.gameObject);
                    schools.RemoveAt(i);
                }
            }
            for (int i = passes.Count - 1; i >= 0; i--)
            {
                if (passes[i].childCount == 0)
                {
                    Destroy(passes[i].gameObject);
                    passes.RemoveAt(i);
                }
            }
        }

        /// <summary>Releases all transient wildlife without retaining per-animal history.</summary>
        private void Clear()
        {
            if (root)
            {
                root.gameObject.SetActive(false);
                Destroy(root.gameObject);
            }
            root = null;
            terrain = null;
            animals.Clear();
            schools.Clear();
            passes.Clear();
        }

        /// <summary>Creates an inactive group for safe prefab initialization.</summary>
        /// <param name="label">Scene hierarchy label.</param>
        /// <returns>An inactive transform owned by this population.</returns>
        private Transform CreateGroup(string label)
        {
            var group = new GameObject(label);
            group.SetActive(false);
            group.transform.SetParent(root, false);
            return group.transform;
        }

        /// <summary>Samples a horizontal unit direction without changing Unity's global random state.</summary>
        /// <returns>Random unit direction on the XZ plane.</returns>
        private Vector3 RandomDirection()
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        /// <summary>Samples an inclusive authored range.</summary>
        /// <param name="range">Ordered lower and upper bounds.</param>
        /// <returns>A value within the bounds.</returns>
        private float Sample(Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
        }

        /// <summary>Measures squared separation on the seabed plane.</summary>
        /// <param name="first">First world position.</param>
        /// <param name="second">Second world position.</param>
        /// <returns>Squared horizontal distance in square metres.</returns>
        private static float HorizontalDistanceSquared(Vector3 first, Vector3 second)
        {
            return new Vector2(first.x - second.x, first.z - second.z).sqrMagnitude;
        }
    }
}
