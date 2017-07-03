//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.Shared.Collision;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.WorldSystem
{
    public abstract class BlockPopulator
    {
        public abstract double GetHeight(int seed, int seed2, int seed3, int seed4, int seed5, double x, double y, bool precise);

        public abstract void Populate(int seed, int seed2, int seed3, int seed4, int seed5, Chunk chunk);

        public abstract byte[] GetSuperLOD(int seed, int seed2, int seed3, int seed4, int seed5, Vector3i cpos);

        public abstract byte[] GetLODSix(int seed, int seed2, int seed3, int seed4, int seed5, Vector3i cpos);

        public abstract List<Tuple<string, double>> GetTimings();

        public abstract void ClearTimings();

        public abstract BiomeGenerator GetBiomeGen();

        public abstract void Tick();
    }
}
