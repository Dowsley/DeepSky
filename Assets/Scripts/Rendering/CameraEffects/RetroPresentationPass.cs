using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>Resamples the completed scene onto a virtual pixel grid, preserving full-resolution overlay UI.</summary>
    internal sealed class RetroPresentationPass : ScriptableRenderPass
    {
        internal Material Material { get; }

        /// <summary>Schedules presentation after scene post-processing and before the final display blit.</summary>
        /// <param name="material">Required shared material with the pixel-grid shader at pass zero; not owned.</param>
        internal RetroPresentationPass(Material material)
        {
            Material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        /// <summary>Records one full-resolution presentation pass. The graph owns its temporary output.</summary>
        /// <param name="graph">Current frame's render graph receiving the deferred draw.</param>
        /// <param name="frameData">URP resources; cameraColor becomes the processed output. An unsampleable back buffer is skipped.</param>
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer)
            {
                return;
            }

            var source = resources.activeColorTexture;
            var descriptor = graph.GetTextureDesc(source);
            descriptor.name = "retro presentation";
            descriptor.clearBuffer = false;
            descriptor.msaaSamples = MSAASamples.None;
            var output = graph.CreateTexture(descriptor);
            graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, output, Material, 0),
                "retro presentation");
            resources.cameraColor = output;
        }
    }
}
