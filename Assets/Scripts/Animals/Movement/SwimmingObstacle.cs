using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.Animals.Movement
{
    /// <summary>Reserves occupied world-space volumes that swimming animals avoid.</summary>
    public sealed class SwimmingObstacle : MonoBehaviour
    {
        private static readonly HashSet<SwimmingObstacle> Active = new();
        private readonly List<Bounds> volumes = new();

        /// <summary>Registers this obstacle while its owner is active.</summary>
        private void OnEnable()
        {
            Active.Add(this);
        }

        /// <summary>Releases the reservation when its owner is disabled.</summary>
        private void OnDisable()
        {
            Active.Remove(this);
        }

        /// <summary>Copies room and structural envelopes after construction changes.</summary>
        /// <param name="occupied">World-space bounds in metres; the caller retains the collection.</param>
        public void SetVolumes(IEnumerable<Bounds> occupied)
        {
            volumes.Clear();
            volumes.AddRange(occupied);
        }

        /// <summary>Checks whether an animal center overlaps a reserved volume.</summary>
        /// <param name="position">World center in metres.</param>
        /// <param name="radius">Nonnegative body clearance in metres.</param>
        /// <returns>True when there is enough clearance.</returns>
        public static bool IsClear(Vector3 position, float radius)
        {
            foreach (SwimmingObstacle obstacle in Active)
            {
                foreach (Bounds volume in obstacle.volumes)
                {
                    Bounds expanded = volume;
                    expanded.Expand(radius * 2f);
                    if (expanded.Contains(position))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Finds the first obstacle along a swept body center.</summary>
        /// <param name="origin">World center in metres.</param>
        /// <param name="motion">World displacement, including look-ahead distance.</param>
        /// <param name="radius">Nonnegative body clearance in metres.</param>
        /// <param name="normal">Outward face normal at the first contact.</param>
        /// <returns>True if movement would enter an obstacle.</returns>
        public static bool Blocks(Vector3 origin, Vector3 motion, float radius, out Vector3 normal)
        {
            normal = Vector3.zero;
            float nearest = motion.magnitude;
            if (nearest < .0001f)
            {
                return false;
            }
            bool blocked = false;
            var ray = new Ray(origin, motion / nearest);
            foreach (SwimmingObstacle obstacle in Active)
            {
                foreach (Bounds volume in obstacle.volumes)
                {
                    Bounds expanded = volume;
                    expanded.Expand(radius * 2f);
                    if (expanded.IntersectRay(ray, out float distance) && distance <= nearest)
                    {
                        nearest = distance;
                        Vector3 point = ray.GetPoint(distance) - expanded.center;
                        Vector3 relative = new(Mathf.Abs(point.x) / expanded.extents.x,
                            Mathf.Abs(point.y) / expanded.extents.y, Mathf.Abs(point.z) / expanded.extents.z);
                        normal = relative.x >= relative.y && relative.x >= relative.z ? Vector3.right * Mathf.Sign(point.x)
                            : relative.y >= relative.z ? Vector3.up * Mathf.Sign(point.y) : Vector3.forward * Mathf.Sign(point.z);
                        blocked = true;
                    }
                }
            }
            return blocked;
        }

        /// <summary>Clears an overlap caused by placing a structure around an animal.</summary>
        /// <param name="position">Current world center in metres.</param>
        /// <param name="radius">Nonnegative body clearance in metres.</param>
        /// <param name="navigation">Body-specific terrain and water clearance checks.</param>
        /// <returns>Nearest valid face exit, or the original position if none is available.</returns>
        internal static Vector3 Resolve(Vector3 position, float radius, SwimmingNavigation navigation)
        {
            if (IsClear(position, radius))
            {
                return position;
            }
            Vector3 nearest = position;
            float distance = float.PositiveInfinity;
            Bounds combined = new(position, Vector3.zero);
            foreach (SwimmingObstacle obstacle in Active)
            {
                foreach (Bounds volume in obstacle.volumes)
                {
                    combined.Encapsulate(volume);
                    FindExit(volume, position, radius, navigation, ref nearest, ref distance);
                }
            }
            FindExit(combined, position, radius, navigation, ref nearest, ref distance);
            return nearest;
        }

        /// <summary>Considers envelope faces without placing the body inside terrain or another room.</summary>
        /// <param name="volume">Candidate exit envelope in world metres.</param>
        /// <param name="position">Overlapping body centre.</param>
        /// <param name="radius">Body clearance in metres.</param>
        /// <param name="navigation">Body-specific water clearance checks.</param>
        /// <param name="nearest">Closest valid exit found across all envelopes.</param>
        /// <param name="distance">Squared distance to the closest valid exit.</param>
        private static void FindExit(Bounds volume, Vector3 position, float radius, SwimmingNavigation navigation,
            ref Vector3 nearest, ref float distance)
        {
            volume.Expand(radius * 2f + .04f);
            for (int axis = 0; axis < 3; axis++)
            {
                for (int side = 0; side < 2; side++)
                {
                    Vector3 candidate = position;
                    candidate[axis] = side == 0 ? volume.min[axis] : volume.max[axis];
                    float separation = (candidate - position).sqrMagnitude;
                    if (separation < distance && IsClear(candidate, radius) && navigation.IsClear(candidate, candidate))
                    {
                        nearest = candidate;
                        distance = separation;
                    }
                }
            }
        }
    }
}
