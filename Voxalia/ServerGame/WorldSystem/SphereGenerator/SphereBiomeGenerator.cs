//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxalia.ServerGame.WorldSystem.SphereGenerator
{
    public class SphereBiomeGenerator : BiomeGenerator
    {
        public SphereBiome Sphere = new SphereBiome();

        public override Biome BiomeFor(int seed2, int seed3, int seed4, double x, double y, double z, double height)
        {
            return Sphere;
        }

        public override double GetDownfallRate(int seed3, int seed4, double x, double y)
        {
            return 0.5f;
        }

        public override double GetTemperature(int seed2, int seed3, double x, double y)
        {
            return 70f;
        }
    }
}
