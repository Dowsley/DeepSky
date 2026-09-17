Shader "DeepSky/Habitat"
{
    Properties
    {
        _BaseMap ("Surface texture", 2D) = "white" {}
        _BaseColor ("Surface tint", Color) = (1,1,1,1)
        _OcclusionMap ("Module recess shading", 2D) = "white" {}
        _OcclusionStrength ("Recess contrast", Range(0,1)) = .65
        _Emission ("Lamp emission", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            #include "Interior.hlsl"
            #include "DiverLight.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            float4 _HabitatLampPositions[4];
            float4 _HabitatLampDirections[4];
            float4 _HabitatLampColor;
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Emission, _OcclusionStrength;
            CBUFFER_END
            struct A { float4 position:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float2 occlusion:TEXCOORD1; };
            struct V { float4 position:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; float2 occlusion:TEXCOORD3; };
            /// <summary>Projects a module with its primary color UVs and secondary occlusion UVs.</summary>
            /// <param name="v">Mesh vertex in module coordinates.</param>
            /// <returns>Interpolated world geometry and both texture coordinates.</returns>
            V Vert(A v)
            {
                V o;
                o.world=TransformObjectToWorld(v.position.xyz);
                o.position=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(v.normal);
                o.uv=v.uv;
                o.occlusion=v.occlusion;
                return o;
            }
            /// <summary>Evaluates nearby warm fixtures with finite range and broad downward cones.</summary>
            /// <param name="world">Surface position in world metres.</param>
            /// <param name="normal">Unit world-space surface normal.</param>
            /// <returns>Display-space lamp irradiance; zero without assigned fixtures.</returns>
            float3 ModuleLighting(float3 world, float3 normal)
            {
                float light = 0;
                for (int index = 0; index < 4; index++)
                {
                    float3 offset = _HabitatLampPositions[index].xyz - world;
                    float distance = length(offset);
                    float3 direction = offset / max(distance, .001);
                    float falloff = saturate(1 - distance / max(_HabitatLampPositions[index].w, .001));
                    float cone = smoothstep(-.15, .35, dot(-direction, _HabitatLampDirections[index].xyz));
                    light += falloff * falloff * cone * saturate(dot(normal, direction));
                }
                return light * _HabitatLampColor.rgb * _HabitatLampColor.a;
            }
            /// <summary>Lights dry module surfaces and attenuates only intervening water.</summary>
            /// <param name="i">Interpolated surface geometry and texture coordinates.</param>
            /// <returns>Opaque output color with local lamp and recess contrast.</returns>
            half4 Frag(V i):SV_Target
            {
                float3 normal=normalize(i.normal);
                float3 albedo=ToDisplayColor(SAMPLE_TEXTURE2D(_BaseMap,sampler_PointClamp,i.uv).rgb)*_BaseColor.rgb;
                float occlusion=lerp(1,SAMPLE_TEXTURE2D(_OcclusionMap,sampler_OcclusionMap,i.occlusion).r,_OcclusionStrength);
                float3 light=float3(.39,.40,.39)*(0.82+0.18*normal.y);
                light += float3(.19,.17,.13)*saturate(dot(normal,normalize(float3(-.3,1,-.5))));
                light = (light + ModuleLighting(i.world,normal)) * occlusion;
                light += DiverTorch(i.world) * lerp(1,occlusion,.35);
                float3 color=albedo*light + _BaseColor.rgb*_Emission;
                float water=WaterPathLength(i.world);
                color=ApplyDistanceTint(color,water);
                // Opaque modules retain depth while losing all contrast at the water visibility limit.
                float3 viewDirection=normalize(i.world-_WorldSpaceCameraPos);
                color=lerp(WaterColor(viewDirection.y),color,VisibilityFade(water));
                return half4(ToOutputColor(color),1);
            }
            ENDHLSL
        }
    }
}
