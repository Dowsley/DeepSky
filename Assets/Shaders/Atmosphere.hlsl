#ifndef DEEPSKY_ATMOSPHERE_INCLUDED
#define DEEPSKY_ATMOSPHERE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

float _DepthAtmosphereActive;
float4 _DepthWaterUpper, _DepthWaterLower, _DepthAmbient;
float4 _DepthVisibilityCaustics, _DepthFogParameters, _DepthCloudMultiplier;

float3 EnvironmentAmbient()
{
    return lerp(float3(.26, .48, .56), _DepthAmbient.rgb, _DepthAtmosphereActive);
}

float EnvironmentCaustics()
{
    return lerp(1, _DepthVisibilityCaustics.y, _DepthAtmosphereActive);
}

float VisibilityFade(float distanceMeters)
{
    float limit = lerp(50, _DepthVisibilityCaustics.x, _DepthAtmosphereActive);
    float width = lerp(4, _DepthFogParameters.z, _DepthAtmosphereActive);
    return saturate((limit - distanceMeters) / max(width, .0001));
}

float4 CloudDepthMultiplier()
{
    return lerp(float4(1, 1, 1, 1), _DepthCloudMultiplier, _DepthAtmosphereActive);
}

/* Display-space shading preserves the reference renderer's texture arithmetic. */
float3 ToDisplayColor(float3 color)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
        return color;
    #else
        return LinearToSRGB(color);
    #endif
}

float3 ToOutputColor(float3 color)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
        return color;
    #else
        return SRGBToLinear(max(color, 0));
    #endif
}

float3 WaterColor(float vertical)
{
    float3 lower = lerp(float3(.125, .415, .55), _DepthWaterLower.rgb, _DepthAtmosphereActive);
    float3 upper = lerp(float3(.175, .57, .74), _DepthWaterUpper.rgb, _DepthAtmosphereActive);
    return lerp(lower, upper, saturate(vertical / .3 + .5));
}

float3 ApplyDistanceTint(float3 color, float distanceMeters)
{
    float limit = lerp(50, _DepthVisibilityCaustics.x, _DepthAtmosphereActive);
    float width = lerp(40, _DepthFogParameters.x, _DepthAtmosphereActive);
    float minimum = lerp(.1, _DepthFogParameters.y, _DepthAtmosphereActive);
    float retained = clamp((limit - distanceMeters) / max(width, .0001), minimum, 1);
    return lerp(WaterColor(0), color, retained);
}

#endif
