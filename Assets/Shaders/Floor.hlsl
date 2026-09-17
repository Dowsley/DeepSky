#ifndef DEEPSKY_FLOOR_INCLUDED
#define DEEPSKY_FLOOR_INCLUDED

/// <summary>Maps absolute ground coordinates to continuous texture coordinates.</summary>
/// <param name="world">World position in metres.</param>
/// <param name="scale">Additional repeat multiplier.</param>
/// <param name="scroll">Texture-coordinate translation.</param>
/// <param name="mapping">XY repeats per metre and ZW texture offset.</param>
/// <returns>Texture coordinates shared across terrain chunks.</returns>
float2 FloorUV(float3 world, float scale, float2 scroll, float4 mapping)
{
    /* Absolute XZ coordinates preserve texture continuity across chunk boundaries. */
    return (world.xz * mapping.xy + mapping.zw) * scale + float2(scroll.x, -scroll.y);
}

/// <summary>Maps a world position into a highlight grid.</summary>
/// <param name="world">World position in metres.</param>
/// <param name="grid">XZ origin followed by XZ extent in metres.</param>
/// <returns>Highlight texture coordinates.</returns>
float2 RockHighlightUV(float3 world, float4 grid)
{
    return (world.xz - grid.xy) / max(grid.zw, 1);
}

/// <summary>Weights a rock highlight by the texture's emission mask.</summary>
/// <param name="chunkColor">Display-space highlight color.</param>
/// <param name="blendedAlpha">Inverse emission coverage.</param>
/// <param name="rockWeight">Positive when rock contributes to the surface.</param>
/// <returns>Display-space light contribution.</returns>
float3 RockHighlight(float3 chunkColor, float blendedAlpha, float rockWeight)
{
    return rockWeight>0 ? max(0,1-blendedAlpha)*chunkColor : 0;
}

/// <summary>Transforms a tangent normal into the floor's surface frame.</summary>
/// <param name="normal">Normalized world-space geometry normal.</param>
/// <param name="bump">Decoded tangent-space normal.</param>
/// <returns>Normalized world-space shading normal.</returns>
float3 FloorNormal(float3 normal, float3 bump)
{
    float3 tangent = cross(normal, float3(0, 0, 1));
    if (dot(tangent, tangent) < .00000001)
    {
        tangent = float3(0, -1, 0);
    }
    tangent = normalize(tangent);
    // The floor maps positive texture V along world Z.
    float3 bitangent = -cross(normal, tangent);
    return normalize(tangent * bump.x + bitangent * bump.y + normal * bump.z);
}

/// <summary>Decodes linear RGB normals into bounded texture-coordinate slopes.</summary>
/// <param name="rgb">Unpacked OpenGL normal texture, sampled without sRGB conversion.</param>
/// <returns>XY perturbation relative to the normal's Z component.</returns>
float2 TerrainNormalSlope(float3 rgb)
{
    float3 bump = rgb * 2 - 1;
    return bump.xy / max(bump.z, .25);
}

/// <summary>Projects blended texture slopes onto the geometric surface.</summary>
/// <param name="normal">Normalized world-space geometry normal.</param>
/// <param name="slope">World-axis perturbation from the matching color-map projections.</param>
/// <param name="strength">Nonnegative normal-detail multiplier; zero preserves geometry.</param>
/// <returns>Normalized world-space shading normal, including on vertical faces.</returns>
float3 TerrainDetailNormal(float3 normal, float3 slope, float strength)
{
    float3 surfaceSlope = slope - normal * dot(slope, normal);
    return normalize(normal + surfaceSlope * strength);
}

#endif
