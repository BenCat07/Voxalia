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
        public int[] Blocks = new int[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BlockIndex(int x, int y)
        {
            return y * Constants.CHUNK_WIDTH + x;
        }

        public bool Darken(int x, int y, int z)
        {
            return Blocks[BlockIndex(x, y)] > z;
        }
    }
}
