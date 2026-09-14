using System;
using UnityEngine;

namespace DeepSky.World.Generation
{
    /// <summary>Seeded, spatially smoothed value noise with additive, unnormalized octaves.</summary>
    internal static class TerrainNoise
    {
        /// <summary>Adds cosine-interpolated, spatially smoothed octaves without amplitude normalization.</summary>
        /// <param name="x">X coordinate in base-octave grid units.</param>
        /// <param name="z">Z coordinate in base-octave grid units.</param>
        /// <param name="seed">Seed controlling the lattice values.</param>
        /// <param name="stream">Discriminator separating independent noise fields.</param>
        /// <param name="octaves">Number of octaves; zero or negative produces zero.</param>
        /// <param name="persistence">Amplitude multiplier per octave; typically between zero and one.</param>
        /// <returns>A signed sum whose magnitude is bounded by the sum of absolute octave amplitudes.</returns>
        internal static float Sample(float x, float z, int seed, int stream, int octaves, float persistence)
        {
            float result = 0f;
            float amplitude = 1f;
            for (int octave = 0; octave < octaves; octave++)
            {
                int ix = (int)Math.Floor(x);
                int iz = (int)Math.Floor(z);
                float u = (1f - (float)Math.Cos((x - ix) * Math.PI)) * 0.5f;
                float v = (1f - (float)Math.Cos((z - iz) * Math.PI)) * 0.5f;
                float south = Mathf.LerpUnclamped(Smooth(ix, iz, seed, stream), Smooth(ix + 1, iz, seed, stream), u);
                float north = Mathf.LerpUnclamped(Smooth(ix, iz + 1, seed, stream), Smooth(ix + 1, iz + 1, seed, stream), u);
                result += Mathf.LerpUnclamped(south, north, v) * amplitude;
                amplitude *= persistence;
                x *= 2f;
                z *= 2f;
            }
            return result;
        }

        /// <summary>Averages a lattice point with its eight neighbors using a separable weighted kernel.</summary>
        /// <param name="x">Integer lattice X coordinate.</param>
        /// <param name="z">Integer lattice Z coordinate.</param>
        /// <param name="seed">Seed controlling the lattice values.</param>
        /// <param name="stream">Discriminator separating independent noise fields.</param>
        /// <returns>The weighted signed lattice value in [-1, 1).</returns>
        private static float Smooth(int x, int z, int seed, int stream)
        {
            float center = Value(x, z, seed, stream) * 0.25f;
            float edges = (Value(x - 1, z, seed, stream) + Value(x + 1, z, seed, stream)
                + Value(x, z - 1, seed, stream) + Value(x, z + 1, seed, stream)) * 0.125f;
            float corners = (Value(x - 1, z - 1, seed, stream) + Value(x + 1, z - 1, seed, stream)
                + Value(x - 1, z + 1, seed, stream) + Value(x + 1, z + 1, seed, stream)) * 0.0625f;
            return center + edges + corners;
        }

        /// <summary>Samples one signed lattice value without changing random state.</summary>
        /// <param name="x">Integer lattice X coordinate.</param>
        /// <param name="z">Integer lattice Z coordinate.</param>
        /// <param name="seed">Seed controlling the lattice values.</param>
        /// <param name="stream">Discriminator separating independent noise fields.</param>
        /// <returns>A deterministic value in [-1, 1).</returns>
        private static float Value(int x, int z, int seed, int stream)
        {
            return CoordinateRandom.Value01(seed, x, z, stream) * 2f - 1f;
        }
    }
}
