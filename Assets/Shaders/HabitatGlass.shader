Shader "DeepSky/Habitat Glass"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            #include "Interior.hlsl"
            struct A { float4 position:POSITION; float3 normal:NORMAL; };
            struct V { float4 position:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
            V Vert(A v)
            {
                V o;
                o.world=TransformObjectToWorld(v.position.xyz);
                o.position=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(v.normal);
                return o;
            }
            half4 Frag(V i):SV_Target
            {
                float grazing=pow(1-abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world))),3);
                float3 color=ApplyDistanceTint(float3(.27,.43,.45),WaterPathLength(i.world));
                return half4(ToOutputColor(color),.025+grazing*.2);
            }
            ENDHLSL
        }
    }
}
