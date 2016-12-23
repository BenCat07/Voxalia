//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.Shared.Files;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    public class NetStringPacketOut: AbstractPacketOut
    {
        public NetStringPacketOut(string str)
        {
            UsageType = NetUsageType.GENERAL;
            ID = ServerToClientPacket.NET_STRING;
            Data = FileHandler.encoding.GetBytes(str);
        }
    }
}
