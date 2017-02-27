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
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    class FlagEntityPacketOut: AbstractPacketOut
    {
        public FlagEntityPacketOut(Entity e, EntityFlag flag, double value)
        {
            UsageType = NetUsageType.ENTITIES;
            ID = ServerToClientPacket.FLAG_ENTITY;
            Data = new byte[8 + 1 + 4];
            Utilities.LongToBytes(e.EID).CopyTo(Data, 0);
            Data[8] = (byte)flag;
            Utilities.FloatToBytes((float)value).CopyTo(Data, 8 + 1);
        }
    }
}
