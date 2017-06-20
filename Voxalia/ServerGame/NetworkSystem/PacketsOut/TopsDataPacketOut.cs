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
using Voxalia.ServerGame.WorldSystem;
using Voxalia.Shared;
using FreneticGameCore;
using FreneticGameCore.Collision;
using FreneticGameCore.Files;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    class TopsDataPacketOut: AbstractPacketOut
    {
        public TopsDataPacketOut(Vector2i chunk_center, byte mode, byte[] bdata)
        {
            UsageType = NetUsageType.CHUNKS;
            ID = ServerToClientPacket.TOPS_DATA;
            byte[] res = new byte[bdata.Length + 8 + 1];
            Utilities.IntToBytes(chunk_center.X).CopyTo(res, 0);
            Utilities.IntToBytes(chunk_center.Y).CopyTo(res, 4);
            res[8] = mode;
            bdata.CopyTo(res, 9);
            Data = res;
        }
    }
}
