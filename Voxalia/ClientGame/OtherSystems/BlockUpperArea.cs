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
using System.Runtime.CompilerServices;

namespace Voxalia.ClientGame.OtherSystems
{
    public class BlockUpperArea
    {
        public struct TopBlock
        {
            public Material BasicMat;

            public int Height;
        }

        public int[] Blocks = new int[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH];

        public TopBlock[] BlocksTrans = new TopBlock[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 4];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BlockIndex(int x, int y)
        {
            return y * Constants.CHUNK_WIDTH + x;
        }

        public bool Darken(int x, int y, int z)
        {
            // TODO: Maybe only apply darkening for invisible areas (chunk not loaded), and use our own local data on chunks for more precise darkening effects?
            return Blocks[BlockIndex(x, y)] > z;
            // TODO: Apply trans data for enhanced darkening precision!
        }
    }
}
