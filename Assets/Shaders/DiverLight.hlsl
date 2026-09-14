#ifndef DEEPSKY_DIVER_LIGHT_INCLUDED
#define DEEPSKY_DIVER_LIGHT_INCLUDED

float4 _DiverLightPosition, _DiverLightDirection;

/// <summary>Evaluates the diver's torch cone and distance falloff.</summary>
/// <param name="worldPosition">Shaded position in world metres.</param>
/// <returns>Light intensity, zero when the torch is disabled or out of range.</returns>
float DiverTorch(float3 worldPosition)
{
    float3 toLight = _DiverLightPosition.xyz - worldPosition;
    float distanceToLight = length(toLight);
    float3 direction = toLight / max(distanceToLight, .0001);
    float cone = smoothstep(.86, .96, dot(-direction, _DiverLightDirection.xyz));
    return cone * saturate(1 - distanceToLight / 18) * _DiverLightPosition.w;
}

#endif
