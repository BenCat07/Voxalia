//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.Shared.Files;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class BlockEditPacketOut: AbstractPacketOut
    {
        public BlockEditPacketOut(Location[] pos, ushort[] mat, byte[] dat, byte[] paints)
        {
            UsageType = NetUsageType.CHUNKS;
            ID = ServerToClientPacket.BLOCK_EDIT;
            DataStream outp = new DataStream();
            DataWriter dw = new DataWriter(outp);
            dw.WriteInt(pos.Length);
            for (int i = 0; i < pos.Length; i++)
            {
                dw.WriteBytes(pos[i].ToDoubleBytes());
            }
            for (int i = 0; i < mat.Length; i++)
            {
                dw.WriteBytes(Utilities.UshortToBytes(mat[i]));
            }
            dw.WriteBytes(dat);
            dw.WriteBytes(paints);
            Data = outp.ToArray();
        }
    }
}
