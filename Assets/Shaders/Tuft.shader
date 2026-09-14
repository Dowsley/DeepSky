Shader "DeepSky/Tuft"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Display-space tint", Vector) = (1,1,1,1)
        _WaveHeight ("Plant wave height", Float) = 2
        _WaveAmplitude ("Plant wave amplitude", Float) = .5
        _WaveFactor ("Wave time factor", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+1" }
        Pass
        {
            Tags { "LightMode"="UnderwaterEffects" }
            Cull Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _DiverLightPosition, _DiverLightDirection;
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor;
                float _WaveHeight, _WaveAmplitude, _WaveFactor;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                float2 uv:TEXCOORD0;
                float4 color:COLOR;
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 world:TEXCOORD0;
                float2 uv:TEXCOORD1;
                float3 tint:TEXCOORD2;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 position=input.positionOS.xyz;
                float height=input.color.a/_WaveHeight;
                float direction=input.color.a*1000;
                float wave=height*height*cos(_Time.y*.5*_WaveFactor-height*6.283185)*_WaveAmplitude;
                position.xz+=wave*float2(cos(direction),-sin(direction));
                output.world=TransformObjectToWorld(position);
                output.positionCS=TransformWorldToHClip(output.world);
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
                output.tint=input.color.rgb;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 textureColor=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv);
                float3 toEye=_WorldSpaceCameraPos-input.world;
                float distanceToCamera=length(toEye);
                float nearby=clamp(100/max(dot(toEye,toEye),.001),1,3);
                float3 light=EnvironmentAmbient()*nearby;
                float3 toLight=_DiverLightPosition.xyz-input.world;
                float cone=smoothstep(.86,.96,dot(-normalize(toLight),_DiverLightDirection.xyz));
                light+=cone*saturate(1-length(toLight)/18)*_DiverLightPosition.w;
                float3 albedo=ToDisplayColor(textureColor.rgb)*_BaseColor.rgb*input.tint;
                float3 color=ApplyDistanceTint(albedo*light,distanceToCamera);
                float fade=VisibilityFade(distanceToCamera);
                clip(fade-.00001);
                float alpha=min(fade,textureColor.a);
                return half4(color*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
