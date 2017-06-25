//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.ServerGame.WorldSystem.SimpleGenerator.Biomes;
using FreneticGameCore;

namespace Voxalia.ServerGame.WorldSystem.SimpleGenerator
{
    public class SimpleBiomeGenerator: BiomeGenerator
    {
        public double TemperatureMapTwoSize = 8000;

        public double TemperatureMapSize = 1200;

        public double DownfallMapSize = 2400;

        public override double GetTemperature(int seed2, int seed3, double x, double y)
        {
            double tempA = SimplexNoise.Generate(seed2 + (x / TemperatureMapSize), seed3 + (y / TemperatureMapSize));
            double tempB = SimplexNoise.Generate(seed3 + (x / TemperatureMapSize), seed2 + (y / TemperatureMapSize));
            double temp2A = SimplexNoise.Generate(seed2 + seed3 + (x / TemperatureMapTwoSize), seed3 - seed2 + (y / TemperatureMapTwoSize));
            double temp2 = (temp2A * temp2A) * 2.0 - 1.0;
            return ((tempA - 0.5) * (tempB - 0.5) * 2.0 + 0.5) * 90.0 + temp2 * 40.0;
        }

        public override double GetDownfallRate(int seed3, int seed4, double x, double y)
        {
            return SimplexNoise.Generate((double)seed3 + (x / DownfallMapSize), (double)seed4 + (y / DownfallMapSize));
        }

        public SimpleRainForestBiome RainForest = new SimpleRainForestBiome();

        public SimpleForestBiome Forest = new SimpleForestBiome();

        public SimpleSwampBiome Swamp = new SimpleSwampBiome();

        public SimplePlainsBiome Plains = new SimplePlainsBiome();

        public SimpleDesertBiome Desert = new SimpleDesertBiome();

        public SimpleIcyBiome Icy = new SimpleIcyBiome();

        public SimpleSnowBiome Snow = new SimpleSnowBiome();

        SimpleLightForestHillBiome LightForestHill = new SimpleLightForestHillBiome();

        SimpleMountainBiome Mountain = new SimpleMountainBiome();

        SimpleColdMountainBiome ColdMountain = new SimpleColdMountainBiome();

        SimpleLakeBiome Lake = new SimpleLakeBiome();

        SimpleFrozenLakeBiome FrozenLake = new SimpleFrozenLakeBiome();

        SimpleHellBiome Hell = new SimpleHellBiome();

        SimpleStoneBiome Stone = new SimpleStoneBiome();

        public override Biome BiomeFor(int seed2, int seed3, int seed4, double x, double y, double z, double height)
        {
            if (z < -300)
            {
                return Hell;
            }
            if (z < height - 90)
            {
                return Stone;
            }
            double temp = GetTemperature(seed2, seed3, x, y) - height * 0.005;
            double down = GetDownfallRate(seed3, seed4, x, y);
            if (height > 0f && height < 20f)
            {
                if (down >= 0.8 && temp >= 80.0)
                {
                    return RainForest;
                }
                if (down >= 0.5 && down < 0.8 && temp >= 60.0)
                {
                    return Forest;
                }
                else if (down >= 0.3 && down < 0.5 && temp >= 90.0)
                {
                    return Swamp;
                }
                if (down >= 0.3 && down < 0.5 && temp >= 50.0 && temp < 90.0)
                {
                    return Plains;
                }
                if (down < 0.3 && temp >= 50.0)
                {
                    return Desert;
                }
                if (temp >= 32.0)
                {
                    return Plains;
                }
                if (down > 0.5)
                {
                    return Snow;
                }
                else
                {
                    return Icy;
                }
            }
            else if (height >= 20 && height < 40)
            {
                return LightForestHill;
                // TODO: Snow hill, etc?
            }
            else if (height >= 40)
            {
                if (temp > 32.0)
                {
                    return Mountain;
                }
                else
                {
                    return ColdMountain;
                }
            }
            else
            {
                if (temp > 32)
                {
                    return Lake;
                }
                else
                {
                    return FrozenLake;
                }
            }
        }
    }
}
