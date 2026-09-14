Shader "DeepSky/Water"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Pass
        {
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            struct A { float4 vertex:POSITION; };
            struct V { float4 vertex:SV_POSITION; float3 direction:TEXCOORD0; };
            V Vert(A v) { V o; o.vertex=TransformObjectToHClip(v.vertex.xyz); o.direction=v.vertex.xyz; return o; }
            half4 Frag(V i):SV_Target
            {
                float3 direction=normalize(i.direction);
                return half4(ToOutputColor(WaterColor(direction.y)),1);
            }
            ENDHLSL
        }
    }
}
