Shader "DeepSky/Water Composite"
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
            #include "Atmosphere.hlsl"
            TEXTURE2D_X(_WaterLayerTexture);

            half4 Frag(Varyings i) : SV_Target
            {
                float3 scene = ToDisplayColor(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,i.texcoord).rgb);
                float4 water = SAMPLE_TEXTURE2D_X(_WaterLayerTexture,sampler_LinearClamp,i.texcoord);
                return half4(ToOutputColor(scene*(1-water.a)+water.rgb),1);
            }
            ENDHLSL
        }
    }
}
