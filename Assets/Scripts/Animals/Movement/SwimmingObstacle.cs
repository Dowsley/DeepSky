using System.Collections.Generic;
using UnityEngine;

namespace DeepSky.Animals.Movement
{
    /// <summary>Reserves a world-space volume that swimming animals avoid.</summary>
    public sealed class SwimmingObstacle : MonoBehaviour
    {
        private static readonly HashSet<SwimmingObstacle> Active = new();
        private Bounds bounds = new();

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

        /// <summary>Updates the occupied envelope after construction changes.</summary>
        /// <param name="volume">World-space bounds in metres.</param>
        public void SetBounds(Bounds volume)
        {
            bounds = volume;
        }

        /// <summary>Checks whether an animal center overlaps a reserved volume.</summary>
        /// <param name="position">World center in metres.</param>
        /// <param name="radius">Nonnegative body clearance in metres.</param>
        /// <returns>True when there is enough clearance.</returns>
        public static bool IsClear(Vector3 position, float radius)
        {
            foreach (SwimmingObstacle obstacle in Active)
            {
                Bounds expanded = obstacle.bounds;
                expanded.Expand(radius * 2f);
                if (expanded.Contains(position))
                {
                    return false;
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
                Bounds expanded = obstacle.bounds;
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
            return blocked;
        }

        /// <summary>Clears an overlap caused by placing a structure around an animal.</summary>
        /// <param name="position">Current world center in metres.</param>
        /// <param name="radius">Nonnegative body clearance in metres.</param>
        /// <returns>Nearest face exit for each overlapping obstacle.</returns>
        public static Vector3 Resolve(Vector3 position, float radius)
        {
            foreach (SwimmingObstacle obstacle in Active)
            {
                Bounds expanded = obstacle.bounds;
                expanded.Expand(radius * 2f + .02f);
                if (!expanded.Contains(position))
                {
                    continue;
                }
                Vector3 nearest = position;
                float distance = float.PositiveInfinity;
                for (int axis = 0; axis < 3; axis++)
                {
                    for (int side = 0; side < 2; side++)
                    {
                        float face = side == 0 ? expanded.min[axis] - .01f : expanded.max[axis] + .01f;
                        if (Mathf.Abs(position[axis] - face) < distance)
                        {
                            distance = Mathf.Abs(position[axis] - face);
                            nearest = position;
                            nearest[axis] = face;
                        }
                    }
                }
                position = nearest;
            }
            return position;
        }
    }
}
