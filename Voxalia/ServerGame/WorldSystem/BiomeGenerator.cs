//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Voxalia.ServerGame.WorldSystem
{
    public abstract class BiomeGenerator
    {
        public abstract double GetTemperature(int seed2, int seed3, double x, double y);

        public abstract double GetDownfallRate(int seed3, int seed4, double x, double y);

        public abstract Biome BiomeFor(int seed2, int seed3, int seed4, double x, double y, double z, double height);
    }
}
