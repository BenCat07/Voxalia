//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Voxalia.ServerGame.WorldSystem
{
    /// <summary>
    /// Abstractly represents a service that generates biomes.
    /// </summary>
    public abstract class BiomeGenerator
    {
        /// <summary>
        /// Gets the temperature at a specific location, using two seeds.
        /// </summary>
        /// <param name="seed2">The first seed.</param>
        /// <param name="seed3">The second seed.</param>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The temperature.</returns>
        public abstract double GetTemperature(int seed2, int seed3, double x, double y);

        /// <summary>
        /// Gets the downfall rate at a specific location, using two seeds.
        /// </summary>
        /// <param name="seed3">The first seed.</param>
        /// <param name="seed4">The second seed.</param>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <returns>The downfall rate.</returns>
        public abstract double GetDownfallRate(int seed3, int seed4, double x, double y);

        /// <summary>
        /// Gets the biome at a specific location, using three seeds.
        /// </summary>
        /// <param name="seed2">The first seed.</param>
        /// <param name="seed3">The second seed.</param>
        /// <param name="seed4">The third seed.</param>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <param name="height">The generated approximate terrain height at the location.</param>
        /// <returns>A biome.</returns>
        public abstract Biome BiomeFor(int seed2, int seed3, int seed4, double x, double y, double z, double height);
    }
}
