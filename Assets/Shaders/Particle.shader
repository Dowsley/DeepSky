Shader "DeepSky/Particle"
{
    Properties
    {
        _BaseMap ("Particle texture", 2D) = "white" {}
        _BaseColor ("Display tint RGB / opacity multiplier", Vector) = (1,1,1,1)
        [Header(Texture shape)]
        _TextureRepeat ("Flake repeats per cloud", Range(1, 4)) = 1
        _CloudConcentration ("Cloud center concentration", Range(0, 4)) = 0
        _TextureColorStrength ("Texture color contribution", Range(0, 1)) = 1
        _OpacityGain ("Texture opacity gain", Range(1, 3)) = 1
        _OpacityContrast ("Soft opaque core", Range(0, 1)) = 0
        [Header(Stylization)]
        _PixelGrid ("Texture cells per axis (0 disables)", Range(0, 128)) = 0
        _OpacitySteps ("Texture opacity bands (0 disables)", Range(0, 16)) = 0
        [Header(Dust transparency)]
        _Porosity ("Porous breakup", Range(0, 1)) = 0
        _PoreFrequency ("Pore cells across texture", Range(16, 512)) = 256
        _CoreOpacity ("Maximum texture opacity", Range(0.01, 1)) = 1
        [Header(Atmosphere)]
        [ToggleUI] _DepthTint ("Environment-tinted cloud", Float) = 0
        [ToggleUI] _DistanceFade ("Camera distance fading", Float) = 0
        _NearFadeStart ("Near fade start", Float) = 0
        _NearFadeEnd ("Near fade end", Float) = 0.5
        _FarFadeStart ("Far fade start", Float) = 12
        _FarFadeEnd ("Far fade end", Float) = 15
        [ToggleUI] _HorizontalDistance ("Use horizontal distance", Float) = 0
        _VolumeFadeStart ("Volume fade start", Float) = 15
        _VolumeFadeEnd ("Volume fade end", Float) = 18
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UnderwaterEffects" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
            #include "Atmosphere.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _TextureRepeat, _CloudConcentration, _TextureColorStrength;
                float _OpacityGain, _OpacityContrast;
                float _PixelGrid, _OpacitySteps;
                float _Porosity, _PoreFrequency, _CoreOpacity;
                float _DistanceFade;
                float _DepthTint;
                float _NearFadeStart, _NearFadeEnd, _FarFadeStart, _FarFadeEnd;
                float _HorizontalDistance, _VolumeFadeStart, _VolumeFadeEnd;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float3 positionWS:TEXCOORD1; };
            /// <summary>Projects a particle billboard while preserving its texture and lifetime color.</summary>
            /// <param name="v">Particle-system vertex with object-space position, UV and color.</param>
            /// <returns>Clip/world positions and interpolated texture/color inputs.</returns>
            V Vert(A v)
            {
                V o;
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=v.uv;
                o.color=v.color;
                o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            /// <summary>Breaks dense dust interiors into a fixed UV-space transparency pattern.</summary>
            /// <param name="uv">Particle texture coordinates in [0, 1].</param>
            /// <param name="textureAlpha">Sampled texture opacity in [0, 1], before lifetime and distance fades.</param>
            /// <returns>Opacity capped by the material setting and attenuated by porous noise.</returns>
            float DustOpacity(float2 uv, float textureAlpha)
            {
                // Compress opaque cores more than their already translucent edges.
                float alpha = textureAlpha * _CoreOpacity
                    / (_CoreOpacity + (1 - _CoreOpacity) * textureAlpha);
                if (_Porosity <= 0)
                {
                    return alpha;
                }

                float2 coordinates = uv * _PoreFrequency;
                uint2 cell = (uint2)floor(coordinates);
                float2 blend = smoothstep(0, 1, frac(coordinates));
                float lower = lerp(GenerateHashedRandomFloat(cell),
                    GenerateHashedRandomFloat(cell + uint2(1, 0)), blend.x);
                float upper = lerp(GenerateHashedRandomFloat(cell + uint2(0, 1)),
                    GenerateHashedRandomFloat(cell + uint2(1, 1)), blend.x);
                float noise = lerp(lower, upper, blend.y);
                float pores = smoothstep(_Porosity - 0.08, _Porosity + 0.08, noise);

                // Resolve subpixel noise toward average coverage to limit distant sparkle.
                float footprint = max(length(ddx(coordinates)), length(ddy(coordinates)));
                pores = lerp(pores, 1 - _Porosity, smoothstep(0.5, 2, footprint));
                return alpha * pores;
            }

            /// <summary>Samples smaller repeated flecks inside a softly concentrated cloud footprint.</summary>
            /// <param name="uv">Billboard coordinates in [0, 1]; repetition affects flecks, not the world-space card size.</param>
            /// <returns>Texture color and shaped opacity, before particle lifetime and atmosphere fading.</returns>
            half4 SampleParticle(float2 uv)
            {
                if (_PixelGrid > 0)
                {
                    uv = (floor(uv * _PixelGrid) + 0.5) / _PixelGrid;
                }
                float2 textureUV = uv;
                float tileFade = 1;
                if (_TextureRepeat > 1)
                {
                    float2 tiled = uv * _TextureRepeat;
                    float2 cell = floor(tiled);
                    textureUV = frac(tiled);
                    // Alternate reflection avoids identical adjacent flake arrangements.
                    textureUV = lerp(textureUV, 1 - textureUV, fmod(cell, 2));
                    float2 edge = min(textureUV, 1 - textureUV);
                    tileFade = smoothstep(0, 0.025, min(edge.x, edge.y));
                }

                half4 sample = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, textureUV, 0);
                float opacity = saturate(sample.a * _OpacityGain);
                opacity = lerp(opacity, smoothstep(0, 1, opacity), _OpacityContrast);
                float2 centered = (uv - 0.5) * 2;
                float concentration = exp2(-dot(centered, centered) * _CloudConcentration);
                sample.a = DustOpacity(textureUV, opacity) * tileFade * concentration;
                // Band only the texture opacity so lifetime and distance fades remain smooth.
                if (_OpacitySteps > 0)
                {
                    sample.a = floor(sample.a * _OpacitySteps) / _OpacitySteps;
                }
                return sample;
            }

            /// <summary>Composites textured particles using premultiplied display-space color.</summary>
            /// <param name="i">Interpolated billboard data including UV, lifetime color and world position.</param>
            /// <returns>Premultiplied RGB and opacity after authored porosity, distance and depth fading.</returns>
            half4 Frag(V i):SV_Target
            {
                half4 sample = SampleParticle(i.uv);
                float alpha = sample.a*_BaseColor.a*i.color.a;
                float3 relative = i.positionWS - GetCameraPositionWS();
                float radialDistance = length(relative);
                float fadeDistance = lerp(radialDistance, length(relative.xz), _HorizontalDistance);
                float nearFade = saturate((fadeDistance - _NearFadeStart) / max(_NearFadeEnd - _NearFadeStart, 0.0001));
                float farFade = saturate((_FarFadeEnd - fadeDistance) / max(_FarFadeEnd - _FarFadeStart, 0.0001));
                float volumeFade = saturate((_VolumeFadeEnd - radialDistance) / max(_VolumeFadeEnd - _VolumeFadeStart, 0.0001));
                alpha *= lerp(1, nearFade * farFade * volumeFade, _DistanceFade);
                float4 depthTint = lerp(float4(1,1,1,1), CloudDepthMultiplier(), _DepthTint);
                alpha = saturate(alpha * depthTint.a);
                float3 textureColor = lerp(float3(1, 1, 1), ToDisplayColor(sample.rgb), _TextureColorStrength);
                return half4(textureColor*_BaseColor.rgb*i.color.rgb*depthTint.rgb*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
