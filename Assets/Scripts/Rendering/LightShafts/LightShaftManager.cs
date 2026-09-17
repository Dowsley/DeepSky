using DeepSky.Rendering.Atmosphere;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeepSky.Rendering.LightShafts
{
    /// <summary>
    /// Spawns prefab sunbeams around the camera, controls their orientation and opacity,
    /// and repositions them while faded out. Mesh, dimensions, and material belong to the prefab.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class LightShaftManager : MonoBehaviour
    {
        private const float PositionToleranceSquared = .00000001f;
        private const float RotationToleranceDegrees = .001f;
        private const float TimeToleranceSeconds = .0001f;
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");

        [Header("References")]
        [SerializeField] private MeshRenderer shaftPrefab = null!;
        [SerializeField] private DepthAtmosphere atmosphere = null!;

        [Header("Population")]
        [Tooltip("Population settings and prefab changes apply when the component is re-enabled.")]
        [SerializeField, Min(0)] private int shaftCount = 12;
        [SerializeField] private int randomSeed = 2404;

        [Header("Placement")]
        [Tooltip("Each beam samples a persistent reference distance in metres when spawned.")]
        [SerializeField] private Vector2 distanceRange = new Vector2(40f, 50f);
        [Tooltip("Each reposition multiplies the beam's reference distance by a random value in this range.")]
        [SerializeField] private Vector2 repositionDistanceScale = new Vector2(.1f, .8f);
        [Tooltip("World-space base height. Beams follow the camera downward below this height.")]
        [SerializeField] private float shaftBaseHeight = 0f;

        [Header("Orientation")]
        [Tooltip("World X and Z tilt in degrees, applied around the camera's yaw.")]
        [SerializeField] private Vector2 tilt = new Vector2(15f, -15f);

        [Header("Visibility")]
        [SerializeField] private bool showShafts = true;
        [SerializeField, Range(0f, 1f)] private float maximumOpacity = .4f;
        [Tooltip("Opacity below which a beam is hidden and can be repositioned.")]
        [SerializeField, Range(0f, 1f)] private float visibilityThreshold = .02f;
        [Tooltip("Opacity pulse speed in radians per second.")]
        [SerializeField, Min(0f)] private float fadeSpeed = .7f;

        [Header("Daylight")]
        [SerializeField, Range(0f, 1f)] private float shaftDaylight = 1f;
        [Tooltip("Daylight levels at which beams start appearing and reach full strength.")]
        [SerializeField] private Vector2 daylightFadeRange = new Vector2(.75f, 1f);

        private Transform root = null!;
        private ShaftInstance[] instances = System.Array.Empty<ShaftInstance>();
        private System.Random random = null!;
        private MaterialPropertyBlock properties = null!;
        private Vector3 preparedPosition = Vector3.zero;
        private Quaternion preparedRotation = Quaternion.identity;
        private float preparedTime = 0f;
        private bool prepared = false;

        public bool CameraStateCurrent => prepared && root
            && (transform.position - preparedPosition).sqrMagnitude < PositionToleranceSquared
            && Quaternion.Angle(transform.rotation, preparedRotation) < RotationToleranceDegrees
            && (Time.timeScale != 0f || Mathf.Abs(Time.time - preparedTime) < TimeToleranceSeconds);

        /// <summary>Validates references and allocates beam state, including after script reloads.</summary>
        private void OnEnable()
        {
            Assert.IsNotNull(shaftPrefab, nameof(shaftPrefab));
            Assert.IsNotNull(atmosphere, nameof(atmosphere));
            Assert.IsNotNull(shaftPrefab.sharedMaterial, "The shaft prefab requires a material.");
            properties = new MaterialPropertyBlock();
            random = new System.Random(randomSeed);
            CreateShafts();
            prepared = false;
        }

        /// <summary>Updates beam presentation and records the camera pose and time used for capture readiness.</summary>
        private void LateUpdate()
        {
            UpdateShafts();
            preparedPosition = transform.position;
            preparedRotation = transform.rotation;
            preparedTime = Time.time;
            prepared = true;
        }

        /// <summary>Hides and destroys owned beam instances and invalidates capture readiness.</summary>
        private void OnDisable()
        {
            if (root)
            {
                root.gameObject.SetActive(false);
                Destroy(root.gameObject);
            }

            instances = System.Array.Empty<ShaftInstance>();
            prepared = false;
        }

        /// <summary>Constrains population, placement and visibility intervals to valid authoring ranges.</summary>
        private void OnValidate()
        {
            shaftCount = Mathf.Max(0, shaftCount);
            distanceRange.x = Mathf.Max(0f, distanceRange.x);
            distanceRange.y = Mathf.Max(distanceRange.x, distanceRange.y);
            repositionDistanceScale.x = Mathf.Max(0f, repositionDistanceScale.x);
            repositionDistanceScale.y = Mathf.Max(repositionDistanceScale.x, repositionDistanceScale.y);
            daylightFadeRange.x = Mathf.Clamp01(daylightFadeRange.x);
            daylightFadeRange.y = Mathf.Clamp(daylightFadeRange.y, daylightFadeRange.x, 1f);
            visibilityThreshold = Mathf.Clamp(visibilityThreshold, 0f, maximumOpacity);
        }

        /// <summary>Instantiates prefab beams beneath an owned root and assigns persistent randomized placement parameters.</summary>
        private void CreateShafts()
        {
            root = new GameObject("Light shafts").transform;
            root.SetParent(transform, false);
            instances = new ShaftInstance[shaftCount];
            for (int i = 0; i < instances.Length; i++)
            {
                MeshRenderer renderer = Instantiate(shaftPrefab, root, false);
                var shaft = new ShaftInstance(renderer, Range(0f, Mathf.PI * 2f),
                    Range(0f, Mathf.PI * 2f), Range(distanceRange.x, distanceRange.y));
                instances[i] = shaft;
                PlaceShaft(shaft);
            }
        }

        /// <summary>Applies camera-relative beam orientation, pulsing opacity and depth/daylight attenuation.</summary>
        private void UpdateShafts()
        {
            Quaternion rotation = GetShaftRotation(transform.eulerAngles.y);
            float daylight = Mathf.InverseLerp(daylightFadeRange.x, daylightFadeRange.y, shaftDaylight * atmosphere.Daylight);
            float depthLimit = atmosphere.ShaftOpacityLimit(transform.position.y);
            foreach (ShaftInstance shaft in instances)
            {
                float alpha = (1f + Mathf.Cos(Time.time * fadeSpeed + shaft.Phase)) * (.5f * maximumOpacity);
                alpha = Mathf.Min(alpha, depthLimit);
                if (alpha < visibilityThreshold)
                {
                    PlaceShaft(shaft);
                }

                Transform beam = shaft.Renderer.transform;
                float baseOffset = -shaft.Renderer.localBounds.min.y * beam.localScale.y;
                beam.SetPositionAndRotation(shaft.BasePosition + rotation * Vector3.up * baseOffset, rotation);
                shaft.Renderer.enabled = showShafts && alpha > visibilityThreshold;
                properties.SetFloat(Opacity, alpha * daylight);
                shaft.Renderer.SetPropertyBlock(properties);
            }
        }

        /// <summary>Draws a bounded value from this population's seeded random sequence.</summary>
        /// <param name="minimum">Lower interpolation endpoint.</param>
        /// <param name="maximum">Upper interpolation endpoint.</param>
        /// <returns>A value between the endpoints; float rounding can include the upper endpoint.</returns>
        private float Range(float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }

        /// <summary>Combines the authored world-axis tilts with the observer's yaw.</summary>
        /// <param name="cameraYaw">Camera heading in degrees around the world Y axis.</param>
        /// <returns>The world-space rotation applied to every beam.</returns>
        private Quaternion GetShaftRotation(float cameraYaw)
        {
            return Quaternion.AngleAxis(tilt.x, Vector3.right) * Quaternion.AngleAxis(tilt.y, Vector3.forward)
                * Quaternion.AngleAxis(cameraYaw, Vector3.up);
        }

        /// <summary>Samples a radial placement around the camera while keeping the base at or below its height.</summary>
        /// <param name="shaft">Owned beam instance whose world-space base position is updated.</param>
        private void PlaceShaft(ShaftInstance shaft)
        {
            var direction = new Vector3(Mathf.Cos(shaft.Angle), 0f, Mathf.Sin(shaft.Angle));
            shaft.BasePosition = transform.position
                + direction * (shaft.Distance * Range(repositionDistanceScale.x, repositionDistanceScale.y));
            shaft.BasePosition = new Vector3(shaft.BasePosition.x,
                Mathf.Min(transform.position.y, shaftBaseHeight), shaft.BasePosition.z);
        }

        private sealed class ShaftInstance
        {
            internal MeshRenderer Renderer { get; }
            internal float Phase { get; }
            internal float Angle { get; }
            internal float Distance { get; }
            internal Vector3 BasePosition { get; set; } = Vector3.zero;

            /// <summary>Captures a spawned beam and its persistent pulse and placement parameters.</summary>
            /// <param name="renderer">Instantiated beam renderer owned by the manager's root.</param>
            /// <param name="phase">Opacity-pulse phase offset in radians.</param>
            /// <param name="angle">Horizontal placement angle in radians.</param>
            /// <param name="distance">Nonnegative reference distance in metres, scaled on each placement.</param>
            internal ShaftInstance(MeshRenderer renderer, float phase, float angle, float distance)
            {
                Renderer = renderer;
                Phase = phase;
                Angle = angle;
                Distance = distance;
            }
        }
    }
}
