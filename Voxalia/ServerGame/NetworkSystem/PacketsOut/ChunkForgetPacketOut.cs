//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using Voxalia.Shared.Collision;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class ChunkForgetPacketOut: AbstractPacketOut
    {
        public ChunkForgetPacketOut(Vector3i cpos)
        {
            UsageType = NetUsageType.CHUNKS;
            ID = ServerToClientPacket.CHUNK_FORGET;
            Data = new byte[12];
            Utilities.IntToBytes(cpos.X).CopyTo(Data, 0);
            Utilities.IntToBytes(cpos.Y).CopyTo(Data, 4);
            Utilities.IntToBytes(cpos.Z).CopyTo(Data, 8);
        }
    }
}
