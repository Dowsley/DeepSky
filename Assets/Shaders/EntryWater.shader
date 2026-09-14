Shader "DeepSky/Entry Water"
{
    Properties
    {
        _BaseColor ("Water tint", Color) = (.375,.525,.75,.7)
        _WaveHeight ("Ripple height (metres)", Range(0,.04)) = .012
        _WaveLength ("Ripple length (metres)", Range(.3,3)) = 1.1
        _WaveSpeed ("Ripple speed", Range(0,2)) = .4
        _RippleContrast ("Ripple shading", Range(0,.3)) = .12
        _PixelDensity ("Surface pixels per metre", Range(8,128)) = 48
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            #include "Interior.hlsl"
            #include "DiverLight.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _WaveHeight, _WaveLength, _WaveSpeed, _RippleContrast, _PixelDensity;
            CBUFFER_END
            struct A { float4 position:POSITION; };
            struct V { float4 position:SV_POSITION; float3 world:TEXCOORD0; };

            /// <summary>Combines gentle intersecting ripples continuously across adjacent tiles.</summary>
            /// <param name="position">Horizontal world position in metres.</param>
            /// <returns>Signed ripple height normalized to the range [-1, 1].</returns>
            float Ripple(float2 position)
            {
                float2 phase = position * (6.283185 / max(_WaveLength, .001));
                float time = _Time.y * _WaveSpeed;
                return sin(phase.x + phase.y * .45 + time) * .55
                    + sin(phase.y * 1.3 - phase.x * .25 - time * .73) * .3
                    + sin(phase.x * .6 - phase.y * .8 + time * .41) * .15;
            }

            /// <summary>Displaces the water mesh around its fixed gameplay waterline.</summary>
            /// <param name="v">Local water-surface vertex.</param>
            /// <returns>Clip and world positions with a small visual ripple.</returns>
            V Vert(A v)
            {
                V o;
                o.world=TransformObjectToWorld(v.position.xyz);
                o.world.y += Ripple(o.world.xz) * _WaveHeight;
                o.position=TransformWorldToHClip(o.world);
                return o;
            }
            /// <summary>Shades translucent blue water with pixel-stepped ripple brightness.</summary>
            /// <param name="i">Interpolated displaced surface position.</param>
            /// <returns>Fogged surface color and coverage from either side of the waterline.</returns>
            half4 Frag(V i):SV_Target
            {
                float density=max(_PixelDensity,1);
                float2 p=(floor(i.world.xz*density)+.5)/density;
                float ripple=round(Ripple(p)*8)/8;
                float3 color=_BaseColor.rgb*(.85+ripple*_RippleContrast+DiverTorch(i.world)*.2);
                float water=WaterPathLength(i.world);
                color=ApplyDistanceTint(color,water);
                return half4(ToOutputColor(color),_BaseColor.a*VisibilityFade(water));
            }
            ENDHLSL
        }
    }
}
