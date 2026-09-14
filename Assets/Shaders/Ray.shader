Shader "DeepSky/Ray"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Phase ("Wing phase", Float) = 0
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
                float _Phase, _AnimationWeight;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 view:TEXCOORD1; float3 normal:TEXCOORD2; };
            /// <summary>Animates living ray wings and prepares view-space shading attributes.</summary>
            /// <param name="i">Object-space mesh attributes.</param>
            /// <returns>Deformed clip position and interpolated surface inputs.</returns>
            Varyings Vert(Attributes i)
            {
                Varyings o;
                float3 p = i.positionOS.xyz;
                float h = abs(p.x) / 4.5;
                p.y += h * h * cos(_Time.y * 3.5 - h * 6.283 + _Phase) * 2 * _AnimationWeight;
                float3 world = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(world);
                o.view = mul(UNITY_MATRIX_V, float4(world, 1)).xyz;
                /* The source shader lights with the undeformed mesh normals. */
                o.normal = mul((float3x3)UNITY_MATRIX_V, TransformObjectToWorldNormal(i.normalOS));
                o.uv = i.uv;
                return o;
            }
            /// <summary>Shades a ray with authored texture coverage and underwater distance tint.</summary>
            /// <param name="i">Interpolated view-space surface attributes.</param>
            /// <returns>Output-space color and distance-faded coverage.</returns>
            half4 Frag(Varyings i):SV_Target
            {
                float distance = length(i.view);
                float fade = VisibilityFade(distance);
                clip(fade-.00001);
                half4 tex = SAMPLE_TEXTURE2D_LOD(_BaseMap,sampler_PointRepeat,i.uv,0);
                float topLight = 1 + dot(normalize(i.normal), normalize(float3(0,100,0)-i.view))/2.5;
                float3 col = ApplyDistanceTint(ToDisplayColor(tex.rgb)*EnvironmentAmbient()*topLight,distance);
                col *= clamp(100/max(distance*distance,.001),1,2);
                return half4(ToOutputColor(col),min(tex.a,fade));
            }
            ENDHLSL
        }
    }
}
