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
        private SwimmingNavigation navigation = null!;
        private SwimmingTerrain terrain = null!;
        private Vector3 velocity = Vector3.zero;
        private Vector3 travelHeading = Vector3.forward;
        private float floorClearance = 0f;
        private float cruiseSpeed = 0f;
        private float bodyRadius = .25f;

        protected Vector3 TravelHeading => travelHeading;
        protected abstract float ModelYawOffset { get; }
        protected virtual bool MaintainCruiseClearance => false;

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
            this.terrain = SwimmingTerrain.For(terrain);
            travelHeading = Vector3.ProjectOnPlane(direction.sqrMagnitude > .001f ? direction : heading, Vector3.up).normalized;
            if (travelHeading.sqrMagnitude < .001f)
            {
                travelHeading = Vector3.forward;
            }
            floorClearance = height >= 0f ? Mathf.Max(minimumClearance, height) : clearance;
            cruiseSpeed = speed >= 0f ? speed : swimSpeed;
            foreach (Renderer visual in GetComponentsInChildren<Renderer>())
            {
                bodyRadius = Mathf.Max(bodyRadius, visual.bounds.extents.magnitude + Vector3.Distance(visual.bounds.center, transform.position));
            }
            float safeHeight = Mathf.Max(minimumClearance, bodyRadius);
            navigation = new SwimmingNavigation(world, bodyRadius, safeHeight);
            Vector3 position = transform.position;
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = i == 0 ? Vector3.zero : (i <= 2 ? Vector3.right : Vector3.forward) * (i % 2 == 0 ? -bodyRadius : bodyRadius);
                float floor = this.terrain.Height(position + offset);
                if (!float.IsInfinity(floor))
                {
                    position.y = Mathf.Max(position.y, floor + safeHeight + .2f);
                }
            }
            transform.position = position;
            transform.position = SwimmingObstacle.Resolve(transform.position, bodyRadius, navigation);
            velocity = travelHeading * cruiseSpeed;
            FaceVelocity();
        }

        /// <summary>Checks sampled terrain along a direct swimming route.</summary>
        /// <param name="destination">End of the route in world metres.</param>
        /// <returns>True when the route remains above terrain and inside the world.</returns>
        protected bool CanSwimTo(Vector3 destination)
        {
            return navigation.IsClear(transform.position, destination);
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
            Vector3 position = SwimmingObstacle.Resolve(transform.position, bodyRadius, navigation);
            float planningDistance = Mathf.Max(lookAheadDistance, bodyRadius * 2f, cruiseSpeed * speedMultiplier * 3f);
            Vector3 ahead = position + Vector3.ProjectOnPlane(direction, Vector3.up).normalized * planningDistance;
            if (!world.Contains(new Vector2(ahead.x, ahead.z)))
            {
                direction = new Vector3(-position.x, 0f, -position.z).normalized;
                ahead = position + direction * planningDistance;
            }
            float speed = cruiseSpeed * Mathf.Max(0f, speedMultiplier);
            Vector3 desired = direction.normalized * speed;
            float aheadFloor = terrain.Height(ahead);
            if (!float.IsInfinity(aheadFloor))
            {
                float safeHeight = Mathf.Max(minimumClearance, bodyRadius);
                float targetHeight = followFloor ? aheadFloor + Mathf.Max(floorClearance, safeHeight) : Mathf.Max(position.y, aheadFloor + safeHeight);
                if (MaintainCruiseClearance)
                {
                    targetHeight = Mathf.Max(terrain.Height(position) + floorClearance, aheadFloor + safeHeight);
                }
                if (followFloor || MaintainCruiseClearance || position.y < targetHeight)
                {
                    float maximumRise = speed * Mathf.Sin(maximumPitch * Mathf.Deg2Rad);
                    desired.y = Mathf.Clamp((targetHeight - position.y) * heightCorrectionRate, -maximumRise, maximumRise);
                }
            }
            desired = navigation.Steer(position, desired, planningDistance) * speed;
            velocity = Vector3.Lerp(velocity, desired.normalized * speed, 1f - Mathf.Exp(-delta / steeringResponseTime));
            Vector3 next = position + velocity * delta;
            if (!navigation.IsClear(position, next))
            {
                velocity = desired;
                next = position + velocity * delta;
                if (!navigation.IsClear(position, next))
                {
                    velocity = Vector3.zero;
                    next = position;
                }
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
