using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.Shared;
using System.Runtime.CompilerServices;
using Voxalia.Shared.Collision;
using Voxalia.Shared.Files;

namespace Voxalia.ServerGame.OtherSystems
{
    public class BlockUpperArea
    {
        public struct TopBlock
        {
            public Material BasicMat;

            public int Height;
        }

        public HashSet<int> ChunksUsing = new HashSet<int>();

        public TopBlock[] Blocks = new TopBlock[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BlockIndex(int x, int y)
        {
            return y * Constants.CHUNK_WIDTH + x;
        }

        public bool TryPush(int x, int y, int z, Material mat)
        {
            if (!mat.IsOpaque())
            {
                return false;
            }
            int ind = BlockIndex(x, y);
            if (Blocks[ind].Height <= z || Blocks[ind].BasicMat == Material.AIR)
            {
                Blocks[ind].Height = z;
                Blocks[ind].BasicMat = mat;
                Edited = true;
                return true;
            }
            return false;
        }
        
        public bool Edited = false;

        public byte[] ToBytes()
        {
            byte[] toret = new byte[(Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH) * (2 + 4)];
            for (int i = 0; i < Blocks.Length; i++)
            {
                Utilities.UshortToBytes((ushort)Blocks[i].BasicMat).CopyTo(toret, i * 2);
                Utilities.IntToBytes(Blocks[i].Height).CopyTo(toret, (Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH) * 2 + i * 4);
            }
            return toret;
        }

        public void FromBytes(byte[] b)
        {
            for (int i = 0; i < Blocks.Length; i++)
            {
                Blocks[i].BasicMat = (Material)Utilities.BytesToUshort(Utilities.BytesPartial(b, i * 2, 2));
                Blocks[i].Height = Utilities.BytesToInt(Utilities.BytesPartial(b, (Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH) * 2 + i * 4, 4));
            }
        }

        public byte[] ToNetBytes()
        {
            byte[] toret = new byte[(Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH) * 4];
            for (int i = 0; i < Blocks.Length; i++)
            {
                Utilities.IntToBytes(Blocks[i].Height).CopyTo(toret, i * 4);
            }
            return FileHandler.Compress(toret);
        }
    }
}
