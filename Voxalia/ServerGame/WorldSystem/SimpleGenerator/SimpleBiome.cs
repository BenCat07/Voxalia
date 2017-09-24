//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;

namespace Voxalia.ServerGame.WorldSystem.SimpleGenerator
{
    public abstract class SimpleBiome: Biome
    {
        public virtual Material SurfaceBlock()
        {
            return Material.GRASS_PLAINS;
        }

        public virtual Material SecondLayerBlock()
        {
            return Material.DIRT;
        }

        public virtual Material BaseBlock()
        {
            return Material.STONE;
        }
        
        public virtual Material WaterMaterial()
        {
            return Material.WATER;
        }

        public virtual Material SandMaterial()
        {
            return Material.SAND;
        }

        public virtual double AirDensity()
        {
            return 0.2f;
        }

        public override Material GetZeroOrLowerMat()
        {
            return WaterMaterial();
        }

        public override Material GetAboveZeromat()
        {
            return SurfaceBlock();
        }

        public override bool LikelyToHaveTrees()
        {
            return true;
        }
    }
}
