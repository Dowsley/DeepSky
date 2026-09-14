Shader "DeepSky/Surface"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        [Header(Caustics)]
        _Caustics ("Caustics", 2D) = "black" {}
        _CausticsSecond ("Second caustic layer", 2D) = "black" {}
        _CausticTileSize ("Caustic tile size (metres)", Range(1,64)) = 12.8
        _CausticStrength ("Caustic strength", Range(0,1)) = 0
        _CausticScroll ("Caustic scroll (UV per second XY)", Vector) = (0.03384095,-0.02785515,0,0)
        [Header(Surface)]
        _NormalMap ("Raw RGB terrain normals", 2D) = "bump" {}
        _RockMap ("Rock terrain color", 2D) = "white" {}
        _RockNormal ("Rock terrain normals", 2D) = "bump" {}
        _AlgaSandMap ("Algae sand terrain color", 2D) = "white" {}
        _RockHighlightMap ("Chunk rock highlight colors", 2D) = "black" {}
        _RockHighlightGrid ("Highlight world XZ origin and extent", Vector) = (0,0,1,1)
        _FloorMapping ("Floor repeats per metre XY and offset ZW", Vector) = (.3125,.3125,0,0)
        _RockWorldScale ("Rock repeats per metre", Float) = 0.125
        _TerrainNormalStrength ("Terrain normal detail", Range(0,1)) = 1
        _TerrainBlend ("Vertex terrain blend", Float) = 0
        _Cutout ("Texture alpha coverage", Float) = 0
        _TextureAlpha ("Blended texture alpha", Float) = 0
        [ToggleUI] _SurfaceOverlay ("Surface overlay coverage", Float) = 0
        _OverlaySheen ("Mineral sheen", Range(0,1)) = 0
        [HDR] _OverlayEmissionColor ("Mineral emission color", Color) = (1,1,1,1)
        _DepthOffset ("Surface depth bias", Float) = 0
        [Toggle] _DepthWrite ("Write depth", Float) = 1
        _AlphaEmission ("Alpha encoded emission", Float) = 0
        _TopLight ("Top directional light", Float) = 0
        _Floor ("Terrain lighting", Float) = 0
        _ProximityLightStrength ("Terrain proximity light strength", Range(0,2)) = 1
        _WorldUV ("World texture scale", Float) = 0
        _Cutoff ("Alpha cutoff", Float) = 0.1
        [Header(Vegetation Motion)]
        _Sway ("Plant sway", Float) = 0
        [ToggleUI] _RootedSway ("Coherent rooted sway", Float) = 0
        _WaveHeight ("Plant wave height", Float) = 0
        _WaveAmplitude ("Plant wave amplitude", Float) = 2
        _UseAlphaAsHeight ("Vertex alpha encodes wave height", Float) = 0
        _WaveFactor ("Wave time factor", Float) = 1
        _CardSway ("Rooted card sway", Float) = 0
        _Fish ("Tail motion", Float) = 0
        _Emission ("Emission", Float) = 0
        _Glyph ("Alpha-only font texture", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite [_DepthWrite]
            Offset [_DepthOffset], [_DepthOffset]
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Atmosphere.hlsl"
            #include "Floor.hlsl"
            #include "PlantWave.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_Caustics); SAMPLER(sampler_Caustics);
            TEXTURE2D(_CausticsSecond); SAMPLER(sampler_CausticsSecond);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RockMap); SAMPLER(sampler_RockMap);
            TEXTURE2D(_RockNormal); SAMPLER(sampler_RockNormal);
            TEXTURE2D(_AlgaSandMap); SAMPLER(sampler_AlgaSandMap);
            TEXTURE2D(_RockHighlightMap); SAMPLER(sampler_RockHighlightMap);
            float4 _DiverLightPosition, _DiverLightDirection;
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor, _OverlayEmissionColor;
                float4 _RockHighlightGrid, _FloorMapping, _CausticScroll;
                float _CausticStrength, _WorldUV, _Cutoff, _Sway, _Fish, _Emission, _Glyph, _Floor;
                float _WaveHeight, _WaveAmplitude, _CardSway, _UseAlphaAsHeight, _WaveFactor, _RootedSway;
                float _TerrainBlend, _Cutout, _TextureAlpha, _AlphaEmission, _TopLight;
                float _RockWorldScale, _TerrainNormalStrength, _CausticTileSize;
                float _ProximityLightStrength;
                float _SurfaceOverlay, _OverlaySheen;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 swayData:TEXCOORD1; float4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; float projectionDepth:TEXCOORD3; float4 color:COLOR; };
            /// <summary>Deforms local geometry and prepares world-space surface attributes.</summary>
            /// <param name="v">Mesh attributes; batched plants store their alpha direction offset in UV2.x.</param>
            /// <returns>Clip position and interpolated inputs for surface shading.</returns>
            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 p = v.positionOS.xyz;
                float3 origin = TransformObjectToWorld(float3(0,0,0));
                float phase = origin.x * 0.7 + origin.z * 0.31;
                if (_RootedSway > .5)
                {
                    bool encoded = _UseAlphaAsHeight > .5;
                    float height = encoded ? v.color.a - v.swayData.x : p.y;
                    float plantPhase = encoded ? v.swayData.x * 1000 : phase;
                    p.xz += RootedPlantSway(height, _WaveHeight, _WaveAmplitude,
                        _WaveFactor, _Time.y, plantPhase);
                }
                else
                {
                    p.xz += PlantWave(p.y,v.color.a,_UseAlphaAsHeight,
                        _WaveHeight,_WaveAmplitude,_WaveFactor,_Time.y);
                }
                p.xz += _CardSway*v.uv.y*v.uv.y*sin(_Time.y*.75+phase)
                    * float2(cos(phase),sin(phase));
                p.x += _Sway * pow(max(p.y,0),1.5) * sin(_Time.y*0.65 + p.y*.55 + phase);
                p.z += _Sway * pow(max(p.y,0),1.5) * cos(_Time.y*.49 + p.y*.35 + phase) * .55;
                p.x += _Fish * sin(_Time.y*7 + p.z*4 + phase) * abs(p.z);
                o.world = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = _TerrainBlend > .5 ? saturate(v.color) : v.color;
                o.projectionDepth = v.swayData.y;
                return o;
            }
            /// <summary>Combines surface lighting, world-space caustics and depth atmosphere.</summary>
            /// <param name="i">Interpolated surface attributes, with world position in metres.</param>
            /// <returns>Output-space color and coverage, discarding clipped or invisible fragments.</returns>
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv = _WorldUV > 0 ? i.world.xz * _WorldUV : i.uv;
                if (_Floor > .5 && _SurfaceOverlay < .5)
                {
                    uv = FloorUV(i.world, 1, 0, _FloorMapping);
                }
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);
                if (_Floor > .5 && _SurfaceOverlay < .5)
                {
                    tex = SAMPLE_TEXTURE2D(_BaseMap,sampler_PointRepeat,uv);
                }
                tex.rgb = ToDisplayColor(tex.rgb);
                float sandWeight = lerp(1,i.color.r,_TerrainBlend);
                float rockWeight = _TerrainBlend * i.color.g;
                float algaSandWeight = _TerrainBlend * i.color.b;
                // Blend world-axis projections so vertical rock faces retain texture area.
                float3 rockWeights = pow(abs(normalize(i.normal)), 4);
                rockWeights /= max(dot(rockWeights, 1), 0.0001);
                float3 rockPosition = i.world * _RockWorldScale;
                half4 rockTex = SAMPLE_TEXTURE2D(_RockMap,sampler_PointRepeat,rockPosition.zy) * rockWeights.x
                    + SAMPLE_TEXTURE2D(_RockMap,sampler_PointRepeat,rockPosition.xz) * rockWeights.y
                    + SAMPLE_TEXTURE2D(_RockMap,sampler_PointRepeat,rockPosition.xy) * rockWeights.z;
                rockTex.rgb = ToDisplayColor(rockTex.rgb);
                half4 algaSandTex = SAMPLE_TEXTURE2D(_AlgaSandMap,sampler_PointRepeat,uv);
                algaSandTex.rgb = ToDisplayColor(algaSandTex.rgb);
                tex = tex*sandWeight + rockTex*rockWeight + algaSandTex*algaSandWeight;
                clip(lerp(1,tex.a,_Cutout) - _Cutoff);
                float3 normal = normalize(i.normal);
                float3 toEye = _WorldSpaceCameraPos - i.world;
                float distanceToCamera = length(toEye);
                float3 eyeDirection = toEye / max(distanceToCamera, .001);
                float3 tint = lerp(i.color.rgb,float3(1,1,1),_TerrainBlend);
                float3 albedo = lerp(tex.rgb,float3(1,1,1),_Glyph) * _BaseColor.rgb * tint;
                float3 light = EnvironmentAmbient();
                if (_Floor > .5)
                {
                    float3 surfaceNormal = normal;
                    if (_TerrainNormalStrength > 0)
                    {
                        float3 bump = (SAMPLE_TEXTURE2D(_NormalMap,sampler_PointRepeat,uv).rgb-.5)*(sandWeight+algaSandWeight)
                            + (SAMPLE_TEXTURE2D(_RockNormal,sampler_PointRepeat,uv).rgb-.5)*rockWeight;
                        bump = normalize(bump + float3(0, 0, 0.0001));
                        surfaceNormal = normalize(lerp(normal, FloorNormal(normal,bump), _TerrainNormalStrength));
                    }
                    float falloff = saturate((10 - distanceToCamera) * .4);
                    float diffuse = saturate(dot(surfaceNormal,eyeDirection));
                    float specular = diffuse > 0 ? saturate(dot(reflect(-eyeDirection,surfaceNormal),eyeDirection)) : 0;
                    light += _ProximityLightStrength * falloff
                        * (float3(.5,.7,.8)*diffuse + float3(.1,.15,.2)*specular);
                    light += dot(normal,eyeDirection)*.05;
                }
                else
                {
                    float nearby = clamp(100 / max(dot(toEye,toEye),.001), 1, 3);
                    float directional = 1 + _TopLight*dot(normal,normalize(toEye+float3(0,50,0))) / 1.5;
                    light *= nearby * directional;
                }
                // World-space projection keeps caustics independent of sand UVs and chunk boundaries.
                float2 causticUV = i.world.xz / max(_CausticTileSize, .001);
                float2 scroll = _Time.y * _CausticScroll.xy;
                float3 c0 = ToDisplayColor(SAMPLE_TEXTURE2D(_Caustics,sampler_Caustics,causticUV + scroll).rgb);
                float3 c1 = ToDisplayColor(SAMPLE_TEXTURE2D(_CausticsSecond,sampler_CausticsSecond,causticUV - scroll).rgb);
                float3 caustic = (c0+c1)*_CausticStrength*EnvironmentCaustics()*(_Floor > .5 ? 1 : saturate(normal.y));
                float3 toLight = _DiverLightPosition.xyz-i.world;
                float cone = smoothstep(.86,.96,dot(-normalize(toLight),_DiverLightDirection.xyz));
                float torch = cone*saturate(1-length(toLight)/18)*_DiverLightPosition.w;
                float3 chunkHighlight=SAMPLE_TEXTURE2D_LOD(_RockHighlightMap,sampler_RockHighlightMap,
                    RockHighlightUV(i.world,_RockHighlightGrid),0).rgb;
                float3 mineral=RockHighlight(chunkHighlight,tex.a,rockWeight);
                if (_SurfaceOverlay > .5 && _OverlaySheen > 0)
                {
                    // Keep mineral highlights inside distance tint and the overlay's alpha footprint.
                    float3 halfDirection = normalize(eyeDirection + normalize(float3(.3,1,.2)));
                    float highlight = pow(saturate(dot(normal,halfDirection)),12);
                    float grain = saturate(dot(tex.rgb,float3(.2126,.7152,.0722)));
                    mineral += lerp(albedo,_BaseColor.rgb,.5) * _OverlaySheen
                        * (.15 + highlight * grain) * saturate(EnvironmentAmbient()*2);
                }
                if (_SurfaceOverlay > .5)
                {
                    mineral += lerp(tex.rgb,float3(1,1,1),.5) * _OverlayEmissionColor.rgb * _Emission;
                }
                float3 col = ApplyDistanceTint(albedo * (light + torch) + caustic + mineral,distanceToCamera);
                if (_SurfaceOverlay < .5)
                {
                    col += albedo * _Emission;
                }
                col += (1-tex.a)*_AlphaEmission;
                float fade = VisibilityFade(distanceToCamera);
                clip(fade - .00001);
                float coverage = lerp(1,tex.a,_TextureAlpha);
                if (_SurfaceOverlay > .5)
                {
                    // Fade every projection boundary before copied terrain triangles end.
                    float footprint = length(float3(i.uv * 2 - 1, i.projectionDepth));
                    float margin = 1 - smoothstep(.65, 1, footprint);
                    coverage *= margin * smoothstep(.5,.75,i.color.a) * _BaseColor.a;
                    clip(coverage - .001);
                }
                return half4(ToOutputColor(col),min(fade,coverage));
            }
            ENDHLSL
        }
    }
}
