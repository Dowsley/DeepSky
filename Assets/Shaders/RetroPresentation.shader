Shader "DeepSky/RetroPresentation"
{
    Properties
    {
        [Header(Pixel Grid)]
        _PixelHeight ("Virtual vertical resolution", Range(180,1080)) = 360
        [Header(Color Precision)]
        _ColorLevels ("Levels per color channel", Range(16,256)) = 32
        _DitherStrength ("Ordered dithering", Range(0,1)) = 0.65
        _Strength ("Presentation blend", Range(0,1)) = 1
        [HideInInspector] _Pixelation ("Pixelation", Float) = 1
        [HideInInspector] _ColorQuantization ("Color quantization", Float) = 1
        [HideInInspector] _Dithering ("Dithering", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Atmosphere.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelHeight, _ColorLevels, _DitherStrength, _Strength;
                float _Pixelation, _ColorQuantization, _Dithering;
            CBUFFER_END

            /* Returns a centered 4x4 Bayer threshold for an integer virtual-pixel coordinate. */
            float DitherThreshold(uint2 pixel)
            {
                static const float bayer[16] = {
                    0, 8, 2, 10,
                    12, 4, 14, 6,
                    3, 11, 1, 9,
                    15, 7, 13, 5
                };
                return (bayer[(pixel.y & 3) * 4 + (pixel.x & 3)] + 0.5) / 16.0 - 0.5;
            }

            /* Samples the source at virtual-pixel centers, quantizes display-space RGB and returns pipeline-space color. */
            half4 Frag(Varyings input) : SV_Target
            {
                float2 sourceSize = _BlitTexture_TexelSize.zw;
                float height = _Pixelation > 0.5 ? min(sourceSize.y, max(1, round(_PixelHeight))) : sourceSize.y;
                float2 grid = float2(max(1, round(height * sourceSize.x / sourceSize.y)), height);
                float2 pixel = min(floor(input.texcoord * grid), grid - 1);
                float2 uv = (pixel + 0.5) / grid;
                float3 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                float3 color = _Pixelation > 0.5
                    ? SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb
                    : original;
                if (_ColorQuantization > 0.5)
                {
                    color = ToDisplayColor(color);
                    float levels = max(2, round(_ColorLevels)) - 1;
                    float threshold = _Dithering > 0.5 ? DitherThreshold((uint2)pixel) * _DitherStrength : 0;
                    color = saturate(floor(color * levels + 0.5 + threshold) / levels);
                    color = ToOutputColor(color);
                }
                return half4(lerp(original, color, _Strength), 1);
            }
            ENDHLSL
        }
    }
}
