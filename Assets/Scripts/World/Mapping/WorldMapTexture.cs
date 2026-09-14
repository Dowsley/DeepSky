using DeepSky.World.Generation;
using UnityEngine;

namespace DeepSky.World.Mapping
{
    /// <summary>North-up depth chart sampled from the terrain formula, including unloaded chunks.</summary>
    public static class WorldMapTexture
    {
        /// <summary>
        /// Creates an owned, transient texture on the main thread. The caller must destroy it.
        /// UV (0, 0) is the southwest world corner; UV (1, 1) is the northeast corner.
        /// </summary>
        /// <param name="world">Generation snapshot defining terrain elevations and map bounds.</param>
        /// <param name="resolution">Texture width and height in pixels, clamped to [32, 1024].</param>
        /// <param name="shallowColor">Chart color at sea level.</param>
        /// <param name="deepColor">Chart color at the world's maximum reference depth.</param>
        /// <param name="contourInterval">Depth spacing in metres between dark contour bands; nonpositive disables them.</param>
        /// <returns>A new RGB texture with CPU pixel data discarded after upload; caller owns its lifetime.</returns>
        public static Texture2D Create(WorldData world, int resolution, Color shallowColor,
            Color deepColor, float contourInterval)
        {
            resolution = Mathf.Clamp(resolution, 32, 1024);
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false)
            {
                name = "World depth chart",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[resolution * resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float wx = ((x + 0.5f) / resolution - 0.5f) * world.Size;
                    float wz = ((z + 0.5f) / resolution - 0.5f) * world.Size;
                    float depth = -world.Height(wx, wz);
                    Color color = Color.Lerp(shallowColor, deepColor, Mathf.Clamp01(depth / world.MaximumDepth));
                    if (contourInterval > 0f && Mathf.Repeat(depth, contourInterval) < contourInterval * 0.05f)
                    {
                        color *= 0.6f;
                    }

                    pixels[z * resolution + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>Converts world XZ metres to north-up chart UVs without clamping.</summary>
        /// <param name="world">Generation snapshot defining the centered world square.</param>
        /// <param name="position">World XZ coordinates in metres.</param>
        /// <returns>Chart UVs, potentially outside [0, 1] for positions outside the world.</returns>
        public static Vector2 ToUV(WorldData world, Vector2 position)
        {
            return position / world.Size + Vector2.one * 0.5f;
        }
    }
}
