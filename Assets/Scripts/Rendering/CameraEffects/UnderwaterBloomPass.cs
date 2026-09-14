using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace DeepSky.Rendering.CameraEffects
{
    /// <summary>
    /// Extracts bright pixels, blurs them at the selected resolution, then composites the glow over the scene.
    /// </summary>
    internal sealed class UnderwaterBloomPass : ScriptableRenderPass
    {
        private const int BrightPass = 0;
        private const int HorizontalBlurPass = 1;
        private const int VerticalBlurPass = 2;
        private const int CompositePass = 3;

        private static readonly int BloomTexture = Shader.PropertyToID("_UnderwaterBloomTexture");

        internal Material Material { get; }
        internal int DownsampleFactor { get; }

        /// <summary>Schedules brightness extraction, separable blur and screen composition before URP post-processing.</summary>
        /// <param name="material">Shared bloom material implementing the four indexed shader passes; not owned.</param>
        /// <param name="downsampleFactor">Positive divisor for blur-buffer width and height; composition stays full resolution.</param>
        internal UnderwaterBloomPass(Material material, int downsampleFactor)
        {
            Material = material;
            DownsampleFactor = downsampleFactor;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        /// <summary>
        /// Records brightness extraction, horizontal and vertical blur, then bloom and edge-shading composition.
        /// The graph owns temporary textures. Direct back-buffer rendering is skipped because the source must be sampled.
        /// </summary>
        /// <param name="graph">Current frame's graph receiving the four deferred image-processing passes.</param>
        /// <param name="frameData">URP frame resources; cameraColor is replaced with the full-resolution composite output.</param>
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer)
            {
                return;
            }

            var source = resources.activeColorTexture;
            var descriptor = graph.GetTextureDesc(source);
            descriptor.clearBuffer = false;
            descriptor.msaaSamples = MSAASamples.None;
            descriptor.name = "screen composite";
            var output = graph.CreateTexture(descriptor);

            descriptor.width = Mathf.Max(1, descriptor.width / DownsampleFactor);
            descriptor.height = Mathf.Max(1, descriptor.height / DownsampleFactor);
            descriptor.filterMode = FilterMode.Bilinear;
            descriptor.colorFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
            descriptor.name = "bright pass";
            var bright = graph.CreateTexture(descriptor);
            descriptor.name = "horizontal blur";
            var horizontal = graph.CreateTexture(descriptor);
            descriptor.name = "vertical blur";
            var bloom = graph.CreateTexture(descriptor);

            graph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, bright, Material, BrightPass),
                "bright pass");
            graph.AddBlitPass(
                new RenderGraphUtils.BlitMaterialParameters(bright, horizontal, Material, HorizontalBlurPass),
                "blur X");
            using (var builder = graph.AddBlitPass(
                       new RenderGraphUtils.BlitMaterialParameters(horizontal, bloom, Material, VerticalBlurPass),
                       "blur Y", true))
            {
                builder.SetGlobalTextureAfterPass(bloom, BloomTexture);
            }

            using (var builder = graph.AddBlitPass(
                       new RenderGraphUtils.BlitMaterialParameters(source, output, Material, CompositePass),
                       "screen composite", true))
            {
                builder.UseGlobalTexture(BloomTexture);
            }

            resources.cameraColor = output;
        }
    }
}
