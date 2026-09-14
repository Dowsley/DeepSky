#ifndef DEEPSKY_FLOOR_INCLUDED
#define DEEPSKY_FLOOR_INCLUDED

float2 FloorUV(float3 world, float scale, float2 scroll, float4 mapping)
{
    /* Absolute XZ coordinates preserve texture continuity across chunk boundaries. */
    return (world.xz * mapping.xy + mapping.zw) * scale + float2(scroll.x, -scroll.y);
}

float2 RockHighlightUV(float3 world, float4 grid)
{
    return (world.xz - grid.xy) / max(grid.zw, 1);
}

float3 RockHighlight(float3 chunkColor, float blendedAlpha, float rockWeight)
{
    return rockWeight>0 ? max(0,1-blendedAlpha)*chunkColor : 0;
}

float3 FloorNormal(float3 normal, float3 bump)
{
    float3 tangent = cross(normal, float3(0, 0, 1));
    if (dot(tangent, tangent) < .00000001) tangent = float3(0, -1, 0);
    tangent = normalize(tangent);
    /* Reflect the original tangent frame through Z; cross products reverse handedness. */
    float3 bitangent = -cross(normal, tangent);
    return normalize(tangent * bump.x + bitangent * bump.y + normal * bump.z);
}

#endif
