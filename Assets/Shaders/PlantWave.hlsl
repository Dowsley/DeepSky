#ifndef DEEPSKY_PLANT_WAVE_INCLUDED
#define DEEPSKY_PLANT_WAVE_INCLUDED

/// <summary>Offsets vegetation horizontally with a height-dependent traveling wave.</summary>
/// <param name="localHeight">Vertex height in object-space units.</param>
/// <param name="vertexAlpha">Authored height and direction seed when encodedHeight is enabled.</param>
/// <param name="encodedHeight">Values above 0.5 select vertexAlpha instead of localHeight.</param>
/// <param name="waveHeight">Height normalization in the selected height units; nonpositive disables motion.</param>
/// <param name="amplitude">Horizontal displacement in object-space units at normalized height one.</param>
/// <param name="factor">Time multiplier; one corresponds to 0.5 radians per second.</param>
/// <param name="time">Animation time in seconds.</param>
/// <returns>Object-space XZ displacement.</returns>
float2 PlantWave(float localHeight, float vertexAlpha, float encodedHeight,
    float waveHeight, float amplitude, float factor, float time)
{
    if (waveHeight <= 0)
    {
        return 0;
    }
    bool encoded = encodedHeight > .5;
    float height = (encoded ? vertexAlpha : localHeight) / waveHeight;
    float direction = encoded ? vertexAlpha * 1000 : 1000;
    /* Imported OBJ meshes reflect X; source-coordinate cards reflect Z. */
    float2 reflection = encoded ? float2(1,-1) : float2(-1,1);
    float wave = height * height * cos(time * .5 * factor - height * 6.283) * amplitude;
    return wave * float2(cos(direction), sin(direction)) * reflection;
}

/// <summary>Bends a rooted plant in one direction at each instant, without opposing height phases.</summary>
/// <param name="localHeight">Root-relative height, either object-space or authored normalized height; nonpositive stays fixed.</param>
/// <param name="waveHeight">Normalization in the supplied height units; nonpositive disables motion.</param>
/// <param name="amplitude">Maximum XZ displacement at waveHeight, in object-space units.</param>
/// <param name="factor">Time multiplier; one corresponds to 0.5 radians per second.</param>
/// <param name="time">Animation time in seconds.</param>
/// <param name="phase">Per-plant phase and horizontal direction in radians, shared by every vertex.</param>
/// <returns>Object-space XZ displacement with a quadratic bend weight above the root.</returns>
float2 RootedPlantSway(float localHeight, float waveHeight, float amplitude,
    float factor, float time, float phase)
{
    if (waveHeight <= 0)
    {
        return 0;
    }
    float height = max(localHeight, 0) / waveHeight;
    float sway = height * height * sin(time * .5 * factor + phase) * amplitude;
    return sway * float2(cos(phase), sin(phase));
}

#endif
