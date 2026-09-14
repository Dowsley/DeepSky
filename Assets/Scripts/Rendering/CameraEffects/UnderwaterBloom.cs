using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>
    /// Applies screen-blended bloom and edge shading to this camera and its Scene view preview.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class UnderwaterBloom : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Material material = null!;

        [Header("Quality")]
        [Tooltip("Resolution of the brightness and blur buffers. Composition remains full resolution.")]
        [SerializeField] private BloomResolution resolution = BloomResolution.Half;

        private Camera view = null!;
        private UnderwaterBloomPass pass = null!;

        /// <summary>Creates the bloom pass from the authored material and quality setting, then subscribes to rendering.</summary>
        private void OnEnable()
        {
            view = GetComponent<Camera>();
            Assert.IsNotNull(view, nameof(view));
            CreatePass();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
        }

        /// <summary>Stops enqueueing bloom when this component is disabled.</summary>
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
        }

        /// <summary>Enqueues bloom for an eligible camera, rebuilding the pass if its authoring inputs changed.</summary>
        /// <param name="context">Current pipeline context; drawing is scheduled through the camera's renderer.</param>
        /// <param name="camera">Camera beginning its render.</param>
        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!CameraEffectRouting.ShouldRender(camera, view))
            {
                return;
            }

            // Inspector material replacement does not re-enable the component.
            if (pass.Material != material || pass.DownsampleFactor != (int)resolution)
            {
                CreatePass();
            }

            var renderer = CameraEffectRouting.GetRenderer(camera);
            renderer.EnqueuePass(pass);
        }

        /// <summary>Validates the assigned material and constructs a pass with the selected downsample factor.</summary>
        private void CreatePass()
        {
            Assert.IsNotNull(material, nameof(material));
            pass = new UnderwaterBloomPass(material, (int)resolution);
        }

        private enum BloomResolution
        {
            Full = 1,
            Half = 2,
            Quarter = 4
        }
    }
}
