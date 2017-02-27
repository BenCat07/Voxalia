//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.Shared.Files;
using Voxalia.Shared.Collision;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class ChunkInfoPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length < 16)
            {
                SysConsole.Output(OutputType.WARNING, "Got chunk info packet of size: " + data.Length);
                return false;
            }
            DataStream ds = new DataStream(data);
            DataReader dr = new DataReader(ds);
            int x = dr.ReadInt();
            int y = dr.ReadInt();
            int z = dr.ReadInt();
            int posMult = dr.ReadInt();
            lock (TheClient.TheRegion.PreppingNow)
            {
                Action act = () =>
                {
                    lock (TheClient.TheRegion.PreppingNow)
                    {
                        TheClient.TheRegion.PreppingNow.Add(new Vector3i(x, y, z));
                    }
                    TheClient.Schedule.StartAsyncTask(() =>
                    {
                        try
                        {
                            ParseData(data, dr, x, y, z, posMult);
                        }
                        catch (Exception ex)
                        {
                            Utilities.CheckException(ex);
                            SysConsole.Output(ex);
                            lock (TheClient.TheRegion.PreppingNow)
                            {
                                TheClient.TheRegion.PreppingNow.Remove(new Vector3i(x, y, z));
                            }
                        }
                    });
                };
                TheClient.TheRegion.PrepChunks.Add(new KeyValuePair<Vector3i, Action>(new Vector3i(x, y, z), act));
            }
            return true;
        }

        void ParseData(byte[] data, DataReader dr, int x, int y, int z, int posMult)
        {
            byte[] reach = dr.ReadBytes((int)ChunkReachability.COUNT);
            int csize = Chunk.CHUNK_SIZE / posMult;
            byte[] data_unzipped = dr.ReadBytes(dr.Available);
            byte[] data_orig = FileHandler.Uncompress(data_unzipped);
            if (posMult == 1)
            {
                if (data_orig.Length != Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * 4)
                {
                    SysConsole.Output(OutputType.WARNING, "Invalid chunk size! Expected " + (Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * 3) + ", got " + data_orig.Length + ")");
                    lock (TheClient.TheRegion.PreppingNow)
                    {
                        TheClient.TheRegion.PreppingNow.Remove(new Vector3i(x, y, z));
                    }
                    return;
                }
            }
            else if (data_orig.Length != csize * csize * csize * 2)
            {
                SysConsole.Output(OutputType.WARNING, "Invalid LOD'ed chunk size! (LOD = " + posMult + ", Expected " + (csize * csize * csize * 2) + ", got " + data_orig.Length + ")");
                lock (TheClient.TheRegion.PreppingNow)
                {
                    TheClient.TheRegion.PreppingNow.Remove(new Vector3i(x, y, z));
                }
                return;
            }
            Action act = () =>
            {
                Chunk chk = TheClient.TheRegion.LoadChunk(new Vector3i(x, y, z), posMult);
                for (int i = 0; i < reach.Length; i++)
                {
                    chk.Reachability[i] = reach[i] == 1;
                }
                chk.LOADING = true;
                chk.PROCESSED = false;
                lock (TheClient.TheRegion.PreppingNow)
                {
                    TheClient.Schedule.StartAsyncTask(() =>
                    {
                        try
                        {
                            parsechunk2(chk, data_orig, posMult);
                        }
                        catch (Exception ex)
                        {
                            Utilities.CheckException(ex);
                            SysConsole.Output(ex);
                            lock (TheClient.TheRegion.PreppingNow)
                            {
                                TheClient.TheRegion.PreppingNow.Remove(chk.WorldPosition);
                            }
                        }
                    });
                }
            };
            TheClient.Schedule.ScheduleSyncTask(act);
        }

        void parsechunk2(Chunk chk, byte[] data_orig, int posMult)
        {
            for (int x = 0; x < chk.CSize; x++)
            {
                for (int y = 0; y < chk.CSize; y++)
                {
                    for (int z = 0; z < chk.CSize; z++)
                    {
                        int sp = (z * chk.CSize * chk.CSize + y * chk.CSize + x) * 2;
                        chk.BlocksInternal[chk.BlockIndex(x, y, z)]._BlockMaterialInternal = (ushort)((ushort)data_orig[sp] + (((ushort)data_orig[sp + 1]) << 8));
                    }
                }
            }
            if (posMult == 1)
            {
                for (int i = 0; i < chk.BlocksInternal.Length; i++)
                {
                    chk.BlocksInternal[i].BlockData = data_orig[chk.BlocksInternal.Length * 2 + i];
                    chk.BlocksInternal[i]._BlockPaintInternal = data_orig[chk.BlocksInternal.Length * 3 + i];
                }
            }
            else
            {
                for (int i = 0; i < chk.BlocksInternal.Length; i++)
                {
                    chk.BlocksInternal[i].BlockData = 0;
                    chk.BlocksInternal[i].BlockPaint = 0;
                }
            }
            chk.LOADING = false;
            chk.PRED = true;
            chk.OwningRegion.Regen(chk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE, chk);
            lock (TheClient.TheRegion.PreppingNow)
            {
                TheClient.TheRegion.PreppingNow.Remove(chk.WorldPosition);
            }
        }
    }
}
