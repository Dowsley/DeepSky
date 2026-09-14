using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>Captures scene color and depth for entrance-water optics.</summary>
    internal sealed class EntryWaterPass : ScriptableRenderPass
    {
        private static readonly int SceneColor = Shader.PropertyToID("_EntrySceneColor");
        private static readonly int SceneDepth = Shader.PropertyToID("_EntrySceneDepth");
        private static readonly ShaderTagId WaterTag = new("EntryWater");

        internal Material Material { get; }

        /// <summary>Schedules capture after the underwater composite.</summary>
        /// <param name="material">Shared depth-copy material; not owned by the pass.</param>
        internal EntryWaterPass(Material material)
        {
            Material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents + 1;
            requiresIntermediateTexture = true;
        }

        /// <summary>Draws water using graph-owned snapshots, preserving the scene's existing depth buffer.</summary>
        /// <param name="graph">Current frame's render graph, which owns all temporary textures.</param>
        /// <param name="frameData">Camera resources; cameraColor is replaced with the water-composited output.</param>
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer)
            {
                return;
            }
            var source = resources.activeColorTexture;
            var descriptor = graph.GetTextureDesc(source);
            descriptor.name = "entrance water composite";
            descriptor.clearBuffer = false;
            descriptor.msaaSamples = MSAASamples.None;
            var output = graph.CreateTexture(descriptor);
            graph.AddBlitPass(source, output, Vector2.one, Vector2.zero, passName: "entrance scene copy");

            descriptor.name = "entrance scene depth";
            descriptor.colorFormat = GraphicsFormat.R32_SFloat;
            descriptor.filterMode = FilterMode.Point;
            var depth = graph.CreateTexture(descriptor);
            using (var builder = graph.AddBlitPass(
                       new RenderGraphUtils.BlitMaterialParameters(resources.activeDepthTexture, depth, Material, 0),
                       "entrance depth copy", true))
            {
                builder.SetGlobalTextureAfterPass(depth, SceneDepth);
                builder.SetGlobalTextureAfterPass(source, SceneColor);
                builder.UseTexture(source, AccessFlags.Read);
            }

            var rendering = frameData.Get<UniversalRenderingData>();
            var camera = frameData.Get<UniversalCameraData>();
            var lighting = frameData.Get<UniversalLightData>();
            var drawing = RenderingUtils.CreateDrawingSettings(WaterTag, rendering, camera, lighting,
                SortingCriteria.CommonTransparent);
            var filtering = new FilteringSettings(RenderQueueRange.transparent, camera.camera.cullingMask);
            var renderers = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
            using (var builder = graph.AddRasterRenderPass<DrawData>("entrance water", out var data))
            {
                data.Renderers = renderers;
                builder.UseRendererList(renderers);
                builder.UseGlobalTexture(SceneColor);
                builder.UseGlobalTexture(SceneDepth);
                builder.SetRenderAttachment(output, 0);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc((DrawData draw, RasterGraphContext context) =>
                    context.cmd.DrawRendererList(draw.Renderers));
            }
            resources.cameraColor = output;
        }

        private sealed class DrawData
        {
            internal RendererListHandle Renderers;
        }
    }
}
