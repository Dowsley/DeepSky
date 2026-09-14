#ifndef DEEPSKY_ENTRY_WATER_OPTICS_INCLUDED
#define DEEPSKY_ENTRY_WATER_OPTICS_INCLUDED

TEXTURE2D_X_FLOAT(_EntrySceneDepth);
TEXTURE2D_X(_EntrySceneColor);

/// <summary>Reads completed scenery, including transparent terrain and underwater effects.</summary>
/// <param name="uv">Normalized screen coordinates.</param>
/// <returns>Display-space scene color at the supplied pixel.</returns>
float3 EntrySceneColor(float2 uv)
{
    return ToDisplayColor(SAMPLE_TEXTURE2D_X_LOD(_EntrySceneColor,sampler_LinearClamp,
        UnityStereoTransformScreenSpaceTex(uv),0).rgb);
}

/// <summary>Reads captured scene depth in camera-space metres.</summary>
/// <param name="uv">Normalized screen coordinates.</param>
/// <returns>Positive eye depth, using the camera's projection convention.</returns>
float EntrySceneEyeDepth(float2 uv)
{
    float depth=SAMPLE_TEXTURE2D_X_LOD(_EntrySceneDepth,sampler_PointClamp,
        UnityStereoTransformScreenSpaceTex(uv),0).r;
    if (unity_OrthoParams.w>0)
    {
        #if UNITY_REVERSED_Z
            depth=1-depth;
        #endif
        return lerp(_ProjectionParams.y,_ProjectionParams.z,depth);
    }
    return LinearEyeDepth(depth,_ZBufferParams);
}

/// <summary>Reconstructs the depth-writing surface behind a screen pixel.</summary>
/// <param name="uv">Normalized screen coordinates.</param>
/// <returns>World-space position in metres.</returns>
float3 EntryScenePosition(float2 uv)
{
    float depth=SAMPLE_TEXTURE2D_X_LOD(_EntrySceneDepth,sampler_PointClamp,
        UnityStereoTransformScreenSpaceTex(uv),0).r;
    #if !UNITY_REVERSED_Z
        depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
    #endif
    return ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
}

/// <summary>Finds reflected depth-writing scenery within a short screen-space ray.</summary>
/// <param name="origin">World-space surface point offset toward the camera side.</param>
/// <param name="direction">Normalized reflected world-space direction.</param>
/// <returns>Display-space reflected color and confidence; misses fade into the material's fallback.</returns>
float4 EntryReflection(float3 origin,float3 direction)
{
    float previousDistance=.04;
    float previousGap=-.01;
    [loop]
    for (int step=0;step<24;step++)
    {
        float distance=.06+step*.10+step*step*.012;
        float3 rayPosition=origin+direction*distance;
        float4 clip=TransformWorldToHClip(rayPosition);
        if (clip.w<=0)
        {
            break;
        }
        float4 screen=ComputeScreenPos(clip);
        float2 uv=screen.xy/screen.w;
        if (any(uv<=0) || any(uv>=1))
        {
            break;
        }
        float gap=-TransformWorldToView(rayPosition).z-EntrySceneEyeDepth(uv);
        if (gap>=0 && previousGap<0 && gap<.4)
        {
            float low=previousDistance;
            float high=distance;
            [unroll]
            for (int refine=0;refine<3;refine++)
            {
                float mid=(low+high)*.5;
                float3 samplePoint=origin+direction*mid;
                float4 projected=ComputeScreenPos(TransformWorldToHClip(samplePoint));
                uv=projected.xy/projected.w;
                float delta=-TransformWorldToView(samplePoint).z-EntrySceneEyeDepth(uv);
                if (delta>0)
                {
                    high=mid;
                }
                else
                {
                    low=mid;
                }
            }
            float edge=min(min(uv.x,uv.y),min(1-uv.x,1-uv.y));
            float confidence=saturate(edge*12)*saturate(distance/.3);
            return float4(EntrySceneColor(uv),confidence);
        }
        previousGap=gap;
        previousDistance=distance;
    }
    return 0;
}

#endif
