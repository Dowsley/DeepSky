Shader "DeepSky/Entry Water"
{
    Properties
    {
        _BaseColor ("Scattered water color", Color) = (.13,.32,.30,1)
        _WaveHeight ("Ripple height (metres)", Range(0,.04)) = .012
        _WaveLength ("Ripple length (metres)", Range(.3,3)) = 1.1
        _WaveSpeed ("Ripple speed", Range(0,2)) = .4
        _RippleContrast ("Fine ripple slope", Range(0,.3)) = .16
        _PixelDensity ("Surface pixels per metre", Range(8,128)) = 32
        _Refraction ("Refraction displacement (metres)", Range(0,.3)) = .075
        _Absorption ("Light absorption per metre (RGB)", Vector) = (.12,.045,.025,0)
        _Reflection ("Reflection strength", Range(0,1)) = .85
        _ReflectionTint ("Off-screen reflection tint", Color) = (.24,.29,.28,1)
        _SurfaceColor ("Uneven surface tint (RGB), strength (A)", Color) = (.18,.43,.38,.3)
        _CrestColor ("Contact foam color (RGB), strength (A)", Color) = (.76,.82,.70,.8)
        _EdgeWidth ("Contact edge width (metres)", Range(.01,.4)) = .14
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="EntryWater" }
            Cull Off ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            #include "Interior.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _Absorption, _ReflectionTint, _SurfaceColor, _CrestColor;
            float _WaveHeight, _WaveLength, _WaveSpeed, _RippleContrast, _PixelDensity;
            float _Refraction, _Reflection, _EdgeWidth;
            CBUFFER_END
            struct A { float4 position:POSITION; };
            struct V { float4 position:SV_POSITION; float3 world:TEXCOORD0; float waterline:TEXCOORD1; };

            #include "EntryWaterOptics.hlsl"

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
                o.waterline=o.world.y;
                o.world.y += Ripple(o.world.xz) * _WaveHeight;
                o.position=TransformWorldToHClip(o.world);
                return o;
            }
            /// <summary>Builds a coarse moving normal from broad waves and smaller crossing ripples.</summary>
            /// <param name="world">Surface position in world metres.</param>
            /// <returns>Upward unit surface normal, continuous across neighboring water tiles.</returns>
            float3 RippleNormal(float3 world)
            {
                float density=max(_PixelDensity,1);
                float2 p=(floor(world.xz*density)+.5)/density;
                float e=1/density;
                float2 slope=float2(Ripple(p+float2(e,0))-Ripple(p-float2(e,0)),
                    Ripple(p+float2(0,e))-Ripple(p-float2(0,e))) * (_WaveHeight/(2*e));
                float t=_Time.y*_WaveSpeed;
                float2 fine=p*(6.283185/max(_WaveLength*.27,.01));
                slope += float2(cos(fine.x+fine.y*.61+t*.83),
                    cos(fine.y-fine.x*.43-t*.67)) * _RippleContrast*.5;
                return normalize(float3(-slope.x,1,-slope.y));
            }

            /// <summary>Builds soft surface color patches and a narrow, uneven contact foam band.</summary>
            /// <param name="world">Surface position in world metres.</param>
            /// <param name="contactDistance">Distance in metres to the scene behind the unperturbed surface pixel.</param>
            /// <returns>Uneven tint coverage in X and contact foam coverage in Y, each in [0, 1].</returns>
            float2 SurfaceCoverage(float3 world, float contactDistance)
            {
                float density=max(_PixelDensity,1);
                float2 p=(floor(world.xz*density)+.5)/density;
                float time=_Time.y*_WaveSpeed;
                float swell=Ripple(p);
                float breakup=.5+.5*sin(p.x*2.7-p.y*3.1+sin(p.y*1.9+time*.31));
                float width=_EdgeWidth*lerp(.45,1,breakup);
                float edge=(1-smoothstep(0,width,contactDistance))*lerp(.3,1,breakup);
                return float2(saturate(.3+swell*.35+breakup*.3),edge);
            }

            /// <summary>Combines refracted scenery, absorption, reflections, surface tint and contact foam from either side.</summary>
            /// <param name="i">Interpolated displaced surface position.</param>
            /// <returns>Composited scene color; atmospheric fade only reduces the surface contribution.</returns>
            half4 Frag(V i):SV_Target
            {
                float2 uv=GetNormalizedScreenSpaceUV(i.position);
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                bool above=_WorldSpaceCameraPos.y>=i.waterline;
                float3 normal=RippleNormal(i.world)*(above ? 1 : -1);
                float facing=saturate(dot(normal,view));
                float sceneDepth=EntrySceneEyeDepth(uv);
                float surfaceDepth=-TransformWorldToView(i.world).z;
                float thickness=max(0,sceneDepth-surfaceDepth);
                float3 normalVS=TransformWorldToViewDir(normal);
                float2 offset=normalVS.xy*_Refraction/max(surfaceDepth,1);
                offset *= saturate(thickness/.25);
                float2 refractedUV=saturate(uv+offset);
                // Foreground geometry must not be dragged across the waterline.
                if (EntrySceneEyeDepth(refractedUV)<=surfaceDepth+.02)
                {
                    refractedUV=uv;
                }
                float3 background=EntrySceneColor(refractedUV);
                float3 behind=EntryScenePosition(refractedUV);
                float path=above ? min(length(behind-i.world),8) : 0;
                float3 transmission=exp(-_Absorption.rgb*path);
                // Captured scenery already includes the scene's underwater fog.
                float3 refracted=background*transmission+_BaseColor.rgb*(1-transmission);
                float fresnel=.02+.98*pow(1-facing,5);
                if (!above)
                {
                    // Water-to-air rays approach total internal reflection at grazing angles.
                    fresnel=lerp(fresnel,1,1-smoothstep(.62,.78,facing));
                }
                float3 reflectionDirection=reflect(-view,normal);
                float4 reflected=EntryReflection(i.world+normal*.025,reflectionDirection);
                float3 fallback=above ? _ReflectionTint.rgb : WaterColor(reflectionDirection.y)*.65;
                float3 reflection=lerp(fallback,reflected.rgb,reflected.a);
                reflection=ApplyDistanceTint(reflection,WaterPathLength(i.world));
                float3 color=lerp(refracted,reflection,saturate(fresnel*_Reflection));
                float contactDistance=length(EntryScenePosition(uv)-i.world);
                float2 coverage=SurfaceCoverage(i.world,contactDistance);
                float surfaceStrength=above ? 1 : .5;
                float waterPath=WaterPathLength(i.world);
                float3 surfaceTint=ApplyDistanceTint(_SurfaceColor.rgb,waterPath);
                float3 crestTint=ApplyDistanceTint(_CrestColor.rgb,waterPath);
                color=lerp(color,surfaceTint,coverage.x*_SurfaceColor.a*surfaceStrength);
                color=lerp(color,crestTint,coverage.y*_CrestColor.a*surfaceStrength);
                return half4(ToOutputColor(color),VisibilityFade(WaterPathLength(i.world)));
            }
            ENDHLSL
        }
    }
}
