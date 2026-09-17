using System.Collections.Generic;
using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.Animals.Movement
{
    /// <summary>Finds bounded local water routes through terrain and occupied habitat volumes.</summary>
    internal sealed class SwimmingNavigation
    {
        private const int SearchBudget = 160;
        private static int searchFrame = -1;
        private static int searchesThisFrame = 0;
        private readonly WorldData world;
        private readonly SwimmingTerrain terrain;
        private readonly float radius;
        private readonly float clearance;
        private readonly List<Vector3> route = new();
        private readonly List<Vector3Int> open = new();
        private readonly Dictionary<Vector3Int, float> costs = new();
        private readonly Dictionary<Vector3Int, Vector3Int> parents = new();
        private readonly HashSet<Vector3Int> closed = new();
        private float retryAt = 0f;
        private float inspectAt = 0f;
        private Vector3 inspectedDirection = Vector3.zero;

        /// <summary>Creates a reusable route workspace for one swimming body.</summary>
        /// <param name="terrain">Numerical terrain shared with world generation.</param>
        /// <param name="bodyRadius">Conservative world-space body radius in metres.</param>
        /// <param name="floorClearance">Minimum centre height above terrain in metres.</param>
        internal SwimmingNavigation(WorldData terrain, float bodyRadius, float floorClearance)
        {
            world = terrain;
            this.terrain = SwimmingTerrain.For(terrain);
            radius = bodyRadius;
            clearance = Mathf.Max(floorClearance, bodyRadius);
        }

        /// <summary>Checks a swept body against habitat bounds and terrain, including unloaded chunks.</summary>
        /// <param name="start">World-space starting centre.</param>
        /// <param name="end">World-space destination centre.</param>
        /// <returns>Whether the complete segment has sufficient clearance.</returns>
        internal bool IsClear(Vector3 start, Vector3 end)
        {
            if (!SwimmingObstacle.IsClear(start, radius) || SwimmingObstacle.Blocks(start, end - start, radius, out _))
            {
                return false;
            }
            if (Mathf.Max(start.y, end.y) >= -radius)
            {
                return false;
            }
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = i == 0 ? Vector3.zero : (i <= 2 ? Vector3.right : Vector3.forward) * (i % 2 == 0 ? -radius : radius);
                if (!TerrainSegment(start + offset, end + offset))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Checks every crossed terrain cell so short and long sweeps agree at cell boundaries.</summary>
        /// <param name="start">Offset start of a body-clearance ray.</param>
        /// <param name="end">Offset end of the ray.</param>
        /// <returns>Whether the ray stays above every crossed cell's conservative height.</returns>
        private bool TerrainSegment(Vector3 start, Vector3 end)
        {
            Vector3 motion = end - start;
            float spacing = world.Spacing;
            float half = world.Size * .5f;
            int x = Mathf.FloorToInt((start.x + half) / spacing);
            int z = Mathf.FloorToInt((start.z + half) / spacing);
            int stepX = motion.x < 0f ? -1 : 1;
            int stepZ = motion.z < 0f ? -1 : 1;
            float nextX = motion.x == 0f ? float.PositiveInfinity
                : ((x + (stepX > 0 ? 1 : 0)) * spacing - half - start.x) / motion.x;
            float nextZ = motion.z == 0f ? float.PositiveInfinity
                : ((z + (stepZ > 0 ? 1 : 0)) * spacing - half - start.z) / motion.z;
            float deltaX = motion.x == 0f ? float.PositiveInfinity : spacing / Mathf.Abs(motion.x);
            float deltaZ = motion.z == 0f ? float.PositiveInfinity : spacing / Mathf.Abs(motion.z);
            float along = 0f;
            int limit = 3 + Mathf.CeilToInt(Mathf.Abs(motion.x) / spacing) + Mathf.CeilToInt(Mathf.Abs(motion.z) / spacing);
            for (int i = 0; i < limit && along < 1f; i++)
            {
                float next = Mathf.Min(1f, Mathf.Min(nextX, nextZ));
                float floor = terrain.Height(new Vector3((x + .5f) * spacing - half, 0f, (z + .5f) * spacing - half));
                float height = Mathf.Min(start.y + motion.y * along, start.y + motion.y * next);
                if (height < floor + clearance)
                {
                    return false;
                }
                along = next;
                if (nextX <= next)
                {
                    x += stepX;
                    nextX += deltaX;
                }
                if (nextZ <= next)
                {
                    z += stepZ;
                    nextZ += deltaZ;
                }
            }
            return along >= 1f && end.y >= terrain.Height(end) + clearance;
        }

        /// <summary>Follows a remembered detour or searches when the intended corridor is obstructed.</summary>
        /// <param name="position">Current world-space centre.</param>
        /// <param name="direction">Desired swimming direction.</param>
        /// <param name="lookAhead">Positive planning distance in metres.</param>
        /// <returns>Direction along a clear route, or zero while no safe route is available.</returns>
        internal Vector3 Steer(Vector3 position, Vector3 direction, float lookAhead)
        {
            while (route.Count > 0 && Vector3.Distance(position, route[0]) < .5f)
            {
                route.RemoveAt(0);
            }
            if (route.Count > 0 && !IsClear(position, route[0]))
            {
                route.Clear();
            }
            if (route.Count == 0)
            {
                Vector3 destination = position + direction.normalized * lookAhead;
                if (Time.time < inspectAt && Vector3.Dot(inspectedDirection, direction.normalized) > .95f)
                {
                    return direction.normalized;
                }
                if (IsClear(position, destination))
                {
                    inspectAt = Time.time + .25f;
                    inspectedDirection = direction.normalized;
                    return direction.normalized;
                }
                if (Time.time < retryAt)
                {
                    return Vector3.zero;
                }
                if (searchFrame != Time.frameCount)
                {
                    searchFrame = Time.frameCount;
                    searchesThisFrame = 0;
                }
                if (searchesThisFrame >= 2)
                {
                    return Vector3.zero;
                }
                searchesThisFrame++;
                retryAt = Time.time + .8f;
                float floor = terrain.Height(destination);
                if (!float.IsInfinity(floor))
                {
                    destination.y = Mathf.Min(-radius - .2f, Mathf.Max(destination.y, floor + clearance + 1f));
                }
                Search(position, destination);
            }
            for (int i = route.Count - 1; i > 0; i--)
            {
                if (IsClear(position, route[i]))
                {
                    route.RemoveRange(0, i);
                    break;
                }
            }
            return route.Count > 0 ? (route[0] - position).normalized : Vector3.zero;
        }

        /// <summary>Searches a local 3D lattice and retains the best reachable corridor within the work budget.</summary>
        /// <param name="origin">Start centre in world metres.</param>
        /// <param name="destination">Requested destination in world metres.</param>
        private void Search(Vector3 origin, Vector3 destination)
        {
            open.Clear();
            costs.Clear();
            parents.Clear();
            closed.Clear();
            float step = Mathf.Max(2f, radius * 1.5f);
            float limit = Mathf.Max(24f, Vector3.Distance(origin, destination) * 2f);
            Vector3Int best = Vector3Int.zero;
            float nearest = Vector3.Distance(origin, destination);
            Vector3Int escape = Vector3Int.zero;
            float escapeScore = float.PositiveInfinity;
            open.Add(best);
            costs[best] = 0f;
            for (int iteration = 0; iteration < SearchBudget && open.Count > 0; iteration++)
            {
                int selected = 0;
                float lowest = float.PositiveInfinity;
                for (int i = 0; i < open.Count; i++)
                {
                    float score = costs[open[i]] + Vector3.Distance(origin + (Vector3)open[i] * step, destination);
                    if (score < lowest)
                    {
                        lowest = score;
                        selected = i;
                    }
                }
                Vector3Int current = open[selected];
                open.RemoveAt(selected);
                closed.Add(current);
                Vector3 point = origin + (Vector3)current * step;
                float distance = Vector3.Distance(point, destination);
                if (current != Vector3Int.zero && costs[current] >= step * 3f && distance < escapeScore)
                {
                    escapeScore = distance;
                    escape = current;
                }
                if (distance < nearest)
                {
                    nearest = distance;
                    best = current;
                }
                if (current != Vector3Int.zero && IsClear(point, destination))
                {
                    best = current;
                    route.Add(destination);
                    break;
                }
                for (int axis = 0; axis < 3; axis++)
                {
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        Vector3Int offset = Vector3Int.zero;
                        offset[axis] = sign;
                        Vector3Int neighbour = current + offset;
                        Vector3 next = origin + (Vector3)neighbour * step;
                        float cost = costs[current] + step;
                        if (closed.Contains(neighbour) || ((Vector3)neighbour * step).sqrMagnitude > limit * limit
                            || costs.TryGetValue(neighbour, out float previous) && previous <= cost || !IsClear(point, next))
                        {
                            continue;
                        }
                        if (!costs.ContainsKey(neighbour))
                        {
                            open.Add(neighbour);
                        }
                        costs[neighbour] = cost;
                        parents[neighbour] = current;
                    }
                }
            }
            if (best == Vector3Int.zero)
            {
                best = escape;
            }
            while (best != Vector3Int.zero)
            {
                route.Insert(0, origin + (Vector3)best * step);
                best = parents[best];
            }
        }
    }
}
