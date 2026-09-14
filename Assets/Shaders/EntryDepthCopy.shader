Shader "DeepSky/Entry Depth Copy"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            /// <summary>Copies the completed scene's single-sample device depth to a sampleable color texture.</summary>
            /// <param name="i">Full-screen blit coordinates.</param>
            /// <returns>Raw device depth, preserving the platform's reversed-Z convention.</returns>
            float Frag(Varyings i):SV_Target
            {
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_PointClamp,i.texcoord,0).r;
            }
            ENDHLSL
        }
    }
}
