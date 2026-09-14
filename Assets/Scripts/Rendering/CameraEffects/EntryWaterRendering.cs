using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>Renders entrance water against the completed underwater scenery.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class EntryWaterRendering : MonoBehaviour
    {
        [SerializeField] private Material depthCopy = null!;

        private Camera view = null!;
        private EntryWaterPass pass = null!;

        /// <summary>Validates the capture material and subscribes the owner camera.</summary>
        private void OnEnable()
        {
            Assert.IsNotNull(depthCopy, nameof(depthCopy));
            view = GetComponent<Camera>();
            Assert.IsNotNull(view, nameof(view));
            pass = new EntryWaterPass(depthCopy);
            RenderPipelineManager.beginCameraRendering += BeginCamera;
        }

        /// <summary>Stops scheduling entrance rendering when disabled.</summary>
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
        }

        /// <summary>Schedules water after scenery and atmospheric particles, before bloom and pixelation.</summary>
        /// <param name="context">Pipeline context; the renderer owns execution.</param>
        /// <param name="camera">Rendering camera; only the owner and its Scene view preview qualify.</param>
        private void BeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!CameraEffectRouting.ShouldRender(camera, view))
            {
                return;
            }
            if (pass.Material != depthCopy)
            {
                Assert.IsNotNull(depthCopy, nameof(depthCopy));
                pass = new EntryWaterPass(depthCopy);
            }
            CameraEffectRouting.GetRenderer(camera).EnqueuePass(pass);
        }
    }
}
