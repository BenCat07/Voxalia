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
using System.Threading.Tasks;
using Voxalia.Shared;
using Voxalia.Shared.Collision;

namespace Voxalia.ServerGame.WorldSystem.SphereGenerator
{
    public class SphereGeneratorCore : BlockPopulator
    {
        public override void ClearTimings()
        {
            // Nothing needed.
        }

        public SphereBiomeGenerator Biomes = new SphereBiomeGenerator();

        public override BiomeGenerator GetBiomeGen()
        {
            return Biomes;
        }

        public override double GetHeight(int seed, int seed2, int seed3, int seed4, int seed5, double x, double y, double z, out Biome biome)
        {
            biome = Biomes.Sphere;
            return 0.0;
        }

        public override List<Tuple<string, double>> GetTimings()
        {
            List<Tuple<string, double>> res = new List<Tuple<string, double>>();
            return res;
        }
        
        public override void Populate(int seed, int seed2, int seed3, int seed4, int seed5, Chunk chunk)
        {
            double scale = chunk.OwningRegion.TheWorld.GeneratorScale;
            scale *= scale;
            Location cCenter = (chunk.WorldPosition.ToLocation() + new Location(0.5, 0.5, 0.5)) * Constants.CHUNK_WIDTH;
            if (cCenter.LengthSquared() > scale * 4.0)
            {
                // TODO: Is this excessive?
                for (int i = 0; i < chunk.BlocksInternal.Length; i++)
                {
                    chunk.BlocksInternal[i] = BlockInternal.AIR;
                }
                return;
            }
            double one_over_scale = 1.0 / scale;
            Vector3i cLow = chunk.WorldPosition * Constants.CHUNK_WIDTH;
            for (int x = 0; x < Constants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < Constants.CHUNK_WIDTH; y++)
                {
                    for (int z = 0; z < Constants.CHUNK_WIDTH; z++)
                    {
                        Vector3i current = cLow + new Vector3i(x, y, z);
                        double distSq = current.ToLocation().LengthSquared();
                        double rel = distSq * one_over_scale;
                        if (rel > 1.0)
                        {
                            chunk.SetBlockAt(x, y, z, BlockInternal.AIR);
                        }
                        else if (rel > (0.9 * 0.9))
                        {
                            chunk.SetBlockAt(x, y, z, new BlockInternal((ushort)Material.DIRT, 0, 0, 0));
                        }
                        // TODO: More layers?
                        else
                        {
                            chunk.SetBlockAt(x, y, z, new BlockInternal((ushort)Material.STONE, 0, 0, 0));
                        }
                    }
                }
            }
        }
    }
}
