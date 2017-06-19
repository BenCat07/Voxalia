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
using Voxalia.Shared.Collision;
using FreneticGameCore.Files;
using FreneticGameCore;

namespace Voxalia.ServerGame.OtherSystems
{
    public class BlockUpperArea
    {
        public struct TopBlock
        {
            public static readonly TopBlock VOID = new TopBlock() { BasicMat = Material.SPECIAL_VOID, Height = 0 };

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

        public void TryPush(int x, int y, int z, Material mat)
        {
            if (!mat.IsOpaque())
            {
                return;
            }
            int ind = BlockIndex(x, y);
            if (Blocks[ind].Height <= z || !Blocks[ind].BasicMat.RendersAtAll())
            {
                Blocks[ind].Height = z;
                Blocks[ind].BasicMat = mat;
                ind *= 4;
                if (BlocksTrans[ind].Height <= z)
                {
                    BlocksTrans[ind + 3] = TopBlock.VOID;
                    BlocksTrans[ind + 2] = TopBlock.VOID;
                    BlocksTrans[ind + 1] = TopBlock.VOID;
                    BlocksTrans[ind + 0] = TopBlock.VOID;
                }
                else if (BlocksTrans[ind + 1].Height <= z)
                {
                    BlocksTrans[ind + 3] = TopBlock.VOID;
                    BlocksTrans[ind + 2] = TopBlock.VOID;
                    BlocksTrans[ind + 1] = TopBlock.VOID;
                }
                else if (BlocksTrans[ind + 2].Height <= z)
                {
                    BlocksTrans[ind + 3] = TopBlock.VOID;
                    BlocksTrans[ind + 2] = TopBlock.VOID;
                }
                else if (BlocksTrans[ind + 3].Height <= z)
                {
                    BlocksTrans[ind + 3] = TopBlock.VOID;
                }
                Edited = true;
                return;
            }
            return;
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
        
        public void TryPushTrans(int x, int y, int z, Material mat)
        {
            if (!mat.RendersAtAll() || mat.IsOpaque())
            {
                return;
            }
            int ind = BlockIndex(x, y);
            if (Blocks[ind].Height >= z && Blocks[ind].BasicMat.RendersAtAll())
            {
                return;
            }
            ind *= 4;
            if (BlocksTrans[ind].Height <= z || !BlocksTrans[ind].BasicMat.RendersAtAll())
            {
                BlocksTrans[ind + 3] = BlocksTrans[ind + 2];
                BlocksTrans[ind + 2] = BlocksTrans[ind + 1];
                BlocksTrans[ind + 1] = BlocksTrans[ind + 0];
                BlocksTrans[ind].Height = z;
                BlocksTrans[ind].BasicMat = mat;
                Edited = true;
                return;
            }
            else if (BlocksTrans[ind + 1].Height <= z || !BlocksTrans[ind + 1].BasicMat.RendersAtAll())
            {
                BlocksTrans[ind + 3] = BlocksTrans[ind + 2];
                BlocksTrans[ind + 2] = BlocksTrans[ind + 1];
                BlocksTrans[ind + 1].Height = z;
                BlocksTrans[ind + 1].BasicMat = mat;
                Edited = true;
                return;
            }
            else if (BlocksTrans[ind + 2].Height <= z || !BlocksTrans[ind + 2].BasicMat.RendersAtAll())
            {
                BlocksTrans[ind + 3] = BlocksTrans[ind + 2];
                BlocksTrans[ind + 2].Height = z;
                BlocksTrans[ind + 2].BasicMat = mat;
                Edited = true;
                return;
            }
            else if (BlocksTrans[ind + 3].Height <= z || !BlocksTrans[ind + 3].BasicMat.RendersAtAll())
            {
                BlocksTrans[ind + 3].Height = z;
                BlocksTrans[ind + 3].BasicMat = mat;
                Edited = true;
                return;
            }
            return;
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
