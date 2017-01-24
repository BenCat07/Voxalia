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
        
        public TopBlock[] BlocksTrans = new TopBlock[Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 4];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int BlockIndexTrans(int x, int y)
        {
            return (y * Constants.CHUNK_WIDTH + x) * 4;
        }

        public bool TryPushTrans(int x, int y, int z, Material mat)
        {
            if (!mat.IsOpaque())
            {
                return false;
            }
            int ind = BlockIndex(x, y);
            if (BlocksTrans[ind].Height <= z || BlocksTrans[ind].BasicMat == Material.AIR)
            {
                BlocksTrans[ind + 3] = BlocksTrans[ind + 2];
                BlocksTrans[ind + 2] = BlocksTrans[ind + 1];
                BlocksTrans[ind + 1] = BlocksTrans[ind + 0];
                BlocksTrans[ind].Height = z;
                BlocksTrans[ind].BasicMat = mat;
                Edited = true;
                return true;
            }
            else if (BlocksTrans[ind + 1].Height <= z || BlocksTrans[ind + 1].BasicMat == Material.AIR)
            {
                BlocksTrans[ind + 3] = BlocksTrans[ind + 2];
                BlocksTrans[ind + 2] = BlocksTrans[ind + 1];
                BlocksTrans[ind + 1].Height = z;
                BlocksTrans[ind + 1].BasicMat = mat;
                Edited = true;
                return true;
            }
            else if (BlocksTrans[ind + 2].Height <= z || BlocksTrans[ind + 2].BasicMat == Material.AIR)
            {
                BlocksTrans[ind + 3] = BlocksTrans[ind + 2];
                BlocksTrans[ind + 2].Height = z;
                BlocksTrans[ind + 2].BasicMat = mat;
                Edited = true;
                return true;
            }
            else if (BlocksTrans[ind + 3].Height <= z || BlocksTrans[ind + 3].BasicMat == Material.AIR)
            {
                BlocksTrans[ind + 3].Height = z;
                BlocksTrans[ind + 3].BasicMat = mat;
                Edited = true;
                return true;
            }
            return false;
        }
        
        public byte[] ToBytesTrans()
        {
            byte[] toret = new byte[(Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 4) * (2 + 4)];
            for (int i = 0; i < BlocksTrans.Length; i++)
            {
                Utilities.UshortToBytes((ushort)BlocksTrans[i].BasicMat).CopyTo(toret, i * 2);
                Utilities.IntToBytes(BlocksTrans[i].Height).CopyTo(toret, (Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 4) * 2 + i * 4);
            }
            return toret;
        }

        public void FromBytesTrans(byte[] b)
        {
            for (int i = 0; i < BlocksTrans.Length; i++)
            {
                BlocksTrans[i].BasicMat = (Material)Utilities.BytesToUshort(Utilities.BytesPartial(b, i * 2, 2));
                BlocksTrans[i].Height = Utilities.BytesToInt(Utilities.BytesPartial(b, (Constants.CHUNK_WIDTH * Constants.CHUNK_WIDTH * 4) * 2 + i * 4, 4));
            }
        }

        public byte[] ToNetBytesTrans()
        {
            return FileHandler.Compress(ToBytesTrans());
        }
    }
}
