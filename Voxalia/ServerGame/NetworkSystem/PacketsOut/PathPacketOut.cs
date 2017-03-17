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
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class PathPacketOut: AbstractPacketOut
    {
        public PathPacketOut(List<Location> locs)
        {
            UsageType = NetUsageType.EFFECTS;
            ID = ServerToClientPacket.PATH;
            Data = new byte[locs.Count * 24 + 4];
            Utilities.IntToBytes(locs.Count).CopyTo(Data, 0);
            for (int i = 0; i < locs.Count; i++)
            {
                locs[i].ToDoubleBytes().CopyTo(Data, 4 + 24 * i);
            }
        }
    }
}
