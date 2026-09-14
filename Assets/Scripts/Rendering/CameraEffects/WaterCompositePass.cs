using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>
    /// Depth-tests underwater effects into a display-space layer, then blends it over the scene color.
    /// </summary>
    internal sealed class WaterCompositePass : ScriptableRenderPass
    {
        private static readonly int WaterTexture = Shader.PropertyToID("_WaterLayerTexture");
        private static readonly ShaderTagId WaterTag = new("UnderwaterEffects");

        internal Material Material { get; }

        /// <summary>Schedules display-space water composition after transparent scene rendering.</summary>
        /// <param name="material">Shared composite material whose first shader pass reads the scene and water layer; not owned.</param>
        internal WaterCompositePass(Material material)
        {
            Material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            requiresIntermediateTexture = true;
        }

        /// <summary>
        /// Records depth-tested underwater-effect drawing and a full-screen composite into a new camera-color texture.
        /// The graph owns temporary textures. Direct back-buffer rendering is skipped because the source must be sampled.
        /// </summary>
        /// <param name="graph">Current frame's graph receiving resource declarations and deferred draw operations.</param>
        /// <param name="frameData">URP camera, culling and resource data; cameraColor is replaced with the composite output.</param>
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer)
            {
                return;
            }

            var source = resources.activeColorTexture;
            var descriptor = graph.GetTextureDesc(source);
            descriptor.name = "display-space water";
            descriptor.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
            descriptor.msaaSamples = graph.GetTextureDesc(resources.activeDepthTexture).msaaSamples;
            descriptor.bindTextureMS = false;
            descriptor.clearBuffer = true;
            descriptor.clearColor = Color.clear;
            var water = graph.CreateTexture(descriptor);
            var rendering = frameData.Get<UniversalRenderingData>();
            var camera = frameData.Get<UniversalCameraData>();
            var lighting = frameData.Get<UniversalLightData>();
            var drawing = RenderingUtils.CreateDrawingSettings(WaterTag, rendering, camera, lighting,
                SortingCriteria.CommonTransparent);
            var filtering = new FilteringSettings(RenderQueueRange.transparent, camera.camera.cullingMask);
            var list = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
            using (var builder = graph.AddRasterRenderPass<DrawData>("water coverage", out var data))
            {
                data.Renderers = list;
                builder.UseRendererList(list);
                builder.SetRenderAttachment(water, 0);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(water, WaterTexture);
                builder.SetRenderFunc((DrawData draw, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(draw.Renderers));
            }

            descriptor = graph.GetTextureDesc(source);
            descriptor.name = "water composite";
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.clearBuffer = false;
            var output = graph.CreateTexture(descriptor);
            using (var builder = graph.AddBlitPass(
                       new RenderGraphUtils.BlitMaterialParameters(source, output, Material, 0), "water composite",
                       true))
            {
                builder.UseGlobalTexture(WaterTexture);
            }

            resources.cameraColor = output;
        }

        private sealed class DrawData
        {
            internal RendererListHandle Renderers;
        }
    }
}
