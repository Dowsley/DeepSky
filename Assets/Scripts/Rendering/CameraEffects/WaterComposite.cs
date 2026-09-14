using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>
    /// Composites underwater effects for this camera and its Scene view preview, before bloom.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class WaterComposite : MonoBehaviour
    {
        [SerializeField] private Material material = null!;

        private Camera view = null!;
        private WaterCompositePass pass = null!;

        /// <summary>Creates the material-backed pass and subscribes to camera rendering.</summary>
        private void OnEnable()
        {
            view = GetComponent<Camera>();
            Assert.IsNotNull(view, nameof(view));
            CreatePass();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
        }

        /// <summary>Stops enqueueing the composite pass when this component is disabled.</summary>
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
        }

        /// <summary>Enqueues water composition for an eligible camera and responds to Inspector material replacement.</summary>
        /// <param name="context">Current pipeline context; drawing is scheduled through the camera's renderer.</param>
        /// <param name="camera">Camera beginning its render.</param>
        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!CameraEffectRouting.ShouldRender(camera, view))
            {
                return;
            }

            // Inspector material replacement does not re-enable the component.
            if (pass.Material != material)
            {
                CreatePass();
            }

            var renderer = CameraEffectRouting.GetRenderer(camera);
            renderer.EnqueuePass(pass);
        }

        /// <summary>Validates the assigned material and constructs a pass that borrows it.</summary>
        private void CreatePass()
        {
            Assert.IsNotNull(material, nameof(material));
            pass = new WaterCompositePass(material);
        }
    }
}
