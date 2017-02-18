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
