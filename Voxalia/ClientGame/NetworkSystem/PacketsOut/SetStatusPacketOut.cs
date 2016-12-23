//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;

namespace Voxalia.ClientGame.NetworkSystem.PacketsOut
{
    public class SetStatusPacketOut: AbstractPacketOut
    {
        public SetStatusPacketOut(ClientStatus opt, byte status)
        {
            ID = ClientToServerPacket.SET_STATUS;
            Data = new byte[2];
            Data[0] = (byte)opt;
            Data[1] = status;
        }
    }
}
