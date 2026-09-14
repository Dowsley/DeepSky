using System;

namespace DeepSky.World.Generation
{
    /// <summary>Small basin dunes and independently thresholded rocky outcrops, in metres.</summary>
    internal sealed class TerrainRelief
    {
        private readonly int seed;
        private readonly float duneWavelength;
        private readonly float duneHeight;
        private readonly int duneOctaves;
        private readonly float persistence;
        private readonly float outcropWavelength;
        private readonly float outcropHeight;
        private readonly int outcropOctaves;
        private readonly float outcropThreshold;
        private readonly float outcropSoftCap;
        private readonly float outcropCompression;

        /// <summary>Captures the independent dune and outcrop noise settings.</summary>
        /// <param name="seed">Seed shared by both noise fields, separated by stream identifiers.</param>
        /// <param name="duneWavelength">Base dune wavelength in metres, clamped to at least one.</param>
        /// <param name="duneHeight">Dune amplitude multiplier in metres; not a peak-height guarantee.</param>
        /// <param name="duneOctaves">Number of additive dune octaves.</param>
        /// <param name="persistence">Amplitude multiplier per successive octave.</param>
        /// <param name="outcropWavelength">Base outcrop wavelength in metres, clamped to at least one.</param>
        /// <param name="outcropHeight">Outcrop amplitude multiplier in metres after thresholding.</param>
        /// <param name="outcropOctaves">Number of additive outcrop octaves.</param>
        /// <param name="outcropThreshold">Noise value below which outcrops have zero height.</param>
        /// <param name="outcropSoftCap">Nonnegative height in metres where compression begins.</param>
        /// <param name="outcropCompression">Divisor for height above the cap, clamped to at least one.</param>
        internal TerrainRelief(int seed, float duneWavelength, float duneHeight, int duneOctaves,
            float persistence, float outcropWavelength, float outcropHeight, int outcropOctaves, float outcropThreshold,
            float outcropSoftCap, float outcropCompression)
        {
            this.seed = seed;
            this.duneWavelength = Math.Max(1f, duneWavelength);
            this.duneHeight = duneHeight;
            this.duneOctaves = duneOctaves;
            this.persistence = persistence;
            this.outcropWavelength = Math.Max(1f, outcropWavelength);
            this.outcropHeight = outcropHeight;
            this.outcropOctaves = outcropOctaves;
            this.outcropThreshold = outcropThreshold;
            this.outcropSoftCap = outcropSoftCap;
            this.outcropCompression = Math.Max(1f, outcropCompression);
        }

        /// <summary>Samples additive dune relief independently of the basin elevation.</summary>
        /// <param name="x">World X coordinate in metres.</param>
        /// <param name="z">World Z coordinate in metres.</param>
        /// <returns>A signed vertical offset in metres.</returns>
        internal float DuneHeight(float x, float z)
        {
            return TerrainNoise.Sample(x / duneWavelength, z / duneWavelength, seed, 31, duneOctaves, persistence)
                * duneHeight;
        }

        /// <summary>Samples thresholded outcrops with compressed peaks.</summary>
        /// <param name="x">World X coordinate in metres.</param>
        /// <param name="z">World Z coordinate in metres.</param>
        /// <returns>A nonnegative vertical offset in metres for nonnegative authored heights and cap.</returns>
        internal float OutcropHeight(float x, float z)
        {
            float value = TerrainNoise.Sample(x / outcropWavelength, z / outcropWavelength, seed, 32, outcropOctaves, persistence);
            float height = Math.Max(0f, value - outcropThreshold) * outcropHeight;
            return Math.Min(height, outcropSoftCap) + Math.Max(0f, height - outcropSoftCap) / outcropCompression;
        }
    }
}
