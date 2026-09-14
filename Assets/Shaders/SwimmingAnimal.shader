Shader "DeepSky/SwimmingAnimal"
{
    Properties
    {
        _BaseMap ("Skin", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _TailStart ("Tail root Z", Float) = 0
        _TailLength ("Tail length", Float) = 2
        _TailAmplitude ("Tail displacement", Float) = .3
        _TailFrequency ("Tail cycles per second", Float) = .8
        _SwimAxis ("Tail motion axis", Vector) = (1,0,0,0)
        _Phase ("Phase", Float) = 0
        _AnimationWeight ("Swimming animation weight", Range(0,1)) = 1
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
                float4 _BaseColor, _SwimAxis;
                float _TailStart, _TailLength, _TailAmplitude, _TailFrequency, _Phase, _AnimationWeight;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 view:TEXCOORD1; float3 normal:TEXCOORD2; };

            /// <summary>Displaces the tail progressively while keeping the head stable.</summary>
            /// <param name="i">Mesh coordinates with the nose along positive Z.</param>
            /// <returns>Animated position, texture coordinates and shading inputs.</returns>
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float3 p = i.positionOS.xyz;
                float tail = saturate((_TailStart - p.z) / max(_TailLength, .01));
                float wave = sin(_Time.y * _TailFrequency * 6.283185 - tail * 2 + _Phase);
                p += _SwimAxis.xyz * wave * tail * tail * _TailAmplitude * _AnimationWeight;
                float3 world = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(world);
                o.view = mul(UNITY_MATRIX_V, float4(world,1)).xyz;
                o.normal = mul((float3x3)UNITY_MATRIX_V, TransformObjectToWorldNormal(i.normalOS));
                o.uv = i.uv;
                return o;
            }

            /// <summary>Shades opaque skin with the shared underwater distance treatment.</summary>
            /// <param name="i">Interpolated view-space surface inputs.</param>
            /// <returns>Output-space colour and distance visibility.</returns>
            half4 Frag(Varyings i):SV_Target
            {
                float distance = length(i.view);
                float fade = VisibilityFade(distance);
                clip(fade - .00001);
                float3 skin = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, i.uv, 0).rgb * _BaseColor.rgb;
                float light = 1 + dot(normalize(i.normal), normalize(float3(0,100,0) - i.view)) / 2.5;
                float3 colour = ApplyDistanceTint(ToDisplayColor(skin) * EnvironmentAmbient() * light, distance);
                colour *= clamp(100 / max(distance * distance, .001), 1, 2);
                return half4(ToOutputColor(colour), fade);
            }
            ENDHLSL
        }
    }
}
