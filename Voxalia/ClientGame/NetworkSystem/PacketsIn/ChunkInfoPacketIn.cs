﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using Voxalia.ClientGame.WorldSystem;
using System.Threading;
using System.Threading.Tasks;

namespace Voxalia.ClientGame.NetworkSystem.PacketsIn
{
    public class ChunkInfoPacketIn: AbstractPacketIn
    {
        public override bool ParseBytesAndExecute(byte[] data)
        {
            Task.Factory.StartNew(() => ParseData(data));
            return true;
        }

        void ParseData(byte[] data)
        {
            DataStream ds = new DataStream(data);
            DataReader dr = new DataReader(ds);
            int x = dr.ReadInt();
            int y = dr.ReadInt();
            int z = dr.ReadInt();
            byte[] data_unzipped = dr.ReadBytes(data.Length - 12);
            byte[] data_orig = FileHandler.UnGZip(data_unzipped);
            if (data_orig.Length != Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * 2)
            {
                SysConsole.Output(OutputType.WARNING, "Invalid chunk size!");
                return;
            }
            TheClient.RunImmediately.Add(new Task(() =>
            {
                Chunk chk = TheClient.TheWorld.GetChunk(new Location(x, y, z));
                Task.Factory.StartNew(() => parsechunk2(chk, data_orig));
            }));
        }

        void parsechunk2(Chunk chk, byte[] data_orig)
        {
            for (int i = 0; i < chk.BlocksInternal.Length; i++)
            {
                chk.BlocksInternal[i] = Utilities.BytesToUshort(Utilities.BytesPartial(data_orig, i * 2, 2));
            }
            TheClient.RunImmediately.Add(new Task(() => { chk.AddToWorld(); chk.CreateVBO(); }));
        }
    }
}
