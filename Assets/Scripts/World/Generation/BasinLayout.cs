using System;
using UnityEngine;

namespace DeepSky.World.Generation
{
    /// <summary>
    /// Coarse, immutable basin elevations. Shallow terrain occupies the center, with deeper regions outward.
    /// Quantized regional relief creates level interiors; smoothing is confined to their boundaries.
    /// </summary>
    internal sealed class BasinLayout
    {
        private readonly float[,] heights;
        private readonly float size;
        private readonly float spacing;
        private readonly int side;

        /// <summary>Builds the coarse elevation lattice and smooths terrace boundaries.</summary>
        /// <param name="seed">Seed for boundary, corridor and terrace placement.</param>
        /// <param name="size">Positive world width in metres.</param>
        /// <param name="cellSize">Positive target lattice spacing in metres.</param>
        /// <param name="depths">Shelf depths in metres, ordered shallow to deep.</param>
        /// <param name="radii">Middle (X) and shallow (Y) outer radii as fractions of world width.</param>
        /// <param name="exteriorRadius">Deep shelf's outer radius as a fraction of world width.</param>
        /// <param name="exteriorDepth">Exterior terrain depth in metres.</param>
        /// <param name="irregularity">Boundary-noise displacement multiplier in metres.</param>
        /// <param name="boundaryWavelength">Positive boundary-noise wavelength in metres.</param>
        /// <param name="transitionWidth">Positive horizontal shelf-transition width in metres.</param>
        /// <param name="rampWidth">Positive transition width in metres within descent corridors.</param>
        /// <param name="rampAngularWidth">Positive corridor half-width in radians.</param>
        /// <param name="terraceWavelength">Positive terrace-noise wavelength in metres.</param>
        /// <param name="terraceDepth">Maximum additional terrace depth in metres.</param>
        /// <param name="terraceSteps">Positive number of terrace quantization steps.</param>
        /// <param name="smoothingPasses">Number of boundary-smoothing passes; zero disables smoothing.</param>
        internal BasinLayout(int seed, float size, float cellSize, Vector3 depths, Vector2 radii,
            float exteriorRadius, float exteriorDepth, float irregularity, float boundaryWavelength,
            float transitionWidth, float rampWidth, float rampAngularWidth,
            float terraceWavelength, float terraceDepth, int terraceSteps, int smoothingPasses)
        {
            this.size = size;
            side = Mathf.CeilToInt(size / cellSize) + 1;
            spacing = size / (side - 1);
            heights = new float[side, side];
            float routeAngle = CoordinateRandom.Value01(seed, 0, 0, 11) * Mathf.PI;
            for (int z = 0; z < side; z++)
            {
                for (int x = 0; x < side; x++)
                {
                    float wx = x * spacing - size * 0.5f;
                    float wz = z * spacing - size * 0.5f;
                    float radius = Mathf.Sqrt(wx * wx + wz * wz);
                    float angle = Mathf.Atan2(wz, wx);
                    float bend = TerrainNoise.Sample(wx / boundaryWavelength, wz / boundaryWavelength, seed, 21, 2, 0.7f) * irregularity;
                    float routeDistance = Mathf.Asin(Mathf.Abs(Mathf.Sin(angle - routeAngle)));
                    float route = 1f - Mathf.SmoothStep(0f, 1f, routeDistance / rampAngularWidth);
                    float width = Mathf.Lerp(transitionWidth, rampWidth, route);
                    float middle = Transition(radius + bend, size * radii.y, width);
                    float deep = Transition(radius + bend, size * radii.x, width);
                    float exterior = Transition(radius + bend, size * exteriorRadius, width);
                    float depth = depths.x + middle * (depths.y - depths.x)
                        + deep * (depths.z - depths.y) + exterior * (exteriorDepth - depths.z);
                    float relief = TerrainNoise.Sample(wx / terraceWavelength, wz / terraceWavelength, seed, 22, 1, 0.7f);
                    float level = Mathf.Floor(Mathf.Clamp01(relief * 2f + 0.5f) * terraceSteps) / terraceSteps;
                    heights[x, z] = -depth - level * terraceDepth * (1f - deep);
                }
            }

            for (int pass = 0; pass < smoothingPasses; pass++)
            {
                var source = (float[,])heights.Clone();
                for (int z = 1; z < side - 1; z++)
                {
                    for (int x = 1; x < side - 1; x++)
                    {
                        float minimum = Mathf.Min(source[x - 1, z], source[x + 1, z], source[x, z - 1], source[x, z + 1]);
                        float maximum = Mathf.Max(source[x - 1, z], source[x + 1, z], source[x, z - 1], source[x, z + 1]);
                        if (maximum - minimum > terraceDepth / terraceSteps * 0.5f)
                        {
                            heights[x, z] = (source[x, z] * 4f + source[x - 1, z] + source[x + 1, z]
                                + source[x, z - 1] + source[x, z + 1]) / 8f;
                        }
                    }
                }
            }
        }

