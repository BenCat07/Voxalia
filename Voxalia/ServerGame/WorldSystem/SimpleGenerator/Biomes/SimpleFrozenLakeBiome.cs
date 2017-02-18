//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using Voxalia.Shared;

namespace Voxalia.ServerGame.WorldSystem.SimpleGenerator.Biomes
{
    public class SimpleFrozenLakeBiome: SimpleBiome
    {
        public override string GetName()
        {
            return "FrozenLake";
        }

        public override Material SurfaceBlock()
        {
            return Material.SNOW_SOLID;
        }

        public override Material SecondLayerBlock()
        {
            return Material.SNOW_SOLID;
        }

        public override Material SandMaterial()
        {
            return Material.SNOW_SOLID;
        }

        public override Material WaterMaterial()
        {
            return Material.ICE;
        }
    }
}
