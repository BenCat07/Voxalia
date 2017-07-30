//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using FreneticGameCore;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class JointUpdatePacketOut: AbstractPacketOut
    {
        public JointUpdatePacketOut(long jid, JointUpdateMode mode, double val)
        {
            UsageType = NetUsageType.ENTITIES;
            ID = ServerToClientPacket.JOINT_UPDATE;
            Data = new byte[8 + 1 + 8];
            Utilities.LongToBytes(jid).CopyTo(Data, 0);
            Data[8] = (byte)mode;
            Utilities.DoubleToBytes(val).CopyTo(Data, 8 + 1);
        }
    }
}
