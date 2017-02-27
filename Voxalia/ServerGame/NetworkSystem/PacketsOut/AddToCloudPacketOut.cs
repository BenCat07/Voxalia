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

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class AddToCloudPacketOut: AbstractPacketOut
    {
        public AddToCloudPacketOut(Cloud c, int s)
        {
            UsageType = NetUsageType.CLOUDS;
            ID = ServerToClientPacket.ADD_TO_CLOUD;
            Data = new byte[24 + 4 + 4 + 8];
            c.Points[s].ToDoubleBytes().CopyTo(Data, 0);
            Utilities.FloatToBytes((float)c.Sizes[s]).CopyTo(Data, 24);
            Utilities.FloatToBytes((float)c.EndSizes[s]).CopyTo(Data, 24 + 4);
            Utilities.LongToBytes(c.CID).CopyTo(Data, 24 + 4 + 4);
        }
    }
}
