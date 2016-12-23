//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;

namespace Voxalia.ServerGame.NetworkSystem
{
    public abstract class AbstractPacketOut
    {
        /// <summary>
        /// The ID of this packet.
        /// </summary>
        public ServerToClientPacket ID = 0;

        /// <summary>
        /// The binary data held in this packet.
        /// </summary>
        public byte[] Data;

        public NetUsageType UsageType = NetUsageType.GENERAL;
    }
}
