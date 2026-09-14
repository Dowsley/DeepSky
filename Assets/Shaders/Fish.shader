Shader "DeepSky/Fish"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Phase ("Tail phase", Float) = 0
        _WaveHeight ("Model wave height", Float) = 3.5
        _AnimationWeight ("Swimming animation weight", Range(0,1)) = 1
        _Cutoff ("Texture alpha cutoff", Range(0,1)) = 0
        _AlphaLightColor ("Inverse alpha light", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float _Phase, _WaveHeight, _Cutoff, _AnimationWeight;
                float4 _AlphaLightColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 view:TEXCOORD1; float3 normal:TEXCOORD2; };
            /// <summary>Animates living fish and prepares view-space shading attributes.</summary>
            /// <param name="i">Object-space mesh attributes.</param>
            /// <returns>Deformed clip position and interpolated surface inputs.</returns>
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float3 p = i.positionOS.xyz;
                float h = abs(p.z)/max(_WaveHeight, 0.01);
                /* Unity's OBJ import reflects source-local X. */
                p.x -= h*h*cos(_Time.y*10-h*6.283+_Phase)*2*_AnimationWeight;
                float3 world = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(world);
                o.view = mul(UNITY_MATRIX_V, float4(world,1)).xyz;
                o.normal = mul((float3x3)UNITY_MATRIX_V, TransformObjectToWorldNormal(i.normalOS));
                o.uv = i.uv;
                return o;
            }
            /// <summary>Shades the fish with distance atmosphere and authored texture coverage.</summary>
            /// <param name="i">Interpolated view-space surface attributes.</param>
            /// <returns>Output-space color and visibility coverage.</returns>
            half4 Frag(Varyings i):SV_Target
            {
                float distance = length(i.view);
                float fade = VisibilityFade(distance);
                clip(fade-.00001);
                half4 tex = SAMPLE_TEXTURE2D_LOD(_BaseMap,sampler_PointRepeat,i.uv,0);
                clip(tex.a - _Cutoff);
                float topLight = 1+dot(normalize(i.normal),normalize(float3(0,100,0)-i.view))/2.5;
                float3 col = ApplyDistanceTint(ToDisplayColor(tex.rgb)*EnvironmentAmbient()*topLight,distance);
                float pulse = .25*(cos(_Time.y*(10.0/3)+_Phase)+1);
                col += (1-tex.a)*pulse*_AlphaLightColor.rgb;
                col *= clamp(100/max(distance*distance,.001),1,2);
                return half4(ToOutputColor(col),min(pulse>0 ? 1 : tex.a,fade));
            }
            ENDHLSL
        }
    }
}
