//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using FreneticGameCore.Files;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class ChunkInfoPacketOut: AbstractPacketOut
    {
        public ChunkInfoPacketOut(Vector3i cpos, byte[] slod, int posMult)
        {
            UsageType = NetUsageType.CHUNKS;
            ID = ServerToClientPacket.CHUNK_INFO;
            bool is_air = true;
            for (int i = 0; i < slod.Length; i += 2)
            {
                Material mat = (Material)(slod[i] | (slod[i + 1] << 8));
                if (mat != Material.AIR)
                {
                    is_air = false;
                    break;
                }
            }
            if (is_air)
            {
                Data = new byte[17];
                Utilities.IntToBytes(cpos.X).CopyTo(Data, 0);
                Utilities.IntToBytes(cpos.Y).CopyTo(Data, 4);
                Utilities.IntToBytes(cpos.Z).CopyTo(Data, 8);
                Utilities.IntToBytes(1).CopyTo(Data, 12);
                Data[16] = 1;
                return;
            }
            DataStream ds = new DataStream(slod.Length + 16);
            DataWriter dw = new DataWriter(ds);
            dw.WriteInt(cpos.X);
            dw.WriteInt(cpos.Y);
            dw.WriteInt(cpos.Z);
            dw.WriteInt(posMult);
            dw.WriteByte(0);
            dw.WriteBytes(slod);
            Data = ds.ToArray();
        }

        public ChunkInfoPacketOut(Chunk chunk, int lod)
        {
            UsageType = NetUsageType.CHUNKS;
            ID = ServerToClientPacket.CHUNK_INFO;
            if (chunk.Flags.HasFlag(ChunkFlags.POPULATING) && (lod != 5 || chunk.LOD == null))
            {
                throw new Exception("Trying to transmit chunk while it's still loading! For chunk at " + chunk.WorldPosition);
            }
            byte[] data_orig;
            if (lod == 1)
            {
                bool isAir = true;
                if (chunk.BlocksInternal == null)
                {
                    data_orig = null;
                }
                else
                {
                    data_orig = new byte[chunk.BlocksInternal.Length * 4];
                    for (int x = 0; x < chunk.BlocksInternal.Length; x++)
                    {
                        ushort mat = chunk.BlocksInternal[x]._BlockMaterialInternal;
                        if (mat != 0)
                        {
                            isAir = false;
                        }
                        data_orig[x * 2] = (byte)(mat & 0xFF);
                        data_orig[x * 2 + 1] = (byte)((mat >> 8) & 0xFF);
                    }
                    if (isAir)
                    {
                        data_orig = null;
                    }
                    else
                    {
                        for (int i = 0; i < chunk.BlocksInternal.Length; i++)
                        {
                            data_orig[chunk.BlocksInternal.Length * 2 + i] = chunk.BlocksInternal[i].BlockData;
                            data_orig[chunk.BlocksInternal.Length * 3 + i] = chunk.BlocksInternal[i]._BlockPaintInternal;
                        }
                    }
                }
            }
            else
            {
                data_orig = chunk.LODBytes(lod, true);
            }
            if (data_orig == null)
            {
                Data = new byte[17];
                Utilities.IntToBytes(chunk.WorldPosition.X).CopyTo(Data, 0);
                Utilities.IntToBytes(chunk.WorldPosition.Y).CopyTo(Data, 4);
                Utilities.IntToBytes(chunk.WorldPosition.Z).CopyTo(Data, 8);
                Utilities.IntToBytes(1).CopyTo(Data, 12);
                Data[16] = 1;
                return;
            }
            byte[] gdata = lod == 15 ? data_orig : FileHandler.Compress(data_orig);
            DataStream ds = new DataStream(gdata.Length + 16);
            DataWriter dw = new DataWriter(ds);
            dw.WriteInt(chunk.WorldPosition.X);
            dw.WriteInt(chunk.WorldPosition.Y);
            dw.WriteInt(chunk.WorldPosition.Z);
            dw.WriteInt(lod);
            dw.WriteByte(0);
            byte[] reach = new byte[chunk.Reachability.Length];
            for (int i = 0; i < reach.Length; i++)
            {
                reach[i] = (byte)(chunk.Reachability[i] ? 1 : 0);
            }
            dw.WriteBytes(reach);
            dw.WriteBytes(gdata);
            Data = ds.ToArray();
        }
    }
}
