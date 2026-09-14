using UnityEngine;

namespace DeepSky.Rendering
{
    internal static class ShaderColors
    {
        internal static void SetDisplayTint(MaterialPropertyBlock properties, int propertyId,
            Color tint, float brightness, Vector2 brightnessRange)
        {
            float multiplier = Mathf.Lerp(brightnessRange.x, brightnessRange.y, brightness);
            // Surface.shader shades in display space; SetColor would convert these RGB values to linear.
            properties.SetVector(propertyId,
                new Vector4(tint.r * multiplier, tint.g * multiplier, tint.b * multiplier, tint.a));
        }
    }
}
