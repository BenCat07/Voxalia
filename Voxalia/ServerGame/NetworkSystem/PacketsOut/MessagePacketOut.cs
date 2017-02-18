//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.Shared.Files;

namespace Voxalia.ServerGame.NetworkSystem.PacketsOut
{
    class MessagePacketOut: AbstractPacketOut
    {
        public MessagePacketOut(TextChannel chan, string msg)
        {
            UsageType = NetUsageType.GENERAL;
            ID = ServerToClientPacket.MESSAGE;
            byte[] text = FileHandler.encoding.GetBytes(msg);
            Data = new byte[1 + text.Length];
            Data[0] = (byte)chan;
            text.CopyTo(Data, 1);
        }
    }
}