        /// <summary>Interpolates the lattice with overshoot-suppressed bicubic sampling.</summary>
        /// <param name="x">World X coordinate in metres; out-of-domain taps use edge cells.</param>
        /// <param name="z">World Z coordinate in metres; out-of-domain taps use edge cells.</param>
        /// <returns>Basin world Y in metres, without dune or outcrop relief.</returns>
        internal float Height(float x, float z)
        {
            float gx = (x + size * 0.5f) / spacing;
            float gz = (z + size * 0.5f) / spacing;
            int ix = (int)Math.Floor(gx);
            int iz = (int)Math.Floor(gz);
            float u = gx - ix;
            float v = gz - iz;
            float a = Row(ix, iz - 1, u);
            float b = Row(ix, iz, u);
            float c = Row(ix, iz + 1, u);
            float d = Row(ix, iz + 2, u);
            return Cubic(a, b, c, d, v);
        }

        /// <summary>Maximum deviation from the center elevation over a circular coarse neighborhood.</summary>
        /// <param name="position">Center in world XZ metres.</param>
        /// <param name="radius">Nonnegative sampling radius in metres.</param>
        /// <returns>The largest sampled absolute height difference in metres.</returns>
        internal float Variation(Vector2 position, float radius)
        {
            float center = Height(position.x, position.y);
            float variation = 0f;
            int cells = Mathf.CeilToInt(radius / spacing);
            for (int z = -cells; z <= cells; z++)
            {
                for (int x = -cells; x <= cells; x++)
                {
                    if ((x * x + z * z) * spacing * spacing <= radius * radius)
                    {
                        variation = Mathf.Max(variation, Mathf.Abs(Height(position.x + x * spacing, position.y + z * spacing) - center));
                    }
                }
            }
            return variation;
        }

        /// <summary>Interpolates four taps along a lattice row, clamping indices at its boundaries.</summary>
        /// <param name="x">Lattice index at the start of the interpolated interval.</param>
        /// <param name="z">Lattice row index.</param>
        /// <param name="t">Fractional X position within the interval, in [0, 1].</param>
        /// <returns>The interpolated row elevation in metres.</returns>
        private float Row(int x, int z, float t)
        {
            z = Mathf.Clamp(z, 0, side - 1);
            return Cubic(heights[Mathf.Clamp(x - 1, 0, side - 1), z], heights[Mathf.Clamp(x, 0, side - 1), z],
                heights[Mathf.Clamp(x + 1, 0, side - 1), z], heights[Mathf.Clamp(x + 2, 0, side - 1), z], t);
        }

        /// <summary>Evaluates Catmull-Rom interpolation constrained to the two central elevations.</summary>
        /// <param name="a">Elevation preceding the interval.</param>
        /// <param name="b">Elevation at the interval start.</param>
        /// <param name="c">Elevation at the interval end.</param>
        /// <param name="d">Elevation following the interval.</param>
        /// <param name="t">Fractional position within the interval, in [0, 1].</param>
        /// <returns>An elevation between b and c, without interpolation overshoot.</returns>
        private static float Cubic(float a, float b, float c, float d, float t)
        {
            float value = b + 0.5f * t * (c - a + t * (2f * a - 5f * b + 4f * c - d + t * (3f * (b - c) + d - a)));
            return Mathf.Clamp(value, Mathf.Min(b, c), Mathf.Max(b, c));
        }

        /// <summary>Calculates a smooth shelf transition centered on its authored boundary.</summary>
        /// <param name="distance">Perturbed radial distance from the world center in metres.</param>
        /// <param name="radius">Boundary radius in metres.</param>
        /// <param name="width">Positive transition width in metres.</param>
        /// <returns>A blend weight from zero on the inner shelf to one on the outer shelf.</returns>
        private static float Transition(float distance, float radius, float width)
        {
            return Mathf.SmoothStep(0f, 1f, (distance - radius) / width + 0.5f);
        }
    }
}
