Shader "DeepSky/Bloom"
{
    Properties
    {
        _Threshold ("Brightness threshold (display space)", Range(0,0.99)) = 0.1
        _Intensity ("Bloom intensity", Range(0,3)) = 1
        _BlurSpacing ("Blur spacing (screen-width pixels)", Range(0,8)) = 2
        _EdgeShade ("Screen edge shading", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Atmosphere.hlsl"
        TEXTURE2D_X(_UnderwaterBloomTexture);
        float _EdgeShade, _Threshold, _Intensity, _BlurSpacing;

        half4 Bright(Varyings i) : SV_Target
        {
            float3 color = ToDisplayColor(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,i.texcoord).rgb);
            float luminance = dot(color,float3(.2126,.7152,.0722));
            return half4(luminance < _Threshold ? 0 : saturate((color-_Threshold)/max(1-_Threshold,.0001)),1);
        }

        half4 Blur(float2 uv, float2 direction)
        {
            const float weights[5] = { .16,.15,.12,.09,.05 };
            float3 color = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).rgb * weights[0];
            for (int tap=1;tap<=4;tap++)
            {
                color += SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+direction*tap).rgb * weights[tap];
                color += SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-direction*tap).rgb * weights[tap];
            }
            return half4(color,1);
        }

        // Both axes use screen width so the kernel has a consistent UV step.
        half4 BlurX(Varyings i) : SV_Target
        {
            return Blur(i.texcoord,float2(_BlurSpacing/_ScreenParams.x,0));
        }

        half4 BlurY(Varyings i) : SV_Target
        {
            return Blur(i.texcoord,float2(0,_BlurSpacing/_ScreenParams.x));
        }
        half4 Composite(Varyings i) : SV_Target
        {
            float3 scene = ToDisplayColor(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,i.texcoord).rgb);
            float3 bloom = SAMPLE_TEXTURE2D_X(_UnderwaterBloomTexture,sampler_LinearClamp,i.texcoord).rgb;
            bloom *= _Intensity;
            float2 edge = abs(i.texcoord-.5)*2;
            float factor = max(0,((length(edge)+max(edge.x,edge.y))*.5-.5)/.65);
            float3 color = saturate(scene+bloom-scene*bloom) * (1-saturate(factor*factor)*_EdgeShade);
            return half4(ToOutputColor(color),1);
        }
        ENDHLSL
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Bright
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurX
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurY
            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Composite
            ENDHLSL
        }
    }
}
