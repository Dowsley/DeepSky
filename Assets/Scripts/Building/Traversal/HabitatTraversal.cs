using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Building.Traversal
{
    /// <summary>Provides dry-room detection and ladder collision response for the diver.</summary>
    public sealed class HabitatTraversal : MonoBehaviour
    {
        private const float HorizontalRetention = .2f;
        private const float ClimbRatio = 1f / 1.5f;

        [SerializeField] private BuildingWorld buildings = null!;
        [SerializeField] private Camera view = null!;
        [SerializeField] private CharacterController body = null!;
        [SerializeField, Min(.1f)] private float climbSpeed = 2.4f;
        [SerializeField, Min(0f)] private float contactTolerance = .12f;

        private readonly RaycastHit[] contacts = new RaycastHit[16];

        public bool IsDry => buildings.AirAt(view.transform.position);

        /// <summary>Validates the player and base references.</summary>
        private void Awake()
        {
            Assert.IsNotNull(buildings, nameof(buildings));
            Assert.IsNotNull(view, nameof(view));
            Assert.IsNotNull(body, nameof(body));
        }

        /// <summary>Converts motion into a ladder into upward movement before character collision resolution.</summary>
        /// <param name="motion">World-space displacement in metres, including gravity; replaced on ladder contact.</param>
        /// <param name="deltaTime">Simulation step in seconds; nonpositive values leave motion unchanged.</param>
        /// <returns>Whether ladder contact replaced vertical motion, requiring vertical velocity to be cleared.</returns>
        public bool ResolveLadderMotion(ref Vector3 motion, float deltaTime)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(motion, Vector3.up);
            float distance = horizontal.magnitude;
            if (deltaTime <= 0f || distance <= Mathf.Epsilon)
            {
                return false;
            }

            Vector3 center = body.transform.TransformPoint(body.center);
            float halfSegment = Mathf.Max(0f, body.height * .5f - body.radius);
            // Shrink inside the controller's contact skin so the sweep starts outside touching geometry.
            float radius = Mathf.Max(.01f, body.radius - body.skinWidth);
            float reach = distance + body.skinWidth + contactTolerance;
            int count = Physics.CapsuleCastNonAlloc(center - Vector3.up * halfSegment,
                center + Vector3.up * halfSegment, radius, horizontal / distance, contacts,
                reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                if (contacts[i].collider == body)
                {
                    continue;
                }
                if (nearest < 0 || contacts[i].distance < contacts[nearest].distance)
                {
                    nearest = i;
                }
            }
            if (nearest < 0)
            {
                return false;
            }
            RaycastHit contact = contacts[nearest];
            ClimbableLadder ladder = contact.collider.GetComponentInParent<ClimbableLadder>();
            if (!ladder || contact.normal.y >= Mathf.Cos(body.slopeLimit * Mathf.Deg2Rad))
            {
                return false;
            }

            motion = horizontal * HorizontalRetention;
            motion.y = Mathf.Min(distance * ClimbRatio, climbSpeed * deltaTime);
            return true;
        }
    }
}
