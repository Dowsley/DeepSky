using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>Applies a coarse pixel grid and ordered color dithering after the camera's atmospheric effects.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class RetroPresentation : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Material containing resolution, color precision and dithering controls.")]
        [SerializeField] private Material material = null!;

        private Camera view = null!;
        private RetroPresentationPass pass = null!;

        /// <summary>Validates the authored material and subscribes this camera's presentation pass to rendering.</summary>
        private void OnEnable()
        {
            view = GetComponent<Camera>();
            Assert.IsNotNull(view, nameof(view));
            CreatePass();
            RenderPipelineManager.beginCameraRendering += BeginCamera;
        }

        /// <summary>Stops scheduling presentation when this component is disabled or destroyed.</summary>
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
        }

        /// <summary>Schedules the effect for the owner camera and its enabled Scene view preview.</summary>
        /// <param name="context">Current pipeline context; the camera renderer owns execution.</param>
        /// <param name="camera">Camera beginning rendering; unrelated cameras are ignored.</param>
        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!CameraEffectRouting.ShouldRender(camera, view))
            {
                return;
            }

            if (pass.Material != material)
            {
                CreatePass();
            }

            CameraEffectRouting.GetRenderer(camera).EnqueuePass(pass);
        }

        /// <summary>Constructs a pass using the required shared material without taking ownership of the asset.</summary>
        private void CreatePass()
        {
            Assert.IsNotNull(material, nameof(material));
            pass = new RetroPresentationPass(material);
        }
    }
}
