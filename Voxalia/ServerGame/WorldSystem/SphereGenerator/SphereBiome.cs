using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.Shared;

namespace Voxalia.ServerGame.WorldSystem.SphereGenerator
{
    public class SphereBiome : Biome
    {
        public override Material GetAboveZeromat()
        {
            return Material.AIR;
        }

        public override string GetName()
        {
            return "sphere";
        }

        public override Material GetZeroOrLowerMat()
        {
            return Material.AIR;
        }
    }
}
