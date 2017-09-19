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
using FreneticGameCore.Files;
using Voxalia.Shared.Collision;
using FreneticGameCore;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class ChunkInfoPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            if (data.Length < 17)
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
            byte oinfo = dr.ReadByte();
            if (oinfo == 1)
            {
                TheClient.TheRegion.ForgetChunk(new Vector3i(x, y, z));
                TheClient.TheRegion.AirChunks.Add(new Vector3i(x, y, z));
                for (int tx = -1; tx <= 1; tx++)
                {
                    for (int ty = -1; ty <= 1; ty++)
                    {
                        for (int tz = -1; tz <= 1; tz++)
                        {
                            Chunk ch = TheClient.TheRegion.GetChunk(new Vector3i(x + tx, y + ty, z + tz));
                            if (ch != null)
                            {
                                ch.AddToWorld();
                                ch.CreateVBO();
                            }
                        }
                    }
                }
                return true;
            }
            lock (TheClient.TheRegion.PreppingNow)
            {
                Chunk chk = TheClient.TheRegion.LoadChunk(new Vector3i(x, y, z), posMult);
                chk.LOADING = true;
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
                            ParseData(dr, x, y, z, posMult, chk);
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
                    }, true);
                };
                TheClient.TheRegion.PrepChunks.Add(new KeyValuePair<Vector3i, Action>(new Vector3i(x, y, z), act));
            }
            return true;
        }

        void ParseData(DataReader dr, int x, int y, int z, int posMult, Chunk chk)
        {
            byte[] reach = posMult != 1 ? new byte[(int)ChunkReachability.COUNT] : dr.ReadBytes((int)ChunkReachability.COUNT);
            int csize = Chunk.CHUNK_SIZE / posMult;
            byte[] data_unzipped = dr.ReadBytes(dr.Available);
            byte[] data_orig;
            try
            {
                data_orig = posMult >= 6 ? data_unzipped : FileHandler.Uncompress(data_unzipped);
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output("handling CHUNK PARSE DATA: " + data_unzipped.Length + ", " + posMult, ex);
                return;
            }
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
                            Parsechunk2(chk, data_orig, posMult);
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
                    }, true);
                }
            };
            TheClient.Schedule.ScheduleSyncTask(act);
        }

        void Parsechunk2(Chunk chk, byte[] data_orig, int posMult)
        {
            for (int x = 0; x < chk.CSize; x++)
            {
                for (int y = 0; y < chk.CSize; y++)
                {
                    for (int z = 0; z < chk.CSize; z++)
                    {
                        int sp = (z * chk.CSize * chk.CSize + y * chk.CSize + x) * 2;
                        chk.BlocksInternal[chk.BlockIndex(x, y, z)]._BlockMaterialInternal = (ushort)(data_orig[sp] + ((data_orig[sp + 1]) << 8));
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
            TheClient.Schedule.ScheduleSyncTask(() =>
            {
                chk.OwningRegion.Regen(chk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE);
            });
            lock (TheClient.TheRegion.PreppingNow)
            {
                TheClient.TheRegion.PreppingNow.Remove(chk.WorldPosition);
            }
        }
    }
}
