using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeepSky.Rendering.CameraEffects
{
    internal static class CameraEffectRouting
    {
        /// <summary>Selects the owning camera and its enabled main-camera post-processing preview in Scene view.</summary>
        /// <param name="camera">Camera currently being rendered.</param>
        /// <param name="owner">Camera owning the effect component.</param>
        /// <returns>Whether this effect should enqueue a pass for the current camera.</returns>
        internal static bool ShouldRender(Camera camera, Camera owner)
        {
            return camera == owner
                || (camera.cameraType == CameraType.SceneView
                    && owner == Camera.main
                    && CoreUtils.ArePostProcessesEnabled(camera));
        }

        /// <summary>Resolves the URP renderer receiving this camera's custom effect passes.</summary>
        /// <param name="camera">Camera to render while a Universal Render Pipeline asset is active.</param>
        /// <returns>The pipeline's default renderer for Scene view, or the camera's selected renderer otherwise.</returns>
        internal static ScriptableRenderer GetRenderer(Camera camera)
        {
            // URP renders Scene view with the pipeline's default renderer.
            return camera.cameraType == CameraType.SceneView
                ? UniversalRenderPipeline.asset.scriptableRenderer
                : camera.GetUniversalAdditionalCameraData().scriptableRenderer;
        }
    }
}
