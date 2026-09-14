#ifndef DEEPSKY_INTERIOR_INCLUDED
#define DEEPSKY_INTERIOR_INCLUDED

int _InteriorVolumeCount;
StructuredBuffer<float4> _InteriorBounds;

// Bounds are disjoint room rows; dry gaps do not contribute water distance.
float WaterPathLength(float3 world)
{
    float3 delta = world - _WorldSpaceCameraPos;
    float distance = length(delta);
    float3 direction = delta / max(distance, .0001);
    float3 inverse = rcp(lerp(direction, float3(.000001,.000001,.000001), abs(direction) < .000001));
    float dryLength = 0;
    for (int i = 0; i < _InteriorVolumeCount; i++)
    {
        float3 a = (_InteriorBounds[i * 2].xyz - _WorldSpaceCameraPos) * inverse;
        float3 b = (_InteriorBounds[i * 2 + 1].xyz - _WorldSpaceCameraPos) * inverse;
        float3 near = min(a,b);
        float3 far = max(a,b);
        float entry = max(0, max(near.x,max(near.y,near.z)));
        float exit = min(distance, min(far.x,min(far.y,far.z)));
        dryLength += max(0, exit-entry);
    }
    return max(0,distance-dryLength);
}

bool PointInInterior(float3 world)
{
    for (int i = 0; i < _InteriorVolumeCount; i++)
    {
        if (all(world >= _InteriorBounds[i*2].xyz) && all(world < _InteriorBounds[i*2+1].xyz))
        {
            return true;
        }
    }
    return false;
}
#endif
