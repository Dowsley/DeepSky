using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace DeepSky.Rendering.Atmosphere
{
    /// <summary>
    /// Applies a depth profile per rendered camera, including Scene view. Depth is sea level minus camera Y.
    /// Shared materials remain unchanged; shader globals are active only during this camera's render.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class DepthAtmosphere : MonoBehaviour
    {
        private static readonly int Active = Shader.PropertyToID("_DepthAtmosphereActive");
        private static readonly int UpperWater = Shader.PropertyToID("_DepthWaterUpper");
        private static readonly int LowerWater = Shader.PropertyToID("_DepthWaterLower");
        private static readonly int Ambient = Shader.PropertyToID("_DepthAmbient");
        private static readonly int VisibilityCaustics = Shader.PropertyToID("_DepthVisibilityCaustics");
        private static readonly int Fog = Shader.PropertyToID("_DepthFogParameters");
        private static readonly int Clouds = Shader.PropertyToID("_DepthCloudMultiplier");

        [Header("References")]
        [SerializeField] private DepthAtmosphereProfile profile = null!;

        [Header("World coordinates")]
        [SerializeField] private float seaLevel = 0f;

        private Camera view = null!;

        internal float Daylight => profile.Daylight;

        /// <summary>Validates the profile and subscribes this camera to per-render atmosphere updates.</summary>
        private void OnEnable()
        {
            view = GetComponent<Camera>();
            Assert.IsNotNull(profile, nameof(profile));
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        /// <summary>Unsubscribes render callbacks and clears the active atmosphere shader flag.</summary>
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            Shader.SetGlobalFloat(Active, 0f);
        }

        /// <summary>Samples the depth-dependent upper bound on beam opacity.</summary>
        /// <param name="cameraHeight">Camera world Y in metres, relative to the configured sea level.</param>
        /// <returns>The profile's interpolated shaft-opacity limit in [0, 1].</returns>
        internal float ShaftOpacityLimit(float cameraHeight)
        {
            return profile.Evaluate(seaLevel - cameraHeight).ShaftOpacityLimit;
        }

        /// <summary>Uploads camera-depth atmosphere globals before an eligible camera renders.</summary>
        /// <param name="context">Pipeline render context; this callback sets globals without submitting draws.</param>
        /// <param name="camera">Camera beginning its render; other cameras leave globals unchanged.</param>
        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!ShouldApply(camera))
            {
                return;
            }

            AtmosphereSample sample = profile.Evaluate(seaLevel - camera.transform.position.y);
            Shader.SetGlobalVector(UpperWater, sample.UpperWater);
            Shader.SetGlobalVector(LowerWater, sample.LowerWater);
            Shader.SetGlobalVector(Ambient, sample.Ambient);
            Shader.SetGlobalVector(VisibilityCaustics, new Vector4(sample.Visibility, sample.Caustics, 0f, 0f));
            Shader.SetGlobalVector(Fog, profile.FogParameters);
            Shader.SetGlobalVector(Clouds, sample.CloudMultiplier);
            Shader.SetGlobalFloat(Active, 1f);
        }

        /// <summary>Deactivates atmosphere globals after an eligible camera finishes rendering.</summary>
        /// <param name="context">Pipeline render context; unused by this callback.</param>
        /// <param name="camera">Camera whose rendering has completed.</param>
        private void EndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (ShouldApply(camera))
            {
                Shader.SetGlobalFloat(Active, 0f);
            }
        }

        /// <summary>Selects the owning camera and, for the main camera, its Scene view preview.</summary>
        /// <param name="camera">Rendering camera to evaluate.</param>
        /// <returns>Whether this component supplies atmosphere settings for that render.</returns>
        private bool ShouldApply(Camera camera)
        {
            return camera == view || camera.cameraType == CameraType.SceneView && view == Camera.main;
        }
    }
}
