using DeepSky.World.Generation;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Animals.Movement
{
    /// <summary>Moves an animal through sampled underwater terrain.</summary>
    public abstract class SwimmingMovement : MonoBehaviour
    {
        [Header("Swimming")]
        [SerializeField] private Vector3 heading = Vector3.forward;
        [SerializeField, Min(0f)] private float swimSpeed = 1.15f;
        [SerializeField, Min(.01f)] private float steeringResponseTime = .28f;
        [SerializeField, Min(0f)] private float heightCorrectionRate = 20f;
        [SerializeField, Min(0f)] private float clearance = 4f;
        [SerializeField, Range(0f, 90f)] private float maximumPitch = 25f;
        [SerializeField, Min(.1f)] private float minimumClearance = .5f;
        [SerializeField, Min(.1f)] private float lookAheadDistance = 2f;

        private WorldData world = null!;
        private Vector3 velocity = Vector3.zero;
        private Vector3 travelHeading = Vector3.forward;
        private float floorClearance = 0f;
        private float cruiseSpeed = 0f;
        private float bodyRadius = .25f;

        protected Vector3 TravelHeading => travelHeading;
        protected abstract float ModelYawOffset { get; }

        /// <summary>Validates the terrain context supplied before activation.</summary>
        protected virtual void Start()
        {
            Assert.IsNotNull(world, nameof(world));
        }

        /// <summary>Initializes swimming before the animal becomes active.</summary>
        /// <param name="terrain">Shared numerical terrain sampler, independent of loaded chunks.</param>
        /// <param name="direction">Initial world-space travel direction; zero uses the prefab heading.</param>
        /// <param name="height">Desired metres above the seabed; a negative value uses the prefab setting.</param>
        /// <param name="speed">Cruising metres per second; a negative value uses the prefab setting.</param>
        public void JoinWorld(WorldData terrain, Vector3 direction, float height = -1f, float speed = -1f)
        {
            world = terrain;
            travelHeading = Vector3.ProjectOnPlane(direction.sqrMagnitude > .001f ? direction : heading, Vector3.up).normalized;
            if (travelHeading.sqrMagnitude < .001f)
            {
                travelHeading = Vector3.forward;
            }
            floorClearance = height >= 0f ? Mathf.Max(minimumClearance, height) : clearance;
            cruiseSpeed = speed >= 0f ? speed : swimSpeed;
            foreach (Renderer visual in GetComponentsInChildren<Renderer>())
            {
                bodyRadius = Mathf.Max(bodyRadius, visual.bounds.extents.magnitude);
            }
            transform.position = SwimmingObstacle.Resolve(transform.position, bodyRadius);
            velocity = travelHeading * cruiseSpeed;
            FaceVelocity();
        }

        /// <summary>Checks sampled terrain along a direct swimming route.</summary>
        /// <param name="destination">End of the route in world metres.</param>
        /// <returns>True when the route remains above terrain and inside the world.</returns>
        protected bool CanSwimTo(Vector3 destination)
        {
            Vector3 start = transform.position;
            if (SwimmingObstacle.Blocks(start, destination - start, bodyRadius, out _))
            {
                return false;
            }
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, destination) / 1.5f));
            for (int i = 1; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(start, destination, (float)i / steps);
                if (!world.TryGetHeight(point, out float floor) || point.y < floor + minimumClearance || point.y >= 0f)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Integrates steering, terrain clearance and orientation for one frame.</summary>
        /// <param name="direction">Desired world-space direction, normalized internally.</param>
        /// <param name="speedMultiplier">Nonnegative multiplier on this animal's cruising speed.</param>
        /// <param name="followFloor">Whether to replace vertical intent with seabed-following motion.</param>
        protected void Swim(Vector3 direction, float speedMultiplier = 1f, bool followFloor = true)
        {
            float delta = Time.deltaTime;
            if (delta <= 0f)
            {
                return;
            }
            Vector3 position = SwimmingObstacle.Resolve(transform.position, bodyRadius);
            Vector3 ahead = position + Vector3.ProjectOnPlane(direction, Vector3.up).normalized * lookAheadDistance;
            if (!world.Contains(new Vector2(ahead.x, ahead.z)))
            {
                direction = new Vector3(-position.x, 0f, -position.z).normalized;
                ahead = position + direction * lookAheadDistance;
            }
            float speed = cruiseSpeed * Mathf.Max(0f, speedMultiplier);
            Vector3 desired = direction.normalized * speed;
            if (world.TryGetHeight(ahead, out float aheadFloor))
            {
                float targetHeight = followFloor ? aheadFloor + floorClearance : Mathf.Max(position.y, aheadFloor + minimumClearance);
                if (followFloor || position.y < targetHeight)
                {
                    desired.y = (targetHeight - position.y) * heightCorrectionRate;
                }
            }
            if (SwimmingObstacle.Blocks(position, desired.normalized * Mathf.Max(lookAheadDistance, speed * 2f), bodyRadius, out Vector3 normal))
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, normal);
                if (tangent.sqrMagnitude < .001f)
                {
                    tangent = travelHeading;
                }
                if (Vector3.Dot(tangent, desired) < 0f)
                {
                    tangent = -tangent;
                }
                desired = (tangent + normal * .65f).normalized * speed;
            }
            velocity = Vector3.Lerp(velocity, desired.normalized * speed, 1f - Mathf.Exp(-delta / steeringResponseTime));
            Vector3 next = position + velocity * delta;
            if (world.TryGetHeight(next, out float floor))
            {
                next.y = Mathf.Max(next.y, floor + minimumClearance);
            }
            next.y = Mathf.Min(next.y, -minimumClearance);
            if (SwimmingObstacle.Blocks(position, next - position, bodyRadius, out Vector3 contact))
            {
                velocity = Vector3.ProjectOnPlane(velocity, contact);
                next = position;
            }
            transform.position = next;
            FaceVelocity();
        }

        /// <summary>Aligns the model with velocity and limits visual pitch.</summary>
        private void FaceVelocity()
        {
            if (velocity.sqrMagnitude <= .0001f)
            {
                return;
            }
            float yaw = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg + ModelYawOffset;
            float pitch = -Mathf.Atan2(velocity.y, new Vector2(velocity.x, velocity.z).magnitude) * Mathf.Rad2Deg;
            if (Mathf.Abs(ModelYawOffset) > 90f)
            {
                pitch = -pitch;
            }
            transform.rotation = Quaternion.Euler(Mathf.Clamp(pitch, -maximumPitch, maximumPitch), yaw, 0f);
        }
    }
}
