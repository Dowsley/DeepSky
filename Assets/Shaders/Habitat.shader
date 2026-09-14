Shader "DeepSky/Habitat"
{
    Properties
    {
        _BaseMap ("Surface texture", 2D) = "white" {}
        _BaseColor ("Surface tint", Color) = (1,1,1,1)
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
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Emission;
            CBUFFER_END
            struct A { float4 position:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 position:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; };
            V Vert(A v)
            {
                V o;
                o.world=TransformObjectToWorld(v.position.xyz);
                o.position=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(v.normal);
                o.uv=v.uv;
                return o;
            }
            half4 Frag(V i):SV_Target
            {
                float3 normal=normalize(i.normal);
                float3 albedo=ToDisplayColor(SAMPLE_TEXTURE2D(_BaseMap,sampler_PointClamp,i.uv).rgb)*_BaseColor.rgb;
                float3 light=float3(.65,.56,.43)*(0.82+0.18*normal.y);
                light += float3(.30,.25,.18)*saturate(dot(normal,normalize(float3(-.3,1,-.5))));
                light += DiverTorch(i.world);
                float3 color=albedo*light + _BaseColor.rgb*_Emission;
                float water=WaterPathLength(i.world);
                color=ApplyDistanceTint(color,water);
                return half4(ToOutputColor(color),1);
            }
            ENDHLSL
        }
    }
}
