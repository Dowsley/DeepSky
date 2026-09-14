Shader "DeepSky/Building Preview"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 position:POSITION; float4 color:COLOR; };
            struct V { float4 position:SV_POSITION; float4 color:COLOR; };
            V Vert(A v)
            {
                V o;
                o.position=TransformObjectToHClip(v.position.xyz);
                o.color=v.color;
                return o;
            }
            half4 Frag(V i):SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
