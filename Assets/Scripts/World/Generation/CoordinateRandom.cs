namespace DeepSky.World.Generation
{
    /// <summary>Stateless coordinate hashing for generation independent of chunk loading order.</summary>
    internal static class CoordinateRandom
    {
        /// <summary>Hashes a seed and integer coordinates without advancing a random generator.</summary>
        /// <param name="seed">World or derived population seed; all signed integer values are valid.</param>
        /// <param name="x">First integer coordinate or candidate index.</param>
        /// <param name="z">Second integer coordinate.</param>
        /// <param name="stream">Stable discriminator for separate generation decisions.</param>
        /// <returns>A repeatable value in [0, 1), quantized to 24 bits.</returns>
        /// <remarks>Integer overflow is intentional. Changing the hash changes seeded world content.</remarks>
        internal static float Value01(int seed, int x, int z, int stream)
        {
            unchecked
            {
                uint value = (uint)seed ^ (uint)x * 374761393u ^ (uint)z * 668265263u ^ (uint)stream * 2246822519u;
                value = (value ^ (value >> 13)) * 1274126177u;
                value ^= value >> 16;
                return (value & 0x00ffffffu) / 16777216f;
            }
        }
    }
}
