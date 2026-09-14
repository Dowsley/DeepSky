Shader "DeepSky/Shaft"
{
    Properties
    {
        _BaseMap ("Ray cross-section", 2D) = "white" {}
        _Opacity ("Opacity", Float) = .2
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
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Interior.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float _Opacity;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; };
            V Vert(A v) { V o; o.world=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.uv=v.uv; return o; }
            half4 Frag(V i):SV_Target
            {
                if (PointInInterior(i.world))
                {
                    discard;
                }
                float edge = SAMPLE_TEXTURE2D_LOD(_BaseMap,sampler_PointRepeat,float2(i.uv.x,.5),0).a;
                float ends = 1-abs(i.uv.y*2-1);
                float alpha = edge*ends*_Opacity;
                return half4(alpha,alpha,alpha,alpha);
            }
            ENDHLSL
        }
    }
}
