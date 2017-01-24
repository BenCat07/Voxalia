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
