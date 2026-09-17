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
        private static readonly int PixelationId = Shader.PropertyToID("_Pixelation");
        private static readonly int ColorQuantizationId = Shader.PropertyToID("_ColorQuantization");
        private static readonly int DitheringId = Shader.PropertyToID("_Dithering");

        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

        internal Material Material { get; }

        /// <summary>Schedules presentation after scene post-processing and before the final display blit.</summary>
        /// <param name="material">Required shared material with the pixel-grid shader at pass zero; not owned.</param>
        internal RetroPresentationPass(Material material)
        {
            Material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        /// <summary>Sets camera-local effect switches without modifying the shared material asset.</summary>
        /// <param name="pixelation">Whether to sample a reduced-resolution pixel grid.</param>
        /// <param name="colorQuantization">Whether to reduce color precision.</param>
        /// <param name="dithering">Whether to dither the color quantization thresholds.</param>
        internal void ConfigureEffects(bool pixelation, bool colorQuantization, bool dithering)
        {
            properties.Clear();
            properties.SetFloat(PixelationId, pixelation ? 1f : 0f);
            properties.SetFloat(ColorQuantizationId, colorQuantization ? 1f : 0f);
            properties.SetFloat(DitheringId, dithering ? 1f : 0f);
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
            graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, output, Material, 0, properties, 0, 0),
                "retro presentation");
            resources.cameraColor = output;
        }
    }
}
