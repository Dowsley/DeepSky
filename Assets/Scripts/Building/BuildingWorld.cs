using System;
using System.Collections;
using System.Collections.Generic;
using DeepSky.World;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Building
{
    /// <summary>Owns session bases independently of terrain streaming.</summary>
    public sealed class BuildingWorld : MonoBehaviour
    {
        [SerializeField] private WorldManager world = null!;
        [SerializeField] private Habitat habitatPrefab = null!;
        [SerializeField] private Transform bases = null!;
        [SerializeField] private Vector2 startingOffset = new(7f, 0f);
        [SerializeField, Min(1f)] private float foundationClearance = 3.35f;
        [SerializeField, Min(0f)] private float separation = 2f;
        [SerializeField, Min(0f)] private float maximumGroundVariation = 2f;

        private readonly List<Habitat> habitats = new();

        public IReadOnlyList<Habitat> Habitats => habitats;
        public event Action? Changed;

        /// <summary>Validates authoring references before world startup.</summary>
        private void Awake()
        {
            Assert.IsNotNull(world, nameof(world));
            Assert.IsNotNull(habitatPrefab, nameof(habitatPrefab));
            Assert.IsNotNull(bases, nameof(bases));
        }

        /// <summary>Places the starting base after the terrain snapshot becomes available.</summary>
        /// <returns>Startup coroutine; no dependency on generated chunk lifetimes.</returns>
        private IEnumerator Start()
        {
            while (!world.HasRuntimeWorld)
            {
                yield return null;
            }
            Vector2 nearSpawn = world.Data.Spawn + startingOffset;
            string reason = CheckNewBase(nearSpawn, out Vector3 anchor);
            if (reason.Length > 0)
            {
                // The cleared spawn itself provides a fallback on unusually uneven seeds.
                reason = CheckNewBase(world.Data.Spawn, out anchor);
            }
            if (reason.Length > 0)
            {
                Debug.LogError("Starting base placement failed: " + reason, this);
                yield break;
            }
            Place(anchor);
        }

        /// <summary>Releases base-event subscriptions.</summary>
        private void OnDestroy()
        {
            foreach (Habitat habitat in habitats)
            {
                if (habitat)
                {
                    habitat.Changed -= NotifyChanged;
                }
            }
        }

        /// <summary>Finds the dry base containing a world point.</summary>
        /// <param name="position">World position in metres.</param>
        /// <returns>The containing habitat, or null outside all rooms.</returns>
        public Habitat? AirAt(Vector3 position)
        {
            foreach (Habitat habitat in habitats)
            {
                if (habitat && habitat.ContainsAir(position))
                {
                    return habitat;
                }
            }
            return null;
        }

        /// <summary>Validates a starter footprint and chooses its floor height above terrain.</summary>
        /// <param name="horizontal">Requested anchor XZ in metres, snapped to the cell grid.</param>
        /// <param name="anchor">Calculated floor origin; meaningful only when valid.</param>
        /// <returns>Empty when valid, otherwise a short reason.</returns>
        public string CheckNewBase(Vector2 horizontal, out Vector3 anchor)
        {
            anchor = Vector3.zero;
            if (!world.HasRuntimeWorld)
            {
                return "Preparing seabed";
            }
            float size = habitatPrefab.Plan.CellSize;
            horizontal = new Vector2(Mathf.Round(horizontal.x / size), Mathf.Round(horizontal.y / size)) * size;
            Vector2Int footprint = habitatPrefab.Plan.Footprint;
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            for (int z = 0; z <= footprint.y; z++)
            {
                for (int x = 0; x <= footprint.x; x++)
                {
                    Vector2 point = horizontal + new Vector2(x - .5f, z - .5f) * size;
                    if (!world.Data.Contains(point))
                    {
                        return "Outside the seabed";
                    }
                    float height = world.Data.Height(point.x, point.y);
                    minimum = Mathf.Min(minimum, height);
                    maximum = Mathf.Max(maximum, height);
                }
            }
            anchor = new Vector3(horizontal.x, maximum + foundationClearance, horizontal.y);
            if (maximum - minimum > maximumGroundVariation)
            {
                return "Find flatter ground";
            }
            Bounds bounds = StarterBounds(anchor);
            return OverlapsOtherBase(bounds, null) ? "Too close to a base" : "";
        }

        /// <summary>Calculates the initial room envelope for placement and preview.</summary>
        /// <param name="anchor">World floor origin in metres.</param>
        /// <returns>World-space bounds of the starter footprint.</returns>
        public Bounds StarterBounds(Vector3 anchor)
        {
            float size = habitatPrefab.Plan.CellSize;
            float height = habitatPrefab.Plan.LevelHeight;
            Vector2Int footprint = habitatPrefab.Plan.Footprint;
            return new Bounds(anchor + new Vector3((footprint.x - 1) * size * .5f, height * .5f, (footprint.y - 1) * size * .5f),
                new Vector3(footprint.x * size, height, footprint.y * size));
        }

        /// <summary>Places a starter base without resource costs.</summary>
        /// <param name="horizontal">Requested seabed XZ position.</param>
        /// <returns>The new base, or null if placement is invalid.</returns>
        public Habitat? TryPlace(Vector2 horizontal)
        {
            return CheckNewBase(horizontal, out Vector3 anchor).Length == 0 ? Place(anchor) : null;
        }

        /// <summary>Checks an expansion's terrain clearance and separation from other bases.</summary>
        /// <param name="habitat">Base receiving the room.</param>
        /// <param name="cell">Prospective room coordinates.</param>
        /// <returns>Empty when clear, otherwise a short reason.</returns>
        public string CheckRoomSpace(Habitat habitat, Vector3Int cell)
        {
            Vector3 center = habitat.CellCenter(cell);
            float half = habitat.Plan.CellSize * .5f;
            for (int z = -1; z <= 1; z++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Vector2 point = new(center.x + x * half, center.z + z * half);
                    if (!world.Data.Contains(point) || world.Data.Height(point.x, point.y) > center.y - .25f)
                    {
                        return "Blocked by terrain";
                    }
                }
            }
            var bounds = new Bounds(center + Vector3.up * habitat.Plan.LevelHeight * .5f,
                new Vector3(half * 2f, habitat.Plan.LevelHeight, half * 2f));
            return OverlapsOtherBase(bounds, habitat) ? "Too close to a base" : "";
        }

        /// <summary>Releases a base after its final room is removed.</summary>
        /// <param name="habitat">Owned habitat with an empty layout.</param>
        public void RemoveEmpty(Habitat habitat)
        {
            if (habitat.Grid.Count != 0 || !habitats.Remove(habitat))
            {
                return;
            }
            habitat.Changed -= NotifyChanged;
            habitat.gameObject.SetActive(false);
            Destroy(habitat.gameObject);
            NotifyChanged();
        }

        /// <summary>Creates an owned base at a validated anchor.</summary>
        /// <param name="anchor">World floor origin in metres.</param>
        /// <returns>The initialized session base.</returns>
        private Habitat Place(Vector3 anchor)
        {
            Habitat habitat = Instantiate(habitatPrefab, anchor, Quaternion.identity, bases);
            habitat.name = "Habitat";
            habitat.Initialize(world.Data);
            habitats.Add(habitat);
            habitat.Changed += NotifyChanged;
            NotifyChanged();
            return habitat;
        }

        /// <summary>Checks horizontal separation without treating different floor heights as separate sites.</summary>
        /// <param name="candidate">Proposed room or starter bounds.</param>
        /// <param name="owner">Base excluded from overlap checks, or null for a new site.</param>
        /// <returns>True when another site's footprint is too close.</returns>
        private bool OverlapsOtherBase(Bounds candidate, Habitat? owner)
        {
            foreach (Habitat other in habitats)
            {
                if (!other || other == owner || other.Grid.Count == 0)
                {
                    continue;
                }
                Bounds bounds = other.Bounds;
                if (candidate.min.x < bounds.max.x + separation && candidate.max.x > bounds.min.x - separation
                    && candidate.min.z < bounds.max.z + separation && candidate.max.z > bounds.min.z - separation)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Announces layout changes to presentation observers.</summary>
        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
